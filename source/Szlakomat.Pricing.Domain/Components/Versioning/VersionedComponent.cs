using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Pricing.Domain.Context;
using Szlakomat.Pricing.Domain.Parameters;
using Szlakomat.Pricing.Domain.Repository;

namespace Szlakomat.Pricing.Domain.Components.Versioning;

/// <summary>
/// Komponent z historią wersji — przy obliczeniu wybiera snapshot obowiązujący w dniu wizyty.
/// </summary>
public sealed class VersionedComponent : IComponent
{
    private readonly List<IComponentVersionData> _versions;
    private readonly ComponentKind _kind;
    private readonly ICalculatorRepository _calculatorRepository;
    private readonly IComponentRepository _componentRepository;

    internal VersionedComponent(
        Guid componentId,
        string name,
        ComponentKind kind,
        IEnumerable<IComponentVersionData> versions,
        ICalculatorRepository calculatorRepository,
        IComponentRepository componentRepository)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(versions, nameof(versions));

        var versionList = versions.ToList();
        if (versionList.Count == 0)
            throw new ArgumentException("Wymagana co najmniej jedna wersja.", nameof(versions));

        ComponentId = componentId;
        Name = name;
        _kind = kind;
        _versions = versionList;
        _calculatorRepository = calculatorRepository;
        _componentRepository = componentRepository;
        ContributesToTotal = ResolveContributesToTotal(kind, versionList);
    }

    public Guid ComponentId { get; }
    public string Name { get; }
    public Calculators.Interpretation Interpretation => Calculators.Interpretation.Total;
    public bool ContributesToTotal { get; }
    public ComponentKind Kind => _kind;
    public int VersionCount => _versions.Count;
    public IReadOnlyList<ComponentVersion> Versions =>
        _versions.Select(v => new ComponentVersion(v.Validity, v.DefinedAt)).ToList();

    public Money Calculate(PricingParameters parameters, PricingContext context)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        Guard.IsNotNull(context, nameof(context));

        var active = ResolveActiveVersion(context.VisitDate);
        return active.Calculate(parameters, context);
    }

    public PriceBreakdown CalculateBreakdown(PricingParameters parameters, PricingContext context)
    {
        Guard.IsNotNull(parameters, nameof(parameters));
        Guard.IsNotNull(context, nameof(context));

        var active = ResolveActiveVersion(context.VisitDate);
        return active.CalculateBreakdown(parameters, context);
    }

    internal void AddVersion(
        IComponentVersionData version,
        VersionAdditionStrategy strategy)
    {
        Guard.IsNotNull(version, nameof(version));
        EnsureKindMatches(version);

        if (strategy == VersionAdditionStrategy.RejectIdentical
            && _versions.Any(existing => existing.IsIdenticalTo(version)))
        {
            throw new DuplicateComponentVersionException(Name);
        }

        _versions.Add(version);
    }

    private IComponent ResolveActiveVersion(DateOnly visitDate)
    {
        var snapshot = ComponentVersionSelector.Select(
            _versions,
            visitDate,
            Name,
            v => v.Validity,
            v => v.DefinedAt);

        return snapshot.Materialize(
            ComponentId,
            Name,
            _calculatorRepository,
            _componentRepository);
    }

    private void EnsureKindMatches(IComponentVersionData version)
    {
        var matches = (_kind, version) switch
        {
            (ComponentKind.Simple, SimpleComponentVersionData) => true,
            (ComponentKind.Composite, CompositeComponentVersionData) => true,
            _ => false,
        };

        if (!matches)
            throw new ArgumentException($"Wersja nie pasuje do rodzaju komponentu '{_kind}'.", nameof(version));
    }

    private static bool ResolveContributesToTotal(
        ComponentKind kind,
        IReadOnlyList<IComponentVersionData> versions) =>
        kind switch
        {
            ComponentKind.Composite => true,
            ComponentKind.Simple => versions.OfType<SimpleComponentVersionData>().Last().ContributesToTotal,
            _ => true,
        };
}

public enum ComponentKind
{
    Simple,
    Composite,
}

/// <summary>
/// Metadane wersji komponentu (okres obowiązywania i moment definicji).
/// </summary>
public sealed record ComponentVersion(Validity Validity, DateTime DefinedAt);
