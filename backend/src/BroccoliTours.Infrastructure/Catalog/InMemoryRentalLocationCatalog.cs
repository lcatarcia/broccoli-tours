using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public sealed class InMemoryRentalLocationCatalog : IRentalLocationCatalog
{
    // Location principali RoadSurfer per il noleggio camper in Europa
    private static readonly IReadOnlyList<RentalLocation> RentalLocations = new List<RentalLocation>
    {
        // Germania
        new("de-munich", "Monaco di Baviera", "München", "Germania", 48.1351, 11.5820, "RoadSurfer Station München"),
        new("de-berlin", "Berlino", "Berlin", "Germania", 52.5200, 13.4050, "RoadSurfer Station Berlin"),
        new("de-frankfurt", "Francoforte", "Frankfurt", "Germania", 50.1109, 8.6821, "RoadSurfer Station Frankfurt"),
        new("de-hamburg", "Amburgo", "Hamburg", "Germania", 53.5511, 9.9937, "RoadSurfer Station Hamburg"),
        new("de-cologne", "Colonia", "Köln", "Germania", 50.9375, 6.9603, "RoadSurfer Station Köln"),
        new("de-stuttgart", "Stoccarda", "Stuttgart", "Germania", 48.7758, 9.1829, "RoadSurfer Station Stuttgart"),
        new("de-nuremberg", "Norimberga", "Nürnberg", "Germania", 49.4521, 11.0767, "RoadSurfer Station Nürnberg"),
        
        // Austria
        new("at-vienna", "Vienna", "Wien", "Austria", 48.2082, 16.3738, "RoadSurfer Station Wien"),
        new("at-salzburg", "Salisburgo", "Salzburg", "Austria", 47.8095, 13.0550, "RoadSurfer Station Salzburg"),
        
        // Svizzera
        new("ch-zurich", "Zurigo", "Zürich", "Svizzera", 47.3769, 8.5417, "RoadSurfer Station Zürich"),
        
        // Italia
        new("it-milan", "Milano", "Milano", "Italia", 45.4642, 9.1900, "RoadSurfer Station Milano"),
        new("it-venice", "Venezia", "Venezia", "Italia", 45.4408, 12.3155, "RoadSurfer Station Venezia"),
        new("it-rome", "Roma", "Roma", "Italia", 41.9028, 12.4964, "RoadSurfer Station Roma"),
        
        // Francia
        new("fr-paris", "Parigi", "Paris", "Francia", 48.8566, 2.3522, "RoadSurfer Station Paris"),
        new("fr-lyon", "Lione", "Lyon", "Francia", 45.7640, 4.8357, "RoadSurfer Station Lyon"),
        new("fr-marseille", "Marsiglia", "Marseille", "Francia", 43.2965, 5.3698, "RoadSurfer Station Marseille"),
        new("fr-bordeaux", "Bordeaux", "Bordeaux", "Francia", 44.8378, -0.5792, "RoadSurfer Station Bordeaux"),
        
        // Spagna
        new("es-barcelona", "Barcellona", "Barcelona", "Spagna", 41.3851, 2.1734, "RoadSurfer Station Barcelona"),
        new("es-madrid", "Madrid", "Madrid", "Spagna", 40.4168, -3.7038, "RoadSurfer Station Madrid"),
        new("es-valencia", "Valencia", "Valencia", "Spagna", 39.4699, -0.3763, "RoadSurfer Station Valencia"),
        
        // Portogallo
        new("pt-lisbon", "Lisbona", "Lisboa", "Portogallo", 38.7223, -9.1393, "RoadSurfer Station Lisboa"),
        new("pt-porto", "Porto", "Porto", "Portogallo", 41.1579, -8.6291, "RoadSurfer Station Porto"),
        
        // Paesi Bassi
        new("nl-amsterdam", "Amsterdam", "Amsterdam", "Paesi Bassi", 52.3676, 4.9041, "RoadSurfer Station Amsterdam"),
        
        // Belgio
        new("be-brussels", "Bruxelles", "Brussels", "Belgio", 50.8503, 4.3517, "RoadSurfer Station Brussels"),
        
        // Regno Unito
        new("uk-london", "Londra", "London", "Regno Unito", 51.5074, -0.1278, "RoadSurfer Station London"),
        
        // Danimarca
        new("dk-copenhagen", "Copenaghen", "København", "Danimarca", 55.6761, 12.5683, "RoadSurfer Station København"),
        
        // Svezia
        new("se-stockholm", "Stoccolma", "Stockholm", "Svezia", 59.3293, 18.0686, "RoadSurfer Station Stockholm"),
        
        // Norvegia
        new("no-oslo", "Oslo", "Oslo", "Norvegia", 59.9139, 10.7522, "RoadSurfer Station Oslo"),
    };

    public IReadOnlyList<RentalLocation> GetAll() =>
        RentalLocations.OrderBy(l => l.Country).ThenBy(l => l.City).ToList();

    public RentalLocation? FindById(string id) => RentalLocations.FirstOrDefault(l => l.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
