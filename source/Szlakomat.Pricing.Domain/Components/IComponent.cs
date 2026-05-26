using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Components;

/// <summary>
/// Komponent cennika — liść (Simple) lub węzeł (Composite).
/// </summary>
public interface IComponent
{
    Guid ComponentId { get; }
    string Name { get; }
    Interpretation Interpretation { get; }
    bool ContributesToTotal { get; }

    Money Calculate(PricingParameters parameters, PricingContext context);

    PriceBreakdown CalculateBreakdown(PricingParameters parameters, PricingContext context);
}
