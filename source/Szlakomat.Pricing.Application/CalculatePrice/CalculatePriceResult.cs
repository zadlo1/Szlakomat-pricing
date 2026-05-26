namespace Szlakomat.Pricing.Application.CalculatePrice;

public sealed record CalculatePriceResult(
    decimal Amount,
    string Currency,
    PriceBreakdownView? Breakdown);

public sealed record PriceBreakdownView(
    string ComponentName,
    decimal Amount,
    string Currency,
    IReadOnlyList<PriceBreakdownView> Children);
