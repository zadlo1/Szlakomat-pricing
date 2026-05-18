using Szlakomat.Pricing.Domain.Calculators;

namespace Szlakomat.Pricing.Domain.Repository;

/// <summary>
/// Prosta implementacja in-memory — używana przez PricingFacade i testy Etapu 1.
/// </summary>
public sealed class InMemoryCalculatorRepository : ICalculatorRepository
{
    private readonly Dictionary<Guid, ICalculator> _byId = new();
    private readonly Dictionary<string, ICalculator> _byName = new();

    public void Save(ICalculator calculator)
    {
        Guard.IsNotNull(calculator, nameof(calculator));
        _byId[calculator.Id] = calculator;
        _byName[calculator.Name] = calculator;
    }

    public ICalculator FindByName(string name)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        if (!_byName.TryGetValue(name, out var calc))
            throw new KeyNotFoundException($"Nie znaleziono kalkulatora o nazwie '{name}'");
        return calc;
    }

    public ICalculator FindById(Guid id)
    {
        if (!_byId.TryGetValue(id, out var calc))
            throw new KeyNotFoundException($"Nie znaleziono kalkulatora o id '{id}'");
        return calc;
    }

    public IReadOnlyList<ICalculator> FindAll() => _byId.Values.ToList();
}
