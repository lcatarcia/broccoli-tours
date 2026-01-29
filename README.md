# 🥦 Broccoli Tours - Camper Adventures

**Piattaforma web innovativa per tour operator specializzato in viaggi in camper**, che combina intelligenza artificiale, cartografia interattiva e generazione automatica di documenti per offrire esperienze di viaggio su misura.

## 📋 Indice

- [Panoramica](#panoramica)
- [Caratteristiche Principali](#caratteristiche-principali)
- [Architettura](#architettura)
- [Requisiti](#requisiti)
- [Avvio Rapido](#avvio-rapido)
- [Configurazione](#configurazione)
- [Sviluppo](#sviluppo)
- [Testing](#testing)
- [Deployment](#deployment)
- [Documentazione Tecnica](#documentazione-tecnica)

## 🌍 Panoramica

Broccoli Tours è una soluzione completa per la pianificazione di viaggi in camper che utilizza l'intelligenza artificiale (Google Gemini e OpenAI) per generare itinerari personalizzati basati sulle preferenze dell'utente. Il sistema gestisce:

- **Catalogo veicoli**: Van, camper compatti, motorhome premium e di lusso
- **Destinazioni**: Database integrato di località europee con attrazioni, servizi e aree sosta
- **Generazione itinerari AI**: Suggerimenti intelligenti con tappe, tempistiche, consigli e punti di interesse
- **Visualizzazione interattiva**: Mappe dinamiche con marker, percorsi e info window
- **Esportazione PDF**: Documenti professionali in formato dettagliato o brochure

## ✨ Caratteristiche Principali

### 🤖 Intelligenza Artificiale
- **Generazione itinerari**: Algoritmi AI (Gemini 1.5 Flash prioritario) per creare percorsi ottimizzati
- **Riparazione JSON automatica**: Sistema ricorsivo che corregge risposte AI troncate fino a 3 tentativi
- **Fallback resiliente**: Architettura a più livelli (Gemini → Stub deterministico) per alta disponibilità
- **Personalizzazione avanzata**: Considera tipo camper, periodo viaggio, preferenze tematiche e budget

### 🗺️ Mappatura Interattiva
- **Leaflet + OpenStreetMap**: Visualizzazione di tappe con marker personalizzati
- **Info window**: Dettagli su attrazioni, servizi, coordinate GPS
- **Responsive design**: Ottimizzazione per dispositivi mobile e desktop

### 📄 Generazione PDF Professionale
- **Modalità dettagliata**: Itinerario completo con timing, descrizioni, consigli e coordinate GPS
- **Modalità brochure**: Formato compatto per stampa e condivisione rapida
- **Branding**: Header decorativo Broccoli Tours su tutti i documenti

### 🔧 Gestione Errori Avanzata
- **Toast notifications**: Feedback visivo in tempo reale per l'utente
- **Retry automatico**: Tentativi multipli con strategie di continuazione ottimizzate
- **Logging dettagliato**: Tracciamento completo per debugging e monitoraggio

## 🏗️ Architettura

### Stack Tecnologico

**Backend (.NET 8)**
- **Framework**: ASP.NET Core Minimal API
- **Pattern**: Clean Architecture (Domain, Infrastructure, API)
- **Dependency Injection**: Service container nativo
- **HTTP Client**: Factory pattern per chiamate AI
- **Serializzazione**: System.Text.Json con enum converter

**Frontend (React 18 + TypeScript)**
- **Build tool**: Vite
- **Routing**: React Router v6
- **HTTP Client**: Fetch API nativo
- **Mapping**: Leaflet + React-Leaflet
- **Styling**: CSS Modules

**AI & External Services**
- **Gemini 1.5 Flash**: Motore AI primario (Google)
- **OpenAI GPT-4o-mini**: Backup (deprecato, usato solo se Gemini non disponibile)
- **OpenStreetMap**: Tile provider per mappe

### Struttura Progetto

```
broccoli-tours/
├── backend/
│   ├── BroccoliTours.sln              # Soluzione .NET
│   ├── src/
│   │   ├── BroccoliTours.Api/         # Layer API (endpoints, middleware)
│   │   ├── BroccoliTours.Domain/      # Layer Domain (entities, enums)
│   │   └── BroccoliTours.Infrastructure/  # Layer Infrastructure (implementations)
│   │       ├── Catalog/               # Cataloghi camper e destinazioni
│   │       ├── Itineraries/           # Engines AI e store
│   │       ├── Pdf/                   # Generazione documenti PDF
│   │       └── DependencyInjection/   # Registrazione servizi
│   └── tests/
│       └── BroccoliTours.Tests/       # Test unitari xUnit
├── frontend/
│   ├── src/
│   │   ├── Home.tsx                   # Pagina principale (form preferenze)
│   │   ├── Itinerary.tsx              # Visualizzazione itinerario
│   │   ├── api.ts                     # Client API backend
│   │   └── types.ts                   # TypeScript definitions
│   └── public/                        # Asset statici (logo, favicon)
├── .github/workflows/                 # CI/CD GitHub Actions
└── [DOCUMENTAZIONE].md                # File documenti vari
```

## 📦 Requisiti

### Software
- **.NET SDK 8.0+** (o .NET 9.0)
- **Node.js 18+** (consigliato 20 LTS)
- **npm** o **pnpm** (per gestione pacchetti frontend)
- **Git** (per clonazione repository)

### Chiavi API (Opzionali ma consigliate)
- **Google Gemini API Key**: [Ottieni qui](https://aistudio.google.com/app/apikey)
  - Priorità: Usata come motore primario
  - Modello default: `gemini-1.5-flash`
- **OpenAI API Key**: [Ottieni qui](https://platform.openai.com/api-keys) (deprecato, fallback opzionale)
  - Modello default: `gpt-4o-mini`

> ⚠️ **Nota**: Se nessuna chiave API è configurata, il sistema utilizza lo **StubItineraryEngine** (itinerario deterministico di fallback).

## 🚀 Avvio Rapido

### Clonazione Repository
```powershell
git clone https://github.com/tuouser/broccoli-tours.git
cd broccoli-tours
```

### 1️⃣ Backend (API .NET)

```powershell
cd backend
dotnet restore                          # Ripristino dipendenze NuGet
dotnet build                             # Compilazione
dotnet run --project .\src\BroccoliTours.Api
```

✅ **Verifica**:
- API disponibile: http://localhost:5080
- Swagger UI: http://localhost:5080/swagger
- Endpoint health: http://localhost:5080/health

### 2️⃣ Frontend (React + Vite)

```powershell
cd ..\frontend
npm install                              # Installa dipendenze npm

# Crea file .env (se non esiste)
Copy-Item .env.example .env -Force

npm run dev                              # Avvia dev server
```

✅ **Verifica**:
- UI disponibile: http://localhost:5173
- Hot reload attivo per modifiche codice

## ⚙️ Configurazione

### Chiavi API Backend

**Opzione A (Consigliata) - Variabili d'ambiente**:
```powershell
# PowerShell
$env:GEMINI_API_KEY = "your-gemini-api-key-here"
$env:OPENAI_API_KEY = "your-openai-api-key-here"  # Opzionale
```

**Opzione B - File appsettings.json**:
```json
// backend/src/BroccoliTours.Api/appsettings.json
{
  "Gemini": {
    "ApiKey": "your-gemini-api-key-here",
    "Model": "gemini-1.5-flash"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here",
    "Model": "gpt-4o-mini"
  }
}
```

> 🔒 **Sicurezza**: Non committare mai chiavi API nei file di configurazione! Usa variabili d'ambiente o Azure Key Vault in produzione.

### Frontend - Endpoint API

**Sviluppo** (automatico):
```env
# frontend/.env
VITE_API_BASE=http://localhost:5080
```

**Produzione**:
```env
# frontend/.env.production
VITE_API_BASE=https://wa-bt-hgged7edc8gph9gz.italynorth-01.azurewebsites.net
```

### CORS Backend

Per permettere richieste da domini specifici:

```json
// backend/src/BroccoliTours.Api/appsettings.json
{
  "Cors": {
    "AllowedOrigins": [
      "https://ambitious-stone-008c87e03.6.azurestaticapps.net",
      "http://localhost:5173"
    ]
  }
}
```

> Se l'array `AllowedOrigins` è vuoto, il backend accetta richieste da qualsiasi origine (solo per sviluppo locale!).

## 💻 Sviluppo

### VS Code - Tasks & Debug

#### Tasks Disponibili
Apri **Command Palette** (`Ctrl+Shift+P`) → `Tasks: Run Task`:

| Task                      | Descrizione                                          |
|---------------------------|------------------------------------------------------|
| `dev: full stack`         | Avvia backend (`dotnet watch`) + frontend (Vite) in parallelo |
| `backend: build`          | Compila progetto .NET                                |
| `backend: test`           | Esegue test unitari xUnit                            |
| `backend: watch`          | `dotnet watch` con hot reload                        |
| `frontend: install`       | `npm install` per installare dipendenze              |
| `frontend: dev`           | `npm run dev` per avviare Vite                       |
| `ops: stop backend+frontend` | Ferma processi node/dotnet su porte 5173/5080      |
| `ops: check ports`        | Verifica stato porte 5173, 5174, 5080, 5081         |

#### Configurazioni di Debug (F5)
- **Frontend: Chrome (full stack)**: Avvia entrambi i server e apre Chrome su http://localhost:5173
- **Backend: API (launch)**: Avvia solo l'API in debug (utile per breakpoint C#)

### Struttura Domain-Driven

#### Layer Domain (`BroccoliTours.Domain`)
**Entities**:
- `Camper`: Veicolo con categoria, capacità, comfort, prezzo
- `Location`: Destinazione con coordinate, descrizione, attrazioni
- `Itinerary`: Itinerario con giorni, tappe, tips generali
- `ItineraryDay`: Giorno singolo con tappe e raccomandazioni
- `ItineraryStop`: Tappa con location, timing, consigli
- `TravelPreferences`: Input utente (destinazione, periodo, budget, etc.)
- `TravelPeriod`: Range date con tipo (flexible, exact, season)

**Enums**:
- `CamperCategory`: Van, CompactCamper, PremiumMotorHome, LuxuryMotorHome
- `TravelPeriodType`: Flexible, Exact, Season

#### Layer Infrastructure (`BroccoliTours.Infrastructure`)
**Catalog**:
- `ICamperCatalog` / `InMemoryCamperCatalog`: Gestione flotta camper
- `ILocationCatalog` / `InMemoryLocationCatalog`: Database destinazioni

**Itineraries**:
- `IItineraryEngine`: Interfaccia generazione itinerari
- `GeminiItineraryEngine`: Implementazione Google Gemini
- `OpenAIItineraryEngine`: Implementazione OpenAI (deprecata)
- `StubItineraryEngine`: Fallback deterministico
- `ResilientItineraryEngine`: Wrapper con fallback automatico

**Pdf**:
- `IPdfRenderer`: Interfaccia generazione PDF
- `MinimalPdfRenderer`: Implementazione formato testo/ASCII

**DependencyInjection**:
- `ServiceCollectionExtensions`: Registrazione servizi nel container

#### Layer API (`BroccoliTours.Api`)
- `Program.cs`: Configurazione app, middleware, endpoints
- Endpoint RESTful per camper, location, itinerari, PDF
- CORS, Swagger, JSON serialization

## 🧪 Testing

### Esecuzione Test
```powershell
cd backend
dotnet test                              # Esegue tutti i test
dotnet test --filter "FullyQualifiedName~CamperCatalog"  # Test specifici
```

### Test Disponibili
- `CamperCatalogTests`: Validazione catalogo veicoli
- `LocationCatalogTests`: Validazione catalogo destinazioni
- `StubItineraryEngineTests`: Test engine fallback
- `ItineraryStoreTests`: Test storage in-memory

### Coverage (Opzionale)
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## 🚢 Deployment

Il progetto include CI/CD automatizzato via **GitHub Actions** per deployment su **Azure**.

### Architettura Azure
- **Backend**: Azure App Service (Linux) - https://wa-bt-hgged7edc8gph9gz.italynorth-01.azurewebsites.net
- **Frontend**: Azure Static Web Apps - https://ambitious-stone-008c87e03.6.azurestaticapps.net

### Workflow GitHub Actions
1. **Backend** (`.github/workflows/deploy-backend.yml`):
   - Trigger: Push su `main` con modifiche in `backend/`
   - Build .NET, esecuzione test, publish su Azure App Service

2. **Frontend** (`.github/workflows/deploy-frontend.yml`):
   - Trigger: Push su `main` con modifiche in `frontend/`
   - Build React+Vite, deploy su Azure Static Web Apps

### Configurazione Secrets
Vedi **[DEPLOYMENT.md](./DEPLOYMENT.md)** per istruzioni dettagliate su:
- `AZURE_WEBAPP_PUBLISH_PROFILE` (Backend)
- `AZURE_STATIC_WEB_APPS_API_TOKEN` (Frontend)

## 📚 Documentazione Tecnica

- **[TECHNICAL_ARCHITECTURE.md](./TECHNICAL_ARCHITECTURE.md)**: Architettura dettagliata, pattern, design decisions
- **[JSON_REPAIR_FEATURE.md](./JSON_REPAIR_FEATURE.md)**: Sistema di riparazione JSON ricorsivo
- **[LOGO_INTEGRATION.md](./LOGO_INTEGRATION.md)**: Integrazione branding Broccoli Tours
- **[DEPLOYMENT.md](./DEPLOYMENT.md)**: Guida deployment Azure con GitHub Actions

## 🛠️ Tecnologie & Librerie

### Backend
| Libreria                  | Versione | Utilizzo                              |
|---------------------------|----------|---------------------------------------|
| .NET                      | 8.0+     | Runtime & SDK                         |
| ASP.NET Core              | 8.0+     | Web framework                         |
| System.Text.Json          | Built-in | Serializzazione JSON                  |
| xUnit                     | 2.4+     | Testing framework                     |

### Frontend
| Libreria                  | Versione | Utilizzo                              |
|---------------------------|----------|---------------------------------------|
| React                     | 18.x     | UI library                            |
| TypeScript                | 5.x      | Type safety                           |
| Vite                      | 5.x      | Build tool & dev server               |
| React Router              | 6.x      | Client-side routing                   |
| Leaflet                   | 1.9+     | Mapping library                       |
| React-Leaflet             | 4.x      | React bindings per Leaflet            |

## 📝 API Endpoints

### Health
```http
GET /health
```
Ritorna stato API e versione.

### Camper
```http
GET /api/campers
```
Ritorna lista completa veicoli disponibili.

### Location
```http
GET /api/locations
```
Ritorna database destinazioni europee.

### Itinerari
```http
POST /api/itineraries/suggest
Content-Type: application/json

{
  "destination": "Italia",
  "travelPeriod": {
    "type": "Flexible",
    "durationDays": 7
  },
  "camperCategory": "CompactCamper",
  "budget": "Medium",
  "interests": ["Culture", "Nature"],
  "pace": "Relaxed"
}
```
Genera itinerario personalizzato con AI.

**Headers di risposta**:
- `X-Json-Repair-Attempts`: Numero tentativi riparazione JSON (se > 0)

### PDF
```http
GET /api/itineraries/{id}/pdf?mode=detailed
GET /api/itineraries/{id}/pdf?mode=brochure
```
Scarica PDF itinerario in formato dettagliato o brochure.

## 🤝 Contributi

Per contribuire al progetto:
1. Fork del repository
2. Crea branch feature (`git checkout -b feature/AmazingFeature`)
3. Commit modifiche (`git commit -m 'Add AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Apri Pull Request

## 📄 Licenza

Questo progetto è proprietario di Broccoli Tours. Tutti i diritti riservati.

## 📧 Contatti

**Broccoli Tours** - Tour Operator Camper Adventures
- Website: (da definire)
- Email: info@broccoliatours.com

---

_Realizzato con ❤️ per gli amanti dei viaggi on the road_ 🚐
