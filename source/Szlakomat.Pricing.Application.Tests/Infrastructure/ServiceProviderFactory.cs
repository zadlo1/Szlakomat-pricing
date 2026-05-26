using Microsoft.Extensions.DependencyInjection;
using Szlakomat.Pricing.Infrastructure;
using Szlakomat.Products.Infrastructure;

namespace Szlakomat.Pricing.Application.Tests.Infrastructure;

internal static class ServiceProviderFactory
{
    public static IServiceProvider Create()
    {
        var services = new ServiceCollection();
        services.AddProductModule();
        services.AddPricingModule();
        return services.BuildServiceProvider();
    }
}
