namespace Szlakomat.Pricing.Domain.Components;

/// <summary>
/// Wstrzykuje sumę wyników wskazanych dzieci do parametru kalkulatora zależnego dziecka
/// (np. baseAmount dla VAT od sumy netto).
/// </summary>
public sealed record ParameterDependency(
    string DependentChildName,
    string ParameterKey,
    IReadOnlyList<string> SourceChildNames);
