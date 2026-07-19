using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Time;
using eHub.Domain.Bookings;
using eHub.Domain.Exceptions;
using eHub.Localization;
using Microsoft.EntityFrameworkCore;

namespace eHub.Persistence.Repositories;

public sealed class EfBookingRepository(EHubDbContext db, IClock clock) : IBookingRepository
{
    public Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
        => AddAsync(booking, clock.UtcNow, cancellationToken);

    public async Task AddAsync(Booking booking, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        // Early UX rejection only. Correctness under concurrency is enforced by
        // PostgreSQL EXCLUDE USING gist (bookings_no_overlap) + unique indexes.
        var conflicts = await ListBlockingByAssetAsync(booking.AssetId, nowUtc, cancellationToken);
        foreach (var existing in conflicts)
        {
            if (BookingAvailability.ConflictsWithBlockingBooking(
                    booking.Period,
                    booking.BufferDays,
                    existing.Period,
                    existing.BufferDays))
            {
                throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingConflict));
            }
        }

        await db.Bookings.AddAsync(booking, cancellationToken);
    }

    public Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        => db.Bookings
            .Include(b => b.Timeline)
            .Include(b => b.StatusHistory)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

    public Task<Booking?> GetByNumberAsync(string bookingNumber, CancellationToken cancellationToken = default)
        => db.Bookings
            .Include(b => b.Timeline)
            .Include(b => b.StatusHistory)
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber, cancellationToken);

    public Task<IReadOnlyList<Booking>> ListBlockingByAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
        => ListBlockingByAssetAsync(assetId, clock.UtcNow, cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListBlockingByAssetAsync(
        Guid assetId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var candidates = await db.Bookings
            .AsNoTracking()
            .Where(b => b.AssetId == assetId)
            .Where(b =>
                b.Status == BookingStatusCode.PendingOwnerApproval
                || b.Status == BookingStatusCode.PendingPayment
                || b.Status == BookingStatusCode.Confirmed
                || b.Status == BookingStatusCode.InProgress)
            .ToListAsync(cancellationToken);

        return candidates.Where(b => b.BlocksCalendar(nowUtc)).OrderBy(b => b.Period.StartDate).ToArray();
    }

    public async Task<IReadOnlyList<Booking>> ListExpiredHoldsAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return [];
        }

        return await db.Bookings
            .Include(b => b.Timeline)
            .Include(b => b.StatusHistory)
            .Where(b =>
                (b.Status == BookingStatusCode.PendingOwnerApproval
                 || b.Status == BookingStatusCode.PendingPayment)
                && b.ExpiresAtUtc != null
                && b.ExpiresAtUtc <= nowUtc)
            .OrderBy(b => b.ExpiresAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
