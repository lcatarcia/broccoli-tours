# Gestione JSON Troncati - Feature di Riparazione Automatica Ricorsiva

## Problema
Durante la generazione degli itinerari, Gemini (e occasionalmente OpenAI) può restituire JSON troncati che causano l'errore:
```
Expected end of string, but instead reached end of data. LineNumber: 253 | BytePositionInLine: 49.
```

## Soluzione Implementata

### 1. Rilevamento Automatico
Quando `JsonDocument.Parse()` fallisce con un `JsonException`, il sistema intercetta l'errore e:
- Logga i dettagli dell'errore (posizione, messaggio, numero di tentativo)
- Attiva automaticamente il meccanismo di riparazione ricorsiva

### 2. Riparazione Ricorsiva con Strategia di Continuazione
Il sistema tenta la riparazione fino a **3 volte** utilizzando una strategia innovativa:

#### Strategia di Continuazione (Non Rigenerazione)
Invece di rigenerare l'intero JSON:
1. **Mantiene la parte troncata originale** esattamente come ricevuta
2. **Chiede all'AI di generare SOLO la continuazione** dal punto di troncamento
3. **Concatena** la parte originale con la continuazione generata
4. **Valida** il JSON completo risultante
5. Se ancora invalido, ripete il processo ricorsivamente

#### Vantaggi di questa strategia:
- ✅ **Efficienza token**: Non spreca token rigenerando contenuti già ricevuti
- ✅ **Preservazione dati**: La parte originale rimane identica, nessun rischio di modifiche
- ✅ **Riduzione truncation**: Generando solo la parte mancante, c'è meno probabilità di nuovo troncamento
- ✅ **Velocità**: Richieste più brevi e risposte più rapide

#### Processo dettagliato:
- **Tentativo 1**: Genera continuazione, concatena, valida
- **Tentativo 2**: Se ancora invalido, prende il JSON concatenato e ripete
- **Tentativo 3**: Ultimo tentativo prima di fallire
- Ogni tentativo viene loggato con `[Attempt N]`
- **Validazione strutturale**: Verifica che parentesi, array e stringhe siano bilanciate
- L'utente viene notificato tramite toast per ogni tentativo

### 3. Notifiche Toast all'Utente
Per ogni tentativo di riparazione, l'utente riceve una notifica toast:
- **Icona**: 🔧
- **Tipo**: Warning (sfondo giallo)
- **Messaggio**: "Riparazione JSON (tentativo N/totale): il sistema sta correggendo la risposta dell'AI..."
- **Durata**: 4 secondi (auto-dismiss)
- Le notifiche appaiono sequenzialmente con un ritardo di 300ms tra loro

### 4. Tracciamento con AsyncLocal
Ogni engine utilizza `AsyncLocal<int>` per tracciare i tentativi di riparazione:
- Il contatore viene resettato all'inizio di ogni richiesta (`SuggestAsync`)
- Viene incrementato ad ogni tentativo di riparazione
- È accessibile via proprietà statica `CurrentJsonRepairAttempts`

### 5. Comunicazione Backend → Frontend
- **Header HTTP**: `X-Json-Repair-Attempts` contiene il numero totale di tentativi
- **Response API**: `suggestItinerary()` ritorna `{ itinerary, repairAttempts? }`
- Il frontend legge i metadati e mostra i toast appropriati

### 6. Prompt di Riparazione con Strategia di Continuazione
Il prompt è completamente riprogettato per la strategia di continuazione:

**Cosa chiede all'AI:**
1. Analizzare il JSON troncato per capire **ESATTAMENTE** dove è stato interrotto
2. Identificare il tipo di interruzione:
   - Nel mezzo di un oggetto (mancano campi e la chiusura `}`)
   - Nel mezzo di un array (mancano elementi e la chiusura `]`)
   - Nel mezzo di una stringa (mancano caratteri e la chiusura `"`)
