namespace Platform.Api.TrustedPath;

/// <summary>Trusted-path header names, fixed in M0 (ADR-04 §4b). Values never appear in logs (M0-10).</summary>
public static class TrustedPathHeaders
{
    /// <summary>Set by the edge from the host it received; the only source of the store host (rule 12).</summary>
    public const string PlatformHost = "X-Platform-Host";

    /// <summary>Edge → API credential (rule 10).</summary>
    public const string ApiEdgeKey = "X-Platform-Api-Edge-Key";

    /// <summary>Edge → SSR credential. The API never accepts it (TH-11).</summary>
    public const string SsrIngressKey = "X-Platform-Ssr-Ingress-Key";

    /// <summary>SSR → API credential, limited to allow-listed public GETs (rule 11).</summary>
    public const string SsrApiKey = "X-Platform-Ssr-Api-Key";
}
