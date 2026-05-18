using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Calculators;

/// <summary>
/// Kalkulator schodkowy: basePrice + floor(qty / stepSize) × increment.
/// Konwencja granic: [od, do).
/// Przykłady: cennik grupowy, opłata za nadwyżkę.
/// </summary>
public sealed class StepFunctionCalculator : ICalculator
{
    private readonly Money _basePrice;
    private readonly int _stepSize;
    private readonly Money _increment;

    public StepFunctionCalculator(
        Guid id,
        string name,
        Money basePrice,
        int stepSize,
        Money increment)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(basePrice, nameof(basePrice));
        Guard.IsGreaterThan(stepSize, 0, nameof(stepSize));
        Guard.IsNotNull(increment, nameof(increment));

        Id = id;
        Name = name;
        _basePrice = basePrice;
        _stepSize = stepSize;
        _increment = increment;
    }

    public Guid Id { get; }
    public string Name { get; }
    public Interpretation Interpretation => Interpretation.Total;
    public string Formula => $"f(qty) = {_basePrice} + floor(qty / {_stepSize}) × {_increment}";
    public string Type => "StepFunction";

    public Money Calculate(PricingParameters parameters)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        var quantity = parameters.GetInt(CalculatorParameterKeys.Quantity);
        Guard.IsGreaterThanOrEqualTo(quantity, 1, nameof(quantity));

        var steps = quantity / _stepSize;
        return _basePrice.Add(_increment.Multiply(steps));
    }
}
