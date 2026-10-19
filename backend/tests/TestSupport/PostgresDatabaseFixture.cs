using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Platform.TestSupport;

/// <summary>
/// A real PostgreSQL container prepared exactly like the local environment: the bootstrap script creates the
/// platform database and its roles, then all migrations are applied from zero as the migration role.
/// </summary>
/// <remarks>
/// Without a container runtime the dependent tests are <b>skipped</b> with an explicit reason — unless
/// <c>PLATFORM_REQUIRE_DOCKER=1</c> is set (CI), in which case the missing runtime fails the run.
/// </remarks>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    /// <summary>Keep in sync with the image tag in <c>src/AppHost/Program.cs</c>.</summary>
    public const string Image = "postgres:17.11";

    private const string RequireDockerVariable = "PLATFORM_REQUIRE_DOCKER";

    private PostgreSqlContainer? container;

    /// <summary>Why the database is unavailable, or <see langword="null"/> when it is ready.</summary>
    public string? UnavailableReason { get; private set; }

    public string MigratorConnectionString { get; private set; } = string.Empty;

    public string AppConnectionString { get; private set; } = string.Empty;

    /// <summary>Number of migrations already applied before this fixture migrated the new database.</summary>
    public int MigrationsAppliedBeforeMigrate { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            container = new PostgreSqlBuilder(Image).Build();
            await container.StartAsync();
        }
        catch (DockerUnavailableException exception)
        {
            if (Environment.GetEnvironmentVariable(RequireDockerVariable) == "1")
            {
                throw;
            }

            UnavailableReason = $"No container runtime available for PostgreSQL tests ({exception.Message}). " +
                $"Set {RequireDockerVariable}=1 to make this a failure.";
            return;
        }

        var migratorPassword = RandomNumberGenerator.GetHexString(32, lowercase: true);
        var appPassword = RandomNumberGenerator.GetHexString(32, lowercase: true);

        await container.CopyAsync(await File.ReadAllBytesAsync(RepositoryPaths.PostgresBootstrapScript), "/tmp/01-bootstrap.sh");
        var bootstrap = await container.ExecAsync(
        [
            "bash", "-c",
            $"PLATFORM_MIGRATOR_PASSWORD={migratorPassword} PLATFORM_APP_PASSWORD={appPassword} bash /tmp/01-bootstrap.sh",
        ]);
        if (bootstrap.ExitCode != 0)
        {
            throw new InvalidOperationException($"Database bootstrap failed ({bootstrap.ExitCode}): {bootstrap.Stderr}");
        }

        var connection = new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = "platform" };
        connection.Username = "platform_migrator";
        connection.Password = migratorPassword;
        MigratorConnectionString = connection.ConnectionString;
        connection.Username = "platform_app";
        connection.Password = appPassword;
        AppConnectionString = connection.ConnectionString;

        await using var context = PlatformModel.CreateDbContext(MigratorConnectionString);
        MigrationsAppliedBeforeMigrate = (await context.Database.GetAppliedMigrationsAsync()).Count();
        await context.Database.MigrateAsync();
    }

    /// <summary>Skips the calling test when no database could be started.</summary>
    public void SkipIfUnavailable()
    {
        if (UnavailableReason is not null)
        {
            Assert.Skip(UnavailableReason);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }
}
