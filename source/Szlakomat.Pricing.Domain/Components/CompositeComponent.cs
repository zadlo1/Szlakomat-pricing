using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Components;

/// <summary>
/// Węzeł drzewa komponentów — sumuje stosowalne dzieci; wspiera parameterDependencies.
/// </summary>
public sealed class CompositeComponent : IComponent
{
    private readonly IReadOnlyList<IComponent> _children;
    private readonly IReadOnlyList<ParameterDependency> _parameterDependencies;
    private readonly IApplicabilityConstraint _applicability;
    private readonly Validity _validity;
    private readonly string _defaultCurrency;

    public CompositeComponent(
        Guid componentId,
        string name,
        IReadOnlyList<IComponent> children,
        IApplicabilityConstraint? applicability = null,
        Validity? validity = null,
        IReadOnlyList<ParameterDependency>? parameterDependencies = null)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(children, nameof(children));
        if (children.Count == 0)
            throw new ArgumentException("CompositeComponent wymaga co najmniej jednego dziecka.", nameof(children));

        ComponentId = componentId;
        Name = name;
        _children = children;
        _applicability = applicability ?? ApplicabilityConstraint.AlwaysTrue();
        _validity = validity ?? Validity.Always();
        _parameterDependencies = parameterDependencies ?? [];
        _defaultCurrency = "PLN";
    }

    public Guid ComponentId { get; }
    public string Name { get; }
    public Calculators.Interpretation Interpretation => Calculators.Interpretation.Total;
    public bool ContributesToTotal => true;

    public Money Calculate(PricingParameters parameters, PricingContext context)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        Guard.IsNotNull(context, nameof(context));

        if (!IsActive(context))
            return Money.Zero(_defaultCurrency);

        var (_, total) = EvaluateChildren(parameters, context);
        return total;
    }

    public PriceBreakdown CalculateBreakdown(PricingParameters parameters, PricingContext context)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        Guard.IsNotNull(context, nameof(context));

        if (!IsActive(context))
            return new PriceBreakdown(Name, Money.Zero(_defaultCurrency), []);

        var (childBreakdowns, total) = EvaluateChildren(parameters, context);
        return new PriceBreakdown(Name, total, childBreakdowns);
    }

    private bool IsActive(PricingContext context)
    {
        if (!_validity.IsValidAt(context.VisitDate))
            return false;

        return _applicability.IsSatisfiedBy(context.ToApplicabilityContext());
    }

    private (IReadOnlyList<PriceBreakdown> Children, Money Total) EvaluateChildren(
        PricingParameters parameters,
        PricingContext context)
    {
        var childAmounts = new Dictionary<string, Money>(StringComparer.Ordinal);
        var childBreakdowns = new List<PriceBreakdown>();
        var contributingAmounts = new List<Money>();

        foreach (var child in _children)
        {
            var childParams = ApplyDependencies(child.Name, parameters, childAmounts);
            var amount = child.Calculate(childParams, context);
            childAmounts[child.Name] = amount;

            if (child.ContributesToTotal && amount.Amount > 0m)
            {
                contributingAmounts.Add(amount);
                childBreakdowns.Add(child.CalculateBreakdown(childParams, context));
            }
        }

        if (contributingAmounts.Count == 0)
            return ([], Money.Zero(_defaultCurrency));

        var total = contributingAmounts.Aggregate((a, b) => a.Add(b));
        return (childBreakdowns, total);
    }

    private PricingParameters ApplyDependencies(
        string childName,
        PricingParameters parameters,
        IReadOnlyDictionary<string, Money> priorAmounts)
    {
        var dependency = _parameterDependencies.FirstOrDefault(d => d.DependentChildName == childName);
        if (dependency is null)
            return parameters;

        var sources = dependency.SourceChildNames
            .Where(priorAmounts.ContainsKey)
            .Select(name => priorAmounts[name])
            .Where(m => m.Amount > 0m)
            .ToList();

        if (sources.Count == 0)
            return parameters;

        var sum = sources.Aggregate((a, b) => a.Add(b));
        return parameters.With(dependency.ParameterKey, sum);
    }
}
