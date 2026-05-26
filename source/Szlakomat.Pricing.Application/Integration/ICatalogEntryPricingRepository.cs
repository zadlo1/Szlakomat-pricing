namespace Szlakomat.Pricing.Application.Integration;

/// <summary>
/// Mapowanie wpisu katalogowego (CommercialOffer) na komponent cennika pricingu.
/// </summary>
public interface ICatalogEntryPricingRepository
{
    string? FindComponentName(string catalogEntryId);

    void Save(CatalogEntryPricing mapping);
}

public sealed record CatalogEntryPricing(string CatalogEntryId, string ComponentName);
