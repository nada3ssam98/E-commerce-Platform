using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Platform.Api.TrustedPath;

/// <summary>
/// Classifies every request before anything else handles it (ADR-04 rules 9, 10, 12):
/// <list type="bullet">
/// <item>valid API edge key from an edge network → <see cref="TrustedCaller.Edge"/>;</item>
/// <item>valid SSR-to-API key from an SSR network → <see cref="TrustedCaller.Ssr"/>;</item>
/// <item>anything else (no key, wrong key, the SSR ingress key, both keys, or a valid key from the wrong network)
/// → <c>403</c> and a security log entry without values.</item>
/// </list>
/// Source checks use the direct connection address, never a forwarded header: this middleware runs before the
/// Forwarded Headers middleware. The store host comes only from <c>X-Platform-Host</c>, normalized again.
/// </summary>
public sealed partial class TrustedPathMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<TrustedPathMiddleware> logger;
    private readonly byte[][] apiEdgeKeys;
    private readonly byte[][] ssrApiKeys;
    private readonly IPNetwork[] edgeNetworks;
    private readonly IPNetwork[] ssrNetworks;

    public TrustedPathMiddleware(RequestDelegate next, IOptions<TrustedPathOptions> options, ILogger<TrustedPathMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.next = next;
        this.logger = logger;
        var settings = options.Value;
        apiEdgeKeys = [.. settings.ApiEdgeKeys().Select(Digest)];
        ssrApiKeys = [.. settings.SsrApiKeys().Select(Digest)];
        edgeNetworks = TrustedPathOptions.Parse(settings.EdgeNetworks);
        ssrNetworks = TrustedPathOptions.Parse(settings.SsrNetworks);
    }

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var headers = context.Request.Headers;

        var (caller, reason) = Classify(
            headers[TrustedPathHeaders.ApiEdgeKey],
            headers[TrustedPathHeaders.SsrApiKey],
            headers.ContainsKey(TrustedPathHeaders.SsrIngressKey),
            context.Connection.RemoteIpAddress);
        if (caller is null)
        {
            return Reject(context, reason);
        }

        var platformHost = headers[TrustedPathHeaders.PlatformHost];
        var host = platformHost.Count == 1 ? PlatformHost.Normalize(platformHost[0]) : null;
        if (host is null)
        {
            return Reject(context, "missing-or-invalid-platform-host");
        }

        context.Features.Set(new TrustedRequest(caller.Value, host));
        return next(context);
    }

    internal Task Reject(HttpContext context, string reason)
    {
        LogRejected(logger, reason, context.Request.Method, context.Request.Path.Value ?? string.Empty);
        return TrustedPathRejection.WriteAsync(context);
    }

    private (TrustedCaller? Caller, string Reason) Classify(
        StringValues edgeKey, StringValues ssrKey, bool ssrIngressKeyPresent, IPAddress? remote)
    {
        // TH-11: the SSR ingress key grants nothing at the API, whatever else is sent with it.
        if (ssrIngressKeyPresent)
        {
            return (null, "ssr-ingress-key");
        }

        if (edgeKey.Count > 1 || ssrKey.Count > 1 || (edgeKey.Count == 1 && ssrKey.Count == 1))
        {
            return (null, "multiple-credentials");
        }

        if (edgeKey.Count == 0 && ssrKey.Count == 0)
        {
            return (null, "missing-credential");
        }

        var (presented, accepted, networks, caller) = edgeKey.Count == 1
            ? (edgeKey[0], apiEdgeKeys, edgeNetworks, TrustedCaller.Edge)
            : (ssrKey[0], ssrApiKeys, ssrNetworks, TrustedCaller.Ssr);

        if (!Matches(presented, accepted))
        {
            return (null, "invalid-credential");
        }

        // TH-12: a valid credential counts only over its own network path.
        if (remote is null || !networks.Any(network => network.Contains(remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote)))
        {
            return (null, caller == TrustedCaller.Edge ? "edge-key-from-wrong-network" : "ssr-key-from-wrong-network");
        }

        return (caller, string.Empty);
    }

    /// <summary>Constant time: both sides are hashed, so lengths do not matter, and every accepted key is compared.</summary>
    private static bool Matches(string? presented, byte[][] accepted)
    {
        if (string.IsNullOrEmpty(presented))
        {
            return false;
        }

        var digest = Digest(presented);
        var matched = false;
        foreach (var key in accepted)
        {
            matched |= CryptographicOperations.FixedTimeEquals(digest, key);
        }

        return matched;
    }

    private static byte[] Digest(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "Rejected request: {Reason} ({Method} {Path})")]
    private static partial void LogRejected(ILogger logger, string reason, string method, string path);
}

/// <summary>
/// After routing: the SSR caller may only <c>GET</c> endpoints marked with <see cref="SsrCallerAllowed"/>
/// (ADR-04 rule 11). Any other method or route, including unknown routes, is rejected with <c>403</c>.
/// </summary>
public sealed partial class SsrAllowListMiddleware(RequestDelegate next, ILogger<SsrAllowListMiddleware> logger)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.GetTrustedRequest().Caller != TrustedCaller.Ssr)
        {
            return next(context);
        }

        var allowed = HttpMethods.IsGet(context.Request.Method)
            && context.GetEndpoint()?.Metadata.GetMetadata<SsrCallerAllowed>() is not null;
        if (allowed)
        {
            return next(context);
        }

        LogRejected(logger, context.Request.Method, context.Request.Path.Value ?? string.Empty);
        return TrustedPathRejection.WriteAsync(context);
    }

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Rejected request: ssr-route-not-allowed ({Method} {Path})")]
    private static partial void LogRejected(ILogger logger, string method, string path);
}

/// <summary>The <c>403</c> response of a rejected request: Problem Details, no store data.</summary>
internal static class TrustedPathRejection
{
    public static async Task WriteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        var problemDetails = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext { HttpContext = context }).ConfigureAwait(false);
    }
}
