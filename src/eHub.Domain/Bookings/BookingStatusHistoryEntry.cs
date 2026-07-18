namespace eHub.Domain.Bookings;

public sealed class BookingStatusHistoryEntry
{
    public Guid Id { get; private set; }
    public string? FromStatus { get; private set; }
    public string ToStatus { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }
    public DateTime AtUtc { get; private set; }

    private BookingStatusHistoryEntry()
    {
    }

    internal static BookingStatusHistoryEntry Create(
        BookingStatusCode? from,
        BookingStatusCode to,
        DateTime atUtc,
        Guid? actorId)
        => new()
        {
            Id = Guid.NewGuid(),
            FromStatus = from?.Value,
            ToStatus = to.Value,
            AtUtc = atUtc,
            ActorId = actorId
        };
}
