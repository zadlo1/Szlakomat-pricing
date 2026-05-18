namespace Szlakomat.Pricing.Domain.Calculators;

public static class CalculatorParameterKeys
{
    /// <summary>int — liczba osób / jednostek.</summary>
    public const string Quantity = "quantity";

    /// <summary>DateOnly — data wizyty; używana przez DailyIncrementCalculator.</summary>
    public const string VisitDate = "visitDate";
}
