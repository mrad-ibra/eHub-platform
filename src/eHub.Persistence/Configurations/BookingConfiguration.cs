using System.Text.Json;
using eHub.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eHub.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BookingNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.BookingNumber).IsUnique();

        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.RenterId).IsRequired();
        builder.Property(x => x.HostId).IsRequired();
        builder.Property(x => x.BufferDays).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion(
                status => status.Value,
                value => BookingStatusCode.Parse(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.PaymentId);

        builder.Property(x => x.ExpiresAtUtc);
        builder.Property(x => x.ConfirmedAtUtc);
        builder.Property(x => x.StartedAtUtc);
        builder.Property(x => x.CompletedAtUtc);

        builder.Property(x => x.AggregateVersion).IsConcurrencyToken();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedBy);

        builder.OwnsOne(x => x.Period, period =>
        {
            period.Property(p => p.StartDate).HasColumnName("start_date").IsRequired();
            period.Property(p => p.EndDate).HasColumnName("end_date").IsRequired();
        });

        builder.OwnsOne(x => x.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("unit_amount").HasPrecision(18, 4);
            money.Property(m => m.CurrencyId).HasColumnName("unit_currency_id");
        });

        builder.OwnsOne(x => x.TotalPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_amount").HasPrecision(18, 4);
            money.Property(m => m.CurrencyId).HasColumnName("total_currency_id");
        });

        builder.OwnsOne(x => x.AssetSnapshot, snapshot =>
        {
            snapshot.Property(s => s.Name).HasColumnName("snapshot_name").HasMaxLength(200);
            snapshot.Property(s => s.Brand).HasColumnName("snapshot_brand").HasMaxLength(100);
            snapshot.Property(s => s.Model).HasColumnName("snapshot_model").HasMaxLength(100);
            snapshot.Property(s => s.HostId).HasColumnName("snapshot_host_id");
            snapshot.Property(s => s.HostDisplayName).HasColumnName("snapshot_host_display_name").HasMaxLength(200);
            snapshot.Property(s => s.CapturedAtUtc).HasColumnName("snapshot_captured_at_utc");
            snapshot.Property(s => s.PrimaryImageUrls)
                .HasColumnName("snapshot_primary_image_urls")
                .HasColumnType("jsonb")
                .HasConversion(
                    urls => JsonSerializer.Serialize(urls, JsonOptions),
                    json => JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? Array.Empty<string>());
        });

        builder.OwnsOne(x => x.Terms, terms =>
        {
            terms.Property(t => t.MinRentalDays).HasColumnName("terms_min_rental_days");
            terms.Property(t => t.MaxRentalDays).HasColumnName("terms_max_rental_days");
            terms.Property(t => t.MinDriverAge).HasColumnName("terms_min_driver_age");
            terms.Property(t => t.RequiresLicense).HasColumnName("terms_requires_license");
            terms.Property(t => t.Notes).HasColumnName("terms_notes").HasMaxLength(2000);
            terms.Property(t => t.BufferDays).HasColumnName("terms_buffer_days");
        });

        builder.OwnsOne(x => x.Pickup, pickup =>
        {
            pickup.Property(p => p.UseAssetLocation).HasColumnName("pickup_use_asset_location");
            pickup.Property(p => p.AddressLine).HasColumnName("pickup_address_line").HasMaxLength(500);
            pickup.Property(p => p.Notes).HasColumnName("pickup_notes").HasMaxLength(500);
        });

        builder.OwnsOne(x => x.Dropoff, dropoff =>
        {
            dropoff.Property(d => d.UseAssetLocation).HasColumnName("dropoff_use_asset_location");
            dropoff.Property(d => d.AddressLine).HasColumnName("dropoff_address_line").HasMaxLength(500);
            dropoff.Property(d => d.Notes).HasColumnName("dropoff_notes").HasMaxLength(500);
        });

        builder.OwnsOne(x => x.Driver, driver =>
        {
            driver.Property(d => d.Requested).HasColumnName("driver_requested");
            driver.OwnsOne(d => d.Fee, fee =>
            {
                fee.Property(m => m.Amount).HasColumnName("driver_fee_amount").HasPrecision(18, 4);
                fee.Property(m => m.CurrencyId).HasColumnName("driver_fee_currency_id");
            });
        });

        builder.OwnsOne(x => x.Delivery, delivery =>
        {
            delivery.Property(d => d.Requested).HasColumnName("delivery_requested");
            delivery.Property(d => d.AddressLine).HasColumnName("delivery_address_line").HasMaxLength(500);
            delivery.OwnsOne(d => d.Fee, fee =>
            {
                fee.Property(m => m.Amount).HasColumnName("delivery_fee_amount").HasPrecision(18, 4);
                fee.Property(m => m.CurrencyId).HasColumnName("delivery_fee_currency_id");
            });
        });

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.OccupiedEnd);

        builder.HasMany(x => x.Timeline)
            .WithOne()
            .HasForeignKey("BookingId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Timeline)
            .HasField("_timeline")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey("BookingId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex(x => new { x.AssetId, x.Status });
        builder.HasIndex(x => new { x.ExpiresAtUtc, x.Status });
        builder.HasIndex(x => new { x.RenterId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.HostId, x.CreatedAtUtc });
    }
}
