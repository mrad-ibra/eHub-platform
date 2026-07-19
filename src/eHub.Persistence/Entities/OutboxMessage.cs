namespace eHub.Persistence.Entities;

/// <summary>
/// Transactional outbox row. Written in the same SaveChanges as the aggregate mutation.
/// Consumers (notification / payment) must be idempotent.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int AttemptCount { get; set; }
}
