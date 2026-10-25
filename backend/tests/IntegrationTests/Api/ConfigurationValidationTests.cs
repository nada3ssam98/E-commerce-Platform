using Microsoft.Extensions.Options;
using Platform.Api.TrustedPath;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary>
/// M0-9 (API): missing or invalid configuration stops startup, names the setting, and prints no secret.
/// Includes the trusted-path settings (ADR-04 rules 9–12) and the diagnostics flag (TH-9).
/// </summary>
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

    [Fact]
    public void MissingTrustedPathSettingsStopStartupAndAreNamed()
    {
        var error = StartupError(ApiFactory.UnreachableDatabase, new Dictionary<string, string?>
        {
            [ApiFactory.Setting(nameof(TrustedPathOptions.ApiEdgeKey))] = null,
            [ApiFactory.Setting(nameof(TrustedPathOptions.ApiEdgeKeyPrevious))] = null,
            [ApiFactory.Setting(nameof(TrustedPathOptions.SsrApiKey))] = null,
            [ApiFactory.Setting($"{nameof(TrustedPathOptions.EdgeNetworks)}:0")] = null,
            [ApiFactory.Setting($"{nameof(TrustedPathOptions.SsrNetworks)}:0")] = null,
        });

        foreach (var name in new[] { "ApiEdgeKey", "SsrApiKey", "EdgeNetworks", "SsrNetworks" })
        {
            error.Message.ShouldContain($"Missing setting 'TrustedPath:{name}'");
        }
    }

    [Theory]
    [InlineData(nameof(TrustedPathOptions.ApiEdgeKey))]
    [InlineData(nameof(TrustedPathOptions.ApiEdgeKeyPrevious))]
    [InlineData(nameof(TrustedPathOptions.SsrApiKey))]
    [InlineData(nameof(TrustedPathOptions.SsrApiKeyPrevious))]
    public void AWeakKeyStopsStartupWithoutPrintingIt(string name)
    {
        var error = StartupError(ApiFactory.UnreachableDatabase, new Dictionary<string, string?> { [ApiFactory.Setting(name)] = SecretValue });

        error.Message.ShouldContain($"'TrustedPath:{name}' must be at least 32 characters");
        error.Message.ShouldNotContain(SecretValue);
    }

    [Fact]
    public void TheSameValueForBothCredentialsStopsStartup()
    {
        var error = StartupError(ApiFactory.UnreachableDatabase, new Dictionary<string, string?>
        {
            [ApiFactory.Setting(nameof(TrustedPathOptions.SsrApiKey))] = ApiFactory.ApiEdgeKey,
        });

        error.Message.ShouldContain("must be different credentials");
        error.Message.ShouldNotContain(ApiFactory.ApiEdgeKey);
    }

    [Fact]
    public void AnInvalidNetworkStopsStartup()
    {
        var error = StartupError(ApiFactory.UnreachableDatabase, new Dictionary<string, string?>
        {
            [ApiFactory.Setting($"{nameof(TrustedPathOptions.SsrNetworks)}:0")] = "not-a-network",
        });

        error.Message.ShouldContain("'TrustedPath:SsrNetworks' must contain only CIDR ranges");
    }

    [Fact]
    public void AnSsrIngressKeySettingStopsStartup()
    {
        // Least privilege (ADR-04 §4b): the API never holds the SSR ingress key.
        var error = StartupError(ApiFactory.UnreachableDatabase, new Dictionary<string, string?>
        {
            [ApiFactory.Setting("SsrIngressKey")] = ApiFactory.SsrIngressKey,
        });

        error.Message.ShouldContain("'TrustedPath:SsrIngressKey' must not be provided to the API");
        error.Message.ShouldNotContain(ApiFactory.SsrIngressKey);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    [InlineData("Staging")]
    public void TheDiagnosticsFlagOutsideTestingStopsStartup(string environment)
    {
        // TH-9.
        var error = StartupError(
            ApiFactory.UnreachableDatabase,
            new Dictionary<string, string?> { [ApiFactory.Setting(nameof(TrustedPathOptions.EnableTestDiagnostics))] = "true" },
            environment);

        error.Message.ShouldContain("'TrustedPath:EnableTestDiagnostics' is allowed only in the 'Testing' environment");
    }

    private static OptionsValidationException StartupError(
        string? connectionString, IReadOnlyDictionary<string, string?>? settings = null, string environment = "Production")
    {
        using var factory = new ApiFactory(connectionString, settings, environment);

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
