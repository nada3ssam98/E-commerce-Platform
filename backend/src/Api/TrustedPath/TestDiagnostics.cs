using Microsoft.Extensions.Options;

namespace Platform.Api.TrustedPath;

/// <summary>
/// Test-only request-context diagnostics (ADR-04 §4d): registered only when <c>TrustedPath:EnableTestDiagnostics</c>
/// is set, which startup validation allows only in the <c>Testing</c> environment (TH-9). Everywhere else the routes
/// do not exist (<c>404</c>). They report which credentials arrived, never their values.
/// </summary>
public static class TestDiagnostics
{
    public const string RequestContextPath = "/api/_test/request-context";

    public const string LastSsrCallPath = "/api/_test/last-ssr-call";

    public static WebApplication MapTestDiagnostics(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!app.Services.GetRequiredService<IOptions<TrustedPathOptions>>().Value.EnableTestDiagnostics)
        {
            return app;
        }

        // The caller and host as the API classified them (TH-1, TH-6, TH-13). Reachable by the SSR server too.
        app.MapGet(RequestContextPath, (HttpContext context) => TypedResults.Ok(RequestContext.From(context)))
            .AllowSsrCaller()
            .ExcludeFromDescription();

        // The most recent request classified as coming from the SSR server (TH-4: what the SSR server sent).
        app.MapGet(LastSsrCallPath, (SsrCallRecorder recorder) =>
                recorder.Last is { } last ? Results.Ok(last) : Results.NotFound())
            .ExcludeFromDescription();
        return app;
    }

    /// <summary>What a diagnostic reports about one request.</summary>
    public sealed record RequestContext(
        string Caller, string PlatformHost, string Method, string Path, string? RequestHost, CredentialHeaders CredentialHeaders)
    {
        internal static RequestContext From(HttpContext context)
        {
            var trusted = context.GetTrustedRequest();
            var headers = context.Request.Headers;
            return new RequestContext(
                trusted.Caller.ToString(),
                trusted.PlatformHost,
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                context.Request.Host.HasValue ? context.Request.Host.Value : null,
                new CredentialHeaders(
                    headers.ContainsKey(TrustedPathHeaders.ApiEdgeKey),
                    headers.ContainsKey(TrustedPathHeaders.SsrApiKey),
                    headers.ContainsKey(TrustedPathHeaders.SsrIngressKey)));
        }
    }

    /// <summary>Presence of each trusted-path credential header (values are never reported).</summary>
    public sealed record CredentialHeaders(bool ApiEdgeKey, bool SsrApiKey, bool SsrIngressKey);
}

/// <summary>Holds the last request from the SSR server; used only by the diagnostics.</summary>
public sealed class SsrCallRecorder
{
    private TestDiagnostics.RequestContext? last;

    public TestDiagnostics.RequestContext? Last => Volatile.Read(ref last);

    internal void Record(TestDiagnostics.RequestContext context) => Volatile.Write(ref last, context);
}

/// <summary>Records SSR requests after classification; added to the pipeline only when diagnostics are enabled.</summary>
internal sealed class SsrCallRecorderMiddleware(RequestDelegate next, SsrCallRecorder recorder)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (context.GetTrustedRequest().Caller == TrustedCaller.Ssr)
        {
            recorder.Record(TestDiagnostics.RequestContext.From(context));
        }

        return next(context);
    }
}
