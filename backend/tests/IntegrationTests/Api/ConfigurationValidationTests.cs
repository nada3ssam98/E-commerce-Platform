using Microsoft.Extensions.Options;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary>M0-9 (API): missing or invalid configuration stops startup, names the setting, and prints no secret.</summary>
public sealed class ConfigurationValidationTests
{
    private const string Setting = "ConnectionStrings:platform";
    private const string SecretValue = "TopSecret-7f3a91";

    [Fact]
    public void MissingConnectionStringStopsStartupAndNamesTheSetting()
    {
        var error = StartupError(connectionString: null);

        error.Message.ShouldContain($"Missing setting '{Setting}'");
    }

    [Theory]
    [InlineData($"Host=db;Database=platform;Username=platform_app;Password={SecretValue};Nonsense=1")]
    [InlineData($"Password={SecretValue}")]
    public void InvalidConnectionStringStopsStartupWithoutPrintingItsValue(string connectionString)
    {
        var error = StartupError(connectionString);

        error.Message.ShouldContain(Setting);
        error.Message.ShouldNotContain(SecretValue);
    }

    private static OptionsValidationException StartupError(string? connectionString)
    {
        using var factory = new ApiFactory(connectionString);

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is OptionsValidationException validation)
            {
                return validation;
            }
        }

        throw new ShouldAssertException($"Expected an options validation failure, got: {exception.GetType().Name}");
    }
}
