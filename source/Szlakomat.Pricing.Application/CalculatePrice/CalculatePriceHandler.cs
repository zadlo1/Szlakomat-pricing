using MediatR;
using Szlakomat.Pricing.Application.Integration;
using Szlakomat.Pricing.Domain.Components;
using Szlakomat.Pricing.Domain.Components.Versioning;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Facade;
using Szlakomat.Pricing.Domain.Parameters;
using Szlakomat.Products.Application.CommercialOffer.FindCatalogEntry;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Pricing.Application.CalculatePrice;

internal sealed class CalculatePriceHandler
    : IRequestHandler<CalculatePrice, Result<string, CalculatePriceResult>>
{
    private readonly PricingFacade _pricingFacade;
    private readonly ICatalogEntryPricingRepository _catalogEntryPricingRepository;
    private readonly IMediator _mediator;

    public CalculatePriceHandler(
        PricingFacade pricingFacade,
        ICatalogEntryPricingRepository catalogEntryPricingRepository,
        IMediator mediator)
    {
        _pricingFacade = pricingFacade;
        _catalogEntryPricingRepository = catalogEntryPricingRepository;
        _mediator = mediator;
    }

    public async Task<Result<string, CalculatePriceResult>> Handle(
        CalculatePrice request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CatalogEntryId))
            return Result<string, CalculatePriceResult>.FailureOf("Catalog entry id is required.");

        if (request.GroupSize < 1)
            return Result<string, CalculatePriceResult>.FailureOf("Group size must be at least 1.");

        var catalogEntry = await _mediator.Send(
            new FindCatalogEntryCriteria(request.CatalogEntryId),
            cancellationToken);

        if (catalogEntry is null)
            return Result<string, CalculatePriceResult>.FailureOf(
                $"Catalog entry not found: {request.CatalogEntryId}");

        var componentName = _catalogEntryPricingRepository.FindComponentName(request.CatalogEntryId);
        if (componentName is null)
            return Result<string, CalculatePriceResult>.FailureOf(
                $"Pricing is not configured for catalog entry: {request.CatalogEntryId}");

        var context = request.GroupSize == 1
            ? PricingContext.For(request.VisitDate, request.CustomerType)
            : PricingContext.ForGroup(request.VisitDate, request.CustomerType, request.GroupSize);

        try
        {
            var parameters = PricingParameters.Empty();
            var total = _pricingFacade.CalculateComponent(componentName, parameters, context);
            var breakdown = request.IncludeBreakdown
                ? _pricingFacade.CalculateComponentBreakdown(componentName, parameters, context)
                : null;

            return Result<string, CalculatePriceResult>.SuccessOf(
                new CalculatePriceResult(
                    total.Amount,
                    total.Currency,
                    breakdown is null ? null : ToView(breakdown)));
        }
        catch (NoActiveComponentVersionException ex)
        {
            return Result<string, CalculatePriceResult>.FailureOf(ex.Message);
        }
    }

    private static PriceBreakdownView ToView(PriceBreakdown breakdown) =>
        new(
            breakdown.ComponentName,
            breakdown.Total.Amount,
            breakdown.Total.Currency,
            breakdown.Children.Select(ToView).ToList());
}
