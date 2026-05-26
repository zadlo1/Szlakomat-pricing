using MediatR;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Pricing.Application.CalculatePrice;

public sealed record CalculatePrice(
    string CatalogEntryId,
    DateOnly VisitDate,
    string CustomerType,
    int GroupSize,
    bool IncludeBreakdown = false) : IRequest<Result<string, CalculatePriceResult>>;
