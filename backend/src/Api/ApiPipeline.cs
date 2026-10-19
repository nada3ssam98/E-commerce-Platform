using System.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Net.Http.Headers;
using Platform.SharedKernel;

namespace Platform.Api;

/// <summary>Cross-cutting HTTP behavior of the API (milestone-m0-m1 §2, M0 step 3).</summary>
public static class ApiPipeline
{
    public const string RequestIdHeader = "X-Request-Id";

    /// <summary>Default for every response that does not opt in to public caching (ADR-04 §4f; M0-15).</summary>
    public const string DefaultCacheControl = "private, no-store";

    /// <summary>Headers whose values never appear in logs (M0-10; trusted-path key names fixed by ADR-04 §4b).</summary>
    public static IReadOnlyList<string> SensitiveHeaders { get; } =
    [
        HeaderNames.Cookie,
        HeaderNames.SetCookie,
        HeaderNames.Authorization,
        "X-Platform-Api-Edge-Key",
        "X-Platform-Ssr-Ingress-Key",
        "X-Platform-Ssr-Api-Key",
    ];

    public static IServiceCollection AddPlatformApi(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var status = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
            context.ProblemDetails.Extensions.TryAdd("code", ErrorCodes.ForStatus(status));

            // One identifier per request: the value of the X-Request-Id response header.
            context.ProblemDetails.Extensions.Remove("traceId");
            context.ProblemDetails.Extensions["requestId"] = context.HttpContext.TraceIdentifier;
        });

        // Request logging without query strings or bodies. ASP.NET Core logs only allow-listed header values and
        // writes "[Redacted]" for every other header; sensitive headers are kept off the allow-list (M0-10).
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.RequestProtocol
                | HttpLoggingFields.RequestScheme
                | HttpLoggingFields.RequestHeaders
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.ResponseHeaders
                | HttpLoggingFields.Duration;
            options.CombineLogs = true;
            foreach (var header in SensitiveHeaders)
            {
                options.RequestHeaders.Remove(header);
                options.ResponseHeaders.Remove(header);
            }
        });

        services.AddHealthChecks().AddPlatformHealthChecks();
        return services;
    }

    public static WebApplication UsePlatformApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Request id: the trace id of the request, returned to the caller and used in logs and errors.
        app.Use((context, next) =>
        {
            var requestId = Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");
            context.TraceIdentifier = requestId;
            context.Response.Headers[RequestIdHeader] = requestId;
            return next(context);
        });

        // Secure default: a response is publicly cacheable only if its endpoint sets Cache-Control itself.
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
                {
                    context.Response.Headers.CacheControl = DefaultCacheControl;
                }

                return Task.CompletedTask;
            });
            return next(context);
        });

        app.UseHttpLogging();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
