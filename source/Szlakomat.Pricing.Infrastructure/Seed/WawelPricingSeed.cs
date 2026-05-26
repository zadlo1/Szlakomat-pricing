using CommunityToolkit.Diagnostics;
using Szlakomat.Pricing.Application.Integration;
using Szlakomat.Pricing.Domain.Facade;
using Szlakomat.Pricing.Domain.Seed;
using Szlakomat.Products.Domain.CommercialOffer;

namespace Szlakomat.Pricing.Infrastructure.Seed;

/// <summary>
/// Seed pricingu Wawelu (Etap 4) — komponenty Skarbca + mapowanie z katalogu CommercialOffer.
/// Pełne drzewo 26 pozycji — Etap 5.
/// </summary>
internal static class WawelPricingSeed
{
    public const string SkarbiecCatalogDisplayNameMarker = "Skarbiec Koronny";

    public static PricingFacade SeedComponents(PricingFacade facade) =>
        WawelSkarbiecTicketSeed.Seed(facade);

    public static void SeedCatalogMappings(
        ICatalogEntryPricingRepository pricingRepository,
        ICatalogEntryRepository catalogRepository)
    {
        Guard.IsNotNull(pricingRepository);
        Guard.IsNotNull(catalogRepository);

        var skarbiecEntry = catalogRepository.FindAll()
            .FirstOrDefault(entry =>
                entry.DisplayName().Contains(SkarbiecCatalogDisplayNameMarker, StringComparison.Ordinal));

        if (skarbiecEntry is null)
            return;

        pricingRepository.Save(new CatalogEntryPricing(
            skarbiecEntry.Id().Value,
            WawelSkarbiecTicketSeed.WawelTicketComponent));
    }
}
