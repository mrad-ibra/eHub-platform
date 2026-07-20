namespace eHub.Infrastructure.Payments;

/// <summary>
/// Maps provider-specific error codes to <see cref="eHub.Application.Payments.PaymentFailureReason"/>.
/// Stripe and Payriff mappings land in Phase A/B — Application never sees raw provider codes (ACL-7).
/// </summary>
internal static class PaymentFailureReasonMapper
{
    // Phase A: MapStripeError(string code) → PaymentFailureReason
    // Phase B: MapPayriffError(string code) → PaymentFailureReason
}