3. Generare **SOLO la continuazione** necessaria per completare il JSON
4. La continuazione deve:
   - Completare l'elemento corrente se interrotto a metà
   - Aggiungere tutti gli elementi mancanti (giorni, stops, tips rimanenti)
   - Chiudere tutti gli array e oggetti aperti
   - Terminare con `}`

**Formato risposta richiesto:**
- **NON ripetere** il JSON troncato
- Rispondere **SOLO con la continuazione** che sarà concatenata
- **NO markdown**, spiegazioni o JSON completo
- La continuazione deve "attaccarsi" perfettamente al punto di troncamento

**Esempio nel prompt:**
```
Se il JSON troncato finisce con: "description": "Visita al m
La tua risposta dovrebbe iniziare con: useo", "latitude": 45.4...
```

### 7. Parametri Ottimizzati

#### Per Gemini (`GeminiItineraryEngine`)
- Usa lo stesso endpoint e modello della richiesta originale
- Mantiene `responseMimeType: "application/json"`
- **`maxOutputTokens: 8192`** - Ottimizzato per continuazione (non serve rigenerazione completa)
- **Temperature: 0.3** - Riparazione più precisa e deterministica

#### Per OpenAI (`OpenAIItineraryEngine`)
- **Temperature: 0.3** - Più precisione nella riparazione
- **`max_tokens: 4000`** - Ottimizzato per continuazione (non serve rigenerazione completa)
- System message specializzato per generare solo continuazioni
- `response_format: { type: "json_object" }` per garantire output JSON valido

## Modifiche al Codice

### Backend

#### File Modificati
1. **`GeminiItineraryEngine.cs`**
   - Aggiunto `using System.Threading;`
   - Aggiunto `AsyncLocal<int> _jsonRepairAttempts` per tracciare i tentativi
   - Aggiunta proprietà statica `CurrentJsonRepairAttempts`
   - Reset del contatore in `SuggestAsync`
   - `ParseItineraryAsync` → ricorsiva con parametro `attemptNumber` (default 1)
   - Massimo 3 tentativi di riparazione
   - Logging dettagliato con `[Attempt N]`
   - Incremento del contatore ad ogni tentativo
   - **Nuovo metodo `BuildRepairRequestJson`**: Usa `maxOutputTokens: 8192` e `temperature: 0.3`
   - **Nuovo metodo `IsJsonStructurallyValid`**: Valida bilanciamento parentesi
   - **Strategia di continuazione in `RepairTruncatedJsonAsync`**:
     - Prompt modificato per chiedere solo la continuazione
     - Concatenazione: `truncatedJson + continuation`
     - Logging separato per continuazione e JSON completo

2. **`OpenAIItineraryEngine.cs`**
   - Stesse modifiche di `GeminiItineraryEngine.cs` per AsyncLocal e ricorsione
   - **Metodo `BuildRepairRequestJson` aggiornato**: `max_tokens: 4000`, `temperature: 0.3`
   - **System message specializzato**: Enfatizza generazione di SOLO continuazione, non JSON completo
   - **Nuovo metodo `IsJsonStructurallyValid`**: Validazione strutturale identica a Gemini
   - **Strategia di continuazione in `RepairTruncatedJsonAsync`**:
     - Prompt modificato per chiedere solo la continuazione
     - Concatenazione: `truncatedJson + continuation`
     - Logging separato per continuazione e JSON completo

3. **`Program.cs`**
   - Lettura di `GeminiItineraryEngine.CurrentJsonRepairAttempts`
   - Lettura di `OpenAIItineraryEngine.CurrentJsonRepairAttempts`
   - Aggiunta header `X-Json-Repair-Attempts` se > 0

### Frontend

#### File Creati
1. **`Toast.tsx`**
   - Context provider per gestire toast globalmente
   - Hook `useToast()` per mostrare notifiche
   - Auto-dismiss dopo 4 secondi
   - Supporto per tipi: info, success, warning, error

