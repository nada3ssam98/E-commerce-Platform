using System.Text.RegularExpressions;

namespace Platform.Api.TrustedPath;

/// <summary>
/// Host normalization (domain-model §0 Hostname; ADR-04 rule 13): lowercase, no port, no trailing dot, valid DNS
/// syntax. Same rules as the SSR server (<c>host.ts</c>); both run <c>contracts/test-vectors/host-normalization.json</c>.
/// </summary>
public static partial class PlatformHost
{
    /// <summary>Returns the normalized host, or <c>null</c> when the value is not a valid host name.</summary>
    public static string? Normalize(string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        var host = raw.Trim().ToLowerInvariant();
        var portSeparator = host.LastIndexOf(':');
        if (portSeparator != -1)
        {
            if (!PortPattern().IsMatch(host[(portSeparator + 1)..]))
            {
                return null;
            }

            host = host[..portSeparator];
        }

        if (host.EndsWith('.'))
        {
            host = host[..^1];
        }

        if (host.Length is 0 or > 253 || !host.Split('.').All(LabelPattern().IsMatch))
        {
            return null;
        }

        return host;
    }

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex LabelPattern();

    [GeneratedRegex("^[0-9]{1,5}$", RegexOptions.CultureInvariant)]
    private static partial Regex PortPattern();
}
