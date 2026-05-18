namespace Szlakomat.Pricing.Domain.Parameters;

/// <summary>
/// Niemutowalna mapa string → object przekazywana do ICalculator.Calculate().
/// Każde With(key, value) zwraca nową instancję.
/// </summary>
public sealed class PricingParameters
{
    private readonly IReadOnlyDictionary<string, object> _values;

    private PricingParameters(IReadOnlyDictionary<string, object> values)
    {
        _values = new Dictionary<string, object>(values);
    }

    public static PricingParameters Empty() => new(new Dictionary<string, object>());

    public static PricingParameters Of(string key, object value)
    {
        Guard.IsNotNullOrWhiteSpace(key, nameof(key));
        Guard.IsNotNull(value, nameof(value));
        return new PricingParameters(new Dictionary<string, object> { [key] = value });
    }

    public PricingParameters With(string key, object value)
    {
        Guard.IsNotNullOrWhiteSpace(key, nameof(key));
        Guard.IsNotNull(value, nameof(value));
        var updated = new Dictionary<string, object>(_values) { [key] = value };
        return new PricingParameters(updated);
    }

    public bool Contains(string key) => _values.ContainsKey(key);

    public Money GetMoney(string key)
    {
        if (!_values.TryGetValue(key, out var raw))
            throw new KeyNotFoundException($"Brak parametru: '{key}'");
        if (raw is not Money money)
            throw new InvalidCastException($"Parametr '{key}' nie jest typu Money (jest: {raw.GetType().Name})");
        return money;
    }

    public decimal GetDecimal(string key)
    {
        if (!_values.TryGetValue(key, out var raw))
            throw new KeyNotFoundException($"Brak parametru: '{key}'");
        return raw switch
        {
            decimal d => d,
            int i => (decimal)i,
            double dbl => (decimal)dbl,
            _ => throw new InvalidCastException($"Parametr '{key}' nie jest liczbą (jest: {raw.GetType().Name})")
        };
    }

    public int GetInt(string key)
    {
        if (!_values.TryGetValue(key, out var raw))
            throw new KeyNotFoundException($"Brak parametru: '{key}'");
        if (raw is not int i)
            throw new InvalidCastException($"Parametr '{key}' nie jest int (jest: {raw.GetType().Name})");
        return i;
    }

    public string GetString(string key)
    {
        if (!_values.TryGetValue(key, out var raw))
            throw new KeyNotFoundException($"Brak parametru: '{key}'");
        if (raw is not string s)
            throw new InvalidCastException($"Parametr '{key}' nie jest string (jest: {raw.GetType().Name})");
        return s;
    }
}
