namespace eHub.Domain.Payments;

/// <summary>User/ops-facing trail (Created, Paid, Failed, Refunded, …).</summary>
public sealed class PaymentTimelineEntry
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }
    public DateTime AtUtc { get; private set; }

    private PaymentTimelineEntry()
    {
    }

    internal static PaymentTimelineEntry Create(string code, string message, DateTime atUtc, Guid? actorId)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Message = message,
            AtUtc = atUtc,
            ActorId = actorId
        };
}
