using eHub.Domain.Bookings;

namespace eHub.Domain.Payments;

/// <summary>Aligned with Booking payment hold window (Sprint 6.0 / BookingDefaults.PaymentTtl).</summary>
public static class PaymentDefaults
{
    public static readonly TimeSpan PaymentWindow = BookingDefaults.PaymentTtl;

    public const int MaxFailureReasonLength = 512;
    public const int MaxRefundReasonLength = 512;
    public const int MaxIdempotencyKeyLength = 128;
    public const int MaxProviderPaymentIdLength = 256;
}
