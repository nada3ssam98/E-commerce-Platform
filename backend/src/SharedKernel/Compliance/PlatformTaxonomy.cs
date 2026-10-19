using Microsoft.Extensions.Compliance.Classification;

namespace Platform.SharedKernel.Compliance;

/// <summary>
/// Data classifications for log redaction (cross-cutting conventions: redacted logging; M0-10).
/// Every classified value is erased from logs.
/// </summary>
public static class PlatformTaxonomy
{
    public const string Name = "Platform";

    /// <summary>Credentials and secrets: session and reset tokens, cookies, authorization headers, trusted-path keys.</summary>
    public static DataClassification Token { get; } = new(Name, nameof(Token));

    public static DataClassification Password { get; } = new(Name, nameof(Password));

    public static DataClassification Phone { get; } = new(Name, nameof(Phone));

    public static DataClassification Email { get; } = new(Name, nameof(Email));

    public static IReadOnlyList<DataClassification> All { get; } = [Token, Password, Phone, Email];
}

/// <summary>Marks a logged value as a token or other secret.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class TokenAttribute() : DataClassificationAttribute(PlatformTaxonomy.Token);

/// <summary>Marks a logged value as a password.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PasswordAttribute() : DataClassificationAttribute(PlatformTaxonomy.Password);

/// <summary>Marks a logged value as a phone number.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PhoneAttribute() : DataClassificationAttribute(PlatformTaxonomy.Phone);

/// <summary>Marks a logged value as an email address.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class EmailAttribute() : DataClassificationAttribute(PlatformTaxonomy.Email);
