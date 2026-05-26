using Szlakomat.Products.Domain.CommercialOffer;
using Szlakomat.Pricing.Domain.Repository;

namespace Szlakomat.Pricing.Domain.Components.Versioning;

/// <summary>
/// Snapshot konfiguracji jednej wersji komponentu.
/// </summary>
internal interface IComponentVersionData
{
    Validity Validity { get; }
    DateTime DefinedAt { get; }

    IComponent Materialize(
        Guid componentId,
        string name,
        ICalculatorRepository calculatorRepository,
        IComponentRepository componentRepository);

    bool IsIdenticalTo(IComponentVersionData other);
}
