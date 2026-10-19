using Npgsql;
using Platform.TestSupport;
using Shouldly;

namespace Platform.IntegrationTests.Persistence;

/// <summary>M0-11: the application role cannot run DDL, yet can use the tables the migration role creates.</summary>
[Collection(PostgresDatabaseGroup.Name)]
public sealed class ApplicationRoleTests(PostgresDatabaseFixture database)
{
    private const string InsufficientPrivilege = "42501";

    [Theory]
    [InlineData("CREATE TABLE ddl_probe (id integer)")]
    [InlineData("CREATE TEMPORARY TABLE ddl_probe (id integer)")]
    [InlineData("CREATE SCHEMA ddl_probe")]
    [InlineData("CREATE SEQUENCE ddl_probe")]
    public async Task ApplicationRoleCannotCreateObjects(string ddl)
    {
        database.SkipIfUnavailable();

        var exception = await Should.ThrowAsync<PostgresException>(() => ExecuteAsync(database.AppConnectionString, ddl));

        exception.SqlState.ShouldBe(InsufficientPrivilege);
    }

    [Fact]
    public async Task ApplicationRoleCanUseButNotAlterOrDropTablesCreatedByTheMigrationRole()
    {
        database.SkipIfUnavailable();
        await ExecuteAsync(database.MigratorConnectionString, "CREATE TABLE privilege_probe (id integer)");
        try
        {
            await ExecuteAsync(database.AppConnectionString, "INSERT INTO privilege_probe (id) VALUES (1)");
            await ExecuteAsync(database.AppConnectionString, "UPDATE privilege_probe SET id = 2");
            await ExecuteAsync(database.AppConnectionString, "DELETE FROM privilege_probe");

            var alter = await Should.ThrowAsync<PostgresException>(
                () => ExecuteAsync(database.AppConnectionString, "ALTER TABLE privilege_probe ADD COLUMN name text"));
            alter.SqlState.ShouldBe(InsufficientPrivilege);

            var drop = await Should.ThrowAsync<PostgresException>(
                () => ExecuteAsync(database.AppConnectionString, "DROP TABLE privilege_probe"));
            drop.SqlState.ShouldBe(InsufficientPrivilege);
        }
        finally
        {
            await ExecuteAsync(database.MigratorConnectionString, "DROP TABLE privilege_probe");
        }
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
