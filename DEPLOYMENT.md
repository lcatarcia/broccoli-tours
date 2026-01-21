# GitHub Actions Deployment Setup

Questo documento descrive come configurare i secrets necessari per il deployment automatico su Azure tramite GitHub Actions.

## Secrets Richiesti

### Backend (Azure App Service)

**Nome Secret:** `AZURE_WEBAPP_PUBLISH_PROFILE`

**Come ottenerlo:**
1. Vai al portale Azure: [wa-bt App Service](https://portal.azure.com/#@agic.it/resource/subscriptions/8f782d6a-7d62-430f-822a-c2b1a49c2bcc/resourceGroups/rg-broccoli-tours/providers/Microsoft.Web/sites/wa-bt/appServices)
2. Nel menu laterale, clicca su **"Get publish profile"** (oppure **"Scarica profilo di pubblicazione"**)
3. Si scaricherà un file XML
4. Copia l'intero contenuto del file
5. Vai su GitHub → Settings → Secrets and variables → Actions
6. Clicca su **"New repository secret"**
7. Nome: `AZURE_WEBAPP_PUBLISH_PROFILE`
8. Valore: Incolla il contenuto del file XML
9. Clicca su **"Add secret"**

### Frontend (Azure Static Web App)

**Nome Secret:** `AZURE_STATIC_WEB_APPS_API_TOKEN`

**Come ottenerlo:**
1. Vai al portale Azure: [swa-bt Static Web App](https://portal.azure.com/#@agic.it/resource/subscriptions/8f782d6a-7d62-430f-822a-c2b1a49c2bcc/resourceGroups/rg-broccoli-tours/providers/Microsoft.Web/staticSites/swa-bt/staticsite)
2. Nel menu laterale, clicca su **"Manage deployment token"** (oppure **"Gestisci token di distribuzione"**)
3. Copia il token visualizzato
4. Vai su GitHub → Settings → Secrets and variables → Actions
5. Clicca su **"New repository secret"**
6. Nome: `AZURE_STATIC_WEB_APPS_API_TOKEN`
7. Valore: Incolla il token
8. Clicca su **"Add secret"**

## Workflow Configurati

### Backend Deployment
- **File:** [.github/workflows/deploy-backend.yml](.github/workflows/deploy-backend.yml)
- **Trigger:** Push su `main` che modifica file in `backend/` o manuale
- **Processo:**
  - Restore dipendenze
  - Build progetto .NET
  - Esegue test
  - Pubblica artifact
  - Deploy su Azure App Service

### Frontend Deployment
- **File:** [.github/workflows/deploy-frontend.yml](.github/workflows/deploy-frontend.yml)
- **Trigger:** Push su `main` che modifica file in `frontend/` o manuale
- **Processo:**
  - Build React + Vite app
  - Deploy su Azure Static Web App
  - Gestione automatica delle preview per le Pull Request

## Configurazioni aggiuntive

### Variabili di build per il frontend
- Il workflow imposta l'endpoint API tramite la variabile `VITE_API_BASE`
- Per override locali crea `frontend/.env.production` partendo da [frontend/.env.production.example](frontend/.env.production.example)
- Valore di default: `https://wa-bt.azurewebsites.net/api`

### CORS del backend
- Gli origin consentiti sono definiti in [backend/src/BroccoliTours.Api/appsettings.json](backend/src/BroccoliTours.Api/appsettings.json)
- In produzione aggiungi/aggiorna la App Setting `Cors__AllowedOrigins` in Azure App Service includendo:
  - `https://ambitious-stone-008c87e03.6.azurestaticapps.net`
  - altri domini pubblici necessari

## Test del Deployment

Dopo aver configurato i secrets:

1. Fai un commit e push su `main`:
   ```bash
   git add .
   git commit -m "Setup GitHub Actions deployment"
   git push origin main
   ```

2. Vai su GitHub → Actions per vedere l'esecuzione dei workflow

3. Verifica che entrambi i deployment siano completati con successo

## Deployment Manuale

Puoi triggerare manualmente un deployment da GitHub:
1. Vai su GitHub → Actions
2. Seleziona il workflow desiderato
3. Clicca su **"Run workflow"**
4. Seleziona il branch e clicca su **"Run workflow"**

## Troubleshooting

### Backend non si deploya
- Verifica che il `AZURE_WEBAPP_PUBLISH_PROFILE` sia corretto
- Controlla i log nel workflow GitHub Actions
- Verifica che l'App Service sia in esecuzione sul portale Azure

### Frontend non si deploya
- Verifica che il `AZURE_STATIC_WEB_APPS_API_TOKEN` sia corretto
- Controlla che la build di Vite funzioni localmente: `npm run build`
- Verifica che la directory di output sia `dist`

## URL delle Applicazioni

- **Backend API:** Controlla l'URL nel portale Azure dell'App Service wa-bt
- **Frontend:** Controlla l'URL nel portale Azure della Static Web App swa-bt
