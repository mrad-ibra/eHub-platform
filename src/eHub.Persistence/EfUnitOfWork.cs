using eHub.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace eHub.Persistence;

public sealed class EfUnitOfWork(EHubDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}
