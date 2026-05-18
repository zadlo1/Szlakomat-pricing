using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Tests;

public class SimpleFixedCalculatorTests
{
    private static SimpleFixedCalculator BuildSkarbiec() =>
        new(Guid.NewGuid(), "Skarbiec Wawelu", Money.Pln(47m));

    [Fact]
    public void Calculate_AlwaysReturnsFixedPrice_RegardlessOfParameters()
    {
        // Arrange
        var calc = BuildSkarbiec();
        var emptyParams = PricingParameters.Empty();

        // Act
        var result = calc.Calculate(emptyParams);

        // Assert
        result.Should().Be(Money.Pln(47m));
    }

    [Fact]
    public void Calculate_WithQuantityParameter_StillReturnsFixedPrice()
    {
        // Arrange
        var calc = BuildSkarbiec();
        var paramsWithQty = PricingParameters.Of(CalculatorParameterKeys.Quantity, 10);

        // Act
        var result = calc.Calculate(paramsWithQty);

        // Assert
        result.Should().Be(Money.Pln(47m));
    }

    [Fact]
    public void Calculate_ForZeroPrice_ReturnsZeroMoney()
    {
        // Arrange
        var ogrody = new SimpleFixedCalculator(Guid.NewGuid(), "Ogrody Wawelu", Money.Pln(0m));

        // Act
        var result = ogrody.Calculate(PricingParameters.Empty());

        // Assert
        result.Should().Be(Money.Pln(0m));
    }

    [Fact]
    public void Interpretation_ShouldBeUnit()
    {
        // Arrange
        var calc = BuildSkarbiec();

        // Assert
        calc.Interpretation.Should().Be(Interpretation.Unit);
    }

    [Fact]
    public void Formula_ShouldDescribeFixedAmount()
    {
        // Arrange
        var calc = BuildSkarbiec();

        // Assert
        calc.Formula.Should().Contain("47");
    }

    [Fact]
    public void Type_ShouldBeSimpleFixed()
    {
        // Arrange
        var calc = BuildSkarbiec();

        // Assert
        calc.Type.Should().Be("SimpleFixed");
    }
}
