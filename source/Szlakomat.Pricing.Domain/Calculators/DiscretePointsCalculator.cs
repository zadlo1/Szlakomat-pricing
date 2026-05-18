using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Calculators;

/// <summary>
/// Kalkulator z tabelą dyskretnych punktów: konkretna wartość (qty) → konkretna cena.
/// Brak dokładnego trafienia w punkt → wyjątek.
/// Konwencja granic: [od, do).
/// Przykłady: bilet normalny / ulgowy / dziecięcy per liczba osób.
/// </summary>
public sealed class DiscretePointsCalculator : ICalculator
{
    private readonly IReadOnlyDictionary<int, Money> _points;

    public DiscretePointsCalculator(Guid id, string name, IReadOnlyDictionary<int, Money> points)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(points, nameof(points));
        Guard.IsGreaterThan(points.Count, 0, nameof(points));

        Id = id;
        Name = name;
        _points = points;
    }

    public Guid Id { get; }
    public string Name { get; }
    public Interpretation Interpretation => Interpretation.Total;
    public string Formula => $"f(qty) ∈ {{{string.Join(", ", _points.Keys.Order())}}}";
    public string Type => "DiscretePoints";

    public Money Calculate(PricingParameters parameters)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        var quantity = parameters.GetInt(CalculatorParameterKeys.Quantity);

        if (!_points.TryGetValue(quantity, out var price))
            throw new InvalidOperationException(
                $"Kalkulator '{Name}': brak zdefiniowanej ceny dla qty={quantity}. " +
                $"Dostępne punkty: {string.Join(", ", _points.Keys.Order())}");

        return price;
    }
}
