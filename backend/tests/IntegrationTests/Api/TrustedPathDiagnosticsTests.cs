using System.Net;
using System.Net.Http.Json;
using Platform.Api.TrustedPath;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary>
/// Test-only request-context diagnostics (ADR-04 §4d) and what they show about host handling: the store host comes only
/// from <c>X-Platform-Host</c>, normalized; <c>Host</c> and forwarded-host headers are ignored (TH-1, TH-6, TH-10, TH-13).
/// </summary>
public sealed class TrustedPathDiagnosticsTests : IDisposable
{
    private static readonly Dictionary<string, string?> DiagnosticsOn = new()
    {
        [ApiFactory.Setting(nameof(TrustedPathOptions.EnableTestDiagnostics))] = "true",
    };

    private readonly ApiFactory factory = new(ApiFactory.UnreachableDatabase, DiagnosticsOn, environment: "Testing");

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task TheStoreHostComesOnlyFromXPlatformHost()
    {
        // TH-6 (API Host brand-b) and TH-1 at the API (forged forwarded-host headers).
        var headers = TrustedPathTests.Edge();
        headers["Host"] = "brand-b.localhost";
        headers["X-Forwarded-Host"] = "brand-b.localhost";
        headers["Forwarded"] = "host=brand-b.localhost";

        var context = await RequestContextAsync(headers);

        context.Caller.ShouldBe("Edge");
        context.PlatformHost.ShouldBe("brand-a.localhost");
        context.RequestHost.ShouldBe("brand-b.localhost");
    }

    [Theory]
    [InlineData("Brand-A.localhost:443")]
    [InlineData("brand-a.localhost.")]
    public async Task TheStoreHostIsNormalizedAgain(string platformHost)
    {
        // TH-10.
        var headers = TrustedPathTests.Edge();
        headers[TrustedPathHeaders.PlatformHost] = platformHost;

        var context = await RequestContextAsync(headers);

        context.PlatformHost.ShouldBe("brand-a.localhost");
    }

    [Fact]
    public async Task TheDiagnosticReportsWhichCredentialsArrived()
    {
        // TH-13 support: an edge request carries only the API edge key.
        var context = await RequestContextAsync(TrustedPathTests.Edge());

        context.CredentialHeaders.ShouldBe(new TestDiagnostics.CredentialHeaders(ApiEdgeKey: true, SsrApiKey: false, SsrIngressKey: false));
    }

    [Fact]
    public async Task TheLastSsrCallIsRecordedForTests()
    {
        // TH-4 support: what the SSR server sent, read back by the test through the edge.
        using var ssrCall = await TrustedPathTests.SendAsync(factory, HttpMethod.Get, "/api/health", TrustedPathTests.Ssr());
        ssrCall.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var response = await TrustedPathTests.SendAsync(factory, HttpMethod.Get, TestDiagnostics.LastSsrCallPath, TrustedPathTests.Edge());
        var last = await response.Content.ReadFromJsonAsync<TestDiagnostics.RequestContext>(TestContext.Current.CancellationToken);

        last.ShouldNotBeNull();
        last.Caller.ShouldBe("Ssr");
        last.Path.ShouldBe("/api/health");
        last.PlatformHost.ShouldBe("brand-a.localhost");
        last.CredentialHeaders.ShouldBe(new TestDiagnostics.CredentialHeaders(ApiEdgeKey: false, SsrApiKey: true, SsrIngressKey: false));
    }

    [Fact]
    public async Task TheSsrServerMayReadOnlyTheRequestContextDiagnostic()
    {
        using var context = await TrustedPathTests.SendAsync(factory, HttpMethod.Get, TestDiagnostics.RequestContextPath, TrustedPathTests.Ssr());
        using var lastCall = await TrustedPathTests.SendAsync(factory, HttpMethod.Get, TestDiagnostics.LastSsrCallPath, TrustedPathTests.Ssr());

        context.StatusCode.ShouldBe(HttpStatusCode.OK);
        lastCall.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Production", null)]
    [InlineData("Production", "false")]
    [InlineData("Testing", null)]
    public async Task DiagnosticsDoNotExistWithoutTestingAndTheFlag(string environment, string? flag)
    {
        // TH-9 / M0-8: 404 even for a fully trusted edge request.
        using var other = new ApiFactory(
            ApiFactory.UnreachableDatabase,
            new Dictionary<string, string?> { [ApiFactory.Setting(nameof(TrustedPathOptions.EnableTestDiagnostics))] = flag },
            environment);

        foreach (var path in new[] { TestDiagnostics.RequestContextPath, TestDiagnostics.LastSsrCallPath })
        {
            using var response = await TrustedPathTests.SendAsync(other, HttpMethod.Get, path, TrustedPathTests.Edge());
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }

    private async Task<TestDiagnostics.RequestContext> RequestContextAsync(IReadOnlyDictionary<string, string> headers)
    {
        using var response = await TrustedPathTests.SendAsync(factory, HttpMethod.Get, TestDiagnostics.RequestContextPath, headers);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var context = await response.Content.ReadFromJsonAsync<TestDiagnostics.RequestContext>(TestContext.Current.CancellationToken);
        return context.ShouldNotBeNull();
    }
}
