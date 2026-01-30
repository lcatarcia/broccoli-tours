using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public sealed class InMemoryRentalLocationCatalog : IRentalLocationCatalog
{
    // Location RoadSurfer ufficiali per il noleggio camper in Europa
    // Basato sui dati ufficiali da roadsurfer.com (aggiornato gennaio 2025)
    private static readonly IReadOnlyList<RentalLocation> RentalLocations = new List<RentalLocation>
    {
        // Austria (4 locations)
        new("at-graz", "Graz", "Graz", "Austria", 47.0707, 15.4395, "RoadSurfer Station Graz"),
        new("at-innsbruck", "Innsbruck", "Innsbruck", "Austria", 47.2692, 11.4041, "RoadSurfer Station Innsbruck"),
        new("at-salzburg", "Salisburgo", "Salzburg", "Austria", 47.8095, 13.0550, "RoadSurfer Station Salzburg"),
        new("at-vienna", "Vienna", "Wien", "Austria", 48.2082, 16.3738, "RoadSurfer Station Wien"),
        
        // Francia (9 locations)
        new("fr-bordeaux", "Bordeaux", "Bordeaux", "Francia", 44.8378, -0.5792, "RoadSurfer Station Bordeaux"),
        new("fr-geneva-gex", "Geneva-Gex", "Geneva-Gex", "Francia", 46.2044, 6.1432, "RoadSurfer Station Geneva-Gex"),
        new("fr-lille", "Lille", "Lille", "Francia", 50.6292, 3.0573, "RoadSurfer Station Lille"),
        new("fr-lyon", "Lione", "Lyon", "Francia", 45.7640, 4.8357, "RoadSurfer Station Lyon"),
        new("fr-marseille", "Marsiglia-Aix", "Marseille", "Francia", 43.2965, 5.3698, "RoadSurfer Station Marseille-Aix"),
        new("fr-nantes", "Nantes", "Nantes", "Francia", 47.2184, -1.5536, "RoadSurfer Station Nantes"),
        new("fr-nice", "Nizza", "Nice", "Francia", 43.7102, 7.2620, "RoadSurfer Station Nice"),
        new("fr-paris", "Parigi", "Paris", "Francia", 48.8566, 2.3522, "RoadSurfer Station Paris"),
        new("fr-toulouse", "Toulouse", "Toulouse", "Francia", 43.6047, 1.4442, "RoadSurfer Station Toulouse"),
        
        // Germania (33 locations)
        new("de-aachen", "Aachen", "Aachen", "Germania", 50.7753, 6.0839, "RoadSurfer Station Aachen"),
        new("de-augsburg", "Augsburg", "Augsburg", "Germania", 48.3705, 10.8978, "RoadSurfer Station Augsburg"),
        new("de-berlin", "Berlino", "Berlin", "Germania", 52.5200, 13.4050, "RoadSurfer Station Berlin"),
        new("de-bielefeld", "Bielefeld", "Bielefeld", "Germania", 52.0302, 8.5325, "RoadSurfer Station Bielefeld"),
        new("de-bochum", "Bochum", "Bochum", "Germania", 51.4818, 7.2162, "RoadSurfer Station Bochum"),
        new("de-bremen", "Brema", "Bremen", "Germania", 53.0793, 8.8017, "RoadSurfer Station Bremen"),
        new("de-cologne", "Colonia", "Köln", "Germania", 50.9375, 6.9603, "RoadSurfer Station Köln"),
        new("de-cologne-bonn", "Colonia-Bonn", "Köln-Bonn", "Germania", 50.8659, 7.1428, "RoadSurfer Station Köln-Bonn"),
        new("de-cologne-dusseldorf", "Colonia-Düsseldorf", "Köln-Düsseldorf", "Germania", 51.2277, 6.7735, "RoadSurfer Station Köln-Düsseldorf"),
        new("de-constance", "Costanza (Aach)", "Konstanz", "Germania", 47.6779, 8.8937, "RoadSurfer Station Konstanz"),
        new("de-dresden", "Dresda", "Dresden", "Germania", 51.0504, 13.7373, "RoadSurfer Station Dresden"),
        new("de-duisburg", "Duisburg", "Duisburg", "Germania", 51.4344, 6.7623, "RoadSurfer Station Duisburg"),
        new("de-erfurt", "Erfurt", "Erfurt", "Germania", 50.9848, 11.0299, "RoadSurfer Station Erfurt"),
        new("de-frankfurt", "Francoforte", "Frankfurt", "Germania", 50.1109, 8.6821, "RoadSurfer Station Frankfurt"),
        new("de-freiburg", "Friburgo", "Freiburg", "Germania", 47.9990, 7.8421, "RoadSurfer Station Freiburg"),
        new("de-freiburg-basel", "Friburgo-Basilea", "Freiburg-Basel", "Germania", 47.9959, 7.8494, "RoadSurfer Station Freiburg-Basel"),
        new("de-hamburg", "Amburgo", "Hamburg", "Germania", 53.5511, 9.9937, "RoadSurfer Station Hamburg"),
        new("de-hanover", "Hannover", "Hannover", "Germania", 52.3759, 9.7320, "RoadSurfer Station Hannover"),
        new("de-heidelberg", "Heidelberg", "Heidelberg", "Germania", 49.3988, 8.6724, "RoadSurfer Station Heidelberg"),
        new("de-karlsruhe", "Karlsruhe-Ettlingen", "Karlsruhe", "Germania", 49.0069, 8.4037, "RoadSurfer Station Karlsruhe"),
        new("de-kassel", "Kassel", "Kassel", "Germania", 51.3127, 9.4797, "RoadSurfer Station Kassel"),
        new("de-kiel", "Kiel", "Kiel", "Germania", 54.3233, 10.1228, "RoadSurfer Station Kiel"),
        new("de-leipzig", "Lipsia", "Leipzig", "Germania", 51.3397, 12.3731, "RoadSurfer Station Leipzig"),
        new("de-lindau", "Lindau-Wangen", "Lindau", "Germania", 47.5460, 9.6842, "RoadSurfer Station Lindau"),
        new("de-lubeck", "Lubecca", "Lübeck", "Germania", 53.8655, 10.6866, "RoadSurfer Station Lübeck"),
        new("de-mainz", "Magonza", "Mainz", "Germania", 49.9929, 8.2473, "RoadSurfer Station Mainz"),
        new("de-munich", "Monaco di Baviera", "München", "Germania", 48.1351, 11.5820, "RoadSurfer Station München"),
        new("de-nuremberg", "Norimberga", "Nürnberg", "Germania", 49.4521, 11.0767, "RoadSurfer Station Nürnberg"),
        new("de-regensburg", "Ratisbona", "Regensburg", "Germania", 49.0134, 12.0961, "RoadSurfer Station Regensburg"),
        new("de-stuttgart", "Stoccarda", "Stuttgart", "Germania", 48.7758, 9.1829, "RoadSurfer Station Stuttgart"),
        new("de-trier", "Treviri", "Trier", "Germania", 49.7596, 6.6441, "RoadSurfer Station Trier"),
        new("de-ulm", "Ulm", "Ulm", "Germania", 48.3984, 9.9917, "RoadSurfer Station Ulm"),
        new("de-wurzburg", "Würzburg", "Würzburg", "Germania", 49.7913, 9.9534, "RoadSurfer Station Würzburg"),
        
        // Italia (5 locations)
        new("it-florence", "Firenze", "Firenze", "Italia", 43.7696, 11.2558, "RoadSurfer Station Firenze"),
        new("it-milan", "Milano", "Milano", "Italia", 45.4642, 9.1900, "RoadSurfer Station Milano"),
        new("it-rome", "Roma", "Roma", "Italia", 41.9028, 12.4964, "RoadSurfer Station Roma"),
        new("it-turin", "Torino", "Torino", "Italia", 45.0703, 7.6869, "RoadSurfer Station Torino"),
        new("it-venice", "Venezia", "Venezia", "Italia", 45.4408, 12.3155, "RoadSurfer Station Venezia"),
        
        // Portogallo (3 locations)
        new("pt-faro", "Faro", "Faro", "Portogallo", 37.0194, -7.9322, "RoadSurfer Station Faro"),
        new("pt-lisbon", "Lisbona", "Lisboa", "Portogallo", 38.7223, -9.1393, "RoadSurfer Station Lisboa"),
        new("pt-porto", "Porto", "Porto", "Portogallo", 41.1579, -8.6291, "RoadSurfer Station Porto"),
        
        // Spagna (6 locations)
        new("es-barcelona", "Barcellona", "Barcelona", "Spagna", 41.3851, 2.1734, "RoadSurfer Station Barcelona"),
        new("es-bilbao", "Bilbao", "Bilbao", "Spagna", 43.2627, -2.9253, "RoadSurfer Station Bilbao"),
        new("es-madrid", "Madrid", "Madrid", "Spagna", 40.4168, -3.7038, "RoadSurfer Station Madrid"),
        new("es-malaga", "Malaga", "Málaga", "Spagna", 36.7213, -4.4214, "RoadSurfer Station Málaga"),
        new("es-seville", "Siviglia", "Sevilla", "Spagna", 37.3891, -5.9845, "RoadSurfer Station Sevilla"),
        new("es-valencia", "Valencia", "Valencia", "Spagna", 39.4699, -0.3763, "RoadSurfer Station Valencia"),
        
        // USA (11 locations)
        new("us-dallas", "Dallas", "Dallas", "Stati Uniti", 32.7767, -96.7970, "RoadSurfer Station Dallas, TX"),
        new("us-denver", "Denver", "Denver", "Stati Uniti", 39.7392, -104.9903, "RoadSurfer Station Denver, CO"),
        new("us-las-vegas", "Las Vegas", "Las Vegas", "Stati Uniti", 36.1699, -115.1398, "RoadSurfer Station Las Vegas, NV"),
        new("us-los-angeles", "Los Angeles", "Los Angeles", "Stati Uniti", 34.0522, -118.2437, "RoadSurfer Station Los Angeles, CA"),
        new("us-miami", "Miami", "Miami", "Stati Uniti", 25.7617, -80.1918, "RoadSurfer Station Miami, FL"),
        new("us-new-york", "New York City", "New York", "Stati Uniti", 40.7128, -74.0060, "RoadSurfer Station New York, NY"),
        new("us-phoenix", "Phoenix", "Phoenix", "Stati Uniti", 33.4484, -112.0740, "RoadSurfer Station Phoenix, AZ"),
        new("us-salt-lake-city", "Salt Lake City", "Salt Lake City", "Stati Uniti", 40.7608, -111.8910, "RoadSurfer Station Salt Lake City, UT"),
        new("us-san-francisco", "San Francisco", "San Francisco", "Stati Uniti", 37.7749, -122.4194, "RoadSurfer Station San Francisco, CA"),
        new("us-seattle", "Seattle", "Seattle", "Stati Uniti", 47.6062, -122.3321, "RoadSurfer Station Seattle, WA"),
        
        // Canada (2 locations)
        new("ca-calgary", "Calgary", "Calgary", "Canada", 51.0447, -114.0719, "RoadSurfer Station Calgary, AB"),
        new("ca-vancouver", "Vancouver", "Vancouver", "Canada", 49.2827, -123.1207, "RoadSurfer Station Vancouver, BC"),
    };

    public IReadOnlyList<RentalLocation> GetAll() =>
        RentalLocations.OrderBy(l => l.Country).ThenBy(l => l.City).ToList();

    public RentalLocation? FindById(string id) => RentalLocations.FirstOrDefault(l => l.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
