using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Components;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Tests;

public class CompositeComponentTests
{
    private static readonly DateOnly SampleDate = new(2025, 7, 15);

    private static SimpleComponent FixedLeaf(string name, Money unitPrice, IApplicabilityConstraint? applicability = null) =>
        new(
            Guid.NewGuid(),
            name,
            new SimpleFixedCalculator(Guid.NewGuid(), $"{name}_calc", unitPrice),
            applicability);

    [Fact]
    public void Calculate_WhenParentApplicabilityFails_ShouldReturnZeroWithoutEvaluatingChildren()
    {
        // Arrange
        var composite = new CompositeComponent(
            Guid.NewGuid(),
            "parent",
            [FixedLeaf("child", Money.Pln(47m))],
            ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard));
        var context = PricingContext.For(SampleDate, CustomerTypes.Senior);

        // Act
        var result = composite.Calculate(PricingParameters.Empty(), context);

        // Assert
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WhenNoChildApplicable_ShouldReturnZero()
    {
        // Arrange
        var composite = new CompositeComponent(
            Guid.NewGuid(),
            "ticket",
            [
                FixedLeaf(
                    "standard",
                    Money.Pln(47m),
                    ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard)),
                FixedLeaf(
                    "reduced",
                    Money.Pln(35m),
                    ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Senior)),
            ]);
        var context = PricingContext.For(SampleDate, CustomerTypes.B2B);

        // Act
        var result = composite.Calculate(PricingParameters.Empty(), context);

        // Assert
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WhenOneChildApplicable_ShouldReturnThatChildTotal()
    {
        // Arrange
        var composite = new CompositeComponent(
            Guid.NewGuid(),
            "ticket",
            [
                FixedLeaf(
                    "standard",
                    Money.Pln(47m),
                    ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard)),
                FixedLeaf(
                    "reduced",
                    Money.Pln(35m),
                    ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Senior)),
            ]);
        var context = PricingContext.For(SampleDate, CustomerTypes.Senior);

        // Act
        var result = composite.Calculate(PricingParameters.Empty(), context);

        // Assert
        result.Should().Be(Money.Pln(35m));
    }

    [Fact]
    public void Calculate_WithParameterDependency_ShouldInjectBaseAmountFromPriorChildren()
    {
        // Arrange
        var net = FixedLeaf(
            "net",
            Money.Pln(100m),
            ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard));
        var vat = new SimpleComponent(
            Guid.NewGuid(),
            "vat",
            new PercentageCalculator(Guid.NewGuid(), "VAT", 8m),
            ApplicabilityConstraint.AlwaysTrue());

        var composite = new CompositeComponent(
            Guid.NewGuid(),
            "ticket_with_vat",
            [net, vat],
            parameterDependencies:
            [
                new ParameterDependency(
                    "vat",
                    PercentageCalculator.BaseAmountKey,
                    ["net"]),
            ]);

        var context = PricingContext.For(SampleDate, CustomerTypes.Standard);

        // Act
        var result = composite.Calculate(PricingParameters.Empty(), context);

        // Assert
        result.Should().Be(Money.Pln(108m));
    }

    [Fact]
    public void CalculateBreakdown_ShouldIncludeOnlyApplicableChildren()
    {
        // Arrange
        var composite = new CompositeComponent(
            Guid.NewGuid(),
            "ticket",
            [
                FixedLeaf(
                    "standard",
                    Money.Pln(47m),
                    ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Standard)),
                FixedLeaf(
                    "reduced",
                    Money.Pln(35m),
                    ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, CustomerTypes.Senior)),
            ]);
        var context = PricingContext.For(SampleDate, CustomerTypes.Standard);

        // Act
        var breakdown = composite.CalculateBreakdown(PricingParameters.Empty(), context);

        // Assert
        breakdown.ComponentName.Should().Be("ticket");
        breakdown.Total.Should().Be(Money.Pln(47m));
        breakdown.Children.Should().ContainSingle(c => c.ComponentName == "standard");
    }
}
