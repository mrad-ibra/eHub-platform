namespace eHub.Domain.Payments;

public sealed class PaymentStatusHistoryEntry
{
    public Guid Id { get; private set; }
    public string? FromStatus { get; private set; }
    public string ToStatus { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public Guid? ActorId { get; private set; }
    public DateTime AtUtc { get; private set; }

    private PaymentStatusHistoryEntry()
    {
    }

    internal static PaymentStatusHistoryEntry Create(
        PaymentStatusCode? from,
        PaymentStatusCode to,
        DateTime atUtc,
        Guid? actorId,
        string? reason)
        => new()
        {
            Id = Guid.NewGuid(),
            FromStatus = from?.Value,
            ToStatus = to.Value,
            Reason = reason,
            AtUtc = atUtc,
            ActorId = actorId
        };
}
