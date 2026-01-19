using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public sealed class InMemoryLocationCatalog : ILocationCatalog
{
    private static readonly IReadOnlyList<Location> Locations = new List<Location>
    {
        new("it-tuscany", "Toscana Slow Roads", "IT", "Toscana", 43.7711, 11.2486, "Borghi, colline e strade panoramiche."),
        new("it-dolomites", "Dolomiti & Passi", "IT", "Trentino-Alto Adige", 46.4102, 11.8440, "Panorami alpini e passi leggendari."),
        new("it-puglia", "Puglia Coste & Masserie", "IT", "Puglia", 40.8518, 17.1220, "Mare, trulli e cucina locale."),
        new("fr-provence", "Provence (fuori rotta)", "FR", "Provence-Alpes-Côte d’Azur", 43.9493, 4.8055, "Lavanda, villaggi e mercati."),
    };

    public IReadOnlyList<Location> GetAll() => Locations;

    public Location? FindById(string id) => Locations.FirstOrDefault(l => l.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
