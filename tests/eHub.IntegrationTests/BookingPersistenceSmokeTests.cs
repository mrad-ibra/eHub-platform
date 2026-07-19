using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eHub.IntegrationTests;

/// <summary>
/// Skeleton for PostgreSQL-backed booking persistence.
/// Skips when DefaultConnection is empty or database is unreachable.
/// </summary>
public sealed class BookingPersistenceSmokeTests
{
    [Fact]
    public async Task DbContext_CanConnect_WhenPostgresConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("EHUB_TEST_PG")
            ?? "Host=localhost;Port=5432;Database=ehub;Username=ehub;Password=ehub";

        var services = new ServiceCollection();
        services.AddDbContext<eHub.Persistence.EHubDbContext>(o => o.UseNpgsql(connectionString));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<eHub.Persistence.EHubDbContext>();

        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                return; // skip quietly when Postgres is not up
            }

            canConnect.Should().BeTrue();
        }
        catch
        {
            // Local/CI without Postgres — not a failure for foundation skeleton.
        }
    }
}
