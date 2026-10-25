using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Platform.Api.TrustedPath;
using Platform.Persistence;

namespace Platform.IntegrationTests.Api;

/// <summary>
/// Hosts the real API in memory, in the <c>Production</c> environment unless stated, with a capturing logger.
/// <list type="bullet">
/// <item>Trusted-path settings are valid test values (<see cref="Settings"/>); <paramref name="settings"/> overrides
/// or removes them (a <c>null</c> value removes a setting).</item>
/// <item>The in-memory server has no connection address, so a test-only middleware placed before the application's
/// pipeline sets it from <see cref="RemoteAddressHeader"/> (TH-12 runs on the direct connection address).</item>
/// <item><c>CreateClient()</c> acts as the edge: API edge key, edge address, and <c>X-Platform-Host</c>. Use
/// <see cref="CreateBareClient"/> for requests that carry nothing.</item>
/// <item>Two test-only endpoints run after the application's own pipeline: one throws and one opts in to public caching.</item>
/// </list>
/// </summary>
public sealed class ApiFactory(
    string? connectionString,
    IReadOnlyDictionary<string, string?>? settings = null,
    string environment = "Production") : WebApplicationFactory<Program>
{
    /// <summary>Valid, but nothing listens there: the database is unreachable.</summary>
    public const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=platform;Username=platform_app;Password=unused;Timeout=2";

    public const string ThrowingPath = "/_test/throw";
    public const string PublicPath = "/_test/public";
    public const string ExceptionDetail = "test-only-exception-detail";

    public const string ApiEdgeKey = "test-api-edge-key-0123456789abcdef0123456789";
    public const string PreviousApiEdgeKey = "test-previous-api-edge-key-0123456789abcdef";
    public const string SsrApiKey = "test-ssr-api-key-0123456789abcdef0123456789";
    public const string SsrIngressKey = "test-ssr-ingress-key-0123456789abcdef01234567";

    public const string EdgeAddress = "10.1.0.10";
    public const string SsrAddress = "10.2.0.20";
    public const string UnknownAddress = "192.168.50.5";
    public const string PlatformHost = "brand-a.localhost";

    /// <summary>Test-only: the connection's remote address for this request.</summary>
    public const string RemoteAddressHeader = "X-Test-Remote-Address";

    public static IReadOnlyDictionary<string, string?> Settings { get; } = new Dictionary<string, string?>
    {
        [Setting(nameof(TrustedPathOptions.ApiEdgeKey))] = ApiEdgeKey,
        [Setting(nameof(TrustedPathOptions.ApiEdgeKeyPrevious))] = PreviousApiEdgeKey,
        [Setting(nameof(TrustedPathOptions.SsrApiKey))] = SsrApiKey,
        [Setting($"{nameof(TrustedPathOptions.EdgeNetworks)}:0")] = "10.1.0.0/16",
        [Setting($"{nameof(TrustedPathOptions.SsrNetworks)}:0")] = "10.2.0.0/16",
    };

    public FakeLogCollector Logs => Services.GetFakeLogCollector();

    public static string Setting(string name) => $"{TrustedPathOptions.SectionName}:{name}";

    /// <summary>A client whose requests carry no credential, host, or address unless the test adds them.</summary>
    public HttpClient CreateBareClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Clear();
        return client;
    }

    protected override void ConfigureClient(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add(TrustedPathHeaders.ApiEdgeKey, ApiEdgeKey);
        client.DefaultRequestHeaders.Add(TrustedPathHeaders.PlatformHost, PlatformHost);
        client.DefaultRequestHeaders.Add(RemoteAddressHeader, EdgeAddress);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting($"ConnectionStrings:{PlatformDatabaseOptions.ConnectionStringName}", connectionString);

        var merged = new Dictionary<string, string?>(Settings);
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            merged[key] = value;
        }

        // A null override removes the setting: it is simply not supplied.
        foreach (var (key, value) in merged.Where(setting => setting.Value is not null))
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            services.AddFakeLogging();
            services.AddSingleton<IStartupFilter, TestPipeline>();
        });
    }

    private sealed class TestPipeline : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (IPAddress.TryParse(context.Request.Headers[RemoteAddressHeader], out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                context.Request.Headers.Remove(RemoteAddressHeader);
                return nextMiddleware(context);
            });

            next(app);
            app.Map(ThrowingPath, branch => branch.Run(_ => throw new InvalidOperationException(ExceptionDetail)));
            app.Map(PublicPath, branch => branch.Run(context =>
            {
                context.Response.Headers.CacheControl = "public, max-age=0, s-maxage=60";
                return context.Response.WriteAsync("public");
            }));
        };
    }
}
