using Szlakomat.Pricing.Domain.Components;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Facade;
using Szlakomat.Pricing.Domain.Parameters;
using Szlakomat.Pricing.Domain.Seed;

namespace Szlakomat.Pricing.Domain.Tests;

/// <summary>
/// Journey testy Etapu 2 — scenariusze Wawelu przez PricingFacade (bez MediatR).
/// </summary>
public class WawelTicketPricingJourneyTests
{
    private static readonly DateOnly VisitDate = new(2025, 7, 15);
    private static readonly PricingParameters EmptyParams = PricingParameters.Empty();

    private static PricingFacade CreateSeededFacade() =>
        WawelSkarbiecTicketSeed.Seed(new PricingFacade());

    [Fact]
    public void TouristBuysStandardTicket_GetsCorrectPrice()
    {
        // Arrange
        var facade = CreateSeededFacade();
        var context = PricingContext.Individual(VisitDate);

        // Act
        var total = facade.CalculateComponent(WawelSkarbiecTicketSeed.WawelTicketComponent, EmptyParams, context);

        // Assert
        total.Should().Be(Money.Pln(47m));
    }

    [Fact]
    public void SeniorBuysTicket_GetsReducedPrice()
    {
        // Arrange
        var facade = CreateSeededFacade();
        var context = PricingContext.For(VisitDate, CustomerTypes.Senior);

        // Act
        var total = facade.CalculateComponent(WawelSkarbiecTicketSeed.WawelTicketComponent, EmptyParams, context);

        // Assert
        total.Should().Be(Money.Pln(35m));
    }

    [Fact]
    public void B2BGroupOf15_GetsDiscountedPrice()
    {
        // Arrange
        var facade = CreateSeededFacade();
        var context = PricingContext.ForGroup(VisitDate, CustomerTypes.B2B, 15);
        var expected = Money.Pln(47m).Multiply(0.85m).Multiply(15m);

        // Act
        var total = facade.CalculateComponent(
            WawelSkarbiecTicketSeed.WawelTicketComponent,
            EmptyParams,
            context);

        // Assert
        total.Should().Be(expected);
    }

    [Fact]
    public void ComponentNotApplicable_ReturnsZero()
    {
        // Arrange
        var facade = CreateSeededFacade();
        var context = PricingContext.For(VisitDate, CustomerTypes.B2B);

        // Act — B2B wymaga groupSize > 9
        var total = facade.CalculateComponent(WawelSkarbiecTicketSeed.WawelTicketComponent, EmptyParams, context);

        // Assert
        total.Amount.Should().Be(0m);
    }

    [Fact]
    public void CompositeWithNoApplicableChild_ReturnsZero()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("orphan_unit", Money.Pln(10m))
            .CreateSimpleComponent(
                "orphan",
                "orphan_unit",
                Szlakomat.Products.Domain.Common.Applicability.ApplicabilityConstraint.EqualsTo(
                    ApplicabilityKeys.CustomerType,
                    CustomerTypes.Standard))
            .CreateCompositeComponent("empty_ticket", ["orphan"]);
        var context = PricingContext.For(VisitDate, CustomerTypes.Senior);

        // Act
        var total = facade.CalculateComponent("empty_ticket", EmptyParams, context);

        // Assert
        total.Amount.Should().Be(0m);
    }

    [Fact]
    public void BreakdownTree_ContainsCorrectComponents()
    {
        // Arrange
        var facade = CreateSeededFacade();
        var context = PricingContext.For(VisitDate, CustomerTypes.Senior);

        // Act
        var breakdown = facade.CalculateComponentBreakdown(
            WawelSkarbiecTicketSeed.WawelTicketComponent,
            EmptyParams,
            context);

        // Assert
        breakdown.ComponentName.Should().Be(WawelSkarbiecTicketSeed.WawelTicketComponent);
        breakdown.Total.Should().Be(Money.Pln(35m));
        breakdown.Children.Should().ContainSingle(c => c.ComponentName == "skarbiec_reduced");
    }

    [Fact]
    public void ParameterDependency_VatCalculatedFromNettoSum()
    {
        // Arrange
        var facade = CreateSeededFacade();
        var context = PricingContext.Individual(VisitDate);
        var expectedNet = Money.Pln(47m);
        var expectedVat = expectedNet.Multiply(0.08m);
        var expectedTotal = expectedNet.Add(expectedVat);

        // Act
        var total = facade.CalculateComponent(
            WawelSkarbiecTicketSeed.WawelTicketWithVatComponent,
            EmptyParams,
            context);

        // Assert
        total.Should().Be(expectedTotal);

        var breakdown = facade.CalculateComponentBreakdown(
            WawelSkarbiecTicketSeed.WawelTicketWithVatComponent,
            EmptyParams,
            context);

        breakdown.Children.Should().HaveCount(2);
        breakdown.Children.Should().Contain(c => c.ComponentName == "skarbiec_standard");
        breakdown.Children.Should().Contain(c => c.ComponentName == "vat_8");
    }

    [Fact]
    public void VatOnB2BBase_IsCalculatedFromB2BChildNode()
    {
        // Arrange
        var facade = CreateSeededFacade();
        var context = PricingContext.ForGroup(VisitDate, CustomerTypes.B2B, 15);

        var net = Money.Pln(47m).Multiply(0.85m).Multiply(15m); // (47 × quantity) × 0.85
        var vat = net.Multiply(0.08m);
        var expectedTotal = net.Add(vat);

        // Act
        var total = facade.CalculateComponent(
            WawelSkarbiecTicketSeed.WawelTicketWithVatComponent,
            EmptyParams,
            context);

        // Assert
        total.Should().Be(expectedTotal);

        var breakdown = facade.CalculateComponentBreakdown(
            WawelSkarbiecTicketSeed.WawelTicketWithVatComponent,
            EmptyParams,
            context);

        breakdown.Children.Should().HaveCount(2);
        breakdown.Children.Should().Contain(c => c.ComponentName == "skarbiec_b2b");
        breakdown.Children.Should().Contain(c => c.ComponentName == "vat_8");
    }

    [Fact]
    public void ListComponents_AfterSeed_ShouldIncludeWawelTree()
    {
        // Arrange
        var facade = CreateSeededFacade();

        // Act
        var components = facade.ListComponents();

        // Assert
        components.Should().Contain(c => c.Name == WawelSkarbiecTicketSeed.WawelTicketComponent);
        components.Should().Contain(c => c.Name == "skarbiec_standard");
        components.Should().Contain(c => c.Name == "vat_8");
    }
}
