using Szlakomat.Pricing.Domain.Calculators;

namespace Szlakomat.Pricing.Domain.Facade;

public sealed record ComponentView(
    Guid Id,
    string Name,
    string Kind,
    Calculators.Interpretation Interpretation);
