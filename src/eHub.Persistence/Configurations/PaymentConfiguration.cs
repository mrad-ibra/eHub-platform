using eHub.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eHub.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", table =>
        {
            table.HasCheckConstraint("ck_payments_amount_positive", "amount > 0");
            table.HasCheckConstraint(
                "ck_payments_refunded_bounds",
                "refunded_amount >= 0 AND refunded_amount <= amount");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingId).IsRequired();
        builder.HasIndex(x => x.BookingId);

        builder.HasIndex(x => x.BookingId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('CREATED', 'PENDING', 'AUTHORIZED')")
            .HasDatabaseName("ux_payments_one_active_per_booking");

        builder.Property(x => x.Provider)
            .HasConversion(
                provider => provider.Value,
                value => PaymentProviderCode.Parse(value))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ProviderPaymentId).HasMaxLength(PaymentDefaults.MaxProviderPaymentIdLength);
        builder.HasIndex(x => new { x.Provider, x.ProviderPaymentId });

        builder.Property(x => x.Status)
            .HasConversion(
                status => status.Value,
                value => PaymentStatusCode.Parse(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(PaymentDefaults.MaxIdempotencyKeyLength)
            .IsRequired();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();

        builder.Property(x => x.FailureReason).HasMaxLength(PaymentDefaults.MaxFailureReasonLength);
        builder.Property(x => x.PaidAtUtc);
        builder.Property(x => x.ExpiresAtUtc);

        builder.Property(x => x.AggregateVersion).IsConcurrencyToken();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedBy);

        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasPrecision(18, 4).IsRequired();
            money.Property(m => m.CurrencyId).HasColumnName("currency_id").IsRequired();
        });

        builder.OwnsOne(x => x.RefundedAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("refunded_amount").HasPrecision(18, 4).IsRequired();
            money.Property(m => m.CurrencyId).HasColumnName("refunded_currency_id").IsRequired();
        });

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.RemainingRefundable);

        builder.HasMany(x => x.Timeline)
            .WithOne()
            .HasForeignKey("PaymentId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Timeline)
            .HasField("_timeline")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey("PaymentId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Attempts)
            .WithOne()
            .HasForeignKey("PaymentId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Attempts)
            .HasField("_attempts")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Refunds)
            .WithOne()
            .HasForeignKey("PaymentId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Refunds)
            .HasField("_refunds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.ExpiresAtUtc, x.Status });
        builder.HasIndex(x => x.Status);
    }
}
