using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Products.Domain.Common.Applicability;

namespace Szlakomat.Pricing.Domain.Tests;

public class PricingContextTests
{
    private static readonly DateOnly SampleDate = new(2025, 8, 15);

    // ── Fabryki ───────────────────────────────────────────────────────────────

    [Fact]
    public void Individual_ShouldCreateStandardContextWithGroupSizeOne()
    {
        // Act
        var ctx = PricingContext.Individual(SampleDate);

        // Assert
        ctx.VisitDate.Should().Be(SampleDate);
        ctx.CustomerType.Should().Be(CustomerTypes.Standard);
        ctx.GroupSize.Should().Be(1);
    }

    [Fact]
    public void For_ShouldSetCustomerTypeWithGroupSizeOne()
    {
        // Act
        var ctx = PricingContext.For(SampleDate, CustomerTypes.Student);

        // Assert
        ctx.CustomerType.Should().Be(CustomerTypes.Student);
        ctx.GroupSize.Should().Be(1);
    }

    [Fact]
    public void ForGroup_ShouldSetAllFields()
    {
        // Act
        var ctx = PricingContext.ForGroup(SampleDate, CustomerTypes.B2B, 25);

        // Assert
        ctx.CustomerType.Should().Be(CustomerTypes.B2B);
        ctx.GroupSize.Should().Be(25);
        ctx.VisitDate.Should().Be(SampleDate);
    }

    [Fact]
    public void CustomerType_ShouldBeNormalizedToUpperCase()
    {
        // Act
        var ctx = PricingContext.For(SampleDate, "student");

        // Assert
        ctx.CustomerType.Should().Be("STUDENT");
    }

    [Fact]
    public void ForGroup_WithGroupSizeZero_ShouldThrow()
    {
        // Act
        var act = () => PricingContext.ForGroup(SampleDate, CustomerTypes.Standard, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Konwersja do Dictionary (most do ApplicabilityContext) ────────────────

    [Fact]
    public void ToApplicabilityDictionary_ShouldContainAllExpectedKeys()
    {
        // Arrange
        var ctx = PricingContext.ForGroup(SampleDate, CustomerTypes.Reduced, 5);

        // Act
        var dict = ctx.ToApplicabilityDictionary();

        // Assert
        dict.Should().ContainKey(ApplicabilityKeys.VisitDate);
        dict.Should().ContainKey(ApplicabilityKeys.CustomerType);
        dict.Should().ContainKey(ApplicabilityKeys.GroupSize);
    }

    [Fact]
    public void ToApplicabilityDictionary_VisitDate_ShouldBeFormattedAsIsoDate()
    {
        // Arrange
        var ctx = PricingContext.Individual(new DateOnly(2025, 12, 31));

        // Act
        var dict = ctx.ToApplicabilityDictionary();

        // Assert
        dict[ApplicabilityKeys.VisitDate].Should().Be("2025-12-31");
    }

    [Fact]
    public void ToApplicabilityDictionary_GroupSize_ShouldBeStringRepresentation()
    {
        // Arrange
        var ctx = PricingContext.ForGroup(SampleDate, CustomerTypes.Standard, 15);

        // Act
        var dict = ctx.ToApplicabilityDictionary();

        // Assert
        dict[ApplicabilityKeys.GroupSize].Should().Be("15");
    }

    // ── Kompatybilność z ApplicabilityContext ─────────────────────────────────

    [Fact]
    public void ToApplicabilityContext_ShouldBeCompatibleWithEqualsConstraint()
    {
        // Arrange
        var ctx = PricingContext.For(SampleDate, CustomerTypes.Student);
        var applicabilityCtx = ctx.ToApplicabilityContext();
        var constraint = ApplicabilityConstraint.EqualsTo(ApplicabilityKeys.CustomerType, "STUDENT");

        // Act
        var satisfied = constraint.IsSatisfiedBy(applicabilityCtx);

        // Assert
        satisfied.Should().BeTrue();
    }

    [Fact]
    public void ToApplicabilityContext_ShouldBeCompatibleWithGreaterThanConstraint()
    {
        // Arrange
        var ctx = PricingContext.ForGroup(SampleDate, CustomerTypes.Standard, 20);
        var applicabilityCtx = ctx.ToApplicabilityContext();
        var constraint = ApplicabilityConstraint.GreaterThan(ApplicabilityKeys.GroupSize, 10);

        // Act
        var satisfied = constraint.IsSatisfiedBy(applicabilityCtx);

        // Assert
        satisfied.Should().BeTrue();
    }

    [Fact]
    public void ToApplicabilityContext_ShouldBeCompatibleWithInConstraint()
    {
        // Arrange
        var ctx = PricingContext.For(SampleDate, CustomerTypes.Senior);
        var applicabilityCtx = ctx.ToApplicabilityContext();
        var constraint = ApplicabilityConstraint.In(
            ApplicabilityKeys.CustomerType,
            CustomerTypes.Senior, CustomerTypes.Reduced, CustomerTypes.Student);

        // Act
        var satisfied = constraint.IsSatisfiedBy(applicabilityCtx);

        // Assert
        satisfied.Should().BeTrue();
    }
}
