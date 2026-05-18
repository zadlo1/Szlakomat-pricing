using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Calculators.Adapters;

/// <summary>
/// Adapter Unit → Total: mnoży wynik kalkulatora przez quantity.
/// Total(q) = Unit(q) × q
/// </summary>
public sealed class UnitToTotalAdapter : ICalculator
{
    private readonly ICalculator _inner;

    public UnitToTotalAdapter(ICalculator inner)
    {
        Guard.IsNotNull(inner, nameof(inner));
        if (inner.Interpretation != Interpretation.Unit)
            throw new InvalidOperationException(
                $"UnitToTotalAdapter wymaga kalkulatora o interpretacji Unit, otrzymano: {inner.Interpretation}");
        _inner = inner;
    }

    public Guid Id => _inner.Id;
    public string Name => _inner.Name;
    public Interpretation Interpretation => Interpretation.Total;
    public string Formula => $"Total({_inner.Formula}) × quantity";
    public string Type => _inner.Type;

    public Money Calculate(PricingParameters parameters)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        var quantity = parameters.GetInt(CalculatorParameterKeys.Quantity);
        Guard.IsGreaterThanOrEqualTo(quantity, 1, nameof(quantity));
        return _inner.Calculate(parameters).Multiply(quantity);
    }
}

/// <summary>
/// Adapter Total → Unit: dzieli wynik kalkulatora przez quantity.
/// Unit(q) = Total(q) / q
/// </summary>
public sealed class TotalToUnitAdapter : ICalculator
{
    private readonly ICalculator _inner;

    public TotalToUnitAdapter(ICalculator inner)
    {
        Guard.IsNotNull(inner, nameof(inner));
        if (inner.Interpretation != Interpretation.Total)
            throw new InvalidOperationException(
                $"TotalToUnitAdapter wymaga kalkulatora o interpretacji Total, otrzymano: {inner.Interpretation}");
        _inner = inner;
    }

    public Guid Id => _inner.Id;
    public string Name => _inner.Name;
    public Interpretation Interpretation => Interpretation.Unit;
    public string Formula => $"Unit({_inner.Formula}) / quantity";
    public string Type => _inner.Type;

    public Money Calculate(PricingParameters parameters)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        var quantity = parameters.GetInt(CalculatorParameterKeys.Quantity);
        Guard.IsGreaterThanOrEqualTo(quantity, 1, nameof(quantity));
        return _inner.Calculate(parameters).Divide(quantity);
    }
}

/// <summary>
/// Adapter Total → Marginal: dwa wywołania kalkulatora, różnica.
/// Marginal(n) = Total(n) − Total(n−1)
/// </summary>
public sealed class TotalToMarginalAdapter : ICalculator
{
    private readonly ICalculator _inner;

    public TotalToMarginalAdapter(ICalculator inner)
    {
        Guard.IsNotNull(inner, nameof(inner));
        if (inner.Interpretation != Interpretation.Total)
            throw new InvalidOperationException(
                $"TotalToMarginalAdapter wymaga kalkulatora o interpretacji Total, otrzymano: {inner.Interpretation}");
        _inner = inner;
    }

    public Guid Id => _inner.Id;
    public string Name => _inner.Name;
    public Interpretation Interpretation => Interpretation.Marginal;
    public string Formula => $"Marginal({_inner.Formula}) = Total(n) − Total(n−1)";
    public string Type => _inner.Type;

    public Money Calculate(PricingParameters parameters)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        var quantity = parameters.GetInt(CalculatorParameterKeys.Quantity);
        Guard.IsGreaterThanOrEqualTo(quantity, 1, nameof(quantity));

        var totalN = _inner.Calculate(parameters);

        if (quantity == 1)
            return totalN;

        var paramsNMinus1 = parameters.With(CalculatorParameterKeys.Quantity, quantity - 1);
        var totalNMinus1 = _inner.Calculate(paramsNMinus1);

        return totalN.Subtract(totalNMinus1);
    }
}
