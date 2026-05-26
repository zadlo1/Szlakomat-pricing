using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Products.Domain.Common.Applicability;
using Szlakomat.Pricing.Domain.Repository;

namespace Szlakomat.Pricing.Domain.Components.Versioning;

internal sealed record SimpleComponentVersionData(
    Validity Validity,
    DateTime DefinedAt,
    string CalculatorName,
    IApplicabilityConstraint Applicability,
    IReadOnlyDictionary<string, string> ParameterMappings,
    bool ContributesToTotal) : IComponentVersionData
{
    public IComponent Materialize(
        Guid componentId,
        string name,
        ICalculatorRepository calculatorRepository,
        IComponentRepository componentRepository)
    {
        _ = componentRepository;
        var calculator = calculatorRepository.FindByName(CalculatorName);
        return new SimpleComponent(
            componentId,
            name,
            calculator,
            Applicability,
            Validity.Always(),
            ParameterMappings,
            ContributesToTotal);
    }

    public bool IsIdenticalTo(IComponentVersionData other) =>
        other is SimpleComponentVersionData simple
        && Validity.Equals(simple.Validity)
        && CalculatorName == simple.CalculatorName
        && ContributesToTotal == simple.ContributesToTotal
        && ApplicabilityConstraintComparer.AreEqual(Applicability, simple.Applicability)
        && ParameterMappingsEqual(ParameterMappings, simple.ParameterMappings);

    private static bool ParameterMappingsEqual(
        IReadOnlyDictionary<string, string> left,
        IReadOnlyDictionary<string, string> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var otherValue) || otherValue != value)
                return false;
        }

        return true;
    }
}
