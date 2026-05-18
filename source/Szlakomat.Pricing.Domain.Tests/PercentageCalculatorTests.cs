using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Tests;

public class PercentageCalculatorTests
{
    private static PricingParameters WithBase(Money baseAmount) =>
        PricingParameters.Of(PercentageCalculator.BaseAmountKey, baseAmount);

    // ── VAT 23% ───────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_Vat23Percent_ShouldReturnCorrectTaxAmount()
    {
        // Arrange
        var vat = new PercentageCalculator(Guid.NewGuid(), "VAT 23%", 23m);
        var baseAmount = Money.Pln(100m);

        // Act
        var result = vat.Calculate(WithBase(baseAmount));

        // Assert
        result.Should().Be(Money.Pln(23m));
    }

    // ── Rabat B2B 15% ─────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_B2BDiscount15Percent_ShouldReturnCorrectDiscountAmount()
    {
        // Arrange
        var discount = new PercentageCalculator(Guid.NewGuid(), "Rabat B2B 15%", 15m);
        var baseAmount = Money.Pln(200m);

        // Act
        var result = discount.Calculate(WithBase(baseAmount));

        // Assert
        result.Should().Be(Money.Pln(30m));
    }

    // ── Ubezpieczenie NNW 2% ──────────────────────────────────────────────────

    [Fact]
    public void Calculate_Insurance2Percent_ShouldReturnCorrectAmount()
    {
        // Arrange
        var insurance = new PercentageCalculator(Guid.NewGuid(), "Ubezpieczenie NNW 2%", 2m);
        var baseAmount = Money.Pln(47m);
        // 47 × 2% = 0.94

        // Act
        var result = insurance.Calculate(WithBase(baseAmount));

        // Assert
        result.Should().Be(Money.Pln(0.94m));
    }

    // ── Zaokrąglanie ──────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_ResultIsRoundedToTwoDecimalPlaces()
    {
        // Arrange
        var calc = new PercentageCalculator(Guid.NewGuid(), "VAT 8%", 8m);
        var baseAmount = Money.Pln(33.33m);
        // 33.33 × 8% = 2.6664 → zaokrąglone do 2.67

        // Act
        var result = calc.Calculate(WithBase(baseAmount));

        // Assert
        result.Amount.Should().Be(2.67m);
    }

    // ── Brak parametru baseAmount ─────────────────────────────────────────────

    [Fact]
    public void Calculate_WithoutBaseAmountParameter_ShouldThrow()
    {
        // Arrange
        var calc = new PercentageCalculator(Guid.NewGuid(), "VAT 23%", 23m);

        // Act
        var act = () => calc.Calculate(PricingParameters.Empty());

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    // ── Walidacja przy tworzeniu ──────────────────────────────────────────────

    [Fact]
    public void Constructor_WithZeroRate_ShouldThrow()
    {
        // Act
        var act = () => new PercentageCalculator(Guid.NewGuid(), "Zero", 0m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Interpretation_ShouldBeTotal()
    {
        // Arrange
        var calc = new PercentageCalculator(Guid.NewGuid(), "VAT", 23m);

        // Assert
        calc.Interpretation.Should().Be(Interpretation.Total);
    }
}
