using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Calculators;

/// <summary>
/// Kalkulator procentowy: baseAmount × rate%.
/// baseAmount pobierany z PricingParameters pod kluczem CalculatorParameterKeys.BaseAmount.
/// Przykłady: rabat B2B -15%, VAT +8%, ubezpieczenie NNW 2%.
/// </summary>
public sealed class PercentageCalculator : ICalculator
{
    private readonly decimal _ratePercent;

    /// <summary>Klucz parametru z kwotą bazową do przemnożenia.</summary>
    public const string BaseAmountKey = "baseAmount";

    public PercentageCalculator(Guid id, string name, decimal ratePercent)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsGreaterThan(ratePercent, 0m, nameof(ratePercent));

        Id = id;
        Name = name;
        _ratePercent = ratePercent;
    }

    public Guid Id { get; }
    public string Name { get; }
    public Interpretation Interpretation => Interpretation.Total;
    public string Formula => $"f(base) = base × {_ratePercent}%";
    public string Type => "Percentage";

    public Money Calculate(PricingParameters parameters)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        var baseAmount = parameters.GetMoney(BaseAmountKey);
        return baseAmount.Multiply(_ratePercent / 100m);
    }
}
