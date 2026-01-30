using BroccoliTours.Domain.Enums;
using BroccoliTours.Domain.Models;
using BroccoliTours.Infrastructure.Catalog;
using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace BroccoliTours.Infrastructure.Itineraries;

public sealed class GeminiItineraryEngine : IItineraryEngine
{
    private readonly HttpClient _http;
    private readonly ILocationCatalog _locations;
    private readonly IRentalLocationCatalog _rentalLocations;
    private readonly string _apiKey;
    private readonly string _model;

    // Track JSON repair attempts for the current async context
    private static readonly AsyncLocal<int> _jsonRepairAttempts = new AsyncLocal<int>();
    public static int CurrentJsonRepairAttempts => _jsonRepairAttempts.Value;

    public GeminiItineraryEngine(HttpClient http, ILocationCatalog locations, IRentalLocationCatalog rentalLocations, string apiKey, string model = "gemini-1.5-flash")
    {
        _http = http;
        _locations = locations;
        _rentalLocations = rentalLocations;
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
        // Reset repair attempts counter for this request
        _jsonRepairAttempts.Value = 0;

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
            itinerary = await ParseItineraryAsync(content, preferences, cancellationToken);
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
        var duration = preferences.TripDurationDays.HasValue
            ? $"{preferences.TripDurationDays.Value} giorni totali di viaggio richiesti"
            : "durata flessibile";

        var destinationName = location?.Name ?? preferences.LocationQuery ?? "Italia";
        var destinationRegion = location?.Region ?? string.Empty;
        var destinationCoords = location != null ? $"{location.Latitude},{location.Longitude}" : "coordinate da determinare";

        // Rental location info
        string rentalInfo = "";
        if (!preferences.IsOwnedCamper && !string.IsNullOrWhiteSpace(preferences.RentalLocationId))
        {
            var rentalLoc = _rentalLocations.FindById(preferences.RentalLocationId);
            if (rentalLoc != null)
            {
                rentalInfo = $"""
        
        PUNTO PARTENZA E RITORNO OBBLIGATORIO:
        Il camper è noleggiato presso RoadSurfer - {rentalLoc.City}, {rentalLoc.Country}
        Coordinate sede: {rentalLoc.Latitude},{rentalLoc.Longitude}
        Indirizzo: {rentalLoc.Address}
        
        VINCOLO CRITICO: L'itinerario DEVE iniziare e terminare esattamente in questa sede.
        - Giorno 1: partenza da {rentalLoc.City} ({rentalLoc.Country})
        - Ultimo giorno: ritorno a {rentalLoc.City} ({rentalLoc.Country})
        - Considera tempi di ritiro/riconsegna camper (1-2 ore il primo e ultimo giorno)
        """;
            }
        }
        else if (preferences.IsOwnedCamper && !string.IsNullOrWhiteSpace(preferences.OwnedCamperModel))
        {
            rentalInfo = $"""
        
        MEZZO DI PROPRIETÀ:
        Il cliente viaggerà con il proprio camper: {preferences.OwnedCamperModel}
        Considera dimensioni e ingombro del mezzo specificato per:
        - Accessibilità ai luoghi (centri storici, strade strette, parcheggi)
        - Aree sosta e campeggi adeguati alle dimensioni
        - Percorsi stradali adatti al tipo di mezzo
        """;
        }

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
        - Durata desiderata: {duration}
        - Weekend trip: {weekend}
        - Evita overtourism: {avoid}
        - Numero persone: {preferences.PartySize}
        - Camper: categoria {camper}, modello {preferences.CamperModelName ?? "non specificato"}{rentalInfo}

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

    private async Task<Itinerary> ParseItineraryAsync(string json, TravelPreferences preferences, CancellationToken cancellationToken, int attemptNumber = 1)
    {
        const int MaxRepairAttempts = 3;

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
            Console.WriteLine($"[Attempt {attemptNumber}] JSON Parse Error at position {ex.BytePositionInLine}: {ex.Message}");

            if (attemptNumber >= MaxRepairAttempts)
            {
                Console.WriteLine($"Failed to repair JSON after {MaxRepairAttempts} attempts");
                throw new InvalidOperationException($"Invalid JSON from Gemini even after {MaxRepairAttempts} repair attempts: {ex.Message}", ex);
            }

            Console.WriteLine($"Attempting to repair truncated JSON (attempt {attemptNumber}/{MaxRepairAttempts})...");

            // Increment the repair attempts counter
            _jsonRepairAttempts.Value = attemptNumber;

            // Try to repair the JSON by completing it
            var repairedJson = await RepairTruncatedJsonAsync(json, preferences, cancellationToken);

            // Recursively try to parse the repaired JSON
            return await ParseItineraryAsync(repairedJson, preferences, cancellationToken, attemptNumber + 1);
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

    private async Task<string> RepairTruncatedJsonAsync(string truncatedJson, TravelPreferences preferences, CancellationToken cancellationToken)
    {
        Console.WriteLine("================== REPAIR REQUEST ==================");
        Console.WriteLine($"Truncated JSON length: {truncatedJson.Length}");
        Console.WriteLine($"Last 200 chars: {(truncatedJson.Length > 200 ? truncatedJson.Substring(truncatedJson.Length - 200) : truncatedJson)}");
        Console.WriteLine("====================================================");

        var location = ResolveLocation(preferences);
        var originalPrompt = BuildPrompt(preferences, location);

        var repairPrompt = $$"""
        Il seguente JSON di un itinerario è stato troncato durante la generazione.
        
        JSON TRONCATO (DA MANTENERE ESATTAMENTE COSÌ):
        {{truncatedJson}}
        
        CONTESTO ORIGINALE:
        {{originalPrompt}}
        
        COMPITO CRITICO:
        Il JSON sopra è stato interrotto. NON devi rigenerare tutto il JSON, ma solo CONTINUARE da dove si è interrotto.
        
        ISTRUZIONI:
        1. Analizza il JSON troncato per capire ESATTAMENTE dove si è interrotto
        2. Identifica se è stato interrotto nel mezzo di:
           - Un oggetto (mancano campi e la chiusura })
           - Un array (mancano elementi e la chiusura ])
           - Una stringa (mancano caratteri e la chiusura ")
        3. Genera SOLO la continuazione necessaria per completare il JSON
        4. La continuazione deve:
           - Completare l'elemento corrente (se interrotto)
           - Aggiungere tutti gli elementi mancanti (giorni, stops, tips rimanenti)
           - Chiudere tutti gli array e oggetti aperti
           - Terminare con }
        5. Rispetta il contesto originale per i contenuti mancanti
        
        FORMATO RISPOSTA:
        Rispondi SOLO con la continuazione del JSON, senza ripetere la parte troncata.
        La tua risposta verrà concatenata direttamente dopo il JSON troncato.
        NON includere markdown, spiegazioni o il JSON completo - SOLO la continuazione.
        
        Esempio: se il JSON troncato finisce con: "description": "Visita al m
        La tua risposta dovrebbe iniziare con: useo", "latitude": 45.4...
        """;

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(BuildRepairRequestJson(repairPrompt), Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"================== REPAIR ERROR ==================");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {responseBody}");
            Console.WriteLine("==================================================");
            throw new InvalidOperationException($"Failed to repair JSON: {response.StatusCode}");
        }

        var continuation = ExtractContent(responseBody);
        continuation = StripCodeFences(continuation);

        Console.WriteLine("================== JSON CONTINUATION ==================");
        Console.WriteLine(continuation);
        Console.WriteLine("=======================================================");

        // Concatenate truncated JSON with continuation
        var repairedContent = truncatedJson + continuation;

        Console.WriteLine("================== REPAIRED JSON (FULL) ==================");
        Console.WriteLine(repairedContent);
        Console.WriteLine("=========================================================");

        // Validate JSON structure
        if (!IsJsonStructurallyValid(repairedContent))
        {
            Console.WriteLine("WARNING: Repaired JSON appears to be structurally invalid (unbalanced brackets/braces)");
        }

        return repairedContent;
    }

    private string BuildRepairRequestJson(string prompt)
    {
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.3, // Lower temperature for more precise repair
                maxOutputTokens = 8192, // Sufficient for continuation only (not full regeneration)
                responseMimeType = "application/json"
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private bool IsJsonStructurallyValid(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        int braceCount = 0;
        int bracketCount = 0;
        bool inString = false;
        bool escaped = false;

        foreach (char c in json)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;
                else if (c == '[') bracketCount++;
                else if (c == ']') bracketCount--;
            }
        }

        bool isValid = braceCount == 0 && bracketCount == 0 && !inString;
        if (!isValid)
        {
            Console.WriteLine($"Structural validation: braces={braceCount}, brackets={bracketCount}, inString={inString}");
        }
        return isValid;
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
