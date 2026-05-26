using CommunityToolkit.Diagnostics;
using Szlakomat.Pricing.Application.Integration;

namespace Szlakomat.Pricing.Infrastructure.Integration;

internal sealed class InMemoryCatalogEntryPricingRepository : ICatalogEntryPricingRepository
{
    private readonly Dictionary<string, string> _byCatalogEntryId = new(StringComparer.Ordinal);

    public string? FindComponentName(string catalogEntryId)
    {
        Guard.IsNotNullOrWhiteSpace(catalogEntryId);
        return _byCatalogEntryId.TryGetValue(catalogEntryId, out var componentName)
            ? componentName
            : null;
    }

    public void Save(CatalogEntryPricing mapping)
    {
        Guard.IsNotNull(mapping);
        Guard.IsNotNullOrWhiteSpace(mapping.CatalogEntryId);
        Guard.IsNotNullOrWhiteSpace(mapping.ComponentName);
        _byCatalogEntryId[mapping.CatalogEntryId] = mapping.ComponentName;
    }
}
