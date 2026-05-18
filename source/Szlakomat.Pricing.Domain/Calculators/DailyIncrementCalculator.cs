using Szlakomat.Pricing.Domain.Parameters;

namespace Szlakomat.Pricing.Domain.Calculators;

/// <summary>
/// Kalkulator z przyrostem dziennym: startPrice + daysBetween(referenceDate, visitDate) × dailyIncrement.
/// Przykład: Early bird — cena rośnie z każdym dniem bliżej wizyty.
/// visitDate pobierany z PricingParameters pod kluczem CalculatorParameterKeys.VisitDate (DateOnly).
/// </summary>
public sealed class DailyIncrementCalculator : ICalculator
{
    private readonly Money _startPrice;
    private readonly DateOnly _referenceDate;
    private readonly Money _dailyIncrement;

    public DailyIncrementCalculator(
        Guid id,
        string name,
        Money startPrice,
        DateOnly referenceDate,
        Money dailyIncrement)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        Guard.IsNotNull(startPrice, nameof(startPrice));
        Guard.IsNotNull(dailyIncrement, nameof(dailyIncrement));

        Id = id;
        Name = name;
        _startPrice = startPrice;
        _referenceDate = referenceDate;
        _dailyIncrement = dailyIncrement;
    }

    public Guid Id { get; }
    public string Name { get; }
    public Interpretation Interpretation => Interpretation.Unit;
    public string Formula => $"f(date) = {_startPrice} + days({_referenceDate:yyyy-MM-dd}, date) × {_dailyIncrement}";
    public string Type => "DailyIncrement";

    public Money Calculate(PricingParameters parameters)
    {
        Guard.IsNotNull(parameters, nameof(parameters));

        if (!parameters.Contains(CalculatorParameterKeys.VisitDate))
            throw new KeyNotFoundException(
                $"Kalkulator '{Name}' wymaga parametru '{CalculatorParameterKeys.VisitDate}'");

        var rawDate = parameters.GetString(CalculatorParameterKeys.VisitDate);
        var visitDate = DateOnly.Parse(rawDate);

        var days = visitDate.DayNumber - _referenceDate.DayNumber;
        if (days < 0) days = 0;

        return _startPrice.Add(_dailyIncrement.Multiply(days));
    }
}
