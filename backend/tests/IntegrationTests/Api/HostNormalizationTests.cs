using System.Text.Json;
using Platform.Api.TrustedPath;
using Platform.TestSupport;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary>
/// <see cref="PlatformHost.Normalize"/> against the shared vectors, which the SSR server's <c>normalizeHost</c> also runs
/// (ADR-03 test vectors; TH-10).
/// </summary>
public sealed class HostNormalizationTests
{
    public static TheoryData<string, string?> Vectors()
    {
        using var file = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RepositoryPaths.Root, "contracts", "test-vectors", "host-normalization.json")));
        var data = new TheoryData<string, string?>();
        foreach (var vector in file.RootElement.GetProperty("vectors").EnumerateArray())
        {
            data.Add(vector.GetProperty("input").GetString()!, vector.GetProperty("expected").GetString());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void NormalizesLikeTheSsrServer(string input, string? expected) =>
        PlatformHost.Normalize(input).ShouldBe(expected);

    [Fact]
    public void AMissingHostIsRejected() => PlatformHost.Normalize(null).ShouldBeNull();
}
