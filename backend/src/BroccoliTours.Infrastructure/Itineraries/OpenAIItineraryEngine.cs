using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BroccoliTours.Domain.Enums;
using BroccoliTours.Domain.Models;
using BroccoliTours.Infrastructure.Catalog;

namespace BroccoliTours.Infrastructure.Itineraries;

public sealed class OpenAIItineraryEngine : IItineraryEngine
{
    private readonly HttpClient _http;
    private readonly ILocationCatalog _locations;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAIItineraryEngine(HttpClient http, ILocationCatalog locations, string apiKey, string model)
    {
        _http = http;
        _locations = locations;
        _apiKey = apiKey;
        _model = model;

        _http.BaseAddress ??= new Uri("https://api.openai.com/");
        _http.Timeout = TimeSpan.FromSeconds(60);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<Itinerary> SuggestAsync(TravelPreferences preferences, CancellationToken cancellationToken = default)
    {
        var location = ResolveLocation(preferences);
        var prompt = BuildPrompt(preferences, location);

        Console.WriteLine("================== OPENAI REQUEST ==================");
        Console.WriteLine(prompt);
        Console.WriteLine("====================================================");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
        {
            Content = new StringContent(BuildRequestJson(prompt), Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var content = ExtractAssistantContent(json);

        Console.WriteLine("================== OPENAI RESPONSE ==================");
        Console.WriteLine(content);
        Console.WriteLine("=====================================================");

        // Some models may wrap JSON in ```json ... ```; strip defensively.
        content = StripCodeFences(content);

        var itinerary = ParseItinerary(content, preferences);

        // Ensure we always have coordinates; if missing, nudge around the main location.
        if (location != null)
            itinerary = EnsureCoordinates(itinerary, location);
        return itinerary;
    }

    private Location? ResolveLocation(TravelPreferences preferences)
    {
        if (!string.IsNullOrWhiteSpace(preferences.LocationId))
        {
            var byId = _locations.FindById(preferences.LocationId);
            if (byId is not null) return byId;
        }

        var query = (preferences.LocationQuery ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(query))
            return _locations.GetAll().First();

        var all = _locations.GetAll();
        var match = all.FirstOrDefault(l => l.Name.ToLowerInvariant().Contains(query) || (l.Region ?? string.Empty).ToLowerInvariant().Contains(query));
        
        // Se c'è una query custom e non trova match nel catalogo, restituisce null
        // in modo che BuildPrompt usi la query custom direttamente
        if (match == null && !string.IsNullOrWhiteSpace(preferences.LocationQuery))
            return null;
            
        return match ?? all.First();
    }

    private string BuildRequestJson(string prompt)
    {
        // Keep it simple and dependency-free.
        // NOTE: For production, consider switching to the newer Responses API.
        var payload = new
        {
            model = _model,
            temperature = 0.6,
            // JSON mode (where supported) to reduce formatting failures.
            // We still keep defensive parsing/stripping as a fallback.
            response_format = new { type = "json_object" },
            max_tokens = 1400,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "Sei l'AI di Broccoli Tours: un tour operator specializzato in viaggi in camper. " +
                              "Rispondi SEMPRE e SOLO con JSON valido (nessun markdown, nessuna spiegazione fuori dal JSON). " +
                              "Il JSON deve rispettare esattamente lo schema richiesto e contenere testo in italiano. " +
                              "Sii pratico: guida/strade/soste camper, anti-overtourism, consigli operativi reali."
                },
                new { role = "user", content = prompt }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private string BuildPrompt(TravelPreferences preferences, Location? location)
    {
        var period = preferences.PeriodType switch
        {
            TravelPeriodType.FixedDates => $"fixed dates {preferences.StartDate:yyyy-MM-dd} to {preferences.EndDate:yyyy-MM-dd}",
            TravelPeriodType.Month => $"month {preferences.Year}-{preferences.Month:00}",
            _ => $"suggest best period (month hint {preferences.Year}-{preferences.Month:00})"
        };

        var camper = preferences.CamperCategory.HasValue
            ? $"{preferences.CamperCategory.Value}"
            : "any camper";

        var weekend = preferences.WeekendTrip ? "yes" : "no";
        var avoid = preferences.AvoidOvertourism ? "yes" : "no";

        // Usa LocationQuery custom se location è null, altrimenti usa i dati del catalogo
        var destinationName = location?.Name ?? preferences.LocationQuery ?? "Italia";
        var destinationRegion = location?.Region ?? string.Empty;
        var destinationCoords = location != null ? $"{location.Latitude},{location.Longitude}" : "coordinate da determinare";

        var schema = """
{
    "id": "string",
    "title": "string",
    "summary": "string",
    "period": {
        "type": "FixedDates|Month|SuggestedBest",
        "startDate": "YYYY-MM-DD or null",
        "endDate": "YYYY-MM-DD or null",
        "month": "number or null",
        "year": "number or null"
    },
    "days": [
        {
            "dayNumber": 1,
            "date": "YYYY-MM-DD or null",
            "title": "string",
            "stops": [
                { "name": "string", "description": "string or null", "latitude": 0.0, "longitude": 0.0, "type": "viewpoint|village|camper_area|attraction|food" }
            ],
            "activities": ["string"]
        }
    ],
    "tips": ["string"],
    "generatedAtUtc": "ISO-8601"
}
""";

        var isBigRig = preferences.CamperCategory is CamperCategory.Integrated or CamperCategory.Motorhome;
        var drivingRule = preferences.WeekendTrip
            ? "Guida breve: max ~1.5–2 ore al giorno, poche tappe ma memorabili."
            : "Guida equilibrata: max ~3–4 ore al giorno; meglio 2–3 soste chiave che 6 micro-tappe.";

        var rigRule = isBigRig
            ? "Il mezzo è grande: evita centri storici stretti, passi molto ripidi e strade bianche; preferisci parcheggi ampi e accessi comodi."
            : "Il mezzo è compatto: puoi includere strade panoramiche più strette, ma sempre con prudenza e alternative.";

        var overtourismRule = preferences.AvoidOvertourism
            ? "Anti-overtourism: proponi alternative meno affollate entro 15–30 minuti dalle mete note, e suggerisci fasce orarie intelligenti (mattina presto / tardo pomeriggio)."
            : "Seleziona comunque soste sostenibili e consigli pratici per evitare congestione.";

        return $"""
        Progetta un itinerario in stile TOUR OPERATOR per Broccoli Tours.

        Vincoli cliente:
        - Destinazione (ancora): {destinationName} ({destinationRegion}), coordinate approx {destinationCoords}
        - Periodo: {period}
        - Weekend trip: {weekend}
        - Evita overtourism: {avoid}
        - Numero persone: {preferences.PartySize}
        - Camper: categoria {camper}, modello {preferences.CamperModelName ?? "non specificato"}

        Regole di qualità (fondamentali):
        - {drivingRule}
        - {rigRule}
        - Inserisci SEMPRE almeno 1 stop di tipo "camper_area" per ogni giorno (area sosta/camping pratico), con descrizione utile.
        - Inserisci almeno 1 gemma "fuori rotta" (villaggio/punto panoramico) e almeno 1 stop "food" (mercato/trattoria/azienda locale) nell'intero itinerario.
        - {overtourismRule}
        - Coord: ogni stop deve avere latitude/longitude realistici (non 0,0).
        - DayNumber sequenziale (1..N). Se date non disponibili, usa null.
        - driveHoursEstimate: stima ore guida totali quel giorno (es. 2.5, 3.0 per viaggio normale; 1.5 per weekend). Se 0 ore (giorno stazionario), usa 0.0.
        - overnightStopRecommendation: nome area sosta/camping consigliata per la notte (può omettere se è l'ultimo giorno).

        Stile Broccoli Tours:
        - Italiano, tono competente e rassicurante, concreto.
        - Descrizioni brevi ma utili (parcheggio, manovra, arrivare presto, alternative).
        - Nei tips includi: strategia di orari, consigli guida/soste, suggerimenti meteo/stagionalità (se periodo SuggestedBest).

        Rispondi SOLO con JSON valido, senza markdown, aderente ESATTAMENTE a questo schema:
        {schema}
        """;
    }

    private static string ExtractAssistantContent(string json)
    {
        using var doc = JsonDocument.Parse(json);

        // chat.completions: choices[0].message.content
        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var msg = choices[0].GetProperty("message");
            if (msg.TryGetProperty("content", out var content))
                return content.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string StripCodeFences(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            // remove first line and last fence
            var lines = trimmed.Split('\n').ToList();
            if (lines.Count >= 2) lines.RemoveAt(0);
            if (lines.Count >= 1 && lines[^1].Trim().StartsWith("```", StringComparison.Ordinal)) lines.RemoveAt(lines.Count - 1);
            return string.Join("\n", lines).Trim();
        }
        return trimmed;
    }

    private static Itinerary ParseItinerary(string json, TravelPreferences preferences)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var id = root.GetProperty("id").GetString() ?? $"iti-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        var title = root.GetProperty("title").GetString() ?? "Broccoli Tours — Itinerary";
        var summary = root.GetProperty("summary").GetString() ?? "";

        var periodEl = root.GetProperty("period");
        var typeStr = periodEl.GetProperty("type").GetString() ?? "SuggestedBest";
        var type = Enum.TryParse<TravelPeriodType>(typeStr, ignoreCase: true, out var parsed) ? parsed : preferences.PeriodType;

        DateOnly? startDate = ParseDateOnlyOrNull(periodEl, "startDate");
        DateOnly? endDate = ParseDateOnlyOrNull(periodEl, "endDate");
        int? month = ParseIntOrNull(periodEl, "month");
        int? year = ParseIntOrNull(periodEl, "year");

        var period = new TravelPeriod(type, startDate, endDate, month, year);

        var tips = root.TryGetProperty("tips", out var tipsEl) && tipsEl.ValueKind == JsonValueKind.Array
            ? tipsEl.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
            : new List<string>();

        var days = new List<ItineraryDay>();
        if (root.TryGetProperty("days", out var daysEl) && daysEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in daysEl.EnumerateArray())
            {
                var dayNumber = d.GetProperty("dayNumber").GetInt32();
                var dayTitle = d.GetProperty("title").GetString() ?? $"Day {dayNumber}";
                var date = ParseDateOnlyOrNull(d, "date");

                var stops = new List<ItineraryStop>();
                if (d.TryGetProperty("stops", out var stopsEl) && stopsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in stopsEl.EnumerateArray())
                    {
                        var name = s.GetProperty("name").GetString() ?? "Stop";
                        var desc = s.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
                        var lat = s.TryGetProperty("latitude", out var latEl) ? latEl.GetDouble() : 0.0;
                        var lon = s.TryGetProperty("longitude", out var lonEl) ? lonEl.GetDouble() : 0.0;
                        var stype = s.TryGetProperty("type", out var typeEl2) ? (typeEl2.GetString() ?? "attraction") : "attraction";

                        stops.Add(new ItineraryStop(name, desc, lat, lon, stype));
                    }
                }

                var activities = d.TryGetProperty("activities", out var actEl) && actEl.ValueKind == JsonValueKind.Array
                    ? actEl.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                    : new List<string>();

                var driveHours = d.TryGetProperty("driveHoursEstimate", out var driveEl) && driveEl.ValueKind == JsonValueKind.Number
                    ? (double?)driveEl.GetDouble()
                    : null;

                var overnight = d.TryGetProperty("overnightStopRecommendation", out var overnightEl) && overnightEl.ValueKind == JsonValueKind.String
                    ? overnightEl.GetString()
                    : null;

                days.Add(new ItineraryDay(dayNumber, date, dayTitle, stops, activities, driveHours, overnight));
            }
        }

        var generatedAt = root.TryGetProperty("generatedAtUtc", out var genEl) && DateTimeOffset.TryParse(genEl.GetString(), out var dto)
            ? dto
            : DateTimeOffset.UtcNow;

        return new Itinerary(id, title, summary, period, days, tips, generatedAt);
    }

    private static DateOnly? ParseDateOnlyOrNull(JsonElement obj, string property)
    {
        if (!obj.TryGetProperty(property, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Null) return null;

        var s = el.GetString();
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateOnly.TryParse(s, out var d) ? d : null;
    }

    private static int? ParseIntOrNull(JsonElement obj, string property)
    {
        if (!obj.TryGetProperty(property, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Null) return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n)) return n;

        var s = el.GetString();
        return int.TryParse(s, out var parsed) ? parsed : null;
    }

    private static Itinerary EnsureCoordinates(Itinerary itinerary, Location anchor)
    {
        var dayIdx = 0;
        var fixedDays = itinerary.Days.Select(day =>
        {
            dayIdx++;
            var stopIdx = 0;
            var fixedStops = day.Stops.Select(stop =>
            {
                stopIdx++;
                var lat = stop.Latitude;
                var lon = stop.Longitude;

                if (Math.Abs(lat) < 0.00001 && Math.Abs(lon) < 0.00001)
                {
                    lat = anchor.Latitude + 0.03 * dayIdx - 0.01 * stopIdx;
                    lon = anchor.Longitude + 0.02 * stopIdx - 0.01 * dayIdx;
                }

                return stop with { Latitude = lat, Longitude = lon };
            }).ToList();

            return day with { Stops = fixedStops };
        }).ToList();

        return itinerary with { Days = fixedDays };
    }
}





