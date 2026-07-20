using eHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eHub.Persistence.Configurations;

public sealed class PaymentWebhookInboxConfiguration : IEntityTypeConfiguration<PaymentWebhookInbox>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookInbox> builder)
    {
        builder.ToTable("payment_webhook_inbox");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProviderEventId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(512);
        builder.Property(x => x.ReceivedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique();
        builder.HasIndex(x => x.PaymentId);
    }
}
