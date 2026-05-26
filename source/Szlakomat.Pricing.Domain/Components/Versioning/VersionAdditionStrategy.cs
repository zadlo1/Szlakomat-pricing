namespace Szlakomat.Pricing.Domain.Components.Versioning;

/// <summary>
/// Strategia dodawania nowej wersji komponentu.
/// </summary>
public enum VersionAdditionStrategy
{
    /// <summary>Odrzuca wersję identyczną z już istniejącą (domyślna).</summary>
    RejectIdentical,

    /// <summary>Pozwala na duplikaty — używane w testach.</summary>
    AllowAll,
}
