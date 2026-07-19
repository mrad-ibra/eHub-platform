using eHub.Application.Common.Time;
using eHub.Domain.Bookings;
using eHub.Domain.Common;
using eHub.Persistence;
using eHub.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace eHub.IntegrationTests;

public sealed class PostgresBookingFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;
    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("ehub_test")
                .WithUsername("ehub")
                .WithPassword("ehub")
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();

            await using var db = CreateDbContext();
            await db.Database.MigrateAsync();
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public EHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EHubDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(EHubDbContext).Assembly.FullName))
            .Options;
        return new EHubDbContext(options);
    }

    public ServiceProvider CreateServices(IClock? clock = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(clock ?? new FixedClock(new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc)));
        services.AddDbContext<EHubDbContext>(o =>
            o.UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(EHubDbContext).Assembly.FullName)));
        services.AddScoped<EfUnitOfWork>();
        services.AddScoped<EfBookingRepository>();
        services.AddScoped<EfBookingIdempotencyStore>();
        services.AddScoped<EfBookingNumberGenerator>();
        return services.BuildServiceProvider();
    }

    public static Booking CreateSoftHold(
        Guid assetId,
        Guid renterId,
        Guid hostId,
        DateOnly start,
        DateOnly end,
        string number,
        DateTime now,
        int bufferDays = 1)
    {
        var currency = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        return Booking.CreateRequest(
            number,
            assetId,
            renterId,
            hostId,
            BookingPeriod.Create(start, end),
            Money.Create(100m, currency),
            BookingAssetSnapshot.Create("Test Asset", hostId, now, "Brand", "Model"),
            BookingTerms.Create(bufferDays),
            now);
    }
}

public sealed class FixedClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; } = utcNow;
}

[CollectionDefinition("PostgresBooking")]
public sealed class PostgresBookingCollection : ICollectionFixture<PostgresBookingFixture>;
