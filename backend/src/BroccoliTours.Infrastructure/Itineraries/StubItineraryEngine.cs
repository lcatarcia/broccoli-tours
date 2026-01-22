using BroccoliTours.Domain.Enums;
using BroccoliTours.Domain.Models;
using BroccoliTours.Infrastructure.Catalog;

namespace BroccoliTours.Infrastructure.Itineraries;

public sealed class StubItineraryEngine : IItineraryEngine
{
    private readonly ILocationCatalog _locations;

    public StubItineraryEngine(ILocationCatalog locations)
    {
        _locations = locations;
    }

    public Task<Itinerary> SuggestAsync(TravelPreferences preferences, CancellationToken cancellationToken = default)
    {
        var location = ResolveLocation(preferences);
        var period = BuildPeriod(preferences);

        var id = $"iti-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100,999)}";
        var title = $"{location.Name} in camper — Broccoli Picks";

        var (tips, days) = BuildSampleItinerary(location, period, preferences);

        var itinerary = new Itinerary(
            id,
            title,
            Summary: "Un itinerario pensato per massimizzare panorami e soste facili, evitando (quando possibile) le ore e i luoghi di picco.",
            Period: period,
            Days: days,
            Tips: tips,
            GeneratedAtUtc: DateTimeOffset.UtcNow
        );

        return Task.FromResult(itinerary);
    }

    private Location ResolveLocation(TravelPreferences preferences)
    {
        if (!string.IsNullOrWhiteSpace(preferences.LocationId))
        {
            var byId = _locations.FindById(preferences.LocationId);
            if (byId is not null) return byId;
        }

        // Simple heuristic for query; otherwise default
        var query = (preferences.LocationQuery ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(query))
            return _locations.GetAll().First();

        var all = _locations.GetAll();
        var match = all.FirstOrDefault(l => l.Name.ToLowerInvariant().Contains(query) || (l.Region ?? "").ToLowerInvariant().Contains(query));
        
        // Se non trova match ma c'è LocationQuery custom, crea una location fittizia con quel nome
        if (match == null && !string.IsNullOrWhiteSpace(preferences.LocationQuery))
        {
            return new Location(
                Id: Guid.NewGuid().ToString(),
                Name: preferences.LocationQuery,
                CountryCode: "IT",
                Region: null,
                Latitude: 42.0, // Centro Italia approssimativo
                Longitude: 12.0,
                Description: $"Destinazione personalizzata: {preferences.LocationQuery}"
            );
        }
        
        return match ?? all.First();
    }

    private static TravelPeriod BuildPeriod(TravelPreferences preferences)
    {
        return preferences.PeriodType switch
        {
            TravelPeriodType.FixedDates => new TravelPeriod(TravelPeriodType.FixedDates, preferences.StartDate, preferences.EndDate, null, null),
            TravelPeriodType.Month => new TravelPeriod(TravelPeriodType.Month, null, null, preferences.Month, preferences.Year),
            _ => new TravelPeriod(TravelPeriodType.SuggestedBest, null, null, preferences.Month, preferences.Year)
        };
    }

    private static (IReadOnlyList<string> tips, IReadOnlyList<ItineraryDay> days) BuildSampleItinerary(Location location, TravelPeriod period, TravelPreferences preferences)
    {
        var baseTips = new List<string>
        {
            "Parti presto: arrivi in area sosta entro le 16:30 riduce lo stress.",
            "Evita centri storici stretti: usa parcheggi scambiatori dove possibile.",
            "Controlla sempre accessi ZTL e altezza/portata dei ponti." ,
            "Alterna tappe iconiche a luoghi minori per ridurre overtourism."
        };

        if (preferences.WeekendTrip)
            baseTips.Insert(0, "Modalità weekend: poche ore di guida e soste semplici.");

        if (preferences.AvoidOvertourism)
            baseTips.Add("Broccoli Tip: scegli attrazioni secondarie a 15–30 min dalle mete più note.");

        var requestedDays = preferences.TripDurationDays.HasValue && preferences.TripDurationDays.Value > 0
            ? preferences.TripDurationDays.Value
            : (int?)null;

        // Build itinerary length based on explicit duration (if provided) or fallback rules
        var dayCount = requestedDays.HasValue
            ? Math.Clamp(requestedDays.Value, 2, 21)
            : preferences.WeekendTrip ? 2 : 3;

        var startDate = period.Type == TravelPeriodType.FixedDates ? period.StartDate : null;

        var days = new List<ItineraryDay>();
        for (var day = 1; day <= dayCount; day++)
        {
            DateOnly? date = startDate.HasValue ? startDate.Value.AddDays(day - 1) : (DateOnly?)null;

            var stops = new List<ItineraryStop>
            {
                new($"Punto panoramico — {location.Region ?? location.Name}", "Sosta breve per foto e respiro.", location.Latitude + 0.05 * day, location.Longitude + 0.03 * day, "viewpoint"),
                new("Area sosta consigliata", "Area camper con servizi essenziali e facile accesso.", location.Latitude + 0.02 * day, location.Longitude - 0.02 * day, "camper_area"),
                new("Borgo fuori rotta", "Piccolo borgo poco affollato, perfetto al tramonto.", location.Latitude - 0.03 * day, location.Longitude + 0.01 * day, "village")
            };

            var activities = new List<string>
            {
                "Passeggiata breve e visita del centro",
                "Degustazione/mercato locale",
                "Cena in trattoria (prenotazione consigliata)"
            };

            days.Add(new ItineraryDay(day, date, $"Giorno {day}: {location.Name}", stops, activities, DriveHoursEstimate: 2.0, OvernightStopRecommendation: day < dayCount ? "Area sosta consigliata" : null));
        }

        return (baseTips, days);
    }
}


