using Szlakomat.Pricing.Application.CalculatePrice;
using Szlakomat.Products.Api.Contracts.Pricing;

namespace Szlakomat.Products.Api.Mappers;

internal static class PricingMapper
{
    public static CalculatePriceResponse ToResponse(CalculatePriceResult result) =>
        new(
            result.Amount,
            result.Currency,
            result.Breakdown is null ? null : ToBreakdownResponse(result.Breakdown));

    private static PriceBreakdownResponse ToBreakdownResponse(PriceBreakdownView view) =>
        new(
            view.ComponentName,
            view.Amount,
            view.Currency,
            view.Children.Select(ToBreakdownResponse)
                .ToList()
        );
}
