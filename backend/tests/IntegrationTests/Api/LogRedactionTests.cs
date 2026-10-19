using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Platform.SharedKernel.Compliance;
using Shouldly;

namespace Platform.IntegrationTests.Api;

/// <summary>M0-10: logs redact cookies, Authorization, the three trusted-path keys, and classified data.</summary>
public sealed partial class LogRedactionTests
{
    private const string TokenValue = "token-value-5d1c";
    private const string PasswordValue = "password-value-9e27";
    private const string PhoneValue = "+201001234567";
    private const string EmailValue = "customer@example.test";

    [Fact]
    public void ClassifiedValuesAreErasedFromLogs()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.EnableRedaction().AddFakeLogging());
        services.AddPlatformRedaction();
        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<LogRedactionTests>>();

        LogSignIn(logger, TokenValue, PasswordValue, PhoneValue, EmailValue, "visible-context");

        var record = provider.GetFakeLogCollector().LatestRecord;
        record.Message.ShouldContain("visible-context");
        AssertContainsNone(record, TokenValue, PasswordValue, PhoneValue, EmailValue);
    }

    [Fact]
    public async Task SensitiveRequestHeadersAreNotLogged()
    {
        string[] secrets = ["cookie-secret-31aa", "authorization-secret-77b0", "edge-key-secret-0c4e", "ingress-key-secret-a19f", "ssr-api-key-secret-62d3"];
        using var factory = new ApiFactory(ApiFactory.UnreachableDatabase);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/health", UriKind.Relative));
        request.Headers.Add("Cookie", $"__Host-sf_session={secrets[0]}");
        request.Headers.Add("Authorization", $"Bearer {secrets[1]}");
        request.Headers.Add("X-Platform-Api-Edge-Key", secrets[2]);
        request.Headers.Add("X-Platform-Ssr-Ingress-Key", secrets[3]);
        request.Headers.Add("X-Platform-Ssr-Api-Key", secrets[4]);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var httpLog = await WaitForHttpLogAsync(factory.Logs);

        httpLog.Message.ShouldContain("/api/health");
        foreach (var record in factory.Logs.GetSnapshot())
        {
            AssertContainsNone(record, secrets);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sign-in {Context}: {Token} {Password} {Phone} {Email}")]
    private static partial void LogSignIn(
        ILogger logger,
        [Token] string token,
        [Password] string password,
        [Phone] string phone,
        [Email] string email,
        string context);

    private static void AssertContainsNone(FakeLogRecord record, params string[] values)
    {
        var text = string.Join(" | ", [record.Message, .. (record.StructuredState ?? []).Select(pair => $"{pair.Key}={pair.Value}")]);
        foreach (var value in values)
        {
            text.ShouldNotContain(value);
        }
    }

    /// <summary>The HTTP log entry is written when the request completes, which can be just after the client sees the response.</summary>
    private static async Task<FakeLogRecord> WaitForHttpLogAsync(FakeLogCollector logs)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var record = logs.GetSnapshot().LastOrDefault(r => r.Category == "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware");
            if (record is not null)
            {
                return record;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        throw new ShouldAssertException("No HTTP log entry was written.");
    }
}
