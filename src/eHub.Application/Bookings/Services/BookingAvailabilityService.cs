using eHub.Domain.Assets;
using eHub.Domain.Bookings;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Application.Bookings.Services;

/// <summary>
/// Application-facing availability orchestration; conflict math lives in <see cref="BookingAvailability"/>.
/// </summary>
public sealed class BookingAvailabilityService
{
    public void EnsureCanBook(
        Asset asset,
        BookingPeriod requested,
        int bufferDays,
        IReadOnlyList<Booking> blockingBookings)
    {
        if (asset.Status != AssetStatusCode.Published || asset.IsDeleted)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingAssetNotBookable));
        }

        foreach (var block in asset.AvailabilityBlocks)
        {
            if (BookingAvailability.ConflictsWithAssetBlock(requested, block))
            {
                throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingConflict));
            }
        }

        foreach (var existing in blockingBookings)
        {
            if (!existing.Status.IsBlocking)
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
