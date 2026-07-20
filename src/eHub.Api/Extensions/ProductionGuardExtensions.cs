using eHub.Persistence;

namespace eHub.Api.Extensions;

public static class ProductionGuardExtensions
{
    /// <summary>
    /// Production must use EF persistence and outbox — not in-memory repos or <see cref="Infrastructure.Persistence.NullOutboxWriter"/>.
    /// </summary>
    public static void EnsureProductionPersistence(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            return;
        }

        if (DependencyInjection.IsEfPersistenceEnabled(builder.Configuration))
        {
            return;
        }

        throw new InvalidOperationException(
            "Production startup blocked: ConnectionStrings:DefaultConnection is required. " +
            "In-memory persistence and NullOutboxWriter are not allowed outside Development.");
    }
}
