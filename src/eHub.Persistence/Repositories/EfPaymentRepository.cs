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

    private IQueryable<Payment> Query()
        => db.Payments
            .Include(p => p.Timeline)
            .Include(p => p.StatusHistory)
            .Include(p => p.Attempts)
            .Include(p => p.Refunds);
}
