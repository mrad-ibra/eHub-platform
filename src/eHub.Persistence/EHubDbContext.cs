using eHub.Domain.Bookings;
using eHub.Domain.Payments;
using eHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace eHub.Persistence;

public sealed class EHubDbContext(DbContextOptions<EHubDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingIdempotencyEntry> BookingIdempotencyEntries => Set<BookingIdempotencyEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EHubDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
