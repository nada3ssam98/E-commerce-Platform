using Microsoft.Extensions.Options;
using Npgsql;

namespace Platform.Persistence;

/// <summary>The application's database connection (<c>ConnectionStrings:platform</c>), validated at startup (M0-9).</summary>
public sealed class PlatformDatabaseOptions
{
    public const string ConnectionStringName = "platform";

    public const string SettingName = "ConnectionStrings:" + ConnectionStringName;

    public string? ConnectionString { get; set; }
}

/// <summary>Fails startup on a missing or invalid connection string. Messages name the setting, never its value.</summary>
internal sealed class PlatformDatabaseOptionsValidator : IValidateOptions<PlatformDatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, PlatformDatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail($"Missing setting '{PlatformDatabaseOptions.SettingName}'.");
        }

        NpgsqlConnectionStringBuilder connection;
        try
        {
            connection = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        }
        catch (ArgumentException)
        {
            // The exception text can quote parts of the connection string; it is deliberately not included.
            return ValidateOptionsResult.Fail(
                $"Setting '{PlatformDatabaseOptions.SettingName}' is not a valid PostgreSQL connection string.");
        }

        if (string.IsNullOrWhiteSpace(connection.Host)
            || string.IsNullOrWhiteSpace(connection.Database)
            || string.IsNullOrWhiteSpace(connection.Username))
        {
            return ValidateOptionsResult.Fail(
                $"Setting '{PlatformDatabaseOptions.SettingName}' must specify Host, Database, and Username.");
        }

        return ValidateOptionsResult.Success;
    }
}
