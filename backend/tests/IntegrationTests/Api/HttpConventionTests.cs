using System.Net;
using System.Text.Json;
using Platform.Api;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary>Problem Details, request id, and the default Cache-Control header (M0-15).</summary>
public sealed class HttpConventionTests : IDisposable
{
    private readonly ApiFactory factory = new(ApiFactory.UnreachableDatabase);

    public void Dispose() => factory.Dispose();

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/does-not-exist")]
    public async Task ResponsesThatDoNotOptInToPublicCachingArePrivateNoStore(string path)
    {
        using var response = await GetAsync(path);

        // ApiPipeline.DefaultCacheControl = "private, no-store": exactly these two directives.
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.Private.ShouldBeTrue();
        cacheControl.NoStore.ShouldBeTrue();
        cacheControl.Public.ShouldBeFalse();
        cacheControl.SharedMaxAge.ShouldBeNull();
        response.Headers.GetValues("Cache-Control").Single().Split(',').Length.ShouldBe(2);
    }

    [Fact]
    public async Task AnEndpointThatOptsInKeepsItsCacheControl()
    {
        using var response = await GetAsync(ApiFactory.PublicPath);

        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.Public.ShouldBeTrue();
        cacheControl.SharedMaxAge.ShouldBe(TimeSpan.FromSeconds(60));
        cacheControl.NoStore.ShouldBeFalse();
    }

    [Fact]
    public async Task EveryResponseCarriesADistinctRequestId()
    {
        using var first = await GetAsync("/api/health");
        using var second = await GetAsync("/api/health");

        var firstId = first.Headers.GetValues(ApiPipeline.RequestIdHeader).Single();
        var secondId = second.Headers.GetValues(ApiPipeline.RequestIdHeader).Single();
        firstId.ShouldMatch("^[0-9a-f]{32}$");
        secondId.ShouldNotBe(firstId);
    }

    [Fact]
    public async Task UnknownRouteReturnsProblemDetailsWithCodeAndRequestId()
    {
        using var response = await GetAsync("/api/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problem = await ProblemOf(response);
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("NOT_FOUND");
        problem.RootElement.GetProperty("requestId").GetString()
            .ShouldBe(response.Headers.GetValues(ApiPipeline.RequestIdHeader).Single());
        problem.RootElement.TryGetProperty("traceId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task UnhandledExceptionReturnsInternalErrorWithoutDetails()
    {
        using var response = await GetAsync(ApiFactory.ThrowingPath);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain(ApiFactory.ExceptionDetail);
        body.ShouldNotContain(nameof(InvalidOperationException));
        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("INTERNAL_ERROR");
    }

    private async Task<HttpResponseMessage> GetAsync(string path)
    {
        using var client = factory.CreateClient();
        return await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
    }

    private static async Task<JsonDocument> ProblemOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
