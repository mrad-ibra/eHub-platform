using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;

namespace eHub.Infrastructure.Payments.Providers.Stripe;

/// <summary>Stripe SDK surface used by the ACL adapter — mockable for behavior tests.</summary>
public interface IStripeGateway
{
    Task<StripeCreateSessionResult> CreateCheckoutSessionAsync(
        StripeCreateSessionRequest request,
        CancellationToken cancellationToken = default);

    Task ExpireCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<string?> GetPaymentIntentIdForSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<StripeRefundGatewayResult> CreateRefundAsync(
        StripeRefundGatewayRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Verify signature and return raw JSON event payload, or null if invalid.</summary>
    string? ConstructEventJson(
        string payload,
        string stripeSignatureHeader,
        string webhookSecret,
        long toleranceSeconds);
}

public sealed record StripeCreateSessionRequest(
    Guid PaymentId,
    Guid BookingId,
    long AmountMinor,
    string CurrencyCode,
    string IdempotencyKey,
    string SuccessUrl,
    string CancelUrl);

public sealed record StripeCreateSessionResult(
    bool IsSuccess,
    string? SessionId,
    string? RedirectUrl,
    ProviderFailure? Failure);

public sealed record StripeRefundGatewayRequest(
    string PaymentIntentId,
    long AmountMinor,
    string IdempotencyKey,
    string Reason);

public sealed record StripeRefundGatewayResult(
    bool IsSuccess,
    string? RefundId,
    ProviderFailure? Failure);
