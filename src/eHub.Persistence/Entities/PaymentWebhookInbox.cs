namespace eHub.Persistence.Entities;

public sealed class PaymentWebhookInbox
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Received";
    public Guid? PaymentId { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
