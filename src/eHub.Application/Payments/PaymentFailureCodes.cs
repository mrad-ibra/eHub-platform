namespace eHub.Application.Payments;

/// <summary>
/// Stable persistent failure codes — independent of C# enum member names.
/// </summary>
public static class PaymentFailureCodes
{
    public static string ToStableCode(PaymentFailureReason reason)
        => reason switch
        {
            PaymentFailureReason.CardDeclined => "payment.card_declined",
            PaymentFailureReason.InsufficientFunds => "payment.insufficient_funds",
            PaymentFailureReason.ExpiredCard => "payment.expired_card",
            PaymentFailureReason.InvalidCard => "payment.invalid_card",
            PaymentFailureReason.AuthenticationRequired => "payment.authentication_required",
            PaymentFailureReason.ProviderUnavailable => "payment.provider_unavailable",
            PaymentFailureReason.Timeout => "payment.timeout",
            PaymentFailureReason.RateLimited => "payment.rate_limited",
            PaymentFailureReason.InvalidRequest => "payment.invalid_request",
            PaymentFailureReason.IdempotencyPayloadMismatch => "payment.idempotency_payload_mismatch",
            _ => "payment.unknown"
        };
}
