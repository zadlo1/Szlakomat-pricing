using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Szlakomat.Pricing.Application.Tests.Helpers;
using Szlakomat.Pricing.Application.Tests.Infrastructure;
using Szlakomat.Pricing.Domain.Context;
using Xunit;
using CalculatePriceCommand = Szlakomat.Pricing.Application.CalculatePrice.CalculatePrice;

namespace Szlakomat.Pricing.Application.Tests.CalculatePrice;

/// <summary>
/// Journey testy Etapu 4 — obliczanie ceny przez MediatR (jak z API).
/// </summary>
public class WawelPricingJourneyTests
{
    private static readonly DateOnly VisitDate = new(2025, 7, 15);
    private readonly IMediator _mediator;

    public WawelPricingJourneyTests()
    {
        _mediator = ServiceProviderFactory.Create().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task TouristStandardTicket_Returns47Pln()
    {
        var catalogEntry = await CatalogEntryTestHelper.FindSkarbiecCatalogEntryAsync(_mediator);

        var result = await _mediator.Send(new CalculatePriceCommand(
            catalogEntry.CatalogEntryId,
            VisitDate,
            CustomerTypes.Standard,
            1));

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Amount.Should().Be(47m);
        result.SuccessValue.Currency.Should().Be("PLN");
    }

    [Fact]
    public async Task SeniorTicket_ReturnsReducedPrice()
    {
        var catalogEntry = await CatalogEntryTestHelper.FindSkarbiecCatalogEntryAsync(_mediator);

        var result = await _mediator.Send(new CalculatePriceCommand(
            catalogEntry.CatalogEntryId,
            VisitDate,
            CustomerTypes.Senior,
            1));

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Amount.Should().Be(35m);
    }

    [Fact]
    public async Task B2BGroupOf15_ReturnsDiscountedTotal()
    {
        var catalogEntry = await CatalogEntryTestHelper.FindSkarbiecCatalogEntryAsync(_mediator);
        var expected = 47m * 0.85m * 15m;

        var result = await _mediator.Send(new CalculatePriceCommand(
            catalogEntry.CatalogEntryId,
            VisitDate,
            CustomerTypes.B2B,
            15));

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Amount.Should().Be(expected);
    }

    [Fact]
    public async Task B2BIndividualGroup_ReturnsZeroAmount()
    {
        var catalogEntry = await CatalogEntryTestHelper.FindSkarbiecCatalogEntryAsync(_mediator);

        var result = await _mediator.Send(new CalculatePriceCommand(
            catalogEntry.CatalogEntryId,
            VisitDate,
            CustomerTypes.B2B,
            1));

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Amount.Should().Be(0m);
    }

    [Fact]
    public async Task UnknownCatalogEntry_ReturnsFailure()
    {
        var result = await _mediator.Send(new CalculatePriceCommand(
            "CATALOG-unknown-entry",
            VisitDate,
            CustomerTypes.Standard,
            1));

        result.IsFailure().Should().BeTrue();
        result.GetFailure().Should().Contain("not found");
    }

    [Fact]
    public async Task CatalogEntryWithoutPricingMapping_ReturnsFailure()
    {
        var entries = await _mediator.Send(
            Products.Application.CommercialOffer.SearchCatalog.SearchCatalogCriteria.All());

        var unmapped = entries.First(e =>
            !e.DisplayName.Contains("Skarbiec Koronny", StringComparison.Ordinal));

        var result = await _mediator.Send(new CalculatePriceCommand(
            unmapped.CatalogEntryId,
            VisitDate,
            CustomerTypes.Standard,
            1));

        result.IsFailure().Should().BeTrue();
        result.GetFailure().Should().Contain("not configured");
    }

    [Fact]
    public async Task WithBreakdown_ReturnsComponentTree()
    {
        var catalogEntry = await CatalogEntryTestHelper.FindSkarbiecCatalogEntryAsync(_mediator);

        var result = await _mediator.Send(new CalculatePriceCommand(
            catalogEntry.CatalogEntryId,
            VisitDate,
            CustomerTypes.Standard,
            1,
            IncludeBreakdown: true));

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Breakdown.Should().NotBeNull();
        result.SuccessValue.Breakdown!.ComponentName.Should().Be("wawel_ticket");
        result.SuccessValue.Breakdown.Children.Should().Contain(c => c.ComponentName == "skarbiec_standard");
    }
}
