using System.Text;
using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Infrastructure.Payments.Providers.Stripe;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace eHub.UnitTests.Infrastructure.Payments.Stripe;

/// <summary>Behavior tests against a mocked <see cref="IStripeGateway"/> (no network).</summary>
public sealed class StripePaymentProviderBehaviorTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly IStripeGateway _gateway = Substitute.For<IStripeGateway>();
    private readonly IOptions<PaymentProviderOptions> _options;
    private readonly StripePaymentProvider _provider;

    public StripePaymentProviderBehaviorTests()
    {
        _options = Options.Create(new PaymentProviderOptions
        {
            Stripe = new StripeProviderOptions
            {
                Enabled = true,
                ApiKey = "sk_test_x",
                WebhookSecret = "whsec_test",
                SuccessUrl = "https://app.ehub.local/ok?paymentId={PAYMENT_ID}",
                CancelUrl = "https://app.ehub.local/cancel?paymentId={PAYMENT_ID}",
                WebhookToleranceSeconds = 300
            }
        });

        var minor = new MinorUnitConverter();
        _provider = new StripePaymentProvider(
            _gateway,
            new StripeWebhookVerifier(_gateway, _options),
            new StripeWebhookParser(minor),
            minor,
            _options);
    }

    [Fact]
    public async Task CreatePayment_PassesIdempotencyKey_AndReturnsSession()
    {
        _gateway.CreateCheckoutSessionAsync(Arg.Any<StripeCreateSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StripeCreateSessionResult(true, "cs_test_1", "https://checkout.stripe.com/c/pay/cs_test_1", null));

        var result = await _provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(Guid.NewGuid(), Guid.NewGuid(), 10.50m, "AZN", "idem-1"));

        result.IsSuccess.Should().BeTrue();
        result.ProviderPaymentId.Should().Be("cs_test_1");
        result.RedirectUrl.Should().Contain("checkout.stripe.com");
        await _gateway.Received(1).CreateCheckoutSessionAsync(
            Arg.Is<StripeCreateSessionRequest>(r =>
                r.IdempotencyKey == "idem-1"
                && r.AmountMinor == 1050
                && r.CurrencyCode == "AZN"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePayment_MapsStripeFailure()
    {
        _gateway.CreateCheckoutSessionAsync(Arg.Any<StripeCreateSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StripeCreateSessionResult(
                false,
                null,
                null,
                new ProviderFailure(PaymentFailureReason.CardDeclined, "card_declined", null, false)));

        var result = await _provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(Guid.NewGuid(), Guid.NewGuid(), 10m, "AZN", "idem-fail"));

        result.IsSuccess.Should().BeFalse();
        result.Failure!.Reason.Should().Be(PaymentFailureReason.CardDeclined);
    }

    [Fact]
    public async Task Refund_UsesPaymentIntent_AndIdempotencyKey()
    {
        _gateway.GetPaymentIntentIdForSessionAsync("cs_1", Arg.Any<CancellationToken>())
            .Returns("pi_1");
        _gateway.CreateRefundAsync(Arg.Any<StripeRefundGatewayRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StripeRefundGatewayResult(true, "re_1", null));

        var result = await _provider.RefundAsync(
            new ProviderRefundRequest("cs_1", 5m, "AZN", "ref-idem", "customer_request"));

        result.IsSuccess.Should().BeTrue();
        result.ProviderRefundId.Should().Be("re_1");
        await _gateway.Received(1).CreateRefundAsync(
            Arg.Is<StripeRefundGatewayRequest>(r =>
                r.PaymentIntentId == "pi_1"
                && r.AmountMinor == 500
                && r.IdempotencyKey == "ref-idem"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_ExpiresSession()
    {
        var result = await _provider.CancelPaymentAsync("cs_cancel");

        result.IsSuccess.Should().BeTrue();
        await _gateway.Received(1).ExpireCheckoutSessionAsync("cs_cancel", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void VerifyWebhook_ValidConstruct_ReturnsTrue()
    {
        const string body = """{"id":"evt_1","type":"checkout.session.completed"}""";
        _gateway.ConstructEventJson(body, "sig", "whsec_test", 300).Returns(body);

        var headers = new Dictionary<string, string> { ["Stripe-Signature"] = "sig" };
        _provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body), Now).Should().BeTrue();
    }

    [Fact]
    public void VerifyWebhook_InvalidSignature_ReturnsFalse()
    {
        const string body = """{"id":"evt_1","type":"checkout.session.completed"}""";
        _gateway.ConstructEventJson(body, "bad", "whsec_test", 300).Returns((string?)null);

        var headers = new Dictionary<string, string> { ["Stripe-Signature"] = "bad" };
        _provider.VerifyWebhook(headers, Encoding.UTF8.GetBytes(body), Now).Should().BeFalse();
    }

    [Fact]
    public void ParseWebhook_CheckoutCompleted_MapsSucceeded()
    {
        var paymentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var body = $$"""
            {
              "id": "evt_ok",
              "type": "checkout.session.completed",
              "created": 1721476800,
              "data": {
                "object": {
                  "id": "cs_test_ok",
                  "currency": "azn",
                  "amount_total": 1050,
                  "metadata": { "payment_id": "{{paymentId}}" }
                }
              }
            }
            """;

        var parsed = _provider.ParseWebhook(Encoding.UTF8.GetBytes(body));

        parsed.Should().NotBeNull();
        parsed!.Outcome.Should().Be(ProviderWebhookOutcome.Succeeded);
        parsed.ProviderPaymentId.Should().Be("cs_test_ok");
        parsed.PaymentId.Should().Be(paymentId);
        parsed.Amount.Should().Be(10.50m);
    }

    [Fact]
    public void ParseWebhook_PaymentFailed_MapsNormalizedFailure()
    {
        const string body = """
            {
              "id": "evt_fail",
              "type": "payment_intent.payment_failed",
              "created": 1721476800,
              "data": {
                "object": {
                  "id": "pi_fail",
                  "currency": "usd",
                  "amount": 1000,
                  "last_payment_error": { "code": "card_declined" }
                }
              }
            }
            """;

        var parsed = _provider.ParseWebhook(Encoding.UTF8.GetBytes(body));

        parsed.Should().NotBeNull();
        parsed!.Outcome.Should().Be(ProviderWebhookOutcome.Failed);
        parsed.FailureReason.Should().Be(PaymentFailureReason.CardDeclined);
    }
}
