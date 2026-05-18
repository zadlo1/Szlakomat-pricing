using Szlakomat.Pricing.Domain.Calculators;

namespace Szlakomat.Pricing.Domain.Facade;

public sealed record CalculatorView(
    Guid Id,
    string Name,
    string Type,
    Interpretation Interpretation,
    string Formula);
