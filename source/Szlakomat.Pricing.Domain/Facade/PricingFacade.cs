using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Calculators.Adapters;
using Szlakomat.Pricing.Domain.Components;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Parameters;
using Szlakomat.Pricing.Domain.Components.Versioning;
using Szlakomat.Pricing.Domain.Repository;

namespace Szlakomat.Pricing.Domain.Facade;

/// <summary>
/// Główny punkt wejścia — rejestracja kalkulatorów i komponentów, orkiestracja obliczeń.
/// </summary>
public sealed class PricingFacade
{
    private readonly ICalculatorRepository _calculatorRepository;
    private readonly IComponentRepository _componentRepository;

    public PricingFacade()
        : this(new InMemoryCalculatorRepository(), new InMemoryComponentRepository())
    {
    }

    public PricingFacade(ICalculatorRepository calculatorRepository)
        : this(calculatorRepository, new InMemoryComponentRepository())
    {
    }

    public PricingFacade(
        ICalculatorRepository calculatorRepository,
        IComponentRepository componentRepository)
    {
        Guard.IsNotNull(calculatorRepository, nameof(calculatorRepository));
        Guard.IsNotNull(componentRepository, nameof(componentRepository));
        _calculatorRepository = calculatorRepository;
        _componentRepository = componentRepository;
    }

    // ── Rejestracja kalkulatorów ─────────────────────────────────────────────

    public PricingFacade AddFixedCalculator(string name, Money price)
    {
        var calc = new SimpleFixedCalculator(Guid.NewGuid(), name, price);
        _calculatorRepository.Save(calc);
        return this;
    }

    public PricingFacade AddPercentageCalculator(string name, decimal ratePercent)
    {
        var calc = new PercentageCalculator(Guid.NewGuid(), name, ratePercent);
        _calculatorRepository.Save(calc);
        return this;
    }

    public PricingFacade AddStepFunctionCalculator(
        string name,
        Money basePrice,
        int stepSize,
        Money increment)
    {
        var calc = new StepFunctionCalculator(Guid.NewGuid(), name, basePrice, stepSize, increment);
        _calculatorRepository.Save(calc);
        return this;
    }

    public PricingFacade AddDiscreteCalculator(string name, IReadOnlyDictionary<int, Money> points)
    {
        var calc = new DiscretePointsCalculator(Guid.NewGuid(), name, points);
        _calculatorRepository.Save(calc);
        return this;
    }

    public PricingFacade AddDailyIncrementCalculator(
        string name,
        Money startPrice,
        DateOnly referenceDate,
        Money dailyIncrement)
    {
        var calc = new DailyIncrementCalculator(Guid.NewGuid(), name, startPrice, referenceDate, dailyIncrement);
        _calculatorRepository.Save(calc);
        return this;
    }

    // ── Rejestracja komponentów ──────────────────────────────────────────────

    public PricingFacade CreateSimpleComponent(
        string name,
        string calculatorName,
        IApplicabilityConstraint? applicability = null,
        Validity? validity = null,
        IReadOnlyDictionary<string, string>? parameterMappings = null,
        bool contributesToTotal = true,
        DateTime? definedAt = null)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNullOrWhiteSpace(calculatorName, nameof(calculatorName));

        _ = _calculatorRepository.FindByName(calculatorName);

        var version = new SimpleComponentVersionData(
            validity ?? Validity.Always(),
            definedAt ?? DateTime.UtcNow,
            calculatorName,
            applicability ?? ApplicabilityConstraint.AlwaysTrue(),
            parameterMappings ?? new Dictionary<string, string>(),
            contributesToTotal);

        var component = new VersionedComponent(
            Guid.NewGuid(),
            name,
            ComponentKind.Simple,
            [version],
            _calculatorRepository,
            _componentRepository);

