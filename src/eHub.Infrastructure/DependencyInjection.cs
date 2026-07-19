using eHub.Application.Abstractions.Email;
using eHub.Application.Assets.Abstractions;
using eHub.Application.Bookings.Abstractions;
using eHub.Application.Bookings.Services;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Infrastructure.Catalog;
using eHub.Infrastructure.Email;
using eHub.Infrastructure.Identity;
using eHub.Infrastructure.Persistence;
using eHub.Infrastructure.Time;
using eHub.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
        services.AddSingleton<ILoginHistoryRepository, InMemoryLoginHistoryRepository>();
        services.AddSingleton<IAssetRepository, InMemoryAssetRepository>();
        services.AddSingleton<BookingAvailabilityService>();
        AddCatalogRepositories(services);
        services.AddHostedService<AuthSeedHostedService>();
        services.AddHostedService<CatalogSeedHostedService>();

        if (eHub.Persistence.DependencyInjection.IsEfPersistenceEnabled(configuration))
        {
            services.AddPersistence(configuration);
        }
        else
        {
            services.AddSingleton<IBookingRepository, InMemoryBookingRepository>();
            services.AddSingleton<IBookingNumberGenerator, InMemoryBookingNumberGenerator>();
            services.AddSingleton<IBookingIdempotencyStore, InMemoryBookingIdempotencyStore>();
            services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
        }

        return services;
    }

    private static void AddCatalogRepositories(IServiceCollection services)
    {
        services.AddSingleton<InMemoryCatalogPersistence>();
        services.AddSingleton<ICategoryRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<ISubCategoryRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IBrandRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IModelRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<ICountryRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<ICityRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IDistrictRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<ICurrencyRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<ILanguageRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<ITransmissionRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IFuelTypeRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IVehicleTypeRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IEquipmentTypeRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IFeatureDefinitionRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IColorRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IDocumentTypeRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IMediaTypeRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IRentalPeriodTypeRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IPaymentMethodRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IBookingStatusRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IAssetStatusRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
        services.AddSingleton<IReviewStatusRepository>(sp => sp.GetRequiredService<InMemoryCatalogPersistence>());
    }
}
