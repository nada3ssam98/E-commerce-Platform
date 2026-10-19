using Microsoft.EntityFrameworkCore.Design;
using Platform.Infrastructure.Persistence;

namespace Platform.Persistence;

/// <summary>
/// Used by the EF Core tools (<c>dotnet ef migrations …</c>). Commands that touch a database read the
/// migration role's connection string from <c>PLATFORM_MIGRATOR_CONNECTION</c>; generating a migration
/// needs no database, so a placeholder without credentials is used otherwise.
/// </summary>
public sealed class DesignTimePlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    private const string PlaceholderConnectionString = "Host=localhost;Database=platform;Username=platform_migrator";

    public PlatformDbContext CreateDbContext(string[] args) =>
        PlatformModel.CreateDbContext(
            Environment.GetEnvironmentVariable("PLATFORM_MIGRATOR_CONNECTION") ?? PlaceholderConnectionString);
}
