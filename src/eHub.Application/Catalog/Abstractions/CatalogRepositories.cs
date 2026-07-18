using eHub.Domain.Catalog;

namespace eHub.Application.Catalog.Abstractions;

public interface ICategoryRepository
{
    Task AddAsync(Category entity, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface ISubCategoryRepository
{
    Task AddAsync(SubCategory entity, CancellationToken cancellationToken = default);
    Task<SubCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubCategory?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubCategory>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubCategory>> ListByCategoryIdAsync(
        Guid categoryId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}

public interface IBrandRepository
{
    Task AddAsync(Brand entity, CancellationToken cancellationToken = default);
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Brand>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IModelRepository
{
    Task AddAsync(Model entity, CancellationToken cancellationToken = default);
    Task<Model?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Model?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Model>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Model>> ListByBrandIdAsync(
        Guid brandId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}

public interface ICountryRepository
{
    Task AddAsync(Country entity, CancellationToken cancellationToken = default);
    Task<Country?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Country?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Country>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface ICityRepository
{
    Task AddAsync(City entity, CancellationToken cancellationToken = default);
    Task<City?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<City?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<City>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<City>> ListByCountryIdAsync(
        Guid countryId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}

public interface IDistrictRepository
{
    Task AddAsync(District entity, CancellationToken cancellationToken = default);
    Task<District?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<District?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<District>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<District>> ListByCityIdAsync(
        Guid cityId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}

public interface ICurrencyRepository
{
    Task AddAsync(Currency entity, CancellationToken cancellationToken = default);
    Task<Currency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Currency>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface ILanguageRepository
{
    Task AddAsync(Language entity, CancellationToken cancellationToken = default);
    Task<Language?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Language>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface ITransmissionRepository
{
    Task AddAsync(Transmission entity, CancellationToken cancellationToken = default);
    Task<Transmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transmission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transmission>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IFuelTypeRepository
{
    Task AddAsync(FuelType entity, CancellationToken cancellationToken = default);
    Task<FuelType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FuelType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FuelType>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IVehicleTypeRepository
{
    Task AddAsync(VehicleType entity, CancellationToken cancellationToken = default);
    Task<VehicleType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VehicleType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehicleType>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IEquipmentTypeRepository
{
    Task AddAsync(EquipmentType entity, CancellationToken cancellationToken = default);
    Task<EquipmentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EquipmentType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EquipmentType>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IFeatureDefinitionRepository
{
    Task AddAsync(FeatureDefinition entity, CancellationToken cancellationToken = default);
    Task<FeatureDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FeatureDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeatureDefinition>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IColorRepository
{
    Task AddAsync(Color entity, CancellationToken cancellationToken = default);
    Task<Color?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Color?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Color>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IDocumentTypeRepository
{
    Task AddAsync(DocumentType entity, CancellationToken cancellationToken = default);
    Task<DocumentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentType>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IMediaTypeRepository
{
    Task AddAsync(MediaType entity, CancellationToken cancellationToken = default);
    Task<MediaType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MediaType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaType>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IRentalPeriodTypeRepository
{
    Task AddAsync(RentalPeriodType entity, CancellationToken cancellationToken = default);
    Task<RentalPeriodType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RentalPeriodType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RentalPeriodType>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IPaymentMethodRepository
{
    Task AddAsync(PaymentMethod entity, CancellationToken cancellationToken = default);
    Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentMethod>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IBookingStatusRepository
{
    Task AddAsync(BookingStatus entity, CancellationToken cancellationToken = default);
    Task<BookingStatus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BookingStatus?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingStatus>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IAssetStatusRepository
{
    Task AddAsync(AssetStatus entity, CancellationToken cancellationToken = default);
    Task<AssetStatus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetStatus?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetStatus>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}

public interface IReviewStatusRepository
{
    Task AddAsync(ReviewStatus entity, CancellationToken cancellationToken = default);
    Task<ReviewStatus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReviewStatus?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewStatus>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
}
