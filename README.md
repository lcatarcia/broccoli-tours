# Broccoli Tours (monorepo)

Tour operator web app specializzato in viaggi in camper (van → motorhome), con suggerimenti AI, mappa e download PDF.

## Requisiti
- .NET SDK 8
- Node.js 18+ (consigliato 20)

## Avvio rapido (Windows PowerShell)

### 1) Backend (API)
```powershell
cd backend
dotnet restore
dotnet run --project .\src\BroccoliTours.Api
```
- API: http://localhost:5080
- Swagger: http://localhost:5080/swagger

### 2) Frontend (React)
```powershell
cd ..\frontend
npm install
Copy-Item .env.example .env -Force
npm run dev
```
- UI: http://localhost:5173

## VS Code (Tasks & Debug)

Apri il Command Palette → **Tasks: Run Task**:
- `dev: full stack` avvia backend (`dotnet watch`) + frontend (Vite) in parallelo.
- `backend: test` esegue i test.

Per il debug (F5):
- **Frontend: Chrome (full stack)** avvia tutto e apre Chrome su http://localhost:5173.
- **Backend: API (launch)** avvia l’API in debug (utile per breakpoint nel backend).

## Configurazione AI (OpenAI)
- Opzione A (consigliata): imposta la variabile d’ambiente `OPENAI_API_KEY`.
- Opzione B: metti la key in `backend/src/BroccoliTours.Api/appsettings.json` sotto `OpenAI:ApiKey`.

Se la key non è presente o se la chiamata fallisce, il backend fa fallback automatico allo stub deterministico.

## API principali
- `GET /health`
- `GET /api/campers`
- `GET /api/locations`
- `POST /api/itineraries/suggest`
- `GET /api/itineraries/{id}/pdf?mode=detailed|brochure`

## Note
- PDF: generazione minimale ma leggibile (modalità `detailed` e `brochure`).
