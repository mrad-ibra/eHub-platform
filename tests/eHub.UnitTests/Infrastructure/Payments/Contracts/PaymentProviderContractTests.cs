using System.Text;
using eHub.Application.Configuration;
using eHub.Application.Payments.Abstractions;
using eHub.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Infrastructure.Payments.Contracts;

/// <summary>
/// Shared certification suite — every <see cref="IPaymentProvider"/> adapter must pass (Phase 0).
/// </summary>
public abstract class PaymentProviderContractTests
{
    protected abstract IPaymentProvider CreateProvider();

    protected virtual DateTime UtcNow { get; } = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    protected virtual string WebhookSecret { get; } = "contract-test-secret";

    protected virtual PaymentProviderOptions ProviderOptions { get; } = new()
    {
        Fake = new FakeProviderOptions
        {
            WebhookSecret = "contract-test-secret",
            TimestampToleranceSeconds = 300
        }
    };

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
                Guid.NewGuid(),
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
            Guid.NewGuid(),
            idempotencyKey);

        var first = await provider.CreatePaymentAsync(request);
        var second = await provider.CreatePaymentAsync(request);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.ProviderPaymentId.Should().Be(first.ProviderPaymentId);
    }

    [Fact]
    public async Task Refund_WithValidAmount_ReturnsSuccess()
    {
        var provider = CreateProvider();
        var paymentId = Guid.NewGuid();
        var create = await provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(
                paymentId,
                Guid.NewGuid(),
                25m,
                Guid.NewGuid(),
                $"refund-setup-{Guid.NewGuid():N}"));

        create.IsSuccess.Should().BeTrue();

        var refund = await provider.RefundAsync(
            new ProviderRefundRequest(
                create.ProviderPaymentId!,
                25m,
                Guid.NewGuid(),
                $"refund-{Guid.NewGuid():N}",
                "contract_test"));

        refund.IsSuccess.Should().BeTrue();
        refund.ProviderRefundId.Should().NotBeNullOrWhiteSpace();
        refund.Failure.Should().BeNull();
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
                Guid.NewGuid(),
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
        var unix = new DateTimeOffset(UtcNow).ToUnixTimeSeconds();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [FakePaymentProvider.TimestampHeader] = unix.ToString(),
            [FakePaymentProvider.SignatureHeader] = "deadbeef"
        };

        provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body), UtcNow).Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhook_WithValidSignature_ReturnsTrue()
    {
        var provider = CreateProvider();
        const string body = """{"eventId":"contract-e2","outcome":"SUCCEEDED"}""";
        var unix = new DateTimeOffset(UtcNow).ToUnixTimeSeconds();
        var sig = FakePaymentProvider.Sign(WebhookSecret, unix, body);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [FakePaymentProvider.TimestampHeader] = unix.ToString(),
            [FakePaymentProvider.SignatureHeader] = sig
        };

        provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body), UtcNow).Should().BeTrue();
    }
}

public sealed class FakePaymentProviderContractTests : PaymentProviderContractTests
{
    protected override IPaymentProvider CreateProvider()
        => new FakePaymentProvider(Options.Create(ProviderOptions));
}
