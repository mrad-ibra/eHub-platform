using eHub.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eHub.Persistence.Configurations;

public sealed class PaymentTimelineEntryConfiguration : IEntityTypeConfiguration<PaymentTimelineEntry>
{
    public void Configure(EntityTypeBuilder<PaymentTimelineEntry> builder)
    {
        builder.ToTable("payment_timeline_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ActorId);
        builder.Property(x => x.AtUtc).IsRequired();
        builder.Property<Guid>("PaymentId").IsRequired();
        builder.HasIndex("PaymentId");
    }
}

public sealed class PaymentStatusHistoryEntryConfiguration : IEntityTypeConfiguration<PaymentStatusHistoryEntry>
{
    public void Configure(EntityTypeBuilder<PaymentStatusHistoryEntry> builder)
    {
        builder.ToTable("payment_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasMaxLength(64);
        builder.Property(x => x.ToStatus).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(PaymentDefaults.MaxFailureReasonLength);
        builder.Property(x => x.ActorId);
        builder.Property(x => x.AtUtc).IsRequired();
        builder.Property<Guid>("PaymentId").IsRequired();
        builder.HasIndex(x => new { x.AtUtc });
        builder.HasIndex("PaymentId", "AtUtc");
    }
}

public sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("payment_attempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Result).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProviderReference).HasMaxLength(PaymentDefaults.MaxProviderPaymentIdLength);
        builder.Property(x => x.Detail).HasMaxLength(PaymentDefaults.MaxFailureReasonLength);
        builder.Property(x => x.AtUtc).IsRequired();
        builder.Property<Guid>("PaymentId").IsRequired();
        builder.HasIndex("PaymentId");
    }
}

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("payment_refunds");
        builder.HasKey(x => x.Id);

        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasPrecision(18, 4).IsRequired();
            money.Property(m => m.CurrencyId).HasColumnName("currency_id").IsRequired();
        });

        builder.Property(x => x.Reason).HasMaxLength(PaymentDefaults.MaxRefundReasonLength).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(PaymentDefaults.MaxIdempotencyKeyLength).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProviderRefundId).HasMaxLength(PaymentDefaults.MaxProviderPaymentIdLength);
        builder.Property(x => x.RequestedByActorId);
        builder.Property(x => x.RequestedAtUtc).IsRequired();
        builder.Property(x => x.SettledAtUtc);
        builder.Property(x => x.PaymentId).IsRequired();
        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => new { x.PaymentId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_payment_refunds_payment_idempotency");
    }
}
