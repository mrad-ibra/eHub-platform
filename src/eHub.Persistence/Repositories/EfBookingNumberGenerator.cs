using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace eHub.Persistence.Repositories;

public sealed class EfBookingNumberGenerator(EHubDbContext db, IClock clock) : IBookingNumberGenerator
{
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        // Requires sequence created in migration: booking_number_seq
        var next = await db.Database
            .SqlQueryRaw<long>("SELECT nextval('booking_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);

        var year = clock.UtcNow.Year;
        return $"BK-{year}-{next:D9}";
    }
}