        _componentRepository.Save(component);
        return this;
    }

    public PricingFacade CreateCompositeComponent(
        string name,
        IReadOnlyList<string> childNames,
        IApplicabilityConstraint? applicability = null,
        Validity? validity = null,
        IReadOnlyList<ParameterDependency>? parameterDependencies = null,
        DateTime? definedAt = null)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(childNames, nameof(childNames));

        foreach (var childName in childNames)
            _ = _componentRepository.FindByName(childName);

        var version = new CompositeComponentVersionData(
            validity ?? Validity.Always(),
            definedAt ?? DateTime.UtcNow,
            childNames.ToList(),
            applicability ?? ApplicabilityConstraint.AlwaysTrue(),
            parameterDependencies ?? []);

        var component = new VersionedComponent(
            Guid.NewGuid(),
            name,
            ComponentKind.Composite,
            [version],
            _calculatorRepository,
            _componentRepository);

        _componentRepository.Save(component);
        return this;
    }

    public PricingFacade UpdateSimpleComponent(
        string name,
        string calculatorName,
        Validity validity,
        IApplicabilityConstraint? applicability = null,
        IReadOnlyDictionary<string, string>? parameterMappings = null,
        bool contributesToTotal = true,
        DateTime? definedAt = null,
        VersionAdditionStrategy strategy = VersionAdditionStrategy.RejectIdentical)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNullOrWhiteSpace(calculatorName, nameof(calculatorName));
        Guard.IsNotNull(validity, nameof(validity));

        _ = _calculatorRepository.FindByName(calculatorName);

        var component = RequireVersionedComponent(name);
        if (component.Kind != ComponentKind.Simple)
            throw new InvalidOperationException($"Komponent '{name}' nie jest komponentem prostym.");

        var version = new SimpleComponentVersionData(
            validity,
            definedAt ?? DateTime.UtcNow,
            calculatorName,
            applicability ?? ApplicabilityConstraint.AlwaysTrue(),
            parameterMappings ?? new Dictionary<string, string>(),
            contributesToTotal);

        component.AddVersion(version, strategy);
        _componentRepository.Save(component);
        return this;
    }

    public PricingFacade UpdateCompositeComponent(
        string name,
        IReadOnlyList<string> childNames,
        Validity validity,
        IApplicabilityConstraint? applicability = null,
        IReadOnlyList<ParameterDependency>? parameterDependencies = null,
        DateTime? definedAt = null,
        VersionAdditionStrategy strategy = VersionAdditionStrategy.RejectIdentical)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(childNames, nameof(childNames));
        Guard.IsNotNull(validity, nameof(validity));

        foreach (var childName in childNames)
            _ = _componentRepository.FindByName(childName);

        var component = RequireVersionedComponent(name);
        if (component.Kind != ComponentKind.Composite)
            throw new InvalidOperationException($"Komponent '{name}' nie jest komponentem złożonym.");

        var version = new CompositeComponentVersionData(
            validity,
            definedAt ?? DateTime.UtcNow,
            childNames.ToList(),
            applicability ?? ApplicabilityConstraint.AlwaysTrue(),
            parameterDependencies ?? []);

        component.AddVersion(version, strategy);
        _componentRepository.Save(component);
        return this;
    }

    // ── Obliczenia kalkulatorów ────────────────────────────────────────────────

    /// <summary>Zwraca wynik w natywnej interpretacji kalkulatora.</summary>
    public Money Calculate(string name, PricingParameters parameters)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(parameters, nameof(parameters));

        var calc = _calculatorRepository.FindByName(name);
        return calc.Calculate(parameters);
    }

    /// <summary>Zwraca Total — automatycznie adaptuje, jeśli kalkulator ma inną interpretację.</summary>
    public Money CalculateTotal(string name, PricingParameters parameters)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(parameters, nameof(parameters));

        var calc = _calculatorRepository.FindByName(name);
        return CalculatorInterpretation.CalculateAsTotal(calc, parameters);
    }

    /// <summary>Zwraca Unit — automatycznie adaptuje, jeśli kalkulator ma inną interpretację.</summary>
    public Money CalculateUnitPrice(string name, PricingParameters parameters)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(parameters, nameof(parameters));

        var calc = _calculatorRepository.FindByName(name);
        var adapted = CalculatorInterpretation.AdaptTo(calc, Interpretation.Unit);
        return adapted.Calculate(parameters);
    }

    // ── Obliczenia komponentów ─────────────────────────────────────────────────

    public Money CalculateComponent(
        string componentName,
        PricingParameters parameters,
        PricingContext context)
    {
        Guard.IsNotNullOrWhiteSpace(componentName, nameof(componentName));
        Guard.IsNotNull(parameters, nameof(parameters));
        Guard.IsNotNull(context, nameof(context));

        var component = _componentRepository.FindByName(componentName);
        return component.Calculate(parameters, context);
    }

    public PriceBreakdown CalculateComponentBreakdown(
        string componentName,
        PricingParameters parameters,
        PricingContext context)
    {
        Guard.IsNotNullOrWhiteSpace(componentName, nameof(componentName));
        Guard.IsNotNull(parameters, nameof(parameters));
        Guard.IsNotNull(context, nameof(context));

        var component = _componentRepository.FindByName(componentName);
        return component.CalculateBreakdown(parameters, context);
    }

    // ── Diagnostyka ───────────────────────────────────────────────────────────

    public IReadOnlyList<CalculatorView> ListCalculators() =>
        _calculatorRepository.FindAll()
            .Select(c => new CalculatorView(c.Id, c.Name, c.Type, c.Interpretation, c.Formula))
            .ToList();

    public IReadOnlyList<ComponentView> ListComponents() =>
        _componentRepository.FindAll()
            .Select(c => c switch
            {
                VersionedComponent versioned => new ComponentView(
                    versioned.ComponentId,
                    versioned.Name,
                    versioned.Kind.ToString(),
                    versioned.Interpretation,
                    versioned.VersionCount),
                CompositeComponent => new ComponentView(
                    c.ComponentId,
                    c.Name,
                    "Composite",
                    c.Interpretation),
                _ => new ComponentView(
                    c.ComponentId,
                    c.Name,
                    "Simple",
                    c.Interpretation),
            })
            .ToList();

    private VersionedComponent RequireVersionedComponent(string name)
    {
        var component = _componentRepository.FindByName(name);
        if (component is not VersionedComponent versioned)
            throw new InvalidOperationException(
                $"Komponent '{name}' nie obsługuje wersjonowania (oczekiwano {nameof(VersionedComponent)}).");
        return versioned;
    }
}
