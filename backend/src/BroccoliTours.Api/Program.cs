using System.Text.Json.Serialization;
using BroccoliTours.Domain.Models;

using BroccoliTours.Infrastructure.Catalog;
using BroccoliTours.Infrastructure.DependencyInjection;
using BroccoliTours.Infrastructure.Itineraries;
using BroccoliTours.Infrastructure.Pdf;

using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load .env from repository root (3 levels up from bin/Debug/net9.0)
var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"✓ Loaded .env from: {Path.GetFullPath(envPath)}");
}
else
{
    Console.WriteLine($"⚠️  .env not found at: {Path.GetFullPath(envPath)}");
    Env.Load(); // Try default location
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddBroccoliToursInfrastructure();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IItineraryEngine>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
    var locations = sp.GetRequiredService<ILocationCatalog>();
    var rentalLocations = sp.GetRequiredService<IRentalLocationCatalog>();
    var stub = sp.GetRequiredService<StubItineraryEngine>();

    // OpenAI configuration
    var openAiKey = configuration["OpenAI:ApiKey"];
    if (string.IsNullOrWhiteSpace(openAiKey) || openAiKey == "__SET_ME__")
        openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    var openAiModel = configuration["OpenAI:Model"];
    if (string.IsNullOrWhiteSpace(openAiModel))
        openAiModel = "gpt-4o-mini";

    // Gemini configuration
    var geminiKey = configuration["Gemini:ApiKey"];
    if (string.IsNullOrWhiteSpace(geminiKey) || geminiKey == "__SET_ME__")
        geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

    var geminiModel = configuration["Gemini:Model"];
    if (string.IsNullOrWhiteSpace(geminiModel))
        geminiModel = "gemini-1.5-flash";

    // Use only Gemini with Stub as fallback
    if (!string.IsNullOrWhiteSpace(geminiKey))
    {
        var http = httpFactory.CreateClient("gemini");
        var geminiEngine = new GeminiItineraryEngine(http, locations, rentalLocations, geminiKey, geminiModel);
        return new ResilientItineraryEngine(geminiEngine, null, stub);
    }

    // If no Gemini key, use stub
    return stub;
});

var app = builder.Build();

// Swagger enabled for all environments (including Production)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("default");

app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .WithDescription("Verifica lo stato dell'API")
    .WithTags("Health");

app.MapGet("/api/campers", (ICamperCatalog catalog) => Results.Ok(catalog.GetAll()))
    .WithName("GetCampers")
    .WithDescription("Ottiene l'elenco completo dei camper disponibili con categorie, capacità e caratteristiche")
    .WithTags("Campers")
    .Produces<List<Camper>>(200);

app.MapGet("/api/locations", (ILocationCatalog catalog) => Results.Ok(catalog.GetAll()))
    .WithName("GetLocations")
    .WithDescription("Ottiene l'elenco delle destinazioni disponibili per viaggi in camper (Italia, Francia, etc.)")
    .WithTags("Locations")
    .Produces<List<Location>>(200);

app.MapGet("/api/rentallocations", (IRentalLocationCatalog catalog) => Results.Ok(catalog.GetAll()))
    .WithName("GetRentalLocations")
    .WithDescription("Ottiene l'elenco delle sedi RoadSurfer per il noleggio camper in Europa")
    .WithTags("RentalLocations")
    .Produces<List<RentalLocation>>(200);

app.MapPost("/api/itineraries/suggest", async (
    TravelPreferences preferences,
    IItineraryEngine engine,
    IItineraryStore store,
    HttpContext context,
    CancellationToken ct) =>
{
    var itinerary = await engine.SuggestAsync(preferences, ct);
    store.Save(itinerary);

    // Check if fallback was used (stub engine returns generic data)
    if (itinerary.Summary.Contains("massimizzare panorami e soste facili"))
    {
        context.Response.Headers["X-Broccoli-Fallback"] = "true";
    }

    // Add header to track JSON repair attempts if any occurred
    var geminiRepairs = BroccoliTours.Infrastructure.Itineraries.GeminiItineraryEngine.CurrentJsonRepairAttempts;
    var openaiRepairs = BroccoliTours.Infrastructure.Itineraries.OpenAIItineraryEngine.CurrentJsonRepairAttempts;
    var totalRepairs = Math.Max(geminiRepairs, openaiRepairs);

    if (totalRepairs > 0)
    {
        context.Response.Headers["X-Json-Repair-Attempts"] = totalRepairs.ToString();
    }

    return Results.Ok(itinerary);
})
    .WithName("SuggestItinerary")
    .WithDescription("Genera un itinerario personalizzato basato su preferenze di viaggio usando AI (OpenAI GPT-4o-mini). Include tappe, attività, ore di guida stimate e raccomandazioni per soste notturne.")
    .WithTags("Itineraries")
    .Accepts<TravelPreferences>("application/json")
    .Produces<Itinerary>(200);

app.MapGet("/api/itineraries/{id}/pdf", async (
    string id,
    string? mode,
    IItineraryStore store,
    IPdfGenerator pdf,
    CancellationToken ct) =>
{
    var itinerary = store.Get(id);
    if (itinerary is null)
        return Results.NotFound(new { message = "Itinerary not found" });

    var selectedMode = string.IsNullOrWhiteSpace(mode) ? "detailed" : mode;
    var bytes = await pdf.GenerateAsync(itinerary, selectedMode, ct);

    var fileName = $"BroccoliTours-{id}-{selectedMode.ToLowerInvariant()}.pdf";
    return Results.File(bytes, "application/pdf", fileName);
})
    .WithName("GeneratePdf")
    .WithDescription("Genera un PDF dell''itinerario. Modalità disponibili: ''detailed'' (con tutti i dettagli) o ''brochure'' (formato depliant)")
    .WithTags("Itineraries")
    .Produces(200, contentType: "application/pdf")
    .Produces(404);

app.Run();

internal sealed class ResilientItineraryEngine : IItineraryEngine
{
    private readonly IItineraryEngine _primary;
    private readonly IItineraryEngine? _secondary;
    private readonly IItineraryEngine _fallback;

    public ResilientItineraryEngine(IItineraryEngine primary, IItineraryEngine? secondary, IItineraryEngine fallback)
    {
        _primary = primary;
        _secondary = secondary;
        _fallback = fallback;
    }

    public async Task<Itinerary> SuggestAsync(TravelPreferences preferences, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("[ResilientEngine] Trying PRIMARY engine...");
            return await _primary.SuggestAsync(preferences, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ResilientEngine] Primary failed: {ex.Message}");

            if (_secondary != null)
            {
                try
                {
                    Console.WriteLine("[ResilientEngine] Trying SECONDARY engine...");
                    return await _secondary.SuggestAsync(preferences, cancellationToken);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"[ResilientEngine] Secondary failed: {ex2.Message}");
                }
            }

            Console.WriteLine("[ResilientEngine] ⚠️  Both AI services unavailable - Using FALLBACK (Stub with sample data)");
            Console.WriteLine("[ResilientEngine] Tip: Check API quotas for Gemini and OpenAI");
            return await _fallback.SuggestAsync(preferences, cancellationToken);
        }
    }
}














