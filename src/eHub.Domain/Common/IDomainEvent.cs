namespace eHub.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
