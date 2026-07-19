namespace eHub.Domain.Bookings;

/// <summary>Platform defaults locked in Sprint 5.0 architecture pack.</summary>
public static class BookingDefaults
{
    public static readonly TimeSpan OwnerApprovalTtl = TimeSpan.FromHours(12);
    public static readonly TimeSpan PaymentTtl = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Lease for an in-flight idempotent create (Begin → Complete/Abandon).
    /// Separate from Soft Hold / payment TTLs.
    /// </summary>
    public static readonly TimeSpan IdempotencyProcessingTtl = TimeSpan.FromMinutes(5);

    public const int DefaultPreparationBufferDays = 1;

    /// <summary>Upper bound for asset-specific preparation buffer days.</summary>
    public const int MaxPreparationBufferDays = 14;
}