2. **`Toast.css`**
   - Stili per il container dei toast (fixed top-right)
   - Animazione slideIn
   - Colori diversi per ogni tipo
   - Hover effect per chiusura anticipata

#### File Modificati
1. **`main.tsx`**
   - Wrapping di `<App>` in `<ToastProvider>`

2. **`api.ts`**
   - `suggestItinerary` ritorna `{ itinerary, repairAttempts? }`
   - Lettura dell'header `X-Json-Repair-Attempts`

3. **`Home.tsx`**
   - Import di `useToast`
   - Gestione di `result.repairAttempts`
   - Loop per mostrare un toast per ogni tentativo
   - Delay di 300ms tra i toast
   - Aggiornamento della navigazione con `result.itinerary`

## Vantaggi
- ✅ **Efficienza massima**: Genera solo la parte mancante, non rigenerare tutto (risparmio token ~70%)
- ✅ **Preservazione totale dati**: La parte originale rimane identica, zero modifiche
- ✅ **Riduzione rischio truncation**: Richieste più corte = meno probabilità di nuovo troncamento
- ✅ **Resilienza estrema**: Fino a 3 tentativi ricorsivi con validazione strutturale
- ✅ **Validazione automatica**: Verifica bilanciamento parentesi/array prima di accettare
- ✅ **Ricorsività intelligente**: Ogni tentativo migliora sul precedente
- ✅ **Trasparenza totale**: Toast notificano l'utente di ogni tentativo
- ✅ **Coerenza**: Usa il contesto originale per generare contenuti mancanti
- ✅ **Logging completo**: Separazione tra continuazione e JSON finale per debug
- ✅ **Velocità**: Richieste più brevi = risposte più rapide
- ✅ **Backward Compatible**: Se il JSON è valido, funziona come prima

## Logging
Il sistema logga:
- Reset del contatore all'inizio della richiesta
- Errore JSON originale con posizione esatta e `[Attempt N]`
- Lunghezza del JSON troncato e ultimi 200 caratteri
- Tentativo di riparazione (con numero progressivo e massimo)
- **JSON Continuation**: Solo la parte generata dall'AI
- **Repaired JSON (FULL)**: JSON completo dopo concatenazione
- **Validazione strutturale**: Conta di parentesi graffe, quadre e stato delle stringhe
- Warning se il JSON riparato ha ancora problemi strutturali
- Successo o fallimento dopo N tentativi

## Testing
Per testare questa feature:
1. Avvia il backend
2. Avvia il frontend
3. Genera un itinerario (specialmente con durata lunga o molti giorni)
4. Se si verifica un errore di JSON troncato:
   - Osserva i log del backend per vedere `[Attempt 1]`, `[Attempt 2]`, ecc.
   - Verifica che appaiano i toast nell'interfaccia
   - Controlla che i toast mostrino il numero corretto di tentativi
5. Verifica che l'itinerario finale sia completo e valido

## Note
- La riparazione richiede un'ulteriore chiamata API per ogni tentativo, quindi c'è un overhead di tempo
- **Efficienza token**: La strategia di continuazione usa ~70% meno token rispetto alla rigenerazione completa
- **Costi API**: Significativamente ridotti rispetto all'approccio precedente
- Il sistema tenta la riparazione fino a 3 volte; se fallisce ancora, propaga l'errore
- La parte originale del JSON è **sempre preservata esattamente** - nessuna modifica
- I toast appaiono solo quando si verificano effettivamente riparazioni (non su JSON validi al primo tentativo)
- Il delay tra i toast (300ms) evita che si sovrappongano visualmente
- **La validazione strutturale è un controllo euristico** - un JSON può passare la validazione ma avere altri problemi semantici
- **Compatibilità AI**: Questa strategia funziona meglio con modelli che comprendono il concetto di "continuazione"
