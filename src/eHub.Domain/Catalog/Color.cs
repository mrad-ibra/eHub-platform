namespace eHub.Domain.Catalog;

public sealed class Color : CatalogEntity
{
    public string? HexCode { get; private set; }

    private Color()
    {
    }

    public static Color Create(
        string code,
        string name,
        DateTime nowUtc,
        string? hexCode = null,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new Color
        {
            HexCode = NormalizeHex(hexCode)
        };
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }

    public void ChangeHexCode(string? hexCode, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        HexCode = NormalizeHex(hexCode);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    private static string? NormalizeHex(string? hexCode)
    {
        if (string.IsNullOrWhiteSpace(hexCode))
        {
            return null;
        }

        var value = hexCode.Trim();
        if (!value.StartsWith('#'))
        {
            value = "#" + value;
        }

        return value.ToUpperInvariant();
    }
}
