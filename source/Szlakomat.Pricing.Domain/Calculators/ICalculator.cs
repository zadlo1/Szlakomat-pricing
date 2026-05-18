using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Calculators;

/// <summary>Sens zwracanej przez kalkulator kwoty.</summary>
public enum Interpretation
{
    /// <summary>Łączna kwota za całą grupę / okres.</summary>
    Total,

    /// <summary>Cena za jedną osobę / jednostkę.</summary>
    Unit,

    /// <summary>Cena n-tej jednostki (przy cennikach malejących / rosnących).</summary>
    Marginal
}

/// <summary>
/// Kalkulator przyjmujący PricingParameters i zwracający Money.
/// Deklaruje Interpretation, Formula i Type.
/// </summary>
public interface ICalculator
{
    Guid Id { get; }
    string Name { get; }
    Interpretation Interpretation { get; }

    /// <summary>Czytelny opis formuły, np. "f(x) = 47.00 PLN".</summary>
    string Formula { get; }

    string Type { get; }

    Money Calculate(PricingParameters parameters);
}
