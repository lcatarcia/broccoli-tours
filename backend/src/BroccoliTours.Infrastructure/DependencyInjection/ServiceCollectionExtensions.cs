using BroccoliTours.Infrastructure.Catalog;
using BroccoliTours.Infrastructure.Itineraries;
using BroccoliTours.Infrastructure.Pdf;
using Microsoft.Extensions.DependencyInjection;

namespace BroccoliTours.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBroccoliToursInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICamperCatalog, InMemoryCamperCatalog>();
        services.AddSingleton<ILocationCatalog, InMemoryLocationCatalog>();
        services.AddSingleton<StubItineraryEngine>();
        services.AddSingleton<IItineraryStore, InMemoryItineraryStore>();
        services.AddSingleton<IPdfGenerator, SimplePdfGenerator>();
        return services;
    }
}
