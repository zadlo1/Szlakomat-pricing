using Szlakomat.Pricing.Domain.Calculators.Adapters;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Calculators;

/// <summary>
/// Konwersja interpretacji kalkulatora — współdzielone przez PricingFacade i SimpleComponent.
/// </summary>
internal static class CalculatorInterpretation
{
    public static ICalculator AdaptTo(ICalculator calc, Interpretation target)
    {
        if (calc.Interpretation == target)
            return calc;

        return (calc.Interpretation, target) switch
        {
            (Interpretation.Unit, Interpretation.Total) => new UnitToTotalAdapter(calc),
            (Interpretation.Total, Interpretation.Unit) => new TotalToUnitAdapter(calc),
            (Interpretation.Total, Interpretation.Marginal) => new TotalToMarginalAdapter(calc),
            _ => throw new InvalidOperationException(
                $"Brak adaptera z interpretacji {calc.Interpretation} do {target}")
        };
    }

    public static Money CalculateAsTotal(ICalculator calculator, PricingParameters parameters) =>
        AdaptTo(calculator, Interpretation.Total).Calculate(parameters);
}
