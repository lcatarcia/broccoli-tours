# 🏗️ Architettura Tecnica - Broccoli Tours

## Indice
- [Overview](#overview)
- [Principi Architetturali](#principi-architetturali)
- [Backend Architecture](#backend-architecture)
- [Frontend Architecture](#frontend-architecture)
- [AI Integration Layer](#ai-integration-layer)
- [Data Flow](#data-flow)
- [Security](#security)
- [Performance & Scalability](#performance--scalability)
- [Error Handling & Resilience](#error-handling--resilience)
- [Design Decisions](#design-decisions)

---

## Overview

Broccoli Tours implementa una **Clean Architecture** moderna basata su:
- **Separazione delle responsabilità** tra Domain, Infrastructure e API
- **Dependency Inversion** tramite interfacce e Dependency Injection
- **Testabilità** attraverso astrazione e mocking
- **Resilienza** con fallback automatici e retry logic
- **Scalabilità** attraverso servizi stateless e storage distribuibile

### Diagramma Architettura di Alto Livello

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend (React + TS)                     │
│  ┌──────────────┐  ┌───────────────┐  ┌─────────────────┐  │
│  │   Home.tsx   │  │ Itinerary.tsx │  │  api.ts (HTTP)  │  │
│  │ (Form Input) │  │  (Map + PDF)  │  │   Client        │  │
│  └──────────────┘  └───────────────┘  └─────────────────┘  │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTP REST
                             ▼
┌─────────────────────────────────────────────────────────────┐
│              Backend (.NET 8 Minimal API)                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                    Program.cs                         │   │
│  │  - Endpoints, Middleware, CORS, Swagger              │   │
│  └──────────────────────────────────────────────────────┘   │
│                             │                                │
│  ┌──────────────────────────▼────────────────────────────┐  │
│  │         BroccoliTours.Infrastructure                   │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌───────────┐   │  │
│  │  │   Catalog    │  │ Itineraries  │  │    Pdf    │   │  │
│  │  │  - Campers   │  │  - Engines   │  │ Renderer  │   │  │
│  │  │  - Locations │  │  - Store     │  │           │   │  │
│  │  └──────────────┘  └──────────────┘  └───────────┘   │  │
│  └───────────────────────────────────────────────────────┘  │
│                             │                                │
│  ┌──────────────────────────▼────────────────────────────┐  │
│  │          BroccoliTours.Domain                          │  │
│  │  - Entities (Itinerary, Location, Camper, ...)        │  │
│  │  - Enums (CamperCategory, TravelPeriodType)           │  │
│  │  - Value Objects (TravelPreferences, TravelPeriod)    │  │
│  └────────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTP REST
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                     External Services                        │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────────┐    │
│  │   Gemini    │  │   OpenAI    │  │  OpenStreetMap   │    │
│  │ 1.5 Flash   │  │ GPT-4o-mini │  │   Tile Server    │    │
│  └─────────────┘  └─────────────┘  └──────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

---

## Principi Architetturali

### 1. Clean Architecture (Onion Architecture)

Il backend segue il pattern Clean Architecture con tre layer principali:

#### **Domain Layer** (`BroccoliTours.Domain`)
- **Responsabilità**: Contiene la business logic pura, entità del dominio, value objects e regole di business
- **Dipendenze**: NESSUNA (zero dipendenze esterne)
- **Contenuto**:
  - **Entities**: `Itinerary`, `Location`, `Camper`, `ItineraryDay`, `ItineraryStop`
  - **Value Objects**: `TravelPreferences`, `TravelPeriod`
  - **Enums**: `CamperCategory`, `TravelPeriodType`
- **Principio**: Il dominio è il nucleo dell'applicazione e non conosce l'esistenza di database, API o UI

#### **Infrastructure Layer** (`BroccoliTours.Infrastructure`)
- **Responsabilità**: Implementazioni concrete di servizi, accesso a risorse esterne (AI, storage), persistenza
- **Dipendenze**: Domain Layer, librerie esterne (HttpClient, etc.)
- **Contenuto**:
  - **Catalog**: Implementazioni in-memory di cataloghi (camper, location)
  - **Itineraries**: Engines AI (Gemini, OpenAI, Stub), store itinerari
  - **Pdf**: Renderer PDF per esportazione documenti
  - **DependencyInjection**: Registrazione servizi nel container
- **Pattern**: Repository, Strategy (per engines AI), Factory (per HttpClient)

#### **API Layer** (`BroccoliTours.Api`)
- **Responsabilità**: Esposizione HTTP endpoints, gestione richieste/risposte, middleware
- **Dipendenze**: Infrastructure Layer, Domain Layer
- **Contenuto**:
  - **Program.cs**: Configurazione app, endpoint mapping, CORS, Swagger
  - **appsettings.json**: Configurazione runtime (CORS, AI keys, etc.)
- **Pattern**: Minimal API (.NET), Dependency Injection, CORS policy

### 2. Dependency Inversion Principle (DIP)

Tutte le dipendenze puntano verso il Domain Layer attraverso interfacce:

```csharp
// Domain definisce il contratto
public interface IItineraryEngine
{
    Task<Itinerary> SuggestAsync(TravelPreferences preferences, CancellationToken ct);
}

// Infrastructure implementa
public class GeminiItineraryEngine : IItineraryEngine { ... }
public class OpenAIItineraryEngine : IItineraryEngine { ... }
public class StubItineraryEngine : IItineraryEngine { ... }

// API usa l'interfaccia (non la concretezza)
app.MapPost("/api/itineraries/suggest", async (
    TravelPreferences prefs, 
    IItineraryEngine engine) => 
{
    var itinerary = await engine.SuggestAsync(prefs);
    return Results.Ok(itinerary);
});
```

**Vantaggi**:
- Testabilità: Mock delle interfacce nei test
- Flessibilità: Swap implementazioni senza modificare consumer
- Manutenibilità: Modifiche localizzate senza impatto a cascata

### 3. Separation of Concerns (SoC)

Ogni componente ha una responsabilità chiara e non sovrapposta:

| Componente                | Responsabilità                                   |
|---------------------------|--------------------------------------------------|
| `ICamperCatalog`          | Gestione e ricerca veicoli                       |
| `ILocationCatalog`        | Gestione e ricerca destinazioni                  |
| `IItineraryEngine`        | Generazione itinerari tramite AI                 |
| `IItineraryStore`         | Persistenza e recupero itinerari                 |
| `IPdfRenderer`            | Conversione itinerari in documenti PDF           |
| `ResilientItineraryEngine`| Fallback automatico tra engines                  |

### 4. Strategy Pattern per AI Engines

Multiple implementazioni di `IItineraryEngine` permettono di:
- Usare provider AI diversi (Gemini, OpenAI) senza modificare consumer
- Implementare fallback automatici con `ResilientItineraryEngine`
- Testare con `StubItineraryEngine` senza chiamate API reali

```csharp
// Registrazione con fallback chain
services.AddSingleton<IItineraryEngine>(sp =>
{
    var gemini = sp.GetRequiredService<GeminiItineraryEngine>();
    var openai = sp.GetRequiredService<OpenAIItineraryEngine>();
    var stub = sp.GetRequiredService<StubItineraryEngine>();
    
    // Gemini → OpenAI → Stub
    return new ResilientItineraryEngine(gemini, openai, stub);
});
```

---

## Backend Architecture

### Minimal API Design

Il progetto usa **ASP.NET Core Minimal API** (introdotto in .NET 6+) per:
- **Riduzione boilerplate**: Nessun controller, action method, attributi routing complessi
- **Performance**: Meno overhead, startup più rapido
- **Semplicità**: Endpoint definiti direttamente in `Program.cs` con lambda expressions

#### Esempio Endpoint
```csharp
app.MapPost("/api/itineraries/suggest", async (
    TravelPreferences preferences,
    IItineraryEngine engine,
    IItineraryStore store,
    CancellationToken ct) =>
{
    var itinerary = await engine.SuggestAsync(preferences, ct);
    store.Save(itinerary);
    
    var attempts = GeminiItineraryEngine.CurrentJsonRepairAttempts;
    
    return Results.Ok(new
    {
        itinerary,
        repairAttempts = attempts > 0 ? attempts : (int?)null
    });
})
.WithName("SuggestItinerary")
.WithOpenApi();
```

### Dependency Injection Container

**Registrazione servizi** avviene in `Program.cs` e `ServiceCollectionExtensions`:

```csharp
// Infrastructure services
builder.Services.AddBroccoliToursInfrastructure();

// Custom factory per IItineraryEngine con fallback logic
builder.Services.AddSingleton<IItineraryEngine>(sp =>
{
    var geminiKey = GetGeminiKey();
    var stub = sp.GetRequiredService<StubItineraryEngine>();
    
    if (!string.IsNullOrWhiteSpace(geminiKey))
    {
        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("gemini");
        var locations = sp.GetRequiredService<ILocationCatalog>();
        var gemini = new GeminiItineraryEngine(http, locations, geminiKey);
        return new ResilientItineraryEngine(gemini, null, stub);
    }
    
    return stub;
});
```

### HttpClient Factory Pattern

Per chiamate AI, il progetto usa `IHttpClientFactory` per:
- **Gestione pooling**: Riuso connessioni HTTP per performance
- **Configurazione centralizzata**: Timeout, base address, headers
- **Resiliency**: Integrazione con Polly (se necessario in futuro)

```csharp
builder.Services.AddHttpClient("gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
});
```

### In-Memory Storage

Attualmente il sistema usa storage in-memory per itinerari:

```csharp
public class InMemoryItineraryStore : IItineraryStore
{
    private readonly ConcurrentDictionary<Guid, Itinerary> _store = new();
    
    public void Save(Itinerary itinerary) => 
        _store[itinerary.Id] = itinerary;
    
    public Itinerary? GetById(Guid id) => 
        _store.TryGetValue(id, out var it) ? it : null;
}
```

**Motivazione**: Semplicità per MVP, nessuna persistenza necessaria per demo.

**Evoluzione futura**: Sostituire con:
- **Azure Cosmos DB** (NoSQL, global distribution)
- **SQL Server** (relazionale, transazioni)
- **Redis** (cache distribuita, TTL automatico)

### JSON Serialization

Il sistema usa `System.Text.Json` con configurazioni custom:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Enum come stringhe invece di numeri
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    
    // Naming policy camelCase (default)
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
```

---

## Frontend Architecture

### Component Structure

```
frontend/src/
├── main.tsx              # Entry point, ReactDOM render
├── App.tsx               # Routing setup (React Router)
├── Home.tsx              # Form preferenze utente
├── Itinerary.tsx         # Visualizzazione itinerario + mappa
├── Toast.tsx             # Notifiche utente
├── ToastContext.ts       # Context API per toast
├── useToast.ts           # Hook custom per toast
├── api.ts                # Client HTTP per backend
├── types.ts              # TypeScript types/interfaces
└── [Component].css       # CSS modules per styling
```

### React Router v6

Routing client-side per navigazione SPA:

```tsx
<BrowserRouter>
  <Routes>
    <Route path="/" element={<Home />} />
    <Route path="/itinerary" element={<Itinerary />} />
  </Routes>
</BrowserRouter>
```

**Flow**:
1. User compila form in `Home.tsx`
2. Submit POST a `/api/itineraries/suggest`
3. Navigate a `/itinerary` con state (itinerario generato)
4. `Itinerary.tsx` renderizza mappa e dettagli

### State Management

**Local State** con `useState`:
- Form input (destinazione, date, budget, etc.)
- Loading states, error states
- Itinerario corrente visualizzato

**URL State** con `useLocation`:
```tsx
const location = useLocation();
const itinerary = location.state?.itinerary;
```

**Context API** per toast:
```tsx
const ToastContext = createContext<ToastContextType | null>(null);

export const useToast = () => {
  const context = useContext(ToastContext);
  if (!context) throw new Error('useToast must be within ToastProvider');
  return context;
};
```

### HTTP Client (api.ts)

Wrapper TypeScript per chiamate backend:

```typescript
export async function suggestItinerary(
  preferences: TravelPreferences
): Promise<{ itinerary: Itinerary; repairAttempts?: number }> {
  const response = await fetch(`${API_BASE}/api/itineraries/suggest`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(preferences),
  });
  
  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }
  
  return response.json();
}
```

### Leaflet Integration

React-Leaflet fornisce componenti dichiarativi per mappe:

```tsx
<MapContainer center={[45.4, 11.8]} zoom={8}>
  <TileLayer
    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
    attribution="© OpenStreetMap contributors"
  />
  
  {itinerary.days.map(day =>
    day.stops.map(stop => (
      <Marker key={stop.location.name} position={[stop.location.latitude, stop.location.longitude]}>
        <Popup>
          <strong>{stop.location.name}</strong>
          <p>{stop.location.description}</p>
        </Popup>
      </Marker>
    ))
  )}
  
  <Polyline positions={routeCoordinates} color="blue" />
</MapContainer>
```

**Features**:
- Marker per ogni tappa
- Popup con info location
- Polyline per collegare tappe
- Responsive bounds fitting

### Vite Build Tool

Vite offre:
- **Dev server ultra-rapido** con HMR (Hot Module Replacement)
- **Build ottimizzato** con Rollup (tree-shaking, code splitting)
- **TypeScript support** nativo
- **Environment variables** tramite `import.meta.env.VITE_*`

```typescript
const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5080';
```

---

## AI Integration Layer

### Gemini 1.5 Flash (Primary)

**Configurazione**:
- **Endpoint**: `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}`
- **Modello**: `gemini-1.5-flash` (default), configurabile
- **Timeout**: 60 secondi
- **Response format**: JSON con `responseMimeType: application/json`

**Request Body**:
```json
{
  "contents": [
    {
      "parts": [
        {
          "text": "Genera itinerario per viaggi in camper in Italia, 7 giorni..."
        }
      ]
    }
  ],
  "generationConfig": {
    "responseMimeType": "application/json",
    "temperature": 0.7,
    "maxOutputTokens": 8192
  }
}
```

**Response Parsing**:
```csharp
var jsonResponse = await response.Content.ReadFromJsonAsync<JsonDocument>();
var text = jsonResponse
    .RootElement
    .GetProperty("candidates")[0]
    .GetProperty("content")
    .GetProperty("parts")[0]
    .GetProperty("text")
    .GetString();

var itinerary = JsonSerializer.Deserialize<Itinerary>(text);
```

### OpenAI GPT-4o-mini (Deprecated Fallback)

**Configurazione**:
- **Endpoint**: `https://api.openai.com/v1/chat/completions`
- **Modello**: `gpt-4o-mini`
- **Timeout**: 60 secondi
- **Response format**: `response_format: { type: "json_object" }`

**Request Body**:
```json
{
  "model": "gpt-4o-mini",
  "messages": [
    {
      "role": "system",
      "content": "Sei un esperto di viaggi in camper..."
    },
    {
      "role": "user",
      "content": "Genera itinerario per Italia, 7 giorni..."
    }
  ],
  "temperature": 0.7,
  "max_tokens": 4000,
  "response_format": { "type": "json_object" }
}
```

**Perché deprecato**:
- Gemini offre quota gratuita più generosa
- Gemini 1.5 Flash ha performance simili/migliori
- Costo OpenAI più elevato per uso produzione

### Stub Engine (Deterministic Fallback)

Engine deterministico per:
- **Sviluppo locale** senza API keys
- **Testing** con output prevedibile
- **Fallback** quando tutti i servizi AI falliscono

```csharp
public class StubItineraryEngine : IItineraryEngine
{
    public Task<Itinerary> SuggestAsync(TravelPreferences preferences, CancellationToken ct)
    {
        // Genera itinerario hardcoded basato su preferences
        var days = GenerateDeterministicDays(preferences);
        return Task.FromResult(new Itinerary { Days = days, ... });
    }
}
```

### Resilient Engine (Fallback Chain)

Wrapper che implementa fallback automatico tra engines:

```csharp
public class ResilientItineraryEngine : IItineraryEngine
{
    private readonly IItineraryEngine _primary;
    private readonly IItineraryEngine? _secondary;
    private readonly IItineraryEngine _fallback;

    public async Task<Itinerary> SuggestAsync(TravelPreferences prefs, CancellationToken ct)
    {
        try
        {
            return await _primary.SuggestAsync(prefs, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Primary engine failed: {ex.Message}");
            
            if (_secondary != null)
            {
                try
                {
                    return await _secondary.SuggestAsync(prefs, ct);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"Secondary engine failed: {ex2.Message}");
                }
            }
            
            Console.WriteLine("Using fallback stub engine");
            return await _fallback.SuggestAsync(prefs, ct);
        }
    }
}
```

**Fallback Chain**: Gemini → OpenAI (se configurato) → Stub

---

## Data Flow

### Generazione Itinerario - Sequence Diagram

```
User          Frontend        Backend API       IItineraryEngine   IItineraryStore   AI Service
 │                │                │                   │                  │              │
 │ Fill form      │                │                   │                  │              │
 │───────────────>│                │                   │                  │              │
 │                │                │                   │                  │              │
 │ Submit         │ POST /suggest  │                   │                  │              │
 │───────────────>│───────────────>│                   │                  │              │
 │                │                │ SuggestAsync()    │                  │              │
 │                │                │──────────────────>│                  │              │
 │                │                │                   │ HTTP POST        │              │
 │                │                │                   │─────────────────────────────────>│
 │                │                │                   │                  │  JSON        │
 │                │                │                   │<─────────────────────────────────│
 │                │                │                   │ (JSON repair)    │              │
 │                │                │                   │ if truncated     │              │
 │                │                │   Itinerary       │                  │              │
 │                │                │<──────────────────│                  │              │
 │                │                │ Save(itinerary)   │                  │              │
 │                │                │────────────────────────────────────> │              │
 │                │    200 OK      │                   │                  │              │
 │                │<───────────────│                   │                  │              │
 │  Navigate      │                │                   │                  │              │
 │  /itinerary    │                │                   │                  │              │
 │<───────────────│                │                   │                  │              │
 │                │                │                   │                  │              │
```

### PDF Download - Sequence Diagram

```
User       Frontend         Backend API       IItineraryStore     IPdfRenderer
 │             │                  │                  │                  │
 │ Click       │                  │                  │                  │
 │ Download PDF│                  │                  │                  │
 │────────────>│ GET /pdf?mode=   │                  │                  │
 │             │   detailed       │                  │                  │
 │             │─────────────────>│                  │                  │
 │             │                  │ GetById(id)      │                  │
 │             │                  │─────────────────>│                  │
 │             │                  │  Itinerary       │                  │
 │             │                  │<─────────────────│                  │
 │             │                  │ Render(it, mode) │                  │
 │             │                  │─────────────────────────────────────>│
 │             │                  │                  │    byte[]        │
 │             │                  │<─────────────────────────────────────│
 │             │   200 OK         │                  │                  │
 │             │   (PDF bytes)    │                  │                  │
 │             │<─────────────────│                  │                  │
 │  Download   │                  │                  │                  │
 │<────────────│                  │                  │                  │
```

---

## Security

### API Key Protection

**Best Practices implementate**:
1. **Environment Variables**: Chiavi in variabili d'ambiente, mai hardcoded
2. **appsettings.json exclusion**: File con chiavi escluso da Git (`.gitignore`)
3. **Azure Key Vault**: In produzione, usare Key Vault per secrets management
4. **Placeholder values**: `__SET_ME__` nei file template per evitare leak

```csharp
var geminiKey = configuration["Gemini:ApiKey"];
if (string.IsNullOrWhiteSpace(geminiKey) || geminiKey == "__SET_ME__")
    geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
```

### CORS Policy

**Configurazione CORS**:
```csharp
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        if (allowedOrigins.Length == 0)
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});
```

**Produzione**:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://ambitious-stone-008c87e03.6.azurestaticapps.net"
    ]
  }
}
```

**Sviluppo locale**: `AllowedOrigins` vuoto → `AllowAnyOrigin()` (NON per produzione!)

### Input Validation

**Validazione lato backend**:
- TravelPreferences deserializzate con controlli tipo
- Budget enum validato automaticamente da serializer
- Camper category enum validato

**Future improvements**:
- FluentValidation per regole complesse
- Data Annotations per validazione dichiarativa
- Custom validators per business rules

---

## Performance & Scalability

### Async/Await Everywhere

Tutti i metodi I/O-bound sono asincroni per:
- **Non bloccare thread pool**: Liberare thread durante I/O
- **Scalabilità**: Gestire più richieste concorrenti
- **Throughput**: Ridurre latenza percepita

```csharp
public async Task<Itinerary> SuggestAsync(TravelPreferences prefs, CancellationToken ct)
{
    var response = await _http.PostAsJsonAsync(url, body, ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Parse(content);
}
```

### CancellationToken Support

Propagazione `CancellationToken` per:
- **Cancellazione richieste**: Se client disconnette, annulla chiamate AI
- **Timeout management**: Evitare richieste infinite
- **Resource cleanup**: Liberare risorse su cancellazione

```csharp
app.MapPost("/api/itineraries/suggest", async (
    TravelPreferences prefs,
    IItineraryEngine engine,
    CancellationToken ct) =>
{
    var itinerary = await engine.SuggestAsync(prefs, ct);
    return Results.Ok(itinerary);
});
```

### HttpClient Pooling

`IHttpClientFactory` gestisce pool di connessioni HTTP per:
- **Riuso connessioni**: Evitare overhead TCP handshake
- **DNS refresh**: Evitare stale DNS entries
- **Performance**: Ridurre latenza chiamate AI

### In-Memory Caching (Potential)

**Future optimization**:
```csharp
public class CachedLocationCatalog : ILocationCatalog
{
    private readonly IMemoryCache _cache;
    private readonly ILocationCatalog _inner;
    
    public Location? GetByName(string name)
    {
        return _cache.GetOrCreate($"loc:{name}", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return _inner.GetByName(name);
        });
    }
}
```

### Scalability Considerations

**Architettura stateless**:
- Nessuna sessione server-side
- Storage condiviso (future: Cosmos DB, Redis)
- Orizzontal scaling su Azure App Service

**Auto-scaling Azure**:
- Scale-out automatico su CPU/memoria threshold
- Scale-in per ridurre costi in periodi low-traffic

---

## Error Handling & Resilience

### JSON Repair System (Ricorsivo)

Sistema avanzato per gestire JSON troncati da AI (vedi [JSON_REPAIR_FEATURE.md](./JSON_REPAIR_FEATURE.md)):

**Strategia**:
1. **Detection**: Catch `JsonException` su parsing
2. **Continuation**: Chiedi all'AI di generare SOLO la parte mancante
3. **Concatenation**: Unisci parte originale + continuazione
4. **Validation**: Verifica JSON completo valido
5. **Retry**: Fino a 3 tentativi ricorsivi

**Tracciamento**:
```csharp
private static readonly AsyncLocal<int> _jsonRepairAttempts = new AsyncLocal<int>();

public async Task<Itinerary> SuggestAsync(...)
{
    _jsonRepairAttempts.Value = 0;
    
    try
    {
        return await GenerateItinerary(...);
    }
    catch (JsonException ex)
    {
        _jsonRepairAttempts.Value++;
        return await RepairAndRetry(truncatedJson, ...);
    }
}
```

**Comunicazione frontend**:
- Header HTTP: `X-Json-Repair-Attempts`
- Toast notifications per ogni tentativo

### Fallback Chain

`ResilientItineraryEngine` garantisce sempre un risultato:
1. **Primary** (Gemini): Tentativo principale
2. **Secondary** (OpenAI, opzionale): Se primary fallisce
3. **Fallback** (Stub): Se tutti falliscono, risultato deterministico

**Logging**:
```csharp
Console.WriteLine("Primary engine failed: {0}", ex.Message);
Console.WriteLine("Trying secondary engine...");
Console.WriteLine("Using fallback stub engine");
```

### Toast Notifications (Frontend)

Sistema di notifiche real-time per:
- **JSON repair progress**: Informare utente su tentativi correzione
- **Errori API**: Mostrare errori in modo user-friendly
- **Successi**: Confermare operazioni completate

```typescript
const { showToast } = useToast();

if (repairAttempts > 0) {
  for (let i = 1; i <= repairAttempts; i++) {
    setTimeout(() => {
      showToast(
        `🔧 Riparazione JSON (tentativo ${i}/${repairAttempts}): il sistema sta correggendo la risposta dell'AI...`,
        'warning',
        4000
      );
    }, (i - 1) * 300);
  }
}
```

### Timeout Configuration

**Backend**:
```csharp
_http.Timeout = TimeSpan.FromSeconds(60);
```

**Future**: Implementare retry con backoff esponenziale usando Polly:
```csharp
services.AddHttpClient("gemini")
    .AddTransientHttpErrorPolicy(p => 
        p.WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

---

## Design Decisions

### Perché Minimal API invece di Controller?

**Pro**:
- ✅ Meno boilerplate (nessun `[ApiController]`, `[Route]`, `[HttpPost]`)
- ✅ Performance leggermente superiore
- ✅ Codice più conciso per API semplici
- ✅ Swagger auto-generation con `.WithOpenApi()`

**Contro**:
- ❌ Meno struttura per API complesse (30+ endpoints)
- ❌ Binding implicito (può confondere su parametri complessi)

**Decisione**: Ottimo per MVP con ~5 endpoint, valutare Controller se API cresce.

### Perché In-Memory Storage invece di Database?

**Pro**:
- ✅ Semplicità implementazione MVP
- ✅ Nessuna configurazione infrastructure
- ✅ Performance eccellente (no latenza I/O)
- ✅ Costo zero

**Contro**:
- ❌ Dati persi al restart
- ❌ Non scalabile orizzontalmente (sessioni non condivise)
- ❌ No persistenza storica

**Decisione**: Sufficiente per demo, sostituire con Cosmos DB/SQL per produzione.

### Perché Gemini invece di OpenAI come Primary?

**Motivazioni**:
- ✅ **Quota gratuita** più generosa (60 req/min vs OpenAI pay-per-use)
- ✅ **Performance**: Gemini 1.5 Flash ottimizzato per speed
- ✅ **Costo**: Significativamente più economico per produzione
- ✅ **JSON native**: `responseMimeType: application/json` più affidabile

**Risultato**: OpenAI mantenuto solo come fallback opzionale.

### Perché System.Text.Json invece di Newtonsoft.Json?

**Motivazioni**:
- ✅ **Built-in**: Nessuna dipendenza esterna
- ✅ **Performance**: Più veloce (span-based)
- ✅ **Modernità**: Supporto nativi per `JsonDocument`, `Utf8JsonReader`
- ✅ **Futuro-proof**: Standard .NET moderno

**Contro**:
- ❌ Meno feature rispetto a Newtonsoft (es. LINQ to JSON)

**Decisione**: Sufficiente per 95% casi d'uso, feature set adeguato.

### Perché React invece di Angular/Vue?

**Motivazioni**:
- ✅ **Ecosistema**: Librerie abbondanti (React-Leaflet, etc.)
- ✅ **Flessibilità**: Non opinionated, componibile
- ✅ **Community**: Più grande, risorse, job market
- ✅ **TypeScript**: Supporto first-class

**Decisione**: Standard de facto per SPA moderne, team familiarity.

### Perché Vite invece di Create React App?

**Motivazioni**:
- ✅ **Speed**: Dev server istantaneo (HMR sub-second)
- ✅ **Modern**: ESM nativo, no bundling in dev
- ✅ **Build size**: Output più piccolo rispetto a CRA
- ✅ **Supporto**: CRA è deprecato/poco mantenuto

**Decisione**: Vite è il nuovo standard per React tooling.

---

## Deployment Architecture (Azure)

```
                      ┌─────────────────────────────────┐
                      │   GitHub Repository (main)      │
                      └────────────┬────────────────────┘
                                   │
                     ┌─────────────┴─────────────┐
                     │                           │
            ┌────────▼────────┐        ┌────────▼────────┐
            │ GitHub Actions  │        │ GitHub Actions  │
            │ Backend Workflow│        │Frontend Workflow│
            └────────┬────────┘        └────────┬────────┘
                     │                           │
         ┌───────────▼───────────┐   ┌──────────▼──────────┐
         │  Azure App Service    │   │ Azure Static Web App│
         │  (Linux, .NET 8)      │   │ (React + Vite)      │
         │  wa-bt-hgged7e...     │   │ ambitious-stone...  │
         └───────────────────────┘   └─────────────────────┘
                     │                           │
                     │    REST API               │
                     └───────────┬───────────────┘
                                 │
                      ┌──────────▼──────────┐
                      │   External Services  │
                      │  - Gemini API        │
                      │  - OpenStreetMap     │
                      └──────────────────────┘
```

### CI/CD Pipeline

**Backend** (`.github/workflows/deploy-backend.yml`):
1. Trigger: Push su `main` con modifiche in `backend/`
2. Checkout code
3. Setup .NET 8
4. `dotnet restore` → `dotnet build` → `dotnet test`
5. `dotnet publish` con release config
6. Deploy su Azure App Service via publish profile

**Frontend** (`.github/workflows/deploy-frontend.yml`):
1. Trigger: Push su `main` con modifiche in `frontend/`
2. Checkout code
3. Setup Node.js 20
4. `npm install` → `npm run build`
5. Deploy su Azure Static Web Apps via token

---

## Future Improvements

### Backend
- [ ] **Database integration**: Cosmos DB per persistenza itinerari
- [ ] **Authentication**: Azure AD B2C per utenti registrati
- [ ] **Caching**: Redis per cataloghi e itinerari popolari
- [ ] **Monitoring**: Application Insights per telemetria
- [ ] **Rate limiting**: Protezione da abuse API
- [ ] **API versioning**: Supporto v1, v2 endpoints
- [ ] **GraphQL**: Alternativa a REST per query flessibili

### Frontend
- [ ] **Progressive Web App**: Offline support, installable
- [ ] **Server-Side Rendering**: Next.js per SEO e performance
- [ ] **State management**: Zustand/Redux per state complesso
- [ ] **Internationalization**: i18n per multi-lingua
- [ ] **Accessibility**: WCAG 2.1 AA compliance
- [ ] **E2E testing**: Playwright/Cypress per test UI

### AI
- [ ] **Streaming responses**: Server-Sent Events per progress real-time
- [ ] **Vector search**: Semantic search per destinazioni simili
- [ ] **Multimodal**: Generazione immagini itinerario con DALL-E/Imagen
- [ ] **User feedback loop**: Fine-tuning modelli su preferenze utenti

### DevOps
- [ ] **Infrastructure as Code**: Bicep/Terraform per Azure resources
- [ ] **Blue-Green Deployment**: Zero-downtime releases
- [ ] **Load testing**: K6/JMeter per performance validation
- [ ] **Security scanning**: Dependabot, Snyk, OWASP ZAP

---

## Conclusioni

L'architettura di Broccoli Tours bilancia **semplicità** (per MVP rapido) con **estensibilità** (per evoluzione futura). I principi di Clean Architecture, Dependency Inversion e Separation of Concerns garantiscono una codebase **manutenibile** e **testabile**.

La scelta di tecnologie moderne (.NET 8, React 18, Vite, Gemini) posiziona il progetto per **performance eccellenti** e **developer experience** ottimale.

Per domande sull'architettura, contattare il team di sviluppo o consultare la documentazione correlata.
