using System.Net;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary>The OpenAPI document is a build output (contracts/openapi.json), never served by the running API (ADR-03).</summary>
public sealed class OpenApiContractTests
{
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/openapi/openapi.json")]
    public async Task TheDocumentIsNotServedAtRuntime(string path)
    {
        using var factory = new ApiFactory(ApiFactory.UnreachableDatabase);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
