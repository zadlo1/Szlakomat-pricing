using Szlakomat.Pricing.Domain.Components;

namespace Szlakomat.Pricing.Domain.Repository;

/// <summary>
/// Prosta implementacja in-memory — używana przez PricingFacade i testy.
/// </summary>
public sealed class InMemoryComponentRepository : IComponentRepository
{
    private readonly Dictionary<Guid, IComponent> _byId = new();
    private readonly Dictionary<string, IComponent> _byName = new();

    public void Save(IComponent component)
    {
        Guard.IsNotNull(component, nameof(component));
        _byId[component.ComponentId] = component;
        _byName[component.Name] = component;
    }

    public IComponent FindByName(string name)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        if (!_byName.TryGetValue(name, out var component))
            throw new KeyNotFoundException($"Nie znaleziono komponentu o nazwie '{name}'");
        return component;
    }

    public IComponent FindById(Guid id)
    {
        if (!_byId.TryGetValue(id, out var component))
            throw new KeyNotFoundException($"Nie znaleziono komponentu o id '{id}'");
        return component;
    }

    public IReadOnlyList<IComponent> FindAll() => _byId.Values.ToList();
}
