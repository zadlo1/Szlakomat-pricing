using Szlakomat.Products.Domain.CommercialOffer;

namespace Szlakomat.Pricing.Domain.Components.Versioning;

/// <summary>
/// Wybiera wersję komponentu: najpóźniejszy <c>validFrom</c>, potem najpóźniejszy <c>DefinedAt</c>.
/// </summary>
internal static class ComponentVersionSelector
{
    public static T Select<T>(
        IReadOnlyList<T> versions,
        DateOnly visitDate,
        string componentName,
        Func<T, Validity> getValidity,
        Func<T, DateTime> getDefinedAt)
    {
        Guard.IsNotNull(versions, nameof(versions));
        if (versions.Count == 0)
            throw new NoActiveComponentVersionException(componentName, visitDate);

        var candidates = versions
            .Where(v => getValidity(v).IsValidAt(visitDate))
            .ToList();

        if (candidates.Count == 0)
            throw new NoActiveComponentVersionException(componentName, visitDate);

        return candidates
            .OrderByDescending(v => getValidity(v).From ?? DateOnly.MinValue)
            .ThenByDescending(getDefinedAt)
            .First();
    }
}
