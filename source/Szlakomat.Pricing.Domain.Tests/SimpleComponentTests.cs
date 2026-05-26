using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Components;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Tests;

public class SimpleComponentTests
{
    private static readonly DateOnly SampleDate = new(2025, 7, 15);

    [Fact]
    public void Calculate_WhenValidityExpired_ShouldReturnZero()
    {
        // Arrange
        var calculator = new SimpleFixedCalculator(Guid.NewGuid(), "Skarbiec", Money.Pln(47m));
        var component = new SimpleComponent(
            Guid.NewGuid(),
            "skarbiec",
            calculator,
            validity: Validity.Between(new DateOnly(2025, 1, 1), new DateOnly(2025, 6, 30)));
        var context = PricingContext.Individual(new DateOnly(2025, 7, 1));

        // Act
        var result = component.Calculate(PricingParameters.Empty(), context);

        // Assert
        result.Amount.Should().Be(0m);
        result.Currency.Should().Be("PLN");
    }

    [Fact]
    public void Calculate_WhenApplicabilityNotSatisfied_ShouldReturnZero()
    {
        // Arrange
        var calculator = new SimpleFixedCalculator(Guid.NewGuid(), "Skarbiec", Money.Pln(47m));
        var component = new SimpleComponent(
            Guid.NewGuid(),
            "skarbiec",
            calculator,
            ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard));
        var context = PricingContext.For(SampleDate, CustomerTypes.Senior);

        // Act
        var result = component.Calculate(PricingParameters.Empty(), context);

        // Assert
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WhenApplicable_ShouldReturnTotalForGroupSize()
    {
        // Arrange
        var calculator = new SimpleFixedCalculator(Guid.NewGuid(), "Skarbiec", Money.Pln(47m));
        var component = new SimpleComponent(
            Guid.NewGuid(),
            "skarbiec",
            calculator,
            ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard));
        var context = PricingContext.ForGroup(SampleDate, CustomerTypes.Standard, 3);

        // Act
        var result = component.Calculate(PricingParameters.Empty(), context);

        // Assert
        result.Should().Be(Money.Pln(141m));
    }

    [Fact]
    public void CalculateBreakdown_WhenNotApplicable_ShouldReturnZeroLeaf()
    {
        // Arrange
        var calculator = new SimpleFixedCalculator(Guid.NewGuid(), "Skarbiec", Money.Pln(47m));
        var component = new SimpleComponent(
            Guid.NewGuid(),
            "skarbiec",
            calculator,
            ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard));
        var context = PricingContext.For(SampleDate, CustomerTypes.Senior);

        // Act
        var breakdown = component.CalculateBreakdown(PricingParameters.Empty(), context);

        // Assert
        breakdown.ComponentName.Should().Be("skarbiec");
        breakdown.Total.Amount.Should().Be(0m);
        breakdown.IsLeaf.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WithParameterMappings_ShouldMapExternalKeyToCalculatorKey()
    {
        // Arrange
        var calculator = new PercentageCalculator(Guid.NewGuid(), "VAT", 8m);
        var component = new SimpleComponent(
            Guid.NewGuid(),
            "vat",
            calculator,
            parameterMappings: new Dictionary<string, string>
            {
                ["netto"] = PercentageCalculator.BaseAmountKey,
            });
        var context = PricingContext.Individual(SampleDate);
        var parameters = PricingParameters.Of("netto", Money.Pln(100m));

        // Act
        var result = component.Calculate(parameters, context);

        // Assert
        result.Should().Be(Money.Pln(8m));
    }
}
