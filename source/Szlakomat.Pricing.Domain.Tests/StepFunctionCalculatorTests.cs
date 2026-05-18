using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Tests;

public class StepFunctionCalculatorTests
{
    /// <summary>
    /// Cennik grupowy Wawelu: podstawa 200 PLN + 50 PLN za każde rozpoczęte 10 osób.
    /// Przykład: 1-9 os. → 200, 10-19 os. → 250, 20-29 os. → 300
    /// </summary>
    private static StepFunctionCalculator BuildWawelowyGroupCalculator() =>
        new(
            Guid.NewGuid(),
            "Cennik grupowy Wawel",
            Money.Pln(200m),
            stepSize: 10,
            Money.Pln(50m));

    private static PricingParameters Qty(int qty) =>
        PricingParameters.Of(CalculatorParameterKeys.Quantity, qty);

    // ── Granice przedziałów ───────────────────────────────────────────────────

    [Fact]
    public void Calculate_Qty1_ShouldReturnBasePrice()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();

        // Act
        var result = calc.Calculate(Qty(1));

        // Assert
        result.Should().Be(Money.Pln(200m));
    }

    [Fact]
    public void Calculate_Qty9_ShouldReturnBasePrice()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();

        // Act
        var result = calc.Calculate(Qty(9));

        // Assert
        result.Should().Be(Money.Pln(200m));
    }

    [Fact]
    public void Calculate_Qty10_ShouldReturnBasePlusSingleIncrement()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();

        // Act
        var result = calc.Calculate(Qty(10));

        // Assert
        result.Should().Be(Money.Pln(250m));
    }

    [Fact]
    public void Calculate_Qty19_ShouldReturnBasePlusSingleIncrement()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();

        // Act
        var result = calc.Calculate(Qty(19));

        // Assert
        result.Should().Be(Money.Pln(250m));
    }

    [Fact]
    public void Calculate_Qty20_ShouldReturnBasePlusTwoIncrements()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();

        // Act
        var result = calc.Calculate(Qty(20));

        // Assert
        result.Should().Be(Money.Pln(300m));
    }

    [Fact]
    public void Calculate_Qty25_ShouldReturnBasePlusTwoIncrements()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();

        // Act
        var result = calc.Calculate(Qty(25));

        // Assert
        result.Should().Be(Money.Pln(300m));
    }

    // ── Scenariusz grupowy Wawelu: 35 osób ────────────────────────────────────

    [Fact]
    public void Calculate_Qty35_ScenariuszWawel_ShouldReturnBasePlusThreeIncrements()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();
        // 35 / 10 = 3 kroki → 200 + 3×50 = 350

        // Act
        var result = calc.Calculate(Qty(35));

        // Assert
        result.Should().Be(Money.Pln(350m));
    }

    // ── Walidacja parametrów ──────────────────────────────────────────────────

    [Fact]
    public void Calculate_WithoutQuantityParameter_ShouldThrow()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();

        // Act
        var act = () => calc.Calculate(PricingParameters.Empty());

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Interpretation_ShouldBeTotal()
    {
        // Arrange
        var calc = BuildWawelowyGroupCalculator();

        // Assert
        calc.Interpretation.Should().Be(Interpretation.Total);
    }
}
