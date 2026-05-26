using MediatR;
using Szlakomat.Products.Application.CommercialOffer.Common;
using Szlakomat.Products.Application.CommercialOffer.SearchCatalog;

namespace Szlakomat.Pricing.Application.Tests.Helpers;

internal static class CatalogEntryTestHelper
{
    private const string SkarbiecDisplayNameMarker = "Skarbiec Koronny";

    public static async Task<CatalogEntryView> FindSkarbiecCatalogEntryAsync(IMediator mediator)
    {
        var entries = await mediator.Send(SearchCatalogCriteria.All());
        return entries.First(e =>
            e.DisplayName.Contains(SkarbiecDisplayNameMarker, StringComparison.Ordinal));
    }
}
