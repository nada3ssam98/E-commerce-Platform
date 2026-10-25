namespace Platform.Api.TrustedPath;

/// <summary>Who sent the request, as established by <see cref="TrustedPathMiddleware"/> (ADR-04 rule 10).</summary>
public enum TrustedCaller
{
    /// <summary>The edge: valid API edge key from an edge network. All routes, subject to normal authorization.</summary>
    Edge,

    /// <summary>The SSR server: valid SSR-to-API key from an SSR network. Allow-listed public GETs only.</summary>
    Ssr,
}

/// <summary>The verified caller and store host of the current request (request feature).</summary>
public sealed record TrustedRequest(TrustedCaller Caller, string PlatformHost);

/// <summary>Endpoint metadata: the SSR server may call this endpoint with <c>GET</c> (ADR-04 rule 11).</summary>
public sealed class SsrCallerAllowed
{
    public static SsrCallerAllowed Instance { get; } = new();

    private SsrCallerAllowed()
    {
    }
}

public static class TrustedRequestExtensions
{
    /// <summary>The verified request. Every request that reaches an endpoint has one; the middleware rejects the rest.</summary>
    public static TrustedRequest GetTrustedRequest(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Features.Get<TrustedRequest>()
            ?? throw new InvalidOperationException("The trusted-path middleware did not run for this request.");
    }

    /// <summary>Adds the endpoints to the SSR allow-list: public storefront <c>GET</c> endpoints only.</summary>
    public static TBuilder AllowSsrCaller<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(SsrCallerAllowed.Instance);
}
