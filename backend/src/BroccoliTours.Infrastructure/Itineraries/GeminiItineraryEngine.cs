using BroccoliTours.Domain.Enums;
using BroccoliTours.Domain.Models;
using BroccoliTours.Infrastructure.Catalog;
using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BroccoliTours.Infrastructure.Itineraries;

public sealed class GeminiItineraryEngine : IItineraryEngine
{
    private readonly HttpClient _http;
    private readonly ILocationCatalog _locations;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiItineraryEngine(HttpClient http, ILocationCatalog locations, string apiKey, string model = "gemini-1.5-flash")
    {
        _http = http;
        _locations = locations;
        _apiKey = apiKey;
        _model = model;

        if (_http.BaseAddress == null)
        {
            _http.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        }
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<Itinerary> SuggestAsync(TravelPreferences preferences, CancellationToken cancellationToken = default)
    {
        var location = ResolveLocation(preferences);
        var prompt = BuildPrompt(preferences, location);

        Console.WriteLine("================== GEMINI REQUEST ==================");
        Console.WriteLine(prompt);
        Console.WriteLine("====================================================");

        // Build the full URL with API key
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        Console.WriteLine($"Request URL: {endpoint}");
        Console.WriteLine($"Model: {_model}");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(BuildRequestJson(prompt), Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"================== GEMINI ERROR ==================");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {responseBody}");
            Console.WriteLine($"Request URL: {endpoint}");
            Console.WriteLine("==================================================");
        }

        response.EnsureSuccessStatusCode();

        var json = responseBody;
        var content = ExtractContent(json);

        Console.WriteLine("================== GEMINI RESPONSE ==================");
        Console.WriteLine(content);
        Console.WriteLine("=====================================================");

        // Strip markdown code fences if present
        content = StripCodeFences(content);

        Console.WriteLine("================== CLEANED JSON ==================");
        Console.WriteLine(content);
        Console.WriteLine("==================================================");

        Itinerary itinerary;
        try
        {
            itinerary = ParseItinerary(content, preferences);
        }
        catch (Exception ex)
        {
            Console.WriteLine("================== PARSE ERROR ==================");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.WriteLine("JSON that failed to parse:");
            Console.WriteLine(content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);
            Console.WriteLine("=================================================");
            throw;
        }

        // Ensure coordinates
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

        if (match == null && !string.IsNullOrWhiteSpace(preferences.LocationQuery))
            return null;

        return match ?? all.First();
    }

