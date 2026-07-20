namespace eHub.Application.Payments.Abstractions;

public static class PaymentWebhookInboxStatuses
{
    public const string Received = "Received";
    public const string Processed = "Processed";
    public const string Ignored = "Ignored";
    public const string Failed = "Failed";
}
