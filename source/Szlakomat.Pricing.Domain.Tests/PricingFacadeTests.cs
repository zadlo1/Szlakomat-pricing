using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Facade;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Tests;

public class PricingFacadeTests
{
    private static PricingParameters Qty(int qty) =>
        PricingParameters.Of(CalculatorParameterKeys.Quantity, qty);

    private static PricingParameters WithBase(Money baseAmount) =>
        PricingParameters.Of(PercentageCalculator.BaseAmountKey, baseAmount);

    // ── Rejestracja i ListCalculators ─────────────────────────────────────────

    [Fact]
    public void ListCalculators_AfterRegistration_ShouldReturnAllCalculators()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("Skarbiec", Money.Pln(47m))
            .AddFixedCalculator("Smocza Jama", Money.Pln(15m));

        // Act
        var list = facade.ListCalculators();

        // Assert
        list.Should().HaveCount(2);
        list.Should().Contain(v => v.Name == "Skarbiec");
        list.Should().Contain(v => v.Name == "Smocza Jama");
    }

    [Fact]
    public void ListCalculators_ShouldExposeCorrectInterpretationAndType()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("Skarbiec", Money.Pln(47m));

        // Act
        var view = facade.ListCalculators().Single();

        // Assert
        view.Interpretation.Should().Be(Interpretation.Unit);
        view.Type.Should().Be("SimpleFixed");
    }

    // ── Calculate (natywna interpretacja) ─────────────────────────────────────

    [Fact]
    public void Calculate_FixedCalculator_ShouldReturnFixedPrice()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("Skarbiec", Money.Pln(47m));

        // Act
        var result = facade.Calculate("Skarbiec", PricingParameters.Empty());

        // Assert
        result.Should().Be(Money.Pln(47m));
    }

    [Fact]
    public void Calculate_UnknownCalculator_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var facade = new PricingFacade();

        // Act
        var act = () => facade.Calculate("Nieistniejący", PricingParameters.Empty());

        // Assert
        act.Should().Throw<KeyNotFoundException>().WithMessage("*Nieistniejący*");
    }

    // ── CalculateTotal — konwersja Unit → Total ───────────────────────────────

    [Fact]
    public void CalculateTotal_ForUnitCalculator_ShouldMultiplyByQuantity()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("Skarbiec", Money.Pln(47m));
        var paramsWithQty = Qty(3);

        // Act
        var total = facade.CalculateTotal("Skarbiec", paramsWithQty);

        // Assert
        total.Should().Be(Money.Pln(141m));
    }

    [Fact]
    public void CalculateTotal_ForTotalCalculator_ShouldReturnSameValue()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddStepFunctionCalculator(
                "Cennik grupowy",
                Money.Pln(200m),
                stepSize: 10,
                Money.Pln(50m));

        // Act
        var total = facade.CalculateTotal("Cennik grupowy", Qty(15));

        // Assert
        total.Should().Be(Money.Pln(250m));
    }

    // ── CalculateUnitPrice — konwersja Total → Unit ───────────────────────────

    [Fact]
    public void CalculateUnitPrice_ForTotalCalculator_ShouldDivideByQuantity()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddStepFunctionCalculator(
                "Cennik grupowy",
                Money.Pln(200m),
                stepSize: 10,
                Money.Pln(50m));
        // qty=10 → total=250 → unit=25

        // Act
        var unit = facade.CalculateUnitPrice("Cennik grupowy", Qty(10));

        // Assert
        unit.Should().Be(Money.Pln(25m));
    }

    [Fact]
    public void CalculateUnitPrice_ForUnitCalculator_ShouldReturnSameValue()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("Smocza Jama", Money.Pln(15m));

        // Act
        var unit = facade.CalculateUnitPrice("Smocza Jama", PricingParameters.Empty());

        // Assert
        unit.Should().Be(Money.Pln(15m));
    }

    // ── Wiele kalkulatorów — orkiestracja ─────────────────────────────────────

    [Fact]
    public void Facade_WithMultipleCalculators_ShouldRouteByName()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddFixedCalculator("Skarbiec", Money.Pln(47m))
            .AddFixedCalculator("Smocza Jama", Money.Pln(15m))
            .AddPercentageCalculator("VAT 8%", 8m);

        // Act
        var skarbiec = facade.Calculate("Skarbiec", PricingParameters.Empty());
        var smoczaJama = facade.Calculate("Smocza Jama", PricingParameters.Empty());
        var vat = facade.Calculate("VAT 8%", WithBase(Money.Pln(100m)));

        // Assert
        skarbiec.Should().Be(Money.Pln(47m));
        smoczaJama.Should().Be(Money.Pln(15m));
        vat.Should().Be(Money.Pln(8m));
    }

    // ── AddDiscreteCalculator i AddDailyIncrementCalculator ──────────────────

    [Fact]
    public void AddDiscreteCalculator_ShouldRegisterAndCalculateCorrectly()
    {
        // Arrange
        var facade = new PricingFacade()
            .AddDiscreteCalculator(
                "Bilet grupowy",
                new Dictionary<int, Money>
                {
                    [1] = Money.Pln(15m),
                    [2] = Money.Pln(28m),
                });

        // Act
        var result = facade.Calculate("Bilet grupowy", Qty(2));

        // Assert
        result.Should().Be(Money.Pln(28m));
    }

    [Fact]
    public void AddDailyIncrementCalculator_ShouldRegisterAndCalculateCorrectly()
    {
        // Arrange
        var referenceDate = new DateOnly(2025, 6, 1);
        var visitDate = referenceDate.AddDays(5);

        var facade = new PricingFacade()
            .AddDailyIncrementCalculator(
                "Early Bird",
                Money.Pln(20m),
                referenceDate,
                Money.Pln(5m));

        var parameters = PricingParameters
            .Of(CalculatorParameterKeys.VisitDate, visitDate.ToString("yyyy-MM-dd"));

        // Act
        var result = facade.Calculate("Early Bird", parameters);

        // Assert
        result.Should().Be(Money.Pln(45m));
    }
}
