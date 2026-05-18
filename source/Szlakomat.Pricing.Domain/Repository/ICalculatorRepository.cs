namespace Szlakomat.Pricing.Domain.Repository;

public interface ICalculatorRepository
{
    void Save(Calculators.ICalculator calculator);
    Calculators.ICalculator FindByName(string name);
    Calculators.ICalculator FindById(Guid id);
    IReadOnlyList<Calculators.ICalculator> FindAll();
}
