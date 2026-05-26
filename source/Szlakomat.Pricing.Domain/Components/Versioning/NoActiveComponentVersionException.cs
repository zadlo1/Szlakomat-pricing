namespace Szlakomat.Pricing.Domain.Components.Versioning;

/// <summary>
/// Brak wersji komponentu obowiązującej w danym dniu wizyty.
/// </summary>
public sealed class NoActiveComponentVersionException : InvalidOperationException
{
    public NoActiveComponentVersionException(string componentName, DateOnly visitDate)
        : base($"Komponent '{componentName}' nie ma aktywnej wersji na dzień {visitDate:yyyy-MM-dd}.")
    {
        ComponentName = componentName;
        VisitDate = visitDate;
    }

    public string ComponentName { get; }
    public DateOnly VisitDate { get; }
}
