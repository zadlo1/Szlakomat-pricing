using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Components;

/// <summary>
/// Liść drzewa komponentów — opakowuje kalkulator z osiami Validity i Applicability.
/// </summary>
public sealed class SimpleComponent : IComponent
{
    private readonly ICalculator _calculator;
    private readonly IApplicabilityConstraint _applicability;
    private readonly Validity _validity;
    private readonly IReadOnlyDictionary<string, string> _parameterMappings;
    private readonly string _defaultCurrency;

    public SimpleComponent(
        Guid componentId,
        string name,
        ICalculator calculator,
        IApplicabilityConstraint? applicability = null,
        Validity? validity = null,
        IReadOnlyDictionary<string, string>? parameterMappings = null,
        bool contributesToTotal = true)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(calculator, nameof(calculator));

        ComponentId = componentId;
        Name = name;
        ContributesToTotal = contributesToTotal;
        _calculator = calculator;
        _applicability = applicability ?? ApplicabilityConstraint.AlwaysTrue();
        _validity = validity ?? Validity.Always();
        _parameterMappings = parameterMappings ?? new Dictionary<string, string>();
        _defaultCurrency = ResolveDefaultCurrency(calculator);
    }

    public Guid ComponentId { get; }
    public string Name { get; }
    public Calculators.Interpretation Interpretation => Calculators.Interpretation.Total;
    public bool ContributesToTotal { get; }

    public Money Calculate(PricingParameters parameters, PricingContext context)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        Guard.IsNotNull(context, nameof(context));

        if (!IsActive(parameters, context, out var resolved))
            return Money.Zero(_defaultCurrency);

        return CalculatorInterpretation.CalculateAsTotal(_calculator, resolved);
    }

    public PriceBreakdown CalculateBreakdown(PricingParameters parameters, PricingContext context)
    {
        var total = Calculate(parameters, context);
        if (total.Amount == 0m)
            return new PriceBreakdown(Name, total, []);

        return new PriceBreakdown(Name, total, []);
    }

    private bool IsActive(
        PricingParameters parameters,
        PricingContext context,
        out PricingParameters resolvedParameters)
    {
        if (!_validity.IsValidAt(context.VisitDate))
        {
            resolvedParameters = parameters;
            return false;
        }

        if (!_applicability.IsSatisfiedBy(context.ToApplicabilityContext()))
        {
            resolvedParameters = parameters;
            return false;
        }

        resolvedParameters = ApplyMappings(EnrichWithQuantity(parameters, context));
        return true;
    }

    private static PricingParameters EnrichWithQuantity(PricingParameters parameters, PricingContext context) =>
        parameters.With(CalculatorParameterKeys.Quantity, context.GroupSize);

    private PricingParameters ApplyMappings(PricingParameters parameters)
    {
        var result = parameters;
        foreach (var (externalKey, internalKey) in _parameterMappings)
        {
            if (!parameters.TryGet(externalKey, out var value) || value is null)
                continue;
            result = result.With(internalKey, value);
        }
        return result;
    }

    private static string ResolveDefaultCurrency(ICalculator calculator)
    {
        try
        {
            var sample = calculator.Calculate(
                PricingParameters.Of(CalculatorParameterKeys.Quantity, 1));
            return sample.Currency;
        }
        catch
        {
            return "PLN";
        }
    }
}
