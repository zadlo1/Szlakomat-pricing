using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Components.Versioning;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Facade;
using Szlakomat.Pricing.Domain.Parameters;
using Szlakomat.Pricing.Domain.Seed;

namespace Szlakomat.Pricing.Domain.Tests;

/// <summary>
/// Journey testy Etapu 3 — wersjonowanie komponentów i obliczenia historyczne.
/// </summary>
public class HistoricalCalculationTests
{
    private static readonly PricingParameters EmptyParams = PricingParameters.Empty();

    [Fact]
    public void Calculate_WhenVisitBeforePriceIncrease_ShouldUseOldPrice()
    {
        // Arrange
        var facade = WawelSkarbiecTicketSeed.Seed(new PricingFacade());
        facade
            .AddFixedCalculator("skarbiec_standard_unit_v2", Money.Pln(52m))
            .UpdateSimpleComponent(
                "skarbiec_standard",
                "skarbiec_standard_unit_v2",
                Validity.FromDate(new DateOnly(2026, 1, 1)),
                ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard),
                definedAt: new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));

        var beforeIncrease = PricingContext.Individual(new DateOnly(2025, 7, 15));
        var afterIncrease = PricingContext.Individual(new DateOnly(2026, 2, 1));

        // Act
        var oldPrice = facade.CalculateComponent(
            WawelSkarbiecTicketSeed.WawelTicketComponent,
            EmptyParams,
            beforeIncrease);
        var newPrice = facade.CalculateComponent(
            WawelSkarbiecTicketSeed.WawelTicketComponent,
            EmptyParams,
            afterIncrease);

        // Assert
        oldPrice.Should().Be(Money.Pln(47m));
        newPrice.Should().Be(Money.Pln(52m));
    }

    [Fact]
    public void Calculate_WhenPromotionExpired_ShouldThrowNoActiveVersion()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("promo_unit", Money.Pln(10m))
            .CreateSimpleComponent(
                "summer_promo",
                "promo_unit",
                validity: Validity.Between(new DateOnly(2025, 6, 1), new DateOnly(2025, 8, 31)),
                definedAt: new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc));

        var afterPromo = PricingContext.Individual(new DateOnly(2025, 9, 1));

        // Act
        var act = () => facade.CalculateComponent("summer_promo", EmptyParams, afterPromo);

        // Assert
        act.Should().Throw<NoActiveComponentVersionException>()
            .Which.ComponentName.Should().Be("summer_promo");
    }

    [Fact]
    public void Calculate_WhenOverlappingVersionsWithSameValidFrom_ShouldPickLatestDefinedAt()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("price_v1", Money.Pln(40m))
            .AddFixedCalculator("price_v2", Money.Pln(45m))
            .CreateSimpleComponent(
                "tiered",
                "price_v1",
                validity: Validity.FromDate(new DateOnly(2025, 1, 1)),
                definedAt: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        facade.UpdateSimpleComponent(
            "tiered",
            "price_v2",
            Validity.FromDate(new DateOnly(2025, 1, 1)),
            definedAt: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            strategy: VersionAdditionStrategy.AllowAll);

        var context = PricingContext.Individual(new DateOnly(2025, 7, 1));

        // Act
        var result = facade.CalculateComponent("tiered", EmptyParams, context);

        // Assert
        result.Should().Be(Money.Pln(45m));
    }

    [Fact]
    public void UpdateSimpleComponent_WhenIdenticalVersion_ShouldRejectByDefault()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("fixed", Money.Pln(20m))
            .CreateSimpleComponent("item", "fixed");

        // Act
        var act = () => facade.UpdateSimpleComponent(
            "item",
            "fixed",
            Validity.Always(),
            definedAt: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        act.Should().Throw<DuplicateComponentVersionException>();
    }

    [Fact]
    public void UpdateSimpleComponent_WhenAllowAllStrategy_ShouldAddDuplicateVersion()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("fixed", Money.Pln(20m))
            .CreateSimpleComponent("item", "fixed");

        // Act
        facade.UpdateSimpleComponent(
            "item",
            "fixed",
            Validity.Always(),
            strategy: VersionAdditionStrategy.AllowAll);

        // Assert
        facade.ListComponents()
            .Single(c => c.Name == "item")
            .VersionCount
            .Should()
            .Be(2);
    }

    [Fact]
    public void Calculate_WhenChildComponentIsVersioned_ShouldResolveChildVersionForVisitDate()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("leaf_v1", Money.Pln(30m))
            .AddFixedCalculator("leaf_v2", Money.Pln(35m))
            .CreateSimpleComponent(
                "leaf",
                "leaf_v1",
                definedAt: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        facade.UpdateSimpleComponent(
            "leaf",
            "leaf_v2",
            Validity.FromDate(new DateOnly(2026, 1, 1)),
            definedAt: new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));

        facade.CreateCompositeComponent("bundle", ["leaf"]);

        var before = PricingContext.Individual(new DateOnly(2025, 7, 1));
        var after = PricingContext.Individual(new DateOnly(2026, 2, 1));

        // Act
        var oldTotal = facade.CalculateComponent("bundle", EmptyParams, before);
        var newTotal = facade.CalculateComponent("bundle", EmptyParams, after);

        // Assert
        oldTotal.Should().Be(Money.Pln(30m));
        newTotal.Should().Be(Money.Pln(35m));
    }

    [Fact]
    public void CalculateBreakdown_AfterPriceChange_ShouldReflectActiveVersion()
    {
        // Arrange
        var facade = WawelSkarbiecTicketSeed.Seed(new PricingFacade());
        facade
            .AddFixedCalculator("skarbiec_standard_unit_v2", Money.Pln(52m))
            .UpdateSimpleComponent(
                "skarbiec_standard",
                "skarbiec_standard_unit_v2",
                Validity.FromDate(new DateOnly(2026, 1, 1)),
                ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard),
                definedAt: new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));

        var context = PricingContext.Individual(new DateOnly(2026, 3, 1));

        // Act
        var breakdown = facade.CalculateComponentBreakdown(
            WawelSkarbiecTicketSeed.WawelTicketComponent,
            EmptyParams,
            context);

        // Assert
        breakdown.Total.Should().Be(Money.Pln(52m));
        breakdown.Children.Should().ContainSingle(c => c.ComponentName == "skarbiec_standard");
    }
}
