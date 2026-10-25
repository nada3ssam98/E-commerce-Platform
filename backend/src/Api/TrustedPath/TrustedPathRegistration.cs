using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Platform.Api.TrustedPath;

/// <summary>Registers the trusted path (ADR-04 rules 9–12) and the test-only diagnostics (§4d).</summary>
public static class TrustedPathRegistration
{
    public static IServiceCollection AddPlatformTrustedPath(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TrustedPathOptions>()
            .Bind(configuration.GetSection(TrustedPathOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<TrustedPathOptions>, TrustedPathOptionsValidator>();

        // Rule 12: X-Forwarded-For / X-Forwarded-Proto only, only from edge networks; never X-Forwarded-Host.
        services.AddOptions<ForwardedHeadersOptions>().Configure<IOptions<TrustedPathOptions>>((forwarded, trustedPath) =>
        {
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            forwarded.ForwardLimit = 1;
            forwarded.KnownProxies.Clear();
            forwarded.KnownIPNetworks.Clear();
            // Invalid ranges are reported by startup validation; they are never trusted here.
            foreach (var range in trustedPath.Value.EdgeNetworks)
            {
                if (System.Net.IPNetwork.TryParse(range.Trim(), out var network))
                {
                    forwarded.KnownIPNetworks.Add(network);
                }
            }
        });

        services.AddSingleton<SsrCallRecorder>();
        return services;
    }

    /// <summary>
    /// Classification first, then forwarded headers (so source checks see the direct address), then routing and
    /// the SSR allow-list.
    /// </summary>
    public static WebApplication UsePlatformTrustedPath(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<TrustedPathMiddleware>();
        if (app.Services.GetRequiredService<IOptions<TrustedPathOptions>>().Value.EnableTestDiagnostics)
        {
            app.UseMiddleware<SsrCallRecorderMiddleware>();
        }

        app.UseForwardedHeaders();
        app.UseRouting();
        app.UseMiddleware<SsrAllowListMiddleware>();
        return app;
    }
}
