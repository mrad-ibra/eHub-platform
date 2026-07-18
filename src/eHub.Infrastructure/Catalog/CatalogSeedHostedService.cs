using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Domain.Catalog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eHub.Infrastructure.Catalog;

public sealed class CatalogSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CatalogOptions> options,
    ILogger<CatalogSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Seed.Enabled)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var categories = sp.GetRequiredService<ICategoryRepository>();
        var subCategories = sp.GetRequiredService<ISubCategoryRepository>();
        var brands = sp.GetRequiredService<IBrandRepository>();
        var models = sp.GetRequiredService<IModelRepository>();
        var countries = sp.GetRequiredService<ICountryRepository>();
        var cities = sp.GetRequiredService<ICityRepository>();
        var districts = sp.GetRequiredService<IDistrictRepository>();
        var currencies = sp.GetRequiredService<ICurrencyRepository>();
        var languages = sp.GetRequiredService<ILanguageRepository>();
        var transmissions = sp.GetRequiredService<ITransmissionRepository>();
        var fuelTypes = sp.GetRequiredService<IFuelTypeRepository>();
        var vehicleTypes = sp.GetRequiredService<IVehicleTypeRepository>();
        var equipmentTypes = sp.GetRequiredService<IEquipmentTypeRepository>();
        var features = sp.GetRequiredService<IFeatureDefinitionRepository>();
        var colors = sp.GetRequiredService<IColorRepository>();
        var documentTypes = sp.GetRequiredService<IDocumentTypeRepository>();
        var mediaTypes = sp.GetRequiredService<IMediaTypeRepository>();
        var rentalPeriodTypes = sp.GetRequiredService<IRentalPeriodTypeRepository>();
        var paymentMethods = sp.GetRequiredService<IPaymentMethodRepository>();
        var bookingStatuses = sp.GetRequiredService<IBookingStatusRepository>();
        var assetStatuses = sp.GetRequiredService<IAssetStatusRepository>();
        var reviewStatuses = sp.GetRequiredService<IReviewStatusRepository>();
        var clock = sp.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        if (await categories.GetByCodeAsync("VEHICLE", cancellationToken) is not null)
        {
            return;
        }

        logger.LogInformation("Seeding catalog dictionaries");

        var vehicle = Category.Create("VEHICLE", "Vehicles", now, 1);
        var equipment = Category.Create("EQUIPMENT", "Equipment", now, 2);
        await categories.AddAsync(vehicle, cancellationToken);
        await categories.AddAsync(equipment, cancellationToken);

        await subCategories.AddAsync(SubCategory.Create(vehicle.Id, "CAR", "Car", now, 1), cancellationToken);
        await subCategories.AddAsync(SubCategory.Create(vehicle.Id, "MOTORCYCLE", "Motorcycle", now, 2), cancellationToken);
        await subCategories.AddAsync(SubCategory.Create(equipment.Id, "EXCAVATOR", "Excavator", now, 1), cancellationToken);

        var toyota = Brand.Create("TOYOTA", "Toyota", now, 1);
        var bmw = Brand.Create("BMW", "BMW", now, 2);
        await brands.AddAsync(toyota, cancellationToken);
        await brands.AddAsync(bmw, cancellationToken);
        await models.AddAsync(Model.Create(toyota.Id, "COROLLA", "Corolla", now, 1), cancellationToken);
        await models.AddAsync(Model.Create(bmw.Id, "X5", "X5", now, 1), cancellationToken);

        var az = Country.Create("AZ", "Azerbaijan", now, 1);
        await countries.AddAsync(az, cancellationToken);
        var baku = City.Create(az.Id, "BAKU", "Baku", now, 1);
        await cities.AddAsync(baku, cancellationToken);
        await districts.AddAsync(District.Create(baku.Id, "NASIMI", "Nasimi", now, 1), cancellationToken);
        await districts.AddAsync(District.Create(baku.Id, "YASAMAL", "Yasamal", now, 2), cancellationToken);

        await currencies.AddAsync(Currency.Create("AZN", "Azerbaijani Manat", "₼", now, 2, 1), cancellationToken);
        await currencies.AddAsync(Currency.Create("USD", "US Dollar", "$", now, 2, 2), cancellationToken);
        await currencies.AddAsync(Currency.Create("EUR", "Euro", "€", now, 2, 3), cancellationToken);

        await languages.AddAsync(Language.Create("AZ", "Azerbaijani", "az-AZ", now, 1), cancellationToken);
        await languages.AddAsync(Language.Create("EN", "English", "en-US", now, 2), cancellationToken);

        await SeedAsync(transmissions.AddAsync, cancellationToken,
            Transmission.Create("MANUAL", "Manual", now, 1),
            Transmission.Create("AUTOMATIC", "Automatic", now, 2));

        await SeedAsync(fuelTypes.AddAsync, cancellationToken,
            FuelType.Create("PETROL", "Petrol", now, 1),
            FuelType.Create("DIESEL", "Diesel", now, 2),
            FuelType.Create("ELECTRIC", "Electric", now, 3),
            FuelType.Create("HYBRID", "Hybrid", now, 4));

        await SeedAsync(vehicleTypes.AddAsync, cancellationToken,
            VehicleType.Create("SEDAN", "Sedan", now, 1),
            VehicleType.Create("SUV", "SUV", now, 2),
            VehicleType.Create("VAN", "Van", now, 3));

        await SeedAsync(equipmentTypes.AddAsync, cancellationToken,
            EquipmentType.Create("CONSTRUCTION", "Construction", now, 1),
            EquipmentType.Create("GENERATOR", "Generator", now, 2));

        await features.AddAsync(FeatureDefinition.Create("GPS", "GPS", now, "SAFETY", 1), cancellationToken);
        await features.AddAsync(FeatureDefinition.Create("AC", "Air Conditioning", now, "COMFORT", 2), cancellationToken);

        await colors.AddAsync(Color.Create("WHITE", "White", now, "#FFFFFF", 1), cancellationToken);
        await colors.AddAsync(Color.Create("BLACK", "Black", now, "#000000", 2), cancellationToken);
        await colors.AddAsync(Color.Create("SILVER", "Silver", now, "#C0C0C0", 3), cancellationToken);

        await SeedAsync(documentTypes.AddAsync, cancellationToken,
            DocumentType.Create("DRIVERS_LICENSE", "Driver's License", now, 1),
            DocumentType.Create("PASSPORT", "Passport", now, 2),
            DocumentType.Create("ID_CARD", "ID Card", now, 3));

        await SeedAsync(mediaTypes.AddAsync, cancellationToken,
            MediaType.Create("IMAGE", "Image", now, 1),
            MediaType.Create("VIDEO", "Video", now, 2),
            MediaType.Create("DOCUMENT", "Document", now, 3));

        await SeedAsync(rentalPeriodTypes.AddAsync, cancellationToken,
            RentalPeriodType.Create("HOURLY", "Hourly", now, 1),
            RentalPeriodType.Create("DAILY", "Daily", now, 2),
            RentalPeriodType.Create("WEEKLY", "Weekly", now, 3),
            RentalPeriodType.Create("MONTHLY", "Monthly", now, 4));

        await SeedAsync(paymentMethods.AddAsync, cancellationToken,
            PaymentMethod.Create("CARD", "Card", now, 1),
            PaymentMethod.Create("CASH", "Cash", now, 2),
            PaymentMethod.Create("BANK_TRANSFER", "Bank Transfer", now, 3));

        await SeedAsync(bookingStatuses.AddAsync, cancellationToken,
            BookingStatus.Create("PENDING", "Pending", now, 1),
            BookingStatus.Create("CONFIRMED", "Confirmed", now, 2),
            BookingStatus.Create("CANCELLED", "Cancelled", now, 3),
            BookingStatus.Create("COMPLETED", "Completed", now, 4));

        await SeedAsync(assetStatuses.AddAsync, cancellationToken,
            AssetStatus.Create("DRAFT", "Draft", now, 1),
            AssetStatus.Create("PENDING_APPROVAL", "Pending Approval", now, 2),
            AssetStatus.Create("PUBLISHED", "Published", now, 3),
            AssetStatus.Create("REJECTED", "Rejected", now, 4),
            AssetStatus.Create("SUSPENDED", "Suspended", now, 5),
            AssetStatus.Create("ARCHIVED", "Archived", now, 6));

        await SeedAsync(reviewStatuses.AddAsync, cancellationToken,
            ReviewStatus.Create("PENDING", "Pending", now, 1),
            ReviewStatus.Create("APPROVED", "Approved", now, 2),
            ReviewStatus.Create("REJECTED", "Rejected", now, 3));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedAsync<T>(
        Func<T, CancellationToken, Task> addAsync,
        CancellationToken cancellationToken,
        params T[] items)
    {
        foreach (var item in items)
        {
            await addAsync(item, cancellationToken);
        }
    }
}
