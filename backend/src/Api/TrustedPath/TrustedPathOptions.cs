using System.Net;
using Microsoft.Extensions.Options;

namespace Platform.Api.TrustedPath;

/// <summary>
/// The API's trusted-path settings (section <c>TrustedPath</c>; ADR-04 rules 9–12, §4d). The API holds only its own
/// two credentials, the API edge key and the SSR-to-API key, each with an optional previous value for rotation.
/// </summary>
public sealed class TrustedPathOptions
{
    public const string SectionName = "TrustedPath";

    public const int MinimumKeyLength = 32;

    public string? ApiEdgeKey { get; set; }

    public string? ApiEdgeKeyPrevious { get; set; }

    public string? SsrApiKey { get; set; }

    public string? SsrApiKeyPrevious { get; set; }

    /// <summary>CIDR ranges of the edge. The API edge key is accepted only from these direct connection addresses.</summary>
    public IList<string> EdgeNetworks { get; } = [];

    /// <summary>CIDR ranges of the SSR server. The SSR-to-API key is accepted only from these addresses.</summary>
    public IList<string> SsrNetworks { get; } = [];

    /// <summary>Test-only request-context diagnostics; allowed only in the <c>Testing</c> environment (TH-9).</summary>
    public bool EnableTestDiagnostics { get; set; }

    internal static string Setting(string name) => $"{SectionName}:{name}";

    internal string[] ApiEdgeKeys() => Keys(ApiEdgeKey, ApiEdgeKeyPrevious);

    internal string[] SsrApiKeys() => Keys(SsrApiKey, SsrApiKeyPrevious);

    internal static IPNetwork[] Parse(IEnumerable<string> networks) =>
        [.. networks.Select(network => IPNetwork.Parse(network.Trim()))];

    private static string[] Keys(string? current, string? previous) =>
        [.. new[] { current, previous }.Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key!.Trim())];
}

/// <summary>Fails startup on missing, weak, or conflicting settings. Messages name settings, never values (M0-9).</summary>
internal sealed class TrustedPathOptionsValidator(IHostEnvironment environment, IConfiguration configuration)
    : IValidateOptions<TrustedPathOptions>
{
    public const string TestingEnvironment = "Testing";

    public ValidateOptionsResult Validate(string? name, TrustedPathOptions options)
    {
        var errors = new List<string>();

        // Least privilege (ADR-04 §4b): the SSR ingress key belongs to the edge and the SSR server only.
        foreach (var setting in configuration.GetSection(TrustedPathOptions.SectionName).GetChildren())
        {
            if (setting.Key.Contains("Ingress", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Setting '{setting.Path}' must not be provided to the API "
                    + "(the SSR ingress key is held by the edge and the SSR server only).");
            }
        }

        RequiredKey(nameof(TrustedPathOptions.ApiEdgeKey), options.ApiEdgeKey, errors);
        OptionalKey(nameof(TrustedPathOptions.ApiEdgeKeyPrevious), options.ApiEdgeKeyPrevious, errors);
        RequiredKey(nameof(TrustedPathOptions.SsrApiKey), options.SsrApiKey, errors);
        OptionalKey(nameof(TrustedPathOptions.SsrApiKeyPrevious), options.SsrApiKeyPrevious, errors);
        if (options.ApiEdgeKeys().Intersect(options.SsrApiKeys(), StringComparer.Ordinal).Any())
        {
            errors.Add($"Settings '{TrustedPathOptions.Setting(nameof(TrustedPathOptions.ApiEdgeKey))}' and "
                + $"'{TrustedPathOptions.Setting(nameof(TrustedPathOptions.SsrApiKey))}' must be different credentials.");
        }

        Networks(nameof(TrustedPathOptions.EdgeNetworks), options.EdgeNetworks, errors);
        Networks(nameof(TrustedPathOptions.SsrNetworks), options.SsrNetworks, errors);

        if (options.EnableTestDiagnostics && !environment.IsEnvironment(TestingEnvironment))
        {
            errors.Add($"Setting '{TrustedPathOptions.Setting(nameof(TrustedPathOptions.EnableTestDiagnostics))}' "
                + $"is allowed only in the '{TestingEnvironment}' environment.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void RequiredKey(string name, string? value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Missing setting '{TrustedPathOptions.Setting(name)}'.");
            return;
        }

        OptionalKey(name, value, errors);
    }

    private static void OptionalKey(string name, string? value, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length < TrustedPathOptions.MinimumKeyLength)
        {
            errors.Add($"Setting '{TrustedPathOptions.Setting(name)}' must be at least {TrustedPathOptions.MinimumKeyLength} characters.");
        }
    }

    private static void Networks(string name, IList<string> networks, List<string> errors)
    {
        if (networks.Count == 0)
        {
            errors.Add($"Missing setting '{TrustedPathOptions.Setting(name)}' (at least one CIDR range).");
            return;
        }

        if (networks.Any(network => !IPNetwork.TryParse(network.Trim(), out _)))
        {
            errors.Add($"Setting '{TrustedPathOptions.Setting(name)}' must contain only CIDR ranges (for example 10.0.0.0/8).");
        }
    }
}
