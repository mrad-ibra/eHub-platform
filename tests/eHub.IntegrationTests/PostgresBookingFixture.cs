using eHub.Application.Bookings.Abstractions;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Application.Payments.Abstractions;
using eHub.Application.Payments.Commands.CreateRefund;
using eHub.Application.Payments.Commands.ProcessWebhook;
using eHub.Domain.Bookings;
using eHub.Domain.Catalog;
using eHub.Domain.Common;
using eHub.Infrastructure.Jobs;
using eHub.Infrastructure.Payments;
using eHub.Persistence;
using eHub.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace eHub.IntegrationTests;

public sealed class PostgresBookingFixture : IAsyncLifetime
{
    public const string RequirePostgresEnv = "EHUB_REQUIRE_POSTGRES_TESTS";
    public const string ConnectionStringEnv = "EHUB_TEST_POSTGRES";

    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;
    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            // Prefer an explicit connection string (server/shared DB) over Testcontainers.
            var external = Environment.GetEnvironmentVariable(ConnectionStringEnv)
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (!string.IsNullOrWhiteSpace(external))
            {
                ConnectionString = external.Trim();
            }
            else
            {
                _container = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .WithDatabase("ehub_test")
                    .WithUsername("ehub")
                    .WithPassword("ehub")
                    .Build();

                await _container.StartAsync();
                ConnectionString = _container.GetConnectionString();
            }

            await using var db = CreateDbContext();
            await db.Database.MigrateAsync();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            if (IsPostgresRequired())
            {
                throw new InvalidOperationException(
                    $"{RequirePostgresEnv}=true but PostgreSQL is unavailable. " +
                    $"Set {ConnectionStringEnv} (or ConnectionStrings__DefaultConnection), " +
                    "or ensure Docker is available for Testcontainers.",
                    ex);
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public static bool IsPostgresRequired()
        => string.Equals(
            Environment.GetEnvironmentVariable(RequirePostgresEnv),
            "true",
            StringComparison.OrdinalIgnoreCase);

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
        AddPostgresDb(services);
        AddPersistence(services);
        return services.BuildServiceProvider();
    }

    public ServiceProvider CreatePaymentServices(IClock? clock = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(clock ?? new FixedClock(new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc)));
        services.Configure<PaymentProviderOptions>(o =>
        {
            o.Fake.WebhookSecret = "pg-test-webhook-secret";
            o.Fake.TimestampToleranceSeconds = 300;
        });
        AddPostgresDb(services);
        AddPersistence(services);
        services.AddSingleton<IPaymentProvider, FakePaymentProvider>();
        services.AddSingleton<IPaymentProviderResolver, PaymentProviderResolver>();
        services.AddScoped<ProcessWebhookCommandHandler>();
        services.AddScoped<PaymentOutboxProcessor>();
        return services.BuildServiceProvider();
    }

    public ServiceProvider CreateRefundServices(IClock? clock = null)
    {
        var adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var currentUser = Substitute.For<eHub.Application.Common.Context.ICurrentUser>();
        currentUser.RequireUserId().Returns(adminId);
        currentUser.HasPermission(eHub.Application.Identity.Authorization.AuthPolicies.PaymentsRefund).Returns(true);
        currentUser.IsInRole("Admin").Returns(true);

        var currencies = Substitute.For<ICurrencyRepository>();
        currencies.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Currency.Create("AZN", "Azerbaijani Manat", "₼", DateTime.UtcNow));

        var services = new ServiceCollection();
        services.AddSingleton(clock ?? new FixedClock(new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton(currentUser);
        services.AddSingleton(currencies);
        services.AddSingleton<IMinorUnitConverter, eHub.Application.Payments.MinorUnitConverter>();
        services.Configure<PaymentProviderOptions>(o =>
        {
            o.Fake.WebhookSecret = "pg-test-webhook-secret";
            o.Fake.TimestampToleranceSeconds = 300;
        });
        AddPostgresDb(services);
        AddPersistence(services);
        services.AddSingleton<IPaymentProvider, FakePaymentProvider>();
        services.AddSingleton<IPaymentProviderResolver, PaymentProviderResolver>();
        services.AddScoped<CreateRefundCommandHandler>();
        return services.BuildServiceProvider();
    }

    private void AddPostgresDb(IServiceCollection services)
    {
        services.AddDbContext<EHubDbContext>(o =>
            o.UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(EHubDbContext).Assembly.FullName)));
    }

    private static void AddPersistence(IServiceCollection services)
    {
        services.AddScoped<EfUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EfUnitOfWork>());

        services.AddScoped<EfBookingRepository>();
        services.AddScoped<IBookingRepository>(sp => sp.GetRequiredService<EfBookingRepository>());

        services.AddScoped<EfBookingIdempotencyStore>();
        services.AddScoped<EfBookingNumberGenerator>();

        services.AddScoped<EfPaymentRepository>();
        services.AddScoped<IPaymentRepository>(sp => sp.GetRequiredService<EfPaymentRepository>());

        services.AddScoped<EfPaymentWebhookInboxStore>();
        services.AddScoped<IPaymentWebhookInboxStore>(sp => sp.GetRequiredService<EfPaymentWebhookInboxStore>());

        services.AddScoped<EfOutboxWriter>();
        services.AddScoped<IOutboxWriter>(sp => sp.GetRequiredService<EfOutboxWriter>());
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
