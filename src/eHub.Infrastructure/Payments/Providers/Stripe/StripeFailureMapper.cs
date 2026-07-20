using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using Stripe;

namespace eHub.Infrastructure.Payments.Providers.Stripe;

internal static class StripeFailureMapper
{
    public static ProviderFailure FromException(StripeException ex)
    {
        var code = ex.StripeError?.Code ?? ex.StripeError?.Type ?? ex.Message;
        var reason = MapCode(code);
        if (IsIdempotencyMismatch(ex))
        {
            reason = PaymentFailureReason.IdempotencyPayloadMismatch;
        }

        var retryable = reason is PaymentFailureReason.ProviderUnavailable
            or PaymentFailureReason.Timeout
            or PaymentFailureReason.RateLimited;

        return new ProviderFailure(
            reason,
            ProviderCode: code,
            SafeMessage: null,
            IsRetryable: retryable);
    }

    public static PaymentFailureReason MapCode(string? code)
        => PaymentFailureReasonMapper.MapStripeError(code);

    private static bool IsIdempotencyMismatch(StripeException ex)
    {
        var message = ex.Message ?? string.Empty;
        var code = ex.StripeError?.Code ?? string.Empty;
        return code.Contains("idempotency", StringComparison.OrdinalIgnoreCase)
               || message.Contains("idempotent", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Keys for idempotent requests", StringComparison.OrdinalIgnoreCase);
    }
}
