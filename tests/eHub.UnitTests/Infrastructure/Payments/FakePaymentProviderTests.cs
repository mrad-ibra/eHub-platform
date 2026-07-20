using System.Text;
using System.Text.Json;
using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Infrastructure.Payments;

public sealed class FakePaymentProviderTests
{
    private const string Secret = "unit-test-secret";
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePaymentProvider _provider = new(Options.Create(new PaymentProviderOptions
    {
        Fake = new FakeProviderOptions { WebhookSecret = Secret, TimestampToleranceSeconds = 300 }
    }));

    [Fact]
    public void VerifyWebhook_ValidSignature_ReturnsTrue()
    {
        const string body = """{"eventId":"e1","outcome":"SUCCEEDED"}""";
        var unix = new DateTimeOffset(Now).ToUnixTimeSeconds();
        var headers = Headers(body, unix);

        _provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body), Now).Should().BeTrue();
    }

    [Fact]
    public void VerifyWebhook_TamperedBody_ReturnsFalse()
    {
        const string body = """{"eventId":"e1","outcome":"SUCCEEDED"}""";
        var unix = new DateTimeOffset(Now).ToUnixTimeSeconds();
        var headers = Headers(body, unix);

        _provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body + " "), Now).Should().BeFalse();
    }

    [Fact]
    public void ParseWebhook_UnknownOutcome_ReturnsUnknownOutcome()
    {
        const string body = """{"eventId":"e2","outcome":"MYSTERY"}""";
        var parsed = _provider.ParseWebhook(Encoding.UTF8.GetBytes(body));

        parsed.Should().NotBeNull();
        parsed!.Outcome.Should().Be(ProviderWebhookOutcome.Unknown);
    }

    [Fact]
    public async Task CreatePaymentAsync_ReturnsDeterministicProviderPaymentId()
    {
        var paymentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var result = await _provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(paymentId, Guid.NewGuid(), 50m, "AZN", "key-1"));

        result.IsSuccess.Should().BeTrue();
        result.ProviderPaymentId.Should().Be($"fake_{paymentId:N}");
        result.RedirectUrl.Should().Contain("fake/checkout");
    }

    private static Dictionary<string, string> Headers(string body, long unix)
    {
        var sig = FakePaymentProvider.Sign(Secret, unix, body);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [FakePaymentProvider.TimestampHeader] = unix.ToString(),
            [FakePaymentProvider.SignatureHeader] = sig
        };
    }
}
