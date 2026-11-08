namespace Studium_Catalog.Services.Options;

/// <summary>
/// Catalog-wide settings bound from the <c>Catalog</c> configuration section.
/// </summary>
public class CatalogOptions
{
    public const string SectionName = "Catalog";

    /// <summary>
    /// IANA time zone in which room operating hours are expressed.
    /// </summary>
    public string TimeZone { get; set; } = "America/Sao_Paulo";
}
