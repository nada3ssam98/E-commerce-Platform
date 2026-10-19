using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.Infrastructure.Persistence;

namespace Platform.Api;

/// <summary><c>GET /api/health</c> (liveness) and <c>GET /api/health/ready</c> (database reachable).</summary>
public static class HealthEndpoints
{
    private const string ReadyTag = "ready";

    public static IHealthChecksBuilder AddPlatformHealthChecks(this IHealthChecksBuilder builder) =>
        builder.AddDbContextCheck<PlatformDbContext>("database", tags: [ReadyTag]);

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Liveness: the process answers; no dependency is checked.
        app.MapHealthChecks("/api/health", Options(_ => false));

        // Readiness: the database is reachable with the application's credentials.
        app.MapHealthChecks("/api/health/ready", Options(check => check.Tags.Contains(ReadyTag)));
        return app;
    }

    private static HealthCheckOptions Options(Func<HealthCheckRegistration, bool> predicate) => new()
    {
        Predicate = predicate,

        // Leave Cache-Control to the API default (private, no-store) instead of the middleware's own headers.
        AllowCachingResponses = true,

        // Status only: no check names, descriptions, or exception text leave the process.
        ResponseWriter = (context, report) =>
        {
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(new { status = report.Status.ToString() }));
        },
    };
}
