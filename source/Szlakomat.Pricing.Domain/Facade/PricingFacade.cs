using Szlakomat.Pricing.Domain.Calculators;
using Szlakomat.Pricing.Domain.Calculators.Adapters;
using Szlakomat.Pricing.Domain.Parameters;
using Szlakomat.Pricing.Domain.Repository;

namespace Szlakomat.Pricing.Domain.Facade;

/// <summary>
/// Główny punkt wejścia Etapu 1.
/// Rejestruje kalkulatory i orkiestruje obliczenia z automatyczną konwersją interpretacji.
/// </summary>
public sealed class PricingFacade
{
    private readonly ICalculatorRepository _repository;

    public PricingFacade() : this(new InMemoryCalculatorRepository()) { }

    public PricingFacade(ICalculatorRepository repository)
    {
        Guard.IsNotNull(repository, nameof(repository));
        _repository = repository;
    }

    // ── Rejestracja ──────────────────────────────────────────────────────────

    public PricingFacade AddFixedCalculator(string name, Money price)
    {
        var calc = new SimpleFixedCalculator(Guid.NewGuid(), name, price);
        _repository.Save(calc);
        return this;
    }

    public PricingFacade AddPercentageCalculator(string name, decimal ratePercent)
    {
        var calc = new PercentageCalculator(Guid.NewGuid(), name, ratePercent);
        _repository.Save(calc);
        return this;
    }

    public PricingFacade AddStepFunctionCalculator(
        string name,
        Money basePrice,
        int stepSize,
        Money increment)
    {
        var calc = new StepFunctionCalculator(Guid.NewGuid(), name, basePrice, stepSize, increment);
        _repository.Save(calc);
        return this;
    }

    public PricingFacade AddDiscreteCalculator(string name, IReadOnlyDictionary<int, Money> points)
    {
        var calc = new DiscretePointsCalculator(Guid.NewGuid(), name, points);
        _repository.Save(calc);
        return this;
    }

    public PricingFacade AddDailyIncrementCalculator(
        string name,
        Money startPrice,
        DateOnly referenceDate,
        Money dailyIncrement)
    {
        var calc = new DailyIncrementCalculator(Guid.NewGuid(), name, startPrice, referenceDate, dailyIncrement);
        _repository.Save(calc);
        return this;
    }

    // ── Obliczenia ───────────────────────────────────────────────────────────

    /// <summary>Zwraca wynik w natywnej interpretacji kalkulatora.</summary>
    public Money Calculate(string name, PricingParameters parameters)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(parameters, nameof(parameters));

        var calc = _repository.FindByName(name);
        return calc.Calculate(parameters);
    }

    /// <summary>Zwraca Total — automatycznie adaptuje, jeśli kalkulator ma inną interpretację.</summary>
    public Money CalculateTotal(string name, PricingParameters parameters)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(parameters, nameof(parameters));

        var calc = _repository.FindByName(name);
        var adapted = AdaptTo(calc, Interpretation.Total);
        return adapted.Calculate(parameters);
    }

    /// <summary>Zwraca Unit — automatycznie adaptuje, jeśli kalkulator ma inną interpretację.</summary>
    public Money CalculateUnitPrice(string name, PricingParameters parameters)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(parameters, nameof(parameters));

        var calc = _repository.FindByName(name);
        var adapted = AdaptTo(calc, Interpretation.Unit);
        return adapted.Calculate(parameters);
    }

    // ── Diagnostyka ──────────────────────────────────────────────────────────

    public IReadOnlyList<CalculatorView> ListCalculators() =>
        _repository.FindAll()
            .Select(c => new CalculatorView(c.Id, c.Name, c.Type, c.Interpretation, c.Formula))
            .ToList();

    // ── Prywatne ─────────────────────────────────────────────────────────────

    private static ICalculator AdaptTo(ICalculator calc, Interpretation target)
    {
        if (calc.Interpretation == target)
            return calc;

        return (calc.Interpretation, target) switch
        {
            (Interpretation.Unit, Interpretation.Total) => new UnitToTotalAdapter(calc),
            (Interpretation.Total, Interpretation.Unit) => new TotalToUnitAdapter(calc),
            (Interpretation.Total, Interpretation.Marginal) => new TotalToMarginalAdapter(calc),
            _ => throw new InvalidOperationException(
                $"Brak adaptera z interpretacji {calc.Interpretation} do {target}")
        };
    }
}
