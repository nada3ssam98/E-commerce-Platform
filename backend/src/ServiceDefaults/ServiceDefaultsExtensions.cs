using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Platform.SharedKernel.Compliance;

namespace Microsoft.Extensions.Hosting;

/// <summary>Logging, redaction, and telemetry defaults for every platform host.</summary>
public static class ServiceDefaultsExtensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        // JSON console logs with scopes (request id / trace id); classified values are erased (M0-10).
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
        builder.Logging.EnableRedaction();
        builder.Services.AddPlatformRedaction();

        builder.AddPlatformOpenTelemetry();
        return builder;
    }

    /// <summary>Erases every value classified with <see cref="PlatformTaxonomy"/> — and any other classified value.</summary>
    public static IServiceCollection AddPlatformRedaction(this IServiceCollection services) =>
        services.AddRedaction(redaction =>
        {
            redaction.SetRedactor<ErasingRedactor>([.. PlatformTaxonomy.All]);
            redaction.SetFallbackRedactor<ErasingRedactor>();
        });

    private static void AddPlatformOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/api/health"))
                .AddHttpClientInstrumentation());

        // Exporter only when an endpoint is configured (Aspire sets it locally; the production exporter is chosen with ADR-12).
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }
}
