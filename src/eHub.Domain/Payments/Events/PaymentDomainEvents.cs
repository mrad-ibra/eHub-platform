using eHub.Domain.Common;

namespace eHub.Domain.Payments.Events;

public sealed record PaymentCreated(
    Guid PaymentId,
    Guid BookingId,
    decimal Amount,
    Guid CurrencyId,
    string Provider,
    string Status,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentPending(
    Guid PaymentId,
    Guid BookingId,
    string? ProviderPaymentId,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentAuthorized(
    Guid PaymentId,
    Guid BookingId,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentSucceeded(
    Guid PaymentId,
    Guid BookingId,
    decimal Amount,
    Guid CurrencyId,
    DateTime PaidAtUtc,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentFailed(
    Guid PaymentId,
    Guid BookingId,
    string FailureReason,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentCancelled(
    Guid PaymentId,
    Guid BookingId,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentExpired(
    Guid PaymentId,
    Guid BookingId,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentRefunded(
    Guid PaymentId,
    Guid BookingId,
    Guid RefundId,
    decimal RefundAmount,
    Guid CurrencyId,
    bool FullyRefunded,
    DateTime OccurredAtUtc) : IDomainEvent;
