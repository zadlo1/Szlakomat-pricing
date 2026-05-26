namespace Szlakomat.Products.Api.Contracts.Pricing;

public sealed record PriceBreakdownResponse(
    string ComponentName,
    decimal Amount,
    string Currency,
    IReadOnlyList<PriceBreakdownResponse> Children);
