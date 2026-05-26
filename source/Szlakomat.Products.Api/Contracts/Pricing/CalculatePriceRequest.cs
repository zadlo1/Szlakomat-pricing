namespace Szlakomat.Products.Api.Contracts.Pricing;

public sealed record CalculatePriceRequest(
    string CatalogEntryId,
    string VisitDate,
    string CustomerType,
    int GroupSize = 1,
    bool IncludeBreakdown = false);
