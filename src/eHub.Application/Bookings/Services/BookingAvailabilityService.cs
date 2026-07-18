using eHub.Domain.Assets;
using eHub.Domain.Bookings;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Application.Bookings.Services;

/// <summary>
/// Orchestrates asset + booking reads; conflict predicates live in <see cref="BookingAvailability"/>.
/// </summary>
public sealed class BookingAvailabilityService
{
    public void EnsureCanBook(
        Asset asset,
        BookingPeriod requested,
        int bufferDays,
        IReadOnlyList<Booking> blockingBookings,
        DateTime nowUtc)
    {
        if (asset.Status != AssetStatusCode.Published || asset.IsDeleted)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingAssetNotBookable));
        }

        BookingAvailability.EnsureStartNotInPast(requested, nowUtc);

        foreach (var block in asset.AvailabilityBlocks)
        {
            if (BookingAvailability.ConflictsWithAssetBlock(requested, bufferDays, block))
            {
                throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingConflict));
            }
        }

        foreach (var existing in blockingBookings)
        {
            if (!existing.BlocksCalendar(nowUtc))
            {
                continue;
            }

            if (BookingAvailability.ConflictsWithBlockingBooking(
                    requested,
                    bufferDays,
                    existing.Period,
                    existing.BufferDays))
            {
                throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingConflict));
            }
        }
    }
}
