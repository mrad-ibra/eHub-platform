using eHub.Application.Common.Persistence;
using eHub.Domain.Exceptions;
using eHub.Localization;
using Microsoft.EntityFrameworkCore;

namespace eHub.Persistence;

public sealed class EfUnitOfWork(EHubDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            PostgresExceptionMapper.ThrowIfMapped(ex);
            if (ex is DbUpdateConcurrencyException)
            {
                throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentRefundAmountInvalid));
            }

            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
