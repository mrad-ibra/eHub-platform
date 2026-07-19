using eHub.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eHub.Persistence.Configurations;

public sealed class BookingTimelineEntryConfiguration : IEntityTypeConfiguration<BookingTimelineEntry>
{
    public void Configure(EntityTypeBuilder<BookingTimelineEntry> builder)
    {
        builder.ToTable("booking_timeline_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ActorId);
        builder.Property(x => x.AtUtc).IsRequired();
        builder.Property<Guid>("BookingId").IsRequired();
        builder.HasIndex("BookingId");
    }
}

public sealed class BookingStatusHistoryEntryConfiguration : IEntityTypeConfiguration<BookingStatusHistoryEntry>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistoryEntry> builder)
    {
        builder.ToTable("booking_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasMaxLength(64);
        builder.Property(x => x.ToStatus).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorId);
        builder.Property(x => x.AtUtc).IsRequired();
        builder.Property<Guid>("BookingId").IsRequired();
        builder.HasIndex("BookingId");
    }
}
