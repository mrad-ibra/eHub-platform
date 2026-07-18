using eHub.Domain.Common;

namespace eHub.Domain.Bookings.Events;

public sealed record BookingCreated(
    Guid BookingId,
    string BookingNumber,
    Guid AssetId,
    Guid RenterId,
    Guid HostId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingApproved(
    Guid BookingId,
    string BookingNumber,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingRejected(
    Guid BookingId,
    string BookingNumber,
    string Reason,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingCancelled(
    Guid BookingId,
    string BookingNumber,
    string Reason,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingExpired(
    Guid BookingId,
    string BookingNumber,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingConfirmed(
    Guid BookingId,
    string BookingNumber,
    Guid PaymentId,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingStarted(
    Guid BookingId,
    string BookingNumber,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingCompleted(
    Guid BookingId,
    string BookingNumber,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingExtended(
    Guid BookingId,
    string BookingNumber,
    DateOnly NewEndDate,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record BookingRefunded(
    Guid BookingId,
    string BookingNumber,
    DateTime OccurredAtUtc) : IDomainEvent;
