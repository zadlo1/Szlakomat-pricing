using System.Globalization;

namespace Szlakomat.Pricing.Domain;

/// <summary>
/// Typ wartościowy łączący kwotę z walutą.
/// Kwota zawsze zaokrąglona do 2 miejsc dziesiętnych, nigdy ujemna.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Guard.IsGreaterThanOrEqualTo(amount, 0m, nameof(amount));
        Guard.IsNotNullOrWhiteSpace(currency, nameof(currency));
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Pln(decimal amount) => new(amount, "PLN");

    public static Money Of(decimal amount, string currency) => new(amount, currency);

    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        AssertSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        AssertSameCurrency(other);
        Guard.IsGreaterThanOrEqualTo(Amount, other.Amount, "Wynik odejmowania nie może być ujemny");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        Guard.IsGreaterThanOrEqualTo(factor, 0m, nameof(factor));
        return new Money(Amount * factor, Currency);
    }

    public Money Divide(decimal divisor)
    {
        Guard.IsGreaterThan(divisor, 0m, nameof(divisor));
        return new Money(Amount / divisor, Currency);
    }

    private void AssertSameCurrency(Money other)
    {
        Guard.IsNotNull(other, nameof(other));
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Nie można wykonać operacji na różnych walutach: {Currency} i {other.Currency}");
    }

    public override string ToString() =>
        $"{Currency} {Amount.ToString("F2", CultureInfo.InvariantCulture)}";
}
