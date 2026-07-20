using eHub.Application.Payments.Abstractions;
using eHub.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace eHub.Persistence.Repositories;

public sealed class EfPaymentRepository(EHubDbContext db) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        => await db.Payments.AddAsync(payment, cancellationToken);

    public Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
        => Query()
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

    public async Task<Payment?> GetByIdForUpdateAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        // Row lock serializes parallel refunds on the same payment (AggregateVersion alone can race
        // under concurrent load when both handlers load before either commits).
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM payments WHERE "Id" = {paymentId} FOR UPDATE""",
            cancellationToken);

        return await GetByIdAsync(paymentId, cancellationToken);
    }

    public Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        => Query()
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<Payment?> GetActiveByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        => Query()
            .Where(p => p.BookingId == bookingId)
            .Where(p =>
                p.Status == PaymentStatusCode.Created
                || p.Status == PaymentStatusCode.Pending
                || p.Status == PaymentStatusCode.Authorized)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Payment?> GetByProviderPaymentIdAsync(
        string provider,
        string providerPaymentId,
        CancellationToken cancellationToken = default)
        => Query()
            .Where(p => p.Provider == PaymentProviderCode.Parse(provider))
            .Where(p => p.ProviderPaymentId == providerPaymentId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Payment>> ListExpiredAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return [];
        }

        return await Query()
            .Where(p =>
                p.Status == PaymentStatusCode.Created
                || p.Status == PaymentStatusCode.Pending
                || p.Status == PaymentStatusCode.Authorized)
            .Where(p => p.ExpiresAtUtc != null && p.ExpiresAtUtc <= nowUtc)
            .OrderBy(p => p.ExpiresAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<Refund?> GetRefundByIdAsync(Guid refundId, CancellationToken cancellationToken = default)
        => db.Set<Refund>()
            .FirstOrDefaultAsync(r => r.Id == refundId, cancellationToken);

    private IQueryable<Payment> Query()
        => db.Payments
            .Include(p => p.Timeline)
            .Include(p => p.StatusHistory)
            .Include(p => p.Attempts)
            .Include(p => p.Refunds);
}
