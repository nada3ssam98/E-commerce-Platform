using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Platform.Persistence;

namespace Platform.IntegrationTests.Api;

/// <summary>
/// Hosts the real API in memory, in the <c>Production</c> environment, with a capturing logger.
/// Adds two test-only endpoints after the application's own pipeline: one that throws and one that opts in to
/// public caching.
/// </summary>
public sealed class ApiFactory(string? connectionString) : WebApplicationFactory<Program>
{
    /// <summary>Valid, but nothing listens there: the database is unreachable.</summary>
    public const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=platform;Username=platform_app;Password=unused;Timeout=2";

    public const string ThrowingPath = "/_test/throw";
    public const string PublicPath = "/_test/public";
    public const string ExceptionDetail = "test-only-exception-detail";

    public FakeLogCollector Logs => Services.GetFakeLogCollector();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting($"ConnectionStrings:{PlatformDatabaseOptions.ConnectionStringName}", connectionString);
        builder.ConfigureServices(services =>
        {
            services.AddFakeLogging();
            services.AddSingleton<IStartupFilter, TestEndpoints>();
        });
    }

    private sealed class TestEndpoints : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
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
