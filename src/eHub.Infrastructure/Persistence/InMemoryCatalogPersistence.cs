using System.Collections.Concurrent;
using eHub.Application.Catalog.Abstractions;
using eHub.Domain.Catalog;

namespace eHub.Infrastructure.Persistence;

/// <summary>
/// In-memory persistence for catalog dictionaries.
/// Implements typed repositories only — no public GenericRepository&lt;T&gt; API.
/// </summary>
public sealed class InMemoryCatalogPersistence :
    ICategoryRepository,
    ISubCategoryRepository,
    IBrandRepository,
    IModelRepository,
    ICountryRepository,
    ICityRepository,
    IDistrictRepository,
    ICurrencyRepository,
    ILanguageRepository,
    ITransmissionRepository,
    IFuelTypeRepository,
    IVehicleTypeRepository,
    IEquipmentTypeRepository,
    IFeatureDefinitionRepository,
    IColorRepository,
    IDocumentTypeRepository,
    IMediaTypeRepository,
    IRentalPeriodTypeRepository,
    IPaymentMethodRepository,
    IBookingStatusRepository,
    IAssetStatusRepository,
    IReviewStatusRepository
{
    private readonly ConcurrentDictionary<Guid, Category> _categories = new();
    private readonly ConcurrentDictionary<Guid, SubCategory> _subCategories = new();
    private readonly ConcurrentDictionary<Guid, Brand> _brands = new();
    private readonly ConcurrentDictionary<Guid, Model> _models = new();
    private readonly ConcurrentDictionary<Guid, Country> _countries = new();
    private readonly ConcurrentDictionary<Guid, City> _cities = new();
    private readonly ConcurrentDictionary<Guid, District> _districts = new();
    private readonly ConcurrentDictionary<Guid, Currency> _currencies = new();
    private readonly ConcurrentDictionary<Guid, Language> _languages = new();
    private readonly ConcurrentDictionary<Guid, Transmission> _transmissions = new();
    private readonly ConcurrentDictionary<Guid, FuelType> _fuelTypes = new();
    private readonly ConcurrentDictionary<Guid, VehicleType> _vehicleTypes = new();
    private readonly ConcurrentDictionary<Guid, EquipmentType> _equipmentTypes = new();
    private readonly ConcurrentDictionary<Guid, FeatureDefinition> _features = new();
    private readonly ConcurrentDictionary<Guid, Color> _colors = new();
    private readonly ConcurrentDictionary<Guid, DocumentType> _documentTypes = new();
    private readonly ConcurrentDictionary<Guid, MediaType> _mediaTypes = new();
    private readonly ConcurrentDictionary<Guid, RentalPeriodType> _rentalPeriodTypes = new();
    private readonly ConcurrentDictionary<Guid, PaymentMethod> _paymentMethods = new();
    private readonly ConcurrentDictionary<Guid, BookingStatus> _bookingStatuses = new();
    private readonly ConcurrentDictionary<Guid, AssetStatus> _assetStatuses = new();
    private readonly ConcurrentDictionary<Guid, ReviewStatus> _reviewStatuses = new();

    Task ICategoryRepository.AddAsync(Category entity, CancellationToken cancellationToken)
        => Add(_categories, entity);
    Task<Category?> ICategoryRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_categories, id);
    Task<Category?> ICategoryRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_categories, code);
    Task<IReadOnlyList<Category>> ICategoryRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_categories, activeOnly);

    Task ISubCategoryRepository.AddAsync(SubCategory entity, CancellationToken cancellationToken)
        => Add(_subCategories, entity);
    Task<SubCategory?> ISubCategoryRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_subCategories, id);
    Task<SubCategory?> ISubCategoryRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_subCategories, code);
    Task<IReadOnlyList<SubCategory>> ISubCategoryRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_subCategories, activeOnly);
    Task<IReadOnlyList<SubCategory>> ISubCategoryRepository.ListByCategoryIdAsync(
        Guid categoryId, bool activeOnly, CancellationToken cancellationToken)
        => ListWhere(_subCategories, activeOnly, x => x.CategoryId == categoryId);

    Task IBrandRepository.AddAsync(Brand entity, CancellationToken cancellationToken)
        => Add(_brands, entity);
    Task<Brand?> IBrandRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_brands, id);
    Task<Brand?> IBrandRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_brands, code);
    Task<IReadOnlyList<Brand>> IBrandRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_brands, activeOnly);

    Task IModelRepository.AddAsync(Model entity, CancellationToken cancellationToken)
        => Add(_models, entity);
    Task<Model?> IModelRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_models, id);
    Task<Model?> IModelRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_models, code);
    Task<IReadOnlyList<Model>> IModelRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_models, activeOnly);
    Task<IReadOnlyList<Model>> IModelRepository.ListByBrandIdAsync(
        Guid brandId, bool activeOnly, CancellationToken cancellationToken)
        => ListWhere(_models, activeOnly, x => x.BrandId == brandId);

    Task ICountryRepository.AddAsync(Country entity, CancellationToken cancellationToken)
        => Add(_countries, entity);
    Task<Country?> ICountryRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_countries, id);
    Task<Country?> ICountryRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_countries, code);
    Task<IReadOnlyList<Country>> ICountryRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_countries, activeOnly);

    Task ICityRepository.AddAsync(City entity, CancellationToken cancellationToken)
        => Add(_cities, entity);
    Task<City?> ICityRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_cities, id);
    Task<City?> ICityRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_cities, code);
    Task<IReadOnlyList<City>> ICityRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_cities, activeOnly);
    Task<IReadOnlyList<City>> ICityRepository.ListByCountryIdAsync(
        Guid countryId, bool activeOnly, CancellationToken cancellationToken)
        => ListWhere(_cities, activeOnly, x => x.CountryId == countryId);

    Task IDistrictRepository.AddAsync(District entity, CancellationToken cancellationToken)
        => Add(_districts, entity);
    Task<District?> IDistrictRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_districts, id);
    Task<District?> IDistrictRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_districts, code);
    Task<IReadOnlyList<District>> IDistrictRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_districts, activeOnly);
    Task<IReadOnlyList<District>> IDistrictRepository.ListByCityIdAsync(
        Guid cityId, bool activeOnly, CancellationToken cancellationToken)
        => ListWhere(_districts, activeOnly, x => x.CityId == cityId);

    Task ICurrencyRepository.AddAsync(Currency entity, CancellationToken cancellationToken)
        => Add(_currencies, entity);
    Task<Currency?> ICurrencyRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_currencies, id);
    Task<Currency?> ICurrencyRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_currencies, code);
    Task<IReadOnlyList<Currency>> ICurrencyRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_currencies, activeOnly);

    Task ILanguageRepository.AddAsync(Language entity, CancellationToken cancellationToken)
        => Add(_languages, entity);
    Task<Language?> ILanguageRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_languages, id);
    Task<Language?> ILanguageRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_languages, code);
    Task<IReadOnlyList<Language>> ILanguageRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_languages, activeOnly);

    Task ITransmissionRepository.AddAsync(Transmission entity, CancellationToken cancellationToken)
        => Add(_transmissions, entity);
    Task<Transmission?> ITransmissionRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_transmissions, id);
    Task<Transmission?> ITransmissionRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_transmissions, code);
    Task<IReadOnlyList<Transmission>> ITransmissionRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_transmissions, activeOnly);

    Task IFuelTypeRepository.AddAsync(FuelType entity, CancellationToken cancellationToken)
        => Add(_fuelTypes, entity);
    Task<FuelType?> IFuelTypeRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_fuelTypes, id);
    Task<FuelType?> IFuelTypeRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_fuelTypes, code);
    Task<IReadOnlyList<FuelType>> IFuelTypeRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_fuelTypes, activeOnly);

    Task IVehicleTypeRepository.AddAsync(VehicleType entity, CancellationToken cancellationToken)
        => Add(_vehicleTypes, entity);
    Task<VehicleType?> IVehicleTypeRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_vehicleTypes, id);
    Task<VehicleType?> IVehicleTypeRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_vehicleTypes, code);
    Task<IReadOnlyList<VehicleType>> IVehicleTypeRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_vehicleTypes, activeOnly);

    Task IEquipmentTypeRepository.AddAsync(EquipmentType entity, CancellationToken cancellationToken)
        => Add(_equipmentTypes, entity);
    Task<EquipmentType?> IEquipmentTypeRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_equipmentTypes, id);
    Task<EquipmentType?> IEquipmentTypeRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_equipmentTypes, code);
    Task<IReadOnlyList<EquipmentType>> IEquipmentTypeRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_equipmentTypes, activeOnly);

    Task IFeatureDefinitionRepository.AddAsync(FeatureDefinition entity, CancellationToken cancellationToken)
        => Add(_features, entity);
    Task<FeatureDefinition?> IFeatureDefinitionRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_features, id);
    Task<FeatureDefinition?> IFeatureDefinitionRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_features, code);
    Task<IReadOnlyList<FeatureDefinition>> IFeatureDefinitionRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_features, activeOnly);

    Task IColorRepository.AddAsync(Color entity, CancellationToken cancellationToken)
        => Add(_colors, entity);
    Task<Color?> IColorRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_colors, id);
    Task<Color?> IColorRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_colors, code);
    Task<IReadOnlyList<Color>> IColorRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_colors, activeOnly);

    Task IDocumentTypeRepository.AddAsync(DocumentType entity, CancellationToken cancellationToken)
        => Add(_documentTypes, entity);
    Task<DocumentType?> IDocumentTypeRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_documentTypes, id);
    Task<DocumentType?> IDocumentTypeRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_documentTypes, code);
    Task<IReadOnlyList<DocumentType>> IDocumentTypeRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_documentTypes, activeOnly);

    Task IMediaTypeRepository.AddAsync(MediaType entity, CancellationToken cancellationToken)
        => Add(_mediaTypes, entity);
    Task<MediaType?> IMediaTypeRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_mediaTypes, id);
    Task<MediaType?> IMediaTypeRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_mediaTypes, code);
    Task<IReadOnlyList<MediaType>> IMediaTypeRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_mediaTypes, activeOnly);

    Task IRentalPeriodTypeRepository.AddAsync(RentalPeriodType entity, CancellationToken cancellationToken)
        => Add(_rentalPeriodTypes, entity);
    Task<RentalPeriodType?> IRentalPeriodTypeRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_rentalPeriodTypes, id);
    Task<RentalPeriodType?> IRentalPeriodTypeRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_rentalPeriodTypes, code);
    Task<IReadOnlyList<RentalPeriodType>> IRentalPeriodTypeRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_rentalPeriodTypes, activeOnly);

    Task IPaymentMethodRepository.AddAsync(PaymentMethod entity, CancellationToken cancellationToken)
        => Add(_paymentMethods, entity);
    Task<PaymentMethod?> IPaymentMethodRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_paymentMethods, id);
    Task<PaymentMethod?> IPaymentMethodRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_paymentMethods, code);
    Task<IReadOnlyList<PaymentMethod>> IPaymentMethodRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_paymentMethods, activeOnly);

    Task IBookingStatusRepository.AddAsync(BookingStatus entity, CancellationToken cancellationToken)
        => Add(_bookingStatuses, entity);
    Task<BookingStatus?> IBookingStatusRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_bookingStatuses, id);
    Task<BookingStatus?> IBookingStatusRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_bookingStatuses, code);
    Task<IReadOnlyList<BookingStatus>> IBookingStatusRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_bookingStatuses, activeOnly);

    Task IAssetStatusRepository.AddAsync(AssetStatus entity, CancellationToken cancellationToken)
        => Add(_assetStatuses, entity);
    Task<AssetStatus?> IAssetStatusRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_assetStatuses, id);
    Task<AssetStatus?> IAssetStatusRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_assetStatuses, code);
    Task<IReadOnlyList<AssetStatus>> IAssetStatusRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_assetStatuses, activeOnly);

    Task IReviewStatusRepository.AddAsync(ReviewStatus entity, CancellationToken cancellationToken)
        => Add(_reviewStatuses, entity);
    Task<ReviewStatus?> IReviewStatusRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetById(_reviewStatuses, id);
    Task<ReviewStatus?> IReviewStatusRepository.GetByCodeAsync(string code, CancellationToken cancellationToken)
        => GetByCode(_reviewStatuses, code);
    Task<IReadOnlyList<ReviewStatus>> IReviewStatusRepository.ListAsync(bool activeOnly, CancellationToken cancellationToken)
        => List(_reviewStatuses, activeOnly);

    private static Task Add<T>(ConcurrentDictionary<Guid, T> bag, T entity)
        where T : CatalogEntity
    {
        if (!bag.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException($"Catalog item '{entity.Code}' already exists.");
        }

        return Task.CompletedTask;
    }

    private static Task<T?> GetById<T>(ConcurrentDictionary<Guid, T> bag, Guid id)
        where T : CatalogEntity
    {
        if (!bag.TryGetValue(id, out var entity) || entity.IsDeleted)
        {
            return Task.FromResult<T?>(null);
        }

        return Task.FromResult<T?>(entity);
    }

    private static Task<T?> GetByCode<T>(ConcurrentDictionary<Guid, T> bag, string code)
        where T : CatalogEntity
    {
        var normalized = code.Trim().ToUpperInvariant();
        var match = bag.Values.FirstOrDefault(x => !x.IsDeleted && x.Code == normalized);
        return Task.FromResult(match);
    }

    private static Task<IReadOnlyList<T>> List<T>(ConcurrentDictionary<Guid, T> bag, bool activeOnly)
        where T : CatalogEntity
    {
        IReadOnlyList<T> items = bag.Values
            .Where(x => !x.IsDeleted)
            .Where(x => !activeOnly || x.IsActive)
            .ToArray();
        return Task.FromResult(items);
    }

    private static Task<IReadOnlyList<T>> ListWhere<T>(
        ConcurrentDictionary<Guid, T> bag,
        bool activeOnly,
        Func<T, bool> predicate)
        where T : CatalogEntity
    {
        IReadOnlyList<T> items = bag.Values
            .Where(x => !x.IsDeleted)
            .Where(x => !activeOnly || x.IsActive)
            .Where(predicate)
            .ToArray();
        return Task.FromResult(items);
    }
}
