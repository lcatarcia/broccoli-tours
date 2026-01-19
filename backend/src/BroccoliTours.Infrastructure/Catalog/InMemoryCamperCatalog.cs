using BroccoliTours.Domain.Enums;
using BroccoliTours.Domain.Models;

namespace BroccoliTours.Infrastructure.Catalog;

public sealed class InMemoryCamperCatalog : ICamperCatalog
{
    private static readonly IReadOnlyList<Camper> Campers = new List<Camper>
    {
        new("van-01", "Fiat Ducato Camperized Van", CamperCategory.Van, 2, 5.40m, "Compatto, perfetto per weekend e strade strette."),
        new("cv-01", "VW Grand California", CamperCategory.Campervan, 4, 6.84m, "Comodo per coppie o famiglie piccole."),
        new("semi-01", "Adria Matrix (semi-integrato)", CamperCategory.SemiIntegrated, 4, 7.40m, "Ottimo equilibrio tra spazio e manovrabilità."),
        new("int-01", "Hymer B-Class (integrale)", CamperCategory.Integrated, 4, 7.80m, "Molto confortevole per lunghi viaggi."),
        new("mh-01", "Concorde Charisma (motorhome)", CamperCategory.Motorhome, 4, 9.50m, "Top di gamma: spazio e comfort assoluti."),
        
        // RoadSurfer Fleet - Europe
        new("rs-beach-hostel", "RoadSurfer Beach Hostel", CamperCategory.Campervan, 4, 5.99m, "VW California Ocean - Iconico van con tetto a soffietto, cucina integrata e bagno. Ideale per 2-4 persone."),
        new("rs-beach-camper", "RoadSurfer Beach Camper", CamperCategory.Van, 2, 5.40m, "Camper van compatto stile VW - Perfetto per coppie, massima manovrabilità in città."),
        new("rs-couple-camper", "RoadSurfer Couple Camper", CamperCategory.Van, 2, 5.40m, "Van compatto Mercedes/Fiat - Design moderno, cucina e letto matrimoniale. Perfetto per due."),
        new("rs-family-van", "RoadSurfer Family Van", CamperCategory.Campervan, 4, 6.00m, "Van spazioso per famiglie - 4 posti letto, cucina completa e bagno interno."),
        new("rs-beach-hostel-xl", "RoadSurfer Beach Hostel XL", CamperCategory.Campervan, 5, 6.20m, "VW California XXL - Versione allungata con più spazio e comfort."),
        new("rs-surfer-suite", "RoadSurfer Surfer Suite", CamperCategory.SemiIntegrated, 4, 7.00m, "Semi-integrato premium - Bagno separato, cucina grande, ideale per viaggi lunghi."),
        new("rs-family-standard", "RoadSurfer Family Standard", CamperCategory.SemiIntegrated, 4, 7.20m, "Mansardato famiglia - 4-5 posti letto, bagno completo, cucina attrezzata."),
        new("rs-family-luxury", "RoadSurfer Family Luxury", CamperCategory.Integrated, 4, 7.50m, "Mansardato premium - Spazio extra, letti comodi, bagno grande. Top comfort."),
        
        // RoadSurfer Fleet - North America
        new("rs-couple-condo", "RoadSurfer Couple Condo", CamperCategory.Campervan, 4, 6.70m, "Class B RV Sprinter-style - Bagno interno, cucina, letto queen. Perfetto per coppie."),
        new("rs-family-freedom", "RoadSurfer Family Freedom", CamperCategory.Motorhome, 5, 7.50m, "Thor Four Winds 22E - Class C RV con alcova, cucina/soggiorno, bagno completo."),
    };

    public IReadOnlyList<Camper> GetAll() => Campers;
}
