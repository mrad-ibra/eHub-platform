namespace eHub.Application.Configuration;

public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    public CatalogSeedOptions Seed { get; set; } = new();
}

public sealed class CatalogSeedOptions
{
    public bool Enabled { get; set; } = true;
}
