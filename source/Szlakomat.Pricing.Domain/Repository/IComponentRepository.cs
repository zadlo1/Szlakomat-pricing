using Szlakomat.Pricing.Domain.Components;

namespace Szlakomat.Pricing.Domain.Repository;

public interface IComponentRepository
{
    void Save(IComponent component);
    IComponent FindByName(string name);
    IComponent FindById(Guid id);
    IReadOnlyList<IComponent> FindAll();
}
