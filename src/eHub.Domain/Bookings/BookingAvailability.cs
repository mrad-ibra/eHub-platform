using eHub.Domain.Assets;
using eHub.Domain.Exceptions;
using eHub.Localization;

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
        var requestOccupiedEnd = requested.OccupiedEnd(requestBufferDays);
        var existingOccupiedEnd = existingPeriod.OccupiedEnd(existingBufferDays);

        return requested.StartDate <= existingOccupiedEnd
               && requestOccupiedEnd >= existingPeriod.StartDate;
    }

    /// <summary>
    /// Asset unavailable/maintenance blocks conflict with the occupied range (rental + preparation buffer).
    /// </summary>
    public static bool ConflictsWithAssetBlock(
        BookingPeriod requested,
        int requestBufferDays,
        AssetAvailabilityBlock block)
    {
        var occupiedEnd = requested.OccupiedEnd(requestBufferDays);
        return requested.StartDate <= block.EndDate && occupiedEnd >= block.StartDate;
    }

    public static void EnsureRentalDaysAllowed(BookingPeriod period, BookingTerms terms)
    {
        if (terms.MinRentalDays is { } min && period.Days < min)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingRentalDaysInvalid));
        }

        if (terms.MaxRentalDays is { } max && period.Days > max)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingRentalDaysInvalid));
        }
    }

    /// <summary>
    /// v1 business date = UTC calendar date from <paramref name="nowUtc"/>.
    /// Location timezone rules are a follow-up decision.
    /// </summary>
    public static void EnsureStartNotInPast(BookingPeriod period, DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc);
        if (period.StartDate < today)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingStartDateInPast));
        }
    }
}
