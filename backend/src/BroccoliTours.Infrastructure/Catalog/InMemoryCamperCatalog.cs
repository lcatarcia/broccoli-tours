using BroccoliTours.Domain.Enums;
using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public sealed class InMemoryCamperCatalog : ICamperCatalog
{
    private static readonly IReadOnlyList<Camper> Campers = new List<Camper>
    {
        // Camper van
        new("surfer-suite", "Surfer Suite", CamperCategory.Campervan, 4, 5.99m, "VW T6.1 California Ocean - Iconico van con tetto a soffietto, cucina integrata."),
        new("sunrise-suite", "Sunrise Suite", CamperCategory.Campervan, 4, 5.99m, "Nuovo VW California Ocean / Coast - Design moderno e funzionale."),
        new("beach-hostel", "Beach Hostel", CamperCategory.Campervan, 4, 5.99m, "VW T6.1 California Beach - Perfetto per piccoli gruppi e famiglie."),
        new("camper-cabin", "Camper Cabin", CamperCategory.Campervan, 4, 5.40m, "Ford Nugget - Compatto e versatile per ogni destinazione."),
        new("camper-cabin-deluxe", "Camper Cabin Deluxe", CamperCategory.Campervan, 4, 5.40m, "Ford Nugget Plus - Versione premium con comfort extra."),
        new("travel-home", "Travel Home", CamperCategory.Campervan, 4, 5.14m, "Mercedes Marco Polo - Eleganza e tecnologia per viaggiatori esigenti."),
        
        // Furgonati
        new("family-finca", "Family Finca", CamperCategory.Van, 4, 6.00m, "Furgonato spazioso per famiglie con cucina completa e bagno interno."),
        new("couple-cottage", "Couple Cottage", CamperCategory.Van, 2, 5.40m, "Van compatto ideale per coppie, design moderno e pratico."),
        new("road-house", "Road House", CamperCategory.Van, 4, 6.00m, "Furgonato versatile con multiple configurazioni disponibili."),
        new("couple-condo", "Couple Condo", CamperCategory.Van, 2, 5.40m, "Van premium per coppie con tutti i comfort."),
        new("liberty-lodge", "Liberty Lodge", CamperCategory.Van, 4, 6.00m, "Furgonato con ampi spazi e dotazioni complete."),
        new("horizon-hopper", "Horizon Hopper", CamperCategory.Van, 2, 5.49m, "Winnebago Revel 44E - Van 4x4 per avventure off-road."),
        
        // Furgone 4x4
        new("couple-cottage-offroad", "Couple Cottage Offroad", CamperCategory.Van, 2, 5.40m, "Versione offroad (4x4) per coppie avventurose."),
        
        // Autocaravan Semi-integrale
        new("camper-castle", "Camper Castle", CamperCategory.SemiIntegrated, 4, 7.00m, "Semi-integrato spazioso con bagno separato e cucina attrezzata."),
        new("cozy-cottage", "Cozy Cottage", CamperCategory.SemiIntegrated, 4, 6.80m, "Semi-integrato confortevole ideale per famiglie."),
        new("van-villa", "Van Villa", CamperCategory.SemiIntegrated, 4, 5.99m, "VW T6.1 Knaus Tourer Van - Compatto ma completo."),
        
        // Autocaravan Mansardato
        new("family-freedom", "Family Freedom", CamperCategory.Motorhome, 5, 7.50m, "Thor Four Winds 22E - Mansardato con alcova, cucina/soggiorno e bagno completo."),
    };

    public IReadOnlyList<Camper> GetAll() => Campers;
}
