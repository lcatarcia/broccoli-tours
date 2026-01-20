using BroccoliTours.Domain.Enums;
using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public sealed class InMemoryCamperCatalog : ICamperCatalog
{
    private static readonly IReadOnlyList<Camper> Campers = new List<Camper>
    {
        // Camper van
        new("surfer-suite", "Surfer Suite", CamperCategory.Campervan, 4, 5.99m, "VW T6.1 California Ocean"),
        new("sunrise-suite", "Sunrise Suite", CamperCategory.Campervan, 4, 5.99m, "Nuovo VW California Ocean / Coast"),
        new("beach-hostel", "Beach Hostel", CamperCategory.Campervan, 4, 5.99m, "VW T6.1 California Beach"),
        new("camper-cabin", "Camper Cabin", CamperCategory.Campervan, 4, 5.40m, "Ford Nugget"),
        new("camper-cabin-deluxe", "Camper Cabin Deluxe", CamperCategory.Campervan, 4, 5.40m, "Ford Nugget Plus"),
        new("travel-home", "Travel Home", CamperCategory.Campervan, 4, 5.14m, "Mercedes Marco Polo"),
        
        // Furgonati
        new("family-finca", "Family Finca", CamperCategory.Van, 4, 6.00m, "Vari produttori"),
        new("couple-cottage", "Couple Cottage", CamperCategory.Van, 2, 5.40m, "Vari produttori"),
        new("road-house", "Road House", CamperCategory.Van, 4, 6.00m, "Vari produttori (più versioni)"),
        new("couple-condo", "Couple Condo", CamperCategory.Van, 2, 5.40m, "Vari produttori"),
        new("liberty-lodge", "Liberty Lodge", CamperCategory.Van, 4, 6.00m, "Vari produttori"),
        new("horizon-hopper", "Horizon Hopper", CamperCategory.Van, 2, 5.49m, "Winnebago Revel 44E"),
        
        // Furgone 4x4
        new("couple-cottage-offroad", "Couple Cottage Offroad", CamperCategory.Van, 2, 5.40m, "Versione offroad (4x4)"),
        
        // Autocaravan Semi-integrale
        new("camper-castle", "Camper Castle", CamperCategory.SemiIntegrated, 4, 7.00m, "Vari produttori"),
        new("cozy-cottage", "Cozy Cottage", CamperCategory.SemiIntegrated, 4, 6.80m, "Vari produttori"),
        new("van-villa", "Van Villa", CamperCategory.SemiIntegrated, 4, 5.99m, "VW T6.1 Knaus Tourer Van"),
        
        // Autocaravan Mansardato
        new("family-freedom", "Family Freedom", CamperCategory.Motorhome, 5, 7.50m, "Thor Four Winds 22E"),
    };

    public IReadOnlyList<Camper> GetAll() => Campers;
}