    private string BuildRequestJson(string prompt)
    {
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.6,
                maxOutputTokens = 8192,
                responseMimeType = "application/json"
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
            "activities": ["string"],
            "driveHoursEstimate": 0.0,
            "overnightStopRecommendation": "string or null"
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

        IMPORTANTE: Rispondi SOLO con JSON valido seguendo esattamente lo schema sopra. Nessun testo aggiuntivo, solo JSON puro.

        Schema JSON richiesto:
        {schema}

        Genera l'itinerario ora in formato JSON.
        """;
    }

    private string ExtractContent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var content))
            {
                if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }
            }
        }

        throw new InvalidOperationException("Failed to extract content from Gemini response");
    }

    private string StripCodeFences(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```json"))
            content = content["```json".Length..];
        else if (content.StartsWith("```"))
            content = content["```".Length..];

        if (content.EndsWith("```"))
            content = content[..^3];

        return content.Trim();
    }

    private Itinerary ParseItinerary(string json, TravelPreferences preferences)
    {
        // Validate it's not empty
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("JSON content is empty");
        }

        // Try to parse and provide better error messages
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Parse Error at position {ex.BytePositionInLine}: {ex.Message}");
            throw new InvalidOperationException($"Invalid JSON from Gemini: {ex.Message}", ex);
        }

        using (doc)
        {
            var root = doc.RootElement;

            var id = root.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
            var title = root.GetProperty("title").GetString() ?? "Itinerario";
            var summary = root.GetProperty("summary").GetString() ?? "";

            var periodEl = root.GetProperty("period");
            var periodType = Enum.Parse<TravelPeriodType>(periodEl.GetProperty("type").GetString() ?? "SuggestedBest");
            var startDate = periodEl.TryGetProperty("startDate", out var sd) && sd.ValueKind != JsonValueKind.Null
                ? DateOnly.Parse(sd.GetString()!)
                : (DateOnly?)null;
            var endDate = periodEl.TryGetProperty("endDate", out var ed) && ed.ValueKind != JsonValueKind.Null
                ? DateOnly.Parse(ed.GetString()!)
                : (DateOnly?)null;
            var month = periodEl.TryGetProperty("month", out var m) && m.ValueKind == JsonValueKind.Number
                ? m.GetInt32()
                : (int?)null;
            var year = periodEl.TryGetProperty("year", out var y) && y.ValueKind == JsonValueKind.Number
                ? y.GetInt32()
                : (int?)null;

            var period = new TravelPeriod(periodType, startDate, endDate, month, year);

            var days = new List<ItineraryDay>();
            foreach (var dayEl in root.GetProperty("days").EnumerateArray())
            {
                var dayNumber = dayEl.GetProperty("dayNumber").GetInt32();
                var dayDate = dayEl.TryGetProperty("date", out var dd) && dd.ValueKind != JsonValueKind.Null
                    ? DateOnly.Parse(dd.GetString()!)
                    : (DateOnly?)null;
                var dayTitle = dayEl.GetProperty("title").GetString() ?? "";

                var stops = new List<ItineraryStop>();
                foreach (var stopEl in dayEl.GetProperty("stops").EnumerateArray())
                {
                    stops.Add(new ItineraryStop(
                        stopEl.GetProperty("name").GetString() ?? "",
                        stopEl.GetProperty("description").GetString(),
                        stopEl.GetProperty("latitude").GetDouble(),
                        stopEl.GetProperty("longitude").GetDouble(),
                        stopEl.GetProperty("type").GetString() ?? "attraction"
                    ));
                }

                var activities = dayEl.GetProperty("activities").EnumerateArray()
                    .Select(a => a.GetString() ?? "")
                    .ToList();

                var driveHours = dayEl.TryGetProperty("driveHoursEstimate", out var dh) && dh.ValueKind == JsonValueKind.Number
                    ? dh.GetDouble()
                    : (double?)null;

                var overnight = dayEl.TryGetProperty("overnightStopRecommendation", out var os) && os.ValueKind == JsonValueKind.String
                    ? os.GetString()
                    : null;

                days.Add(new ItineraryDay(dayNumber, dayDate, dayTitle, stops, activities, driveHours, overnight));
            }

            var tips = root.GetProperty("tips").EnumerateArray()
                .Select(t => t.GetString() ?? "")
                .ToList();

            return new Itinerary(id, title, summary, period, days, tips, DateTimeOffset.UtcNow);
        }
    }

    private Itinerary EnsureCoordinates(Itinerary itinerary, Location baseLocation)
    {
        var days = new List<ItineraryDay>();
        var rng = Random.Shared;

        foreach (var day in itinerary.Days)
        {
            var stops = new List<ItineraryStop>();
            foreach (var stop in day.Stops)
            {
                if (stop.Latitude == 0.0 && stop.Longitude == 0.0)
                {
                    var nudgeLat = baseLocation.Latitude + (rng.NextDouble() - 0.5) * 0.1;
                    var nudgeLng = baseLocation.Longitude + (rng.NextDouble() - 0.5) * 0.1;
                    stops.Add(stop with { Latitude = nudgeLat, Longitude = nudgeLng });
                }
                else
                {
                    stops.Add(stop);
                }
            }

            days.Add(day with { Stops = stops });
        }

        return itinerary with { Days = days };
    }
}
