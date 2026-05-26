using Microsoft.Extensions.DependencyInjection;
using Szlakomat.Pricing.Application;
using Szlakomat.Pricing.Application.Integration;
using Szlakomat.Pricing.Domain.Facade;
using Szlakomat.Pricing.Domain.Repository;
using Szlakomat.Pricing.Infrastructure.Integration;
using Szlakomat.Pricing.Infrastructure.Seed;
using Szlakomat.Products.Domain.CommercialOffer;

namespace Szlakomat.Pricing.Infrastructure;

public static class PricingServiceExtensions
{
    public static void AddPricingModule(this IServiceCollection services)
    {
        var calculatorRepository = new InMemoryCalculatorRepository();
        var componentRepository = new InMemoryComponentRepository();
        var facade = new PricingFacade(calculatorRepository, componentRepository);
        WawelPricingSeed.SeedComponents(facade);

        services.AddSingleton<ICalculatorRepository>(calculatorRepository);
        services.AddSingleton<IComponentRepository>(componentRepository);
        services.AddSingleton(facade);

        services.AddSingleton<ICatalogEntryPricingRepository>(sp =>
        {
            var pricingMappings = new InMemoryCatalogEntryPricingRepository();
            var catalogRepository = sp.GetRequiredService<ICatalogEntryRepository>();
            WawelPricingSeed.SeedCatalogMappings(pricingMappings, catalogRepository);
            return pricingMappings;
        });

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<PricingModule>());
    }
}
