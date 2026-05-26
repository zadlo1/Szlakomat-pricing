namespace Szlakomat.Pricing.Domain.Components;

/// <summary>
/// Węzeł drzewa rozbicia ceny — odpowiedź na pytanie „skąd wzięła się ta kwota?".
/// </summary>
public sealed record PriceBreakdown(
    string ComponentName,
    Money Total,
    IReadOnlyList<PriceBreakdown> Children)
{
    public bool IsLeaf => Children.Count == 0;
}
