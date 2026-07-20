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
}
