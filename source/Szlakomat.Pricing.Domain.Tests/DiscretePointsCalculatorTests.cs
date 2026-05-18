using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Tests;

public class DiscretePointsCalculatorTests
{
    /// <summary>
    /// Bilet wstępu: 1 osoba → 30 PLN, 2 osoby → 55 PLN, 3 osoby → 75 PLN.
    /// </summary>
    private static DiscretePointsCalculator BuildBiletWstępu() =>
        new(
            Guid.NewGuid(),
            "Bilet wstępu Smocza Jama",
            new Dictionary<int, Money>
            {
                [1] = Money.Pln(15m),
                [2] = Money.Pln(28m),
                [3] = Money.Pln(39m),
                [5] = Money.Pln(60m),
            });

    private static PricingParameters Qty(int qty) =>
        PricingParameters.Of(CalculatorParameterKeys.Quantity, qty);

    // ── Dokładne trafienie w punkt ────────────────────────────────────────────

    [Fact]
    public void Calculate_Qty1_ShouldReturnPriceForOneTicket()
    {
        // Arrange
        var calc = BuildBiletWstępu();

        // Act
        var result = calc.Calculate(Qty(1));

        // Assert
        result.Should().Be(Money.Pln(15m));
    }

    [Fact]
    public void Calculate_Qty2_ShouldReturnPriceForTwoTickets()
    {
        // Arrange
        var calc = BuildBiletWstępu();

        // Act
        var result = calc.Calculate(Qty(2));

        // Assert
        result.Should().Be(Money.Pln(28m));
    }

    [Fact]
    public void Calculate_Qty5_ShouldReturnPriceForFiveTickets()
    {
        // Arrange
        var calc = BuildBiletWstępu();

        // Act
        var result = calc.Calculate(Qty(5));

        // Assert
        result.Should().Be(Money.Pln(60m));
    }

    // ── Brak punktu → wyjątek ─────────────────────────────────────────────────

    [Fact]
    public void Calculate_QuantityNotInTable_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var calc = BuildBiletWstępu();

        // Act
        var act = () => calc.Calculate(Qty(4));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*qty=4*");
    }

    [Fact]
    public void Calculate_QuantityZero_ShouldThrowBecauseNotDefined()
    {
        // Arrange
        var calc = BuildBiletWstępu();

        // Act
        var act = () => calc.Calculate(Qty(0));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Calculate_WithoutQuantityParameter_ShouldThrow()
    {
        // Arrange
        var calc = BuildBiletWstępu();

        // Act
        var act = () => calc.Calculate(PricingParameters.Empty());

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Interpretation_ShouldBeTotal()
    {
        // Arrange
        var calc = BuildBiletWstępu();

        // Assert
        calc.Interpretation.Should().Be(Interpretation.Total);
    }
}
