using eHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eHub.Persistence.Configurations;

public sealed class BookingIdempotencyEntryConfiguration : IEntityTypeConfiguration<BookingIdempotencyEntry>
{
    public void Configure(EntityTypeBuilder<BookingIdempotencyEntry> builder)
    {
        builder.ToTable("booking_idempotency_entries");
        builder.HasKey(x => new { x.RenterId, x.IdempotencyKey });
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.BookingId);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
