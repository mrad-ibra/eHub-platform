using System.Text;
using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Infrastructure.Payments.Contracts;

/// <summary>
/// Shared certification suite — every <see cref="IPaymentProvider"/> adapter must pass (Phase 0/A).
/// Webhook signing is provider-specific; concrete tests supply headers via hooks.
/// </summary>
public abstract class PaymentProviderContractTests
{
    protected abstract IPaymentProvider CreateProvider();

    protected virtual DateTime UtcNow { get; } = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    protected abstract IReadOnlyDictionary<string, string> CreateValidWebhookHeaders(
        string body,
        DateTime nowUtc);

    protected abstract IReadOnlyDictionary<string, string> CreateInvalidWebhookHeaders(
        string body,
        DateTime nowUtc);

    protected virtual string CreateSucceededWebhookBody(string eventId, string providerPaymentId)
        => $$"""
           {"eventId":"{{eventId}}","outcome":"SUCCEEDED","providerPaymentId":"{{providerPaymentId}}"}
           """;

    [Fact]
    public async Task CreatePayment_WithValidRequest_ReturnsProviderReference()
    {
        var provider = CreateProvider();
        var paymentId = Guid.NewGuid();

        var result = await provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(
                paymentId,
                Guid.NewGuid(),
                10m,
                "AZN",
                $"create-{Guid.NewGuid():N}"));

        result.IsSuccess.Should().BeTrue();
        result.ProviderPaymentId.Should().NotBeNullOrWhiteSpace();
        result.Failure.Should().BeNull();
    }

    [Fact]
    public async Task DuplicateCreate_WithSameIdempotencyKey_IsSafe()
    {
        var provider = CreateProvider();
        var paymentId = Guid.NewGuid();
        const string idempotencyKey = "contract-idem-create-1";
        var request = new ProviderCreatePaymentRequest(
            paymentId,
            Guid.NewGuid(),
            10m,
            "AZN",
            idempotencyKey);

        var first = await provider.CreatePaymentAsync(request);
        var second = await provider.CreatePaymentAsync(request);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.ProviderPaymentId.Should().Be(first.ProviderPaymentId);
    }

    [Fact]
    public async Task DuplicateCreate_WithSameKeyDifferentPayload_ReturnsFailure()
    {
        var provider = CreateProvider();
        const string idempotencyKey = "contract-idem-create-mismatch";
        var first = await provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                10m,
                "AZN",
                idempotencyKey));
        var second = await provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                99m,
                "AZN",
                idempotencyKey));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeFalse();
        second.Failure!.Reason.Should().Be(PaymentFailureReason.IdempotencyPayloadMismatch);
    }

    [Fact]
    public async Task Refund_WithValidAmount_ReturnsSuccess()
    {
        var provider = CreateProvider();
        var create = await provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                25m,
                "AZN",
                $"refund-setup-{Guid.NewGuid():N}"));

        create.IsSuccess.Should().BeTrue();

        var refund = await provider.RefundAsync(
            new ProviderRefundRequest(
                create.ProviderPaymentId!,
                25m,
                "AZN",
                $"refund-{Guid.NewGuid():N}",
                "contract_test"));

        refund.IsSuccess.Should().BeTrue();
        refund.ProviderRefundId.Should().NotBeNullOrWhiteSpace();
        refund.Failure.Should().BeNull();
    }

    [Fact]
    public async Task DuplicateRefund_WithSameKeyDifferentPayload_ReturnsFailure()
    {
        var provider = CreateProvider();
        var create = await provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                50m,
                "AZN",
                $"refund-mismatch-setup-{Guid.NewGuid():N}"));
        create.IsSuccess.Should().BeTrue();

        const string key = "contract-idem-refund-mismatch";
        var first = await provider.RefundAsync(
            new ProviderRefundRequest(create.ProviderPaymentId!, 10m, "AZN", key, "a"));
        var second = await provider.RefundAsync(
            new ProviderRefundRequest(create.ProviderPaymentId!, 20m, "AZN", key, "b"));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeFalse();
        second.Failure!.Reason.Should().Be(PaymentFailureReason.IdempotencyPayloadMismatch);
    }

    [Fact]
    public async Task Cancel_WithValidPayment_ReturnsSuccess()
    {
        var provider = CreateProvider();
        var create = await provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                15m,
                "AZN",
                $"cancel-setup-{Guid.NewGuid():N}"));

        create.IsSuccess.Should().BeTrue();

        var cancel = await provider.CancelPaymentAsync(create.ProviderPaymentId!);

        cancel.IsSuccess.Should().BeTrue();
        cancel.Failure.Should().BeNull();
    }

    [Fact]
    public void VerifyWebhook_WithInvalidSignature_ReturnsFalse()
    {
        var provider = CreateProvider();
        const string body = """{"eventId":"contract-e1","outcome":"SUCCEEDED"}""";
        var headers = CreateInvalidWebhookHeaders(body, UtcNow);

        provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body), UtcNow).Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhook_WithValidSignature_ReturnsTrue()
    {
        var provider = CreateProvider();
        const string body = """{"eventId":"contract-e2","outcome":"SUCCEEDED"}""";
        var headers = CreateValidWebhookHeaders(body, UtcNow);

        provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body), UtcNow).Should().BeTrue();
    }

    [Fact]
    public void VerifyWebhook_OutsideTimestampWindow_ReturnsFalse()
    {
        var provider = CreateProvider();
        const string body = """{"eventId":"contract-e3","outcome":"SUCCEEDED"}""";
        var stale = UtcNow.AddHours(-2);
        var headers = CreateValidWebhookHeaders(body, stale);

        provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body), UtcNow).Should().BeFalse();
    }
}

public sealed class FakePaymentProviderContractTests : PaymentProviderContractTests
{
    private static readonly PaymentProviderOptions Options = new()
    {
        Fake = new FakeProviderOptions
        {
            Enabled = true,
            WebhookSecret = "contract-test-secret",
            TimestampToleranceSeconds = 300
        }
    };

    protected override IPaymentProvider CreateProvider()
        => new FakePaymentProvider(Microsoft.Extensions.Options.Options.Create(Options));

    protected override IReadOnlyDictionary<string, string> CreateValidWebhookHeaders(
        string body,
        DateTime nowUtc)
    {
        var unix = new DateTimeOffset(nowUtc).ToUnixTimeSeconds();
        var sig = FakePaymentProvider.Sign(Options.Fake.WebhookSecret, unix, body);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [FakePaymentProvider.TimestampHeader] = unix.ToString(),
            [FakePaymentProvider.SignatureHeader] = sig
        };
    }

    protected override IReadOnlyDictionary<string, string> CreateInvalidWebhookHeaders(
        string body,
        DateTime nowUtc)
    {
        var unix = new DateTimeOffset(nowUtc).ToUnixTimeSeconds();
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [FakePaymentProvider.TimestampHeader] = unix.ToString(),
            [FakePaymentProvider.SignatureHeader] = "deadbeef"
        };
    }
}
