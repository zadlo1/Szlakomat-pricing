using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Tests;

public class DailyIncrementCalculatorTests
{
    private static readonly DateOnly ReferenceDate = new(2025, 6, 1);

    /// <summary>
    /// Early bird: start 20 PLN, +5 PLN za każdy dzień od 1 czerwca.
    /// </summary>
    private static DailyIncrementCalculator BuildEarlyBird() =>
        new(
            Guid.NewGuid(),
            "Early bird Wawel",
            Money.Pln(20m),
            ReferenceDate,
            Money.Pln(5m));

    private static PricingParameters WithDate(DateOnly date) =>
        PricingParameters.Of(CalculatorParameterKeys.VisitDate, date.ToString("yyyy-MM-dd"));

    // ── Ten sam dzień co referenceDate ────────────────────────────────────────

    [Fact]
    public void Calculate_OnReferenceDate_ShouldReturnStartPrice()
    {
        // Arrange
        var calc = BuildEarlyBird();

        // Act
        var result = calc.Calculate(WithDate(ReferenceDate));

        // Assert
        result.Should().Be(Money.Pln(20m));
    }

    // ── Wzrost dzienny ────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_OneDayAfterReferenceDate_ShouldReturnStartPlusSingleIncrement()
    {
        // Arrange
        var calc = BuildEarlyBird();
        var visitDate = ReferenceDate.AddDays(1);

        // Act
        var result = calc.Calculate(WithDate(visitDate));

        // Assert
        result.Should().Be(Money.Pln(25m));
    }

    [Fact]
    public void Calculate_TenDaysAfterReferenceDate_ShouldReturnStartPlusTenIncrements()
    {
        // Arrange
        var calc = BuildEarlyBird();
        var visitDate = ReferenceDate.AddDays(10);
        // 20 + 10×5 = 70

        // Act
        var result = calc.Calculate(WithDate(visitDate));

        // Assert
        result.Should().Be(Money.Pln(70m));
    }

    [Fact]
    public void Calculate_ThirtyDaysAfterReferenceDate_ShouldReturnCorrectPrice()
    {
        // Arrange
        var calc = BuildEarlyBird();
        var visitDate = ReferenceDate.AddDays(30);
        // 20 + 30×5 = 170

        // Act
        var result = calc.Calculate(WithDate(visitDate));

        // Assert
        result.Should().Be(Money.Pln(170m));
    }

    // ── Data przed referenceDate → brak ujemnych kwot ─────────────────────────

    [Fact]
    public void Calculate_BeforeReferenceDate_ShouldReturnStartPrice()
    {
        // Arrange
        var calc = BuildEarlyBird();
        var visitDate = ReferenceDate.AddDays(-5);

        // Act
        var result = calc.Calculate(WithDate(visitDate));

        // Assert
        result.Should().Be(Money.Pln(20m));
    }

    // ── Brak parametru daty ───────────────────────────────────────────────────

    [Fact]
    public void Calculate_WithoutVisitDateParameter_ShouldThrow()
    {
        // Arrange
        var calc = BuildEarlyBird();

        // Act
        var act = () => calc.Calculate(PricingParameters.Empty());

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage($"*{CalculatorParameterKeys.VisitDate}*");
    }

    [Fact]
    public void Interpretation_ShouldBeUnit()
    {
        // Arrange
        var calc = BuildEarlyBird();

        // Assert
        calc.Interpretation.Should().Be(Interpretation.Unit);
    }
}
