namespace Szlakomat.Products.Api.Contracts.Pricing;

public sealed record CalculatePriceResponse(
    decimal Amount,
    string Currency,
    PriceBreakdownResponse? Breakdown);
