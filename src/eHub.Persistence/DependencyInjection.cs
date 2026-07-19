using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Persistence;
using eHub.Application.Configuration;
using eHub.Application.Payments.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eHub.Persistence;

public static class DependencyInjection
{
    public const string UseEfPersistenceKey = "Persistence:UseEntityFramework";

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        var timeout = configuration.GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>()?.CommandTimeoutSeconds ?? 30;

        services.AddDbContext<EHubDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.CommandTimeout(timeout);
                npgsql.MigrationsAssembly(typeof(EHubDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IBookingRepository, Repositories.EfBookingRepository>();
        services.AddScoped<IBookingIdempotencyStore, Repositories.EfBookingIdempotencyStore>();
        services.AddScoped<IBookingNumberGenerator, Repositories.EfBookingNumberGenerator>();
        services.AddScoped<IPaymentRepository, Repositories.EfPaymentRepository>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();

        return services;
    }

    public static bool IsEfPersistenceEnabled(IConfiguration configuration)
        => !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection"));
}
