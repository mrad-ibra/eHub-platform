using eHub.Domain.Assets;

namespace eHub.Domain.Bookings;

/// <summary>
/// Domain component: conflict detection for Soft/Hard holds and preparation buffers.
/// </summary>
public static class BookingAvailability
{
    public static bool ConflictsWithBlockingBooking(
        BookingPeriod requested,
        int requestBufferDays,
        BookingPeriod existingPeriod,
        int existingBufferDays)
    {
        // Request occupied range vs existing occupied range (symmetric).
        var requestOccupiedEnd = requested.OccupiedEnd(requestBufferDays);
        var existingOccupiedEnd = existingPeriod.OccupiedEnd(existingBufferDays);

        return requested.StartDate <= existingOccupiedEnd
               && requestOccupiedEnd >= existingPeriod.StartDate;
    }

    public static bool ConflictsWithAssetBlock(
        BookingPeriod requested,
        AssetAvailabilityBlock block)
        => requested.Overlaps(block.StartDate, block.EndDate);

    public static void EnsureRentalDaysAllowed(BookingPeriod period, BookingTerms terms)
    {
        if (terms.MinRentalDays is { } min && period.Days < min)
        {
            throw new Exceptions.ValidationFailedException(
                Localization.ErrorResources.Get(Localization.ErrorCodes.BookingRentalDaysInvalid));
        }

        if (terms.MaxRentalDays is { } max && period.Days > max)
        {
            throw new Exceptions.ValidationFailedException(
                Localization.ErrorResources.Get(Localization.ErrorCodes.BookingRentalDaysInvalid));
        }
    }
}
