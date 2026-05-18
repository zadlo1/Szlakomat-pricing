
namespace Szlakomat.Pricing.Domain.Tests;

public class MoneyTests
{
    // ── Fabryki ───────────────────────────────────────────────────────────────

    [Fact]
    public void Pln_ShouldCreateMoneyWithPlnCurrency()
    {
        // Act
        var money = Money.Pln(47m);

        // Assert
        money.Amount.Should().Be(47.00m);
        money.Currency.Should().Be("PLN");
    }

    [Fact]
    public void Of_ShouldCreateMoneyWithGivenCurrency()
    {
        // Act
        var money = Money.Of(10m, "EUR");

        // Assert
        money.Amount.Should().Be(10.00m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Zero_ShouldCreateMoneyWithZeroAmount()
    {
        // Act
        var money = Money.Zero("PLN");

        // Assert
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("PLN");
    }

    [Fact]
    public void Currency_ShouldBeNormalizedToUpperCase()
    {
        // Act
        var money = Money.Of(1m, "pln");

        // Assert
        money.Currency.Should().Be("PLN");
    }

    // ── Zaokrąglanie ──────────────────────────────────────────────────────────

    [Fact]
    public void Amount_ShouldBeRoundedToTwoDecimalPlaces()
    {
        // Act
        var money = Money.Pln(10.555m);

        // Assert
        money.Amount.Should().Be(10.56m);
    }

    [Fact]
    public void Amount_ShouldRoundHalfAwayFromZero()
    {
        // Act
        var money = Money.Pln(10.545m);

        // Assert
        money.Amount.Should().Be(10.55m);
    }

    // ── Guard na ujemne kwoty ─────────────────────────────────────────────────

    [Fact]
    public void Pln_WithNegativeAmount_ShouldThrow()
    {
        // Act
        var act = () => Money.Pln(-1m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Of_WithNegativeAmount_ShouldThrow()
    {
        // Act
        var act = () => Money.Of(-0.01m, "EUR");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_SameCurrency_ShouldReturnSum()
    {
        // Arrange
        var a = Money.Pln(10m);
        var b = Money.Pln(5m);

        // Act
        var result = a.Add(b);

        // Assert
        result.Amount.Should().Be(15m);
        result.Currency.Should().Be("PLN");
    }

    [Fact]
    public void Add_ShouldReturnNewInstance()
    {
        // Arrange
        var a = Money.Pln(10m);
        var b = Money.Pln(5m);

        // Act
        var result = a.Add(b);

        // Assert
        result.Should().NotBeSameAs(a);
        result.Should().NotBeSameAs(b);
    }

    [Fact]
    public void Add_DifferentCurrencies_ShouldThrow()
    {
        // Arrange
        var pln = Money.Pln(10m);
        var eur = Money.Of(5m, "EUR");

        // Act
        var act = () => pln.Add(eur);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*PLN*EUR*");
    }

    // ── Subtract ──────────────────────────────────────────────────────────────

    [Fact]
    public void Subtract_SameCurrency_ShouldReturnDifference()
    {
        // Arrange
        var a = Money.Pln(10m);
        var b = Money.Pln(3m);

        // Act
        var result = a.Subtract(b);

        // Assert
        result.Amount.Should().Be(7m);
    }

    [Fact]
    public void Subtract_ResultWouldBeNegative_ShouldThrow()
    {
        // Arrange
        var a = Money.Pln(3m);
        var b = Money.Pln(10m);

        // Act
        var act = () => a.Subtract(b);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Subtract_DifferentCurrencies_ShouldThrow()
    {
        // Arrange
        var pln = Money.Pln(10m);
        var eur = Money.Of(5m, "EUR");

        // Act
        var act = () => pln.Subtract(eur);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Multiply ─────────────────────────────────────────────────────────────

    [Fact]
    public void Multiply_ShouldScaleAmount()
    {
        // Arrange
        var money = Money.Pln(10m);

        // Act
        var result = money.Multiply(3m);

        // Assert
        result.Amount.Should().Be(30m);
        result.Currency.Should().Be("PLN");
    }

    [Fact]
    public void Multiply_ByZero_ShouldReturnZero()
    {
        // Arrange
        var money = Money.Pln(47m);

        // Act
        var result = money.Multiply(0m);

        // Assert
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Multiply_ByNegative_ShouldThrow()
    {
        // Arrange
        var money = Money.Pln(10m);

        // Act
        var act = () => money.Multiply(-1m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Divide ────────────────────────────────────────────────────────────────

    [Fact]
    public void Divide_ShouldReturnQuotient()
    {
        // Arrange
        var money = Money.Pln(10m);

        // Act
        var result = money.Divide(4m);

        // Assert
        result.Amount.Should().Be(2.50m);
    }

    [Fact]
    public void Divide_ByZero_ShouldThrow()
    {
        // Arrange
        var money = Money.Pln(10m);

        // Act
        var act = () => money.Divide(0m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Równość i ToString ────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameAmountAndCurrency_ShouldBeTrue()
    {
        // Arrange
        var a = Money.Pln(47m);
        var b = Money.Pln(47m);

        // Assert
        a.Should().Be(b);
    }

    [Fact]
    public void Equals_DifferentAmount_ShouldBeFalse()
    {
        // Arrange
        var a = Money.Pln(47m);
        var b = Money.Pln(48m);

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_DifferentCurrency_ShouldBeFalse()
    {
        // Arrange
        var a = Money.Pln(47m);
        var b = Money.Of(47m, "EUR");

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_ShouldReturnCurrencyAndAmount()
    {
        // Arrange
        var money = Money.Pln(47m);

        // Assert
        money.ToString().Should().Be("PLN 47.00");
    }
}
