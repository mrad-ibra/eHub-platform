namespace eHub.Domain.Bookings;

/// <summary>Platform defaults locked in Sprint 5.0 architecture pack.</summary>
public static class BookingDefaults
{
    public static readonly TimeSpan OwnerApprovalTtl = TimeSpan.FromHours(12);
    public static readonly TimeSpan PaymentTtl = TimeSpan.FromMinutes(15);
    public const int DefaultPreparationBufferDays = 1;
}
