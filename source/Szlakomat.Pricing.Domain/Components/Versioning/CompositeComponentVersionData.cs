using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Repository;

namespace Szlakomat.Pricing.Domain.Components.Versioning;

internal sealed record CompositeComponentVersionData(
    Validity Validity,
    DateTime DefinedAt,
    IReadOnlyList<string> ChildNames,
    IApplicabilityConstraint Applicability,
    IReadOnlyList<ParameterDependency> ParameterDependencies) : IComponentVersionData
{
    public IComponent Materialize(
        Guid componentId,
        string name,
        ICalculatorRepository calculatorRepository,
        IComponentRepository componentRepository)
    {
        _ = calculatorRepository;
        var children = ChildNames
            .Select(childName => componentRepository.FindByName(childName))
            .ToList();

        return new CompositeComponent(
            componentId,
            name,
            children,
            Applicability,
            Validity.Always(),
            ParameterDependencies);
    }

    public bool IsIdenticalTo(IComponentVersionData other) =>
        other is CompositeComponentVersionData composite
        && Validity.Equals(composite.Validity)
        && ApplicabilityConstraintComparer.AreEqual(Applicability, composite.Applicability)
        && ChildNames.SequenceEqual(composite.ChildNames)
        && DependenciesEqual(ParameterDependencies, composite.ParameterDependencies);

    private static bool DependenciesEqual(
        IReadOnlyList<ParameterDependency> left,
        IReadOnlyList<ParameterDependency> right)
    {
        if (left.Count != right.Count)
            return false;

        return left.Zip(right).All(pair =>
            pair.First.DependentChildName == pair.Second.DependentChildName
            && pair.First.ParameterKey == pair.Second.ParameterKey
            && pair.First.SourceChildNames.SequenceEqual(pair.Second.SourceChildNames));
    }
}
