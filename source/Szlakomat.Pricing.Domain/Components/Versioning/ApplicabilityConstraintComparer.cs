using Szlakomat.Products.Domain.Common.Applicability;

namespace Szlakomat.Pricing.Domain.Components.Versioning;

internal static class ApplicabilityConstraintComparer
{
    public static bool AreEqual(IApplicabilityConstraint? left, IApplicabilityConstraint? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        if (left.GetType() != right.GetType())
            return false;

        return left switch
        {
            AlwaysTrueConstraint => right is AlwaysTrueConstraint,
            EqualsConstraint l => right is EqualsConstraint r
                && l.ParameterName == r.ParameterName
                && l.ExpectedValue == r.ExpectedValue,
            InConstraint l => right is InConstraint r
                && l.ParameterName == r.ParameterName
                && l.AllowedValues.SetEquals(r.AllowedValues),
            GreaterThanConstraint l => right is GreaterThanConstraint r
                && l.ParameterName == r.ParameterName
                && l.Threshold == r.Threshold,
            LessThanConstraint l => right is LessThanConstraint r
                && l.ParameterName == r.ParameterName
                && l.Threshold == r.Threshold,
            BetweenConstraint l => right is BetweenConstraint r
                && l.ParameterName == r.ParameterName
                && l.Min == r.Min
                && l.Max == r.Max,
            AndConstraint l => right is AndConstraint r
                && l.Constraints.Count == r.Constraints.Count
                && l.Constraints.Zip(r.Constraints).All(pair => AreEqual(pair.First, pair.Second)),
            OrConstraint l => right is OrConstraint r
                && l.Constraints.Count == r.Constraints.Count
                && l.Constraints.Zip(r.Constraints).All(pair => AreEqual(pair.First, pair.Second)),
            NotConstraint l => right is NotConstraint r
                && AreEqual(l.Constraint, r.Constraint),
            _ => false,
        };
    }
}
