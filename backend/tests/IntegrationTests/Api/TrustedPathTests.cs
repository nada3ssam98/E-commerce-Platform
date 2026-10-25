using System.Net;
using System.Text.Json;
using Platform.Api.TrustedPath;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary>
/// The API side of the trusted path (ADR-04 rules 9–12): caller classification by credential and source network,
/// the SSR allow-list, and rejection with <c>403</c>. TH-2, TH-5, TH-11, TH-12 (milestone-m0-m1 M0-5, M0-6, M0-8a, M0-8b).
/// </summary>
public sealed class TrustedPathTests : IDisposable
{
    private const string Health = "/api/health";

    /// <summary>Routes outside the SSR allow-list: admin, auth, cart, checkout, account (TH-5). None exist yet.</summary>
    public static TheoryData<string> NonAllowListedRoutes { get; } =
        ["/api/admin/me", "/api/auth/login", "/api/cart", "/api/checkout", "/api/account/orders", "/api/does-not-exist"];

    private readonly ApiFactory factory = new(ApiFactory.UnreachableDatabase);

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task TheEdgeWithTheCurrentOrPreviousKeyIsAccepted()
    {
        foreach (var key in new[] { ApiFactory.ApiEdgeKey, ApiFactory.PreviousApiEdgeKey })
        {
            using var response = await SendAsync(HttpMethod.Get, Health, Edge(key));
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Theory]
    [InlineData("no key")]
    [InlineData("wrong key")]
    [InlineData("forged host without key")]
    [InlineData("empty key")]
    public async Task RequestsWithoutAValidCredentialAreForbidden(string scenario)
    {
        var headers = scenario switch
        {
            "no key" => new Dictionary<string, string> { [TrustedPathHeaders.PlatformHost] = ApiFactory.PlatformHost },
            "wrong key" => Edge("wrong-key-wrong-key-wrong-key-wrong-key"),
            "forged host without key" => new Dictionary<string, string>
            {
                [TrustedPathHeaders.PlatformHost] = "brand-b.localhost",
                ["X-Forwarded-Host"] = "brand-b.localhost",
            },
            _ => Edge(string.Empty),
        };
        headers[ApiFactory.RemoteAddressHeader] = ApiFactory.EdgeAddress;

        using var response = await SendAsync(HttpMethod.Get, Health, headers);

        await AssertForbiddenAsync(response);
    }

    [Theory]
    [InlineData("GET", Health)]
    [InlineData("GET", "/api/admin/me")]
    [InlineData("POST", "/api/cart")]
    public async Task TheSsrIngressKeyIsNeverAcceptedByTheApi(string method, string path)
    {
        // TH-11: alone, from either network, and even next to a valid API edge key.
        Dictionary<string, string>[] attempts =
        [
            new() { [TrustedPathHeaders.SsrIngressKey] = ApiFactory.SsrIngressKey, [TrustedPathHeaders.PlatformHost] = ApiFactory.PlatformHost, [ApiFactory.RemoteAddressHeader] = ApiFactory.EdgeAddress },
            new() { [TrustedPathHeaders.SsrIngressKey] = ApiFactory.SsrIngressKey, [TrustedPathHeaders.PlatformHost] = ApiFactory.PlatformHost, [ApiFactory.RemoteAddressHeader] = ApiFactory.SsrAddress },
            new(Edge()) { [TrustedPathHeaders.SsrIngressKey] = ApiFactory.SsrIngressKey },
        ];

        foreach (var headers in attempts)
        {
            using var response = await SendAsync(new HttpMethod(method), path, headers);
            await AssertForbiddenAsync(response);
        }
    }

    [Theory]
    [InlineData("GET", "/api/admin/me")]
    [InlineData("POST", "/api/auth/login")]
    [InlineData("POST", "/api/cart/items")]
    [InlineData("GET", Health)]
    public async Task TheApiEdgeKeyFromAnSsrOrUnknownAddressIsForbidden(string method, string path)
    {
        // TH-12: a valid edge key counts only from an edge network.
        foreach (var address in new[] { ApiFactory.SsrAddress, ApiFactory.UnknownAddress })
        {
            using var response = await SendAsync(new HttpMethod(method), path, Edge(address: address));
            await AssertForbiddenAsync(response);
        }
    }

    [Fact]
    public async Task TheSsrKeyFromAnEdgeOrUnknownAddressIsForbidden()
    {
        // TH-12: a valid SSR-to-API key counts only from an SSR network.
        foreach (var address in new[] { ApiFactory.EdgeAddress, ApiFactory.UnknownAddress })
        {
            using var response = await SendAsync(HttpMethod.Get, Health, Ssr(address));
            await AssertForbiddenAsync(response);
        }
    }

    [Fact]
    public async Task ACredentialWithoutAConnectionAddressIsForbidden()
    {
        var headers = Edge();
        headers.Remove(ApiFactory.RemoteAddressHeader);

        using var response = await SendAsync(HttpMethod.Get, Health, headers);

        await AssertForbiddenAsync(response);
    }

    [Fact]
    public async Task TheSsrServerMayGetAllowListedEndpoints()
    {
        using var response = await SendAsync(HttpMethod.Get, Health, Ssr());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("HEAD")]
    public async Task TheSsrServerMayNotUseOtherMethodsOnAllowListedEndpoints(string method)
    {
        // TH-5: GET only.
        using var response = await SendAsync(new HttpMethod(method), Health, Ssr());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(NonAllowListedRoutes))]
    public async Task TheSsrServerMayNotReachRoutesOutsideTheAllowList(string path)
    {
        // TH-5: the same routes give the edge a 404 (they do not exist yet); the SSR caller gets 403 before routing matters.
        using var ssrResponse = await SendAsync(HttpMethod.Get, path, Ssr());
        using var ssrPost = await SendAsync(HttpMethod.Post, path, Ssr());
        using var edgeResponse = await SendAsync(HttpMethod.Get, path, Edge());

        await AssertForbiddenAsync(ssrResponse);
        await AssertForbiddenAsync(ssrPost);
        edgeResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BothCredentialsTogetherAreForbidden()
    {
        var headers = Edge();
        headers[TrustedPathHeaders.SsrApiKey] = ApiFactory.SsrApiKey;

        using var response = await SendAsync(HttpMethod.Get, Health, headers);

        await AssertForbiddenAsync(response);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("brand_b.localhost")]
    [InlineData("brand-a.localhost/admin")]
    public async Task AValidCredentialWithoutAValidPlatformHostIsForbidden(string? platformHost)
    {
        var headers = Edge();
        headers.Remove(TrustedPathHeaders.PlatformHost);
        if (platformHost is not null)
        {
            headers[TrustedPathHeaders.PlatformHost] = platformHost;
        }

        using var response = await SendAsync(HttpMethod.Get, Health, headers);

        await AssertForbiddenAsync(response);
    }

    [Fact]
    public async Task ARejectedRequestIsLoggedWithoutCredentialValues()
    {
        const string wrongKey = "wrong-key-value-3c9d1e-wrong-key-value";

        using var response = await SendAsync(HttpMethod.Get, Health, Edge(wrongKey));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var entry = factory.Logs.GetSnapshot().Single(record => record.Category == typeof(TrustedPathMiddleware).FullName);
        entry.Message.ShouldContain("invalid-credential");
        entry.Message.ShouldContain(Health);
        foreach (var record in factory.Logs.GetSnapshot())
        {
            record.Message.ShouldNotContain(wrongKey);
            record.Message.ShouldNotContain(ApiFactory.ApiEdgeKey);
        }
    }

    internal static Dictionary<string, string> Edge(string key = ApiFactory.ApiEdgeKey, string address = ApiFactory.EdgeAddress) => new()
    {
        [TrustedPathHeaders.ApiEdgeKey] = key,
        [TrustedPathHeaders.PlatformHost] = ApiFactory.PlatformHost,
        [ApiFactory.RemoteAddressHeader] = address,
    };

    internal static Dictionary<string, string> Ssr(string address = ApiFactory.SsrAddress) => new()
    {
        [TrustedPathHeaders.SsrApiKey] = ApiFactory.SsrApiKey,
        [TrustedPathHeaders.PlatformHost] = ApiFactory.PlatformHost,
        [ApiFactory.RemoteAddressHeader] = address,
    };

    internal static async Task<HttpResponseMessage> SendAsync(
        ApiFactory factory, HttpMethod method, string path, IReadOnlyDictionary<string, string> headers)
    {
        using var client = factory.CreateBareClient();
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        foreach (var (name, value) in headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task AssertForbiddenAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("code").GetString().ShouldBe("FORBIDDEN");

        // No store data: only the Problem Details members.
        body.RootElement.EnumerateObject().Select(member => member.Name)
            .ShouldBeSubsetOf(["type", "title", "status", "code", "requestId"]);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, IReadOnlyDictionary<string, string> headers) =>
        SendAsync(factory, method, path, headers);
}
