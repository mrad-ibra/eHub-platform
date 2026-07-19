using eHub.Application.Common.Persistence;
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
            throw;
        }
    }
}
