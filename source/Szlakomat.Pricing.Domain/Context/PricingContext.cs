using Szlakomat.Products.Domain.Common.Applicability;

namespace Szlakomat.Pricing.Domain.Context;

/// <summary>
/// Jawny parametr przekazywany do każdego kalkulatora i komponentu.
/// Zawiera wszystko co zewnętrzne wobec matematyki cennika.
/// </summary>
public sealed class PricingContext
{
    public DateOnly VisitDate { get; }
    public DateTime Timestamp { get; }
    public string CustomerType { get; }
    public int GroupSize { get; }

    private PricingContext(DateOnly visitDate, DateTime timestamp, string customerType, int groupSize)
    {
        Guard.IsGreaterThanOrEqualTo(groupSize, 1, nameof(groupSize));
        Guard.IsNotNullOrWhiteSpace(customerType, nameof(customerType));

        VisitDate = visitDate;
        Timestamp = timestamp;
        CustomerType = customerType.ToUpperInvariant();
        GroupSize = groupSize;
    }

    /// <summary>Klient indywidualny, typ STANDARD.</summary>
    public static PricingContext Individual(DateOnly visitDate) =>
        new(visitDate, DateTime.UtcNow, CustomerTypes.Standard, 1);

    /// <summary>Klient z jawnym typem, rozmiar grupy = 1.</summary>
    public static PricingContext For(DateOnly visitDate, string customerType) =>
        new(visitDate, DateTime.UtcNow, customerType, 1);

    /// <summary>Dla grup z jawnym typem klienta i rozmiarem.</summary>
    public static PricingContext ForGroup(DateOnly visitDate, string customerType, int groupSize) =>
        new(visitDate, DateTime.UtcNow, customerType, groupSize);

    /// <summary>
    /// Zwraca Dictionary kompatybilny z ApplicabilityContext.Of(...)
    /// z Products.Domain — istniejące IApplicabilityConstraint działają bez zmian.
    /// </summary>
    public Dictionary<string, string> ToApplicabilityDictionary() => new()
    {
        [ApplicabilityKeys.VisitDate] = VisitDate.ToString("yyyy-MM-dd"),
        [ApplicabilityKeys.CustomerType] = CustomerType,
        [ApplicabilityKeys.GroupSize] = GroupSize.ToString(),
    };

    public ApplicabilityContext ToApplicabilityContext() =>
        ApplicabilityContext.Of(ToApplicabilityDictionary());
}
