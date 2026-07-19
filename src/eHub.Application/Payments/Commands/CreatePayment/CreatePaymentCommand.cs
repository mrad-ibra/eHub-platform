using eHub.Application.Common.Messaging;

namespace eHub.Application.Payments.Commands.CreatePayment;

public sealed record CreatePaymentCommand(
    Guid BookingId,
    string IdempotencyKey,
    string Provider = "TEST") : ICommand<CreatePaymentResult>;

public sealed record CreatePaymentResult(
    Guid Id,
    Guid BookingId,
    string Status,
    string Provider,
    decimal Amount,
    Guid CurrencyId,
    string IdempotencyKey,
    DateTime? ExpiresAtUtc,
    int AggregateVersion);
