using eHub.Application.Payments;

namespace eHub.Infrastructure.Payments;

/// <summary>
/// Maps provider-specific error codes to <see cref="PaymentFailureReason"/>.
/// Only Infrastructure references provider codes (ACL-7).
/// </summary>
internal static class PaymentFailureReasonMapper
{
    public static PaymentFailureReason MapStripeError(string? code)
        => code?.Trim().ToLowerInvariant() switch
        {
            "card_declined" => PaymentFailureReason.CardDeclined,
            "expired_card" => PaymentFailureReason.ExpiredCard,
            "insufficient_funds" => PaymentFailureReason.InsufficientFunds,
            "incorrect_cvc" or "invalid_cvc" => PaymentFailureReason.InvalidCard,
            "authentication_required" => PaymentFailureReason.AuthenticationRequired,
            "rate_limit" => PaymentFailureReason.RateLimited,
            _ => PaymentFailureReason.Unknown
        };

    public static PaymentFailureReason MapPayriffError(string? code)
        => code?.Trim().ToUpperInvariant() switch
        {
            "INVALID_CARD" or "CARD_DECLINED" => PaymentFailureReason.CardDeclined,
            "INSUFFICIENT_FUNDS" => PaymentFailureReason.InsufficientFunds,
            "EXPIRED_CARD" => PaymentFailureReason.ExpiredCard,
            _ => PaymentFailureReason.Unknown
        };

    public static PaymentFailureReason MapFakeWebhookFailure(string? code)
        => code?.Trim().ToLowerInvariant() switch
        {
            "declined" or "card_declined" or "failed" => PaymentFailureReason.CardDeclined,
            "insufficient_funds" => PaymentFailureReason.InsufficientFunds,
            "expired_card" => PaymentFailureReason.ExpiredCard,
            _ => PaymentFailureReason.Unknown
        };
}
