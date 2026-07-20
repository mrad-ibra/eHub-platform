using eHub.Domain.Payments;

namespace eHub.Application.Payments.Abstractions;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the payment aggregate and takes a row lock (<c>SELECT … FOR UPDATE</c>) when a
    /// transaction is open — required for parallel partial-refund correctness.
    /// </summary>
    Task<Payment?> GetByIdForUpdateAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Active (non-terminal) payment for a booking, if any.</summary>
    Task<Payment?> GetActiveByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<Payment?> GetByProviderPaymentIdAsync(
        string provider,
        string providerPaymentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> ListExpiredAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task<Refund?> GetRefundByIdAsync(Guid refundId, CancellationToken cancellationToken = default);
}
