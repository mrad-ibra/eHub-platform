namespace eHub.Application.Configuration;

public static class PaymentWebhookInboxOptions
{
    public const string SectionName = "Payments:WebhookInbox";

    /// <summary>
    /// How long a <c>Received</c> inbox row may stay uncompleted before another worker may reclaim it.
    /// </summary>
    public static TimeSpan ProcessingLease { get; set; } = TimeSpan.FromMinutes(5);
}
