using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.Api.TrustedPath;
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

        // Public storefront GETs: the SSR server may call them (M0 SSR allow-list, ADR-04 rule 11).
        var health = app.MapGroup("/api/health").WithTags("Health").AllowSsrCaller();

        // Liveness: the process answers; no dependency is checked.
        health.MapGet("", () => TypedResults.Ok(new HealthResponse(HealthStatus.Healthy)))
            .WithName("getHealth")
            .WithSummary("Liveness: the API process answers. No dependency is checked.");

        // Readiness: the database is reachable with the application's credentials.
        health.MapGet("/ready", ReadyAsync)
            .WithName("getReadiness")
            .WithSummary("Readiness: the database is reachable with the application's credentials.")
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);
        return app;
    }

    // Status only: no check names, descriptions, or exception text leave the process.
    private static async Task<Results<Ok<HealthResponse>, JsonHttpResult<HealthResponse>>> ReadyAsync(
        HealthCheckService health, CancellationToken cancellationToken)
    {
        var report = await health.CheckHealthAsync(check => check.Tags.Contains(ReadyTag), cancellationToken);
        var response = new HealthResponse(report.Status);
        return report.Status == HealthStatus.Unhealthy
            ? TypedResults.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable)
            : TypedResults.Ok(response);
    }
}

/// <summary>Body of both health endpoints.</summary>
public sealed record HealthResponse(HealthStatus Status);
