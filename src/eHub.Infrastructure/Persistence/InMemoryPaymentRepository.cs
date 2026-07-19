using System.Collections.Concurrent;
using eHub.Application.Payments.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Localization;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    private readonly ConcurrentDictionary<string, Guid> _byIdempotencyKey =
        new(StringComparer.Ordinal);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var active = _payments.Values.FirstOrDefault(p =>
            p.BookingId == payment.BookingId
            && p.Status.IsOneOf(
                PaymentStatusCode.Created,
                PaymentStatusCode.Pending,
                PaymentStatusCode.Authorized));

        if (active is not null)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentActiveAlreadyExists));
        }

        if (!_byIdempotencyKey.TryAdd(payment.IdempotencyKey, payment.Id))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentIdempotencyPayloadMismatch));
        }

        if (!_payments.TryAdd(payment.Id, payment))
        {
            _byIdempotencyKey.TryRemove(payment.IdempotencyKey, out _);
            throw new InvalidOperationException($"Payment '{payment.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        _payments.TryGetValue(paymentId, out var payment);
        return Task.FromResult(payment);
    }

    public Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (!_byIdempotencyKey.TryGetValue(idempotencyKey, out var id))
        {
            return Task.FromResult<Payment?>(null);
        }

        _payments.TryGetValue(id, out var payment);
        return Task.FromResult(payment);
    }

    public Task<Payment?> GetActiveByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var payment = _payments.Values.FirstOrDefault(p =>
            p.BookingId == bookingId
            && p.Status.IsOneOf(
                PaymentStatusCode.Created,
                PaymentStatusCode.Pending,
                PaymentStatusCode.Authorized));
        return Task.FromResult(payment);
    }

    public Task<IReadOnlyList<Payment>> ListExpiredAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return Task.FromResult<IReadOnlyList<Payment>>([]);
        }

        var list = _payments.Values
            .Where(p => p.Status.IsOneOf(
                PaymentStatusCode.Created,
                PaymentStatusCode.Pending,
                PaymentStatusCode.Authorized))
            .Where(p => p.ExpiresAtUtc is not null && p.ExpiresAtUtc <= nowUtc)
            .OrderBy(p => p.ExpiresAtUtc)
            .Take(take)
            .ToArray();

        return Task.FromResult<IReadOnlyList<Payment>>(list);
    }
}
