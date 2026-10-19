using Microsoft.EntityFrameworkCore;
using Platform.Persistence;
using Platform.TestSupport;
using Shouldly;

namespace Platform.IntegrationTests.Persistence;

/// <summary>M0-11: migrations run from zero on an empty database.</summary>
[Collection(PostgresDatabaseGroup.Name)]
public sealed class MigrationsFromZeroTests(PostgresDatabaseFixture database)
{
    [Fact]
    public async Task EveryMigrationAppliesToAnEmptyDatabase()
    {
        database.SkipIfUnavailable();
        var cancellationToken = TestContext.Current.CancellationToken;

        database.MigrationsAppliedBeforeMigrate.ShouldBe(0, "the fixture must start from an empty database");

        await using var context = PlatformModel.CreateDbContext(database.MigratorConnectionString);
        (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ShouldBeEmpty();
        (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ShouldBe(context.Database.GetMigrations());
    }
}
