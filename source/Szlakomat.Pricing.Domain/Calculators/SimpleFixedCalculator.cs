using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Calculators;

/// <summary>
/// Kalkulator ze stałą ceną niezależną od parametrów.
/// f(x) = kwota
/// Przykłady: Skarbiec 47 PLN, Smocza Jama 15 PLN, Ogrody 0 PLN.
/// </summary>
public sealed class SimpleFixedCalculator : ICalculator
{
    private readonly Money _price;

    public SimpleFixedCalculator(Guid id, string name, Money price)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(price, nameof(price));

        Id = id;
        Name = name;
        _price = price;
    }

    public Guid Id { get; }
    public string Name { get; }
    public Interpretation Interpretation => Interpretation.Unit;
    public string Formula => $"f(x) = {_price}";
    public string Type => "SimpleFixed";

    public Money Calculate(PricingParameters parameters)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        return _price;
    }
}
