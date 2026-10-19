using System.Net;
using System.Text.Json;
using Platform.TestSupport;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary><c>GET /api/health</c> and <c>GET /api/health/ready</c> (milestone-m0-m1 §2).</summary>
public sealed class HealthEndpointTests
{
    [Fact]
    public async Task LivenessIsHealthyWithoutDatabase()
    {
        using var factory = new ApiFactory(ApiFactory.UnreachableDatabase);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        (await StatusOf(response)).ShouldBe("Healthy");
    }

    [Fact]
    public async Task ReadinessIsUnavailableWhenTheDatabaseIsUnreachable()
    {
        using var factory = new ApiFactory(ApiFactory.UnreachableDatabase);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/health/ready", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await StatusOf(response)).ShouldBe("Unhealthy");
    }

    internal static async Task<string?> StatusOf(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("status").GetString();
    }
}

/// <summary>Readiness against a real database, connected as the application role.</summary>
[Collection(PostgresDatabaseGroup.Name)]
public sealed class ReadinessWithDatabaseTests(PostgresDatabaseFixture database)
{
    [Fact]
    public async Task ReadinessIsHealthyWhenTheDatabaseIsReachable()
    {
        database.SkipIfUnavailable();
        using var factory = new ApiFactory(database.AppConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/health/ready", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await HealthEndpointTests.StatusOf(response)).ShouldBe("Healthy");
    }
}
