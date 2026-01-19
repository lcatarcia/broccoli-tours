# Integrazione Logo Broccoli Tours

## Logo del Tour Operator
Il logo "Broccoli Tours - Camper Adventures" è stato integrato nel progetto.

### Posizionamento del logo

Per completare l'integrazione, salva il file immagine del logo (broccoli-logo.png) nelle seguenti posizioni:

1. **Backend**: `backend/src/BroccoliTours.Api/wwwroot/images/broccoli-logo.png`
   - Utilizzato per servire il logo via API (opzionale)

2. **Frontend**: `frontend/public/broccoli-logo.png`
   - Utilizzato nel frontend React (Home page e pagina Itinerario)

### Integrazione completata

✅ **Frontend**:
- Home page: Logo visibile nell'header con dimensione massima 300px
- Pagina Itinerario: Logo visibile nell'header con dimensione massima 200px

✅ **PDF**:
- Header decorativo "Broccoli Tours - Camper Adventures" aggiunto a tutti i PDF
- PDF dettagliato ora include:
  - Ore di guida stimate per giorno
  - Raccomandazioni per soste notturne
- PDF brochure mantiene il formato compatto

### Note tecniche

Il generatore PDF utilizza un header testuale decorato con box drawing characters per rappresentare il brand Broccoli Tours. Per includere immagini reali nei PDF sarebbe necessario:
- Utilizzare una libreria PDF più completa (es. QuestPDF, iTextSharp)
- Convertire l'immagine in formato compatibile (JPEG/PNG inline in base64)
- Gestire la compressione e il rendering dell'immagine nel PDF

L'attuale implementazione garantisce:
- PDF validi e leggibili
- Branding chiaro e professionale
- Compatibilità con tutti i lettori PDF
- Dimensione file ridotta
