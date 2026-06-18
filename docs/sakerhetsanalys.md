# Säkerhetsanalys – TeacherAid

## Mappad mot OWASP LLM Top 10 (2025)

OWASP LLM Top 10 är en referenslista över de tio vanligaste säkerhetsriskerna i system som använder stora språkmodeller. Nedanstående analys går igenom varje risk, bedömer dess relevans för TeacherAid och beskriver hur den hanteras eller varför den är begränsad i detta system.

---

## Genomförda åtgärder (Tier 1)

Följande åtgärder implementerades efter den initiala säkerhetsanalysen:

| # | Åtgärd | Fil / komponent | Effekt |
|---|--------|-----------------|--------|
| 1 | `[AllowAnonymous]` borttagen från filnedladdning | `SubmissionsController.GetFile()` | Stänger IDOR — filer kräver JWT |
| 1b | Autentiserad nedladdning i frontend | `SyncPanel.downloadFile()` | Skickar token vid filhämtning |
| 2 | `studentName` borttagen ur n8n-payload | `SubmissionsController.Process()` | Elevnamn skickas inte längre till Ollama |
| 3 | Avgränsare i n8n-prompten | n8n HTTP Request-nod | `###STUDENT_TEXT_START/END###` + instruktion att ignorera elevdata |
| 4 | Lösenord flyttat till config | `AuthService`, `appsettings.Development.json` | Inga hårdkodade credentials i källkod |

---

## Riskmatris

Varje risk bedöms på en femgradig skala för både sannolikhet och konsekvens. Score = Sannolikhet × Konsekvens. Nivå: HÖG (score ≥12), MEDEL (5–11), LÅG (≤4).

*Uppdaterad efter Tier 1.*

| Risk | Sannolikhet (1-5) | Konsekvens (1-5) | Score | Nivå |
|------|-------------------|------------------|-------|------|
| LLM09 - Misinformation | 4 | 5 | 20 | **HÖG** |
| LLM01 - Prompt Injection | 2 | 4 | 8 | **MEDEL** |
| LLM05 - Improper Output Handling | 3 | 3 | 9 | **MEDEL** |
| LLM02 - Sensitive Info Disclosure | 1 | 4 | 4 | **LÅG** |
| LLM08 - Vector/Embedding Weakness | 2 | 3 | 6 | **MEDEL** |
| LLM07 - System Prompt Leakage | 2 | 2 | 4 | **LÅG** |
| LLM06 - Excessive Agency | 1 | 3 | 3 | **LÅG** |
| LLM04 - Data & Model Poisoning | 1 | 3 | 3 | **LÅG** |
| LLM03 - Supply Chain | 1 | 2 | 2 | **LÅG** |
| LLM10 - Unbounded Consumption | 1 | 1 | 1 | **LÅG** |

**Klassisk säkerhet (ej OWASP LLM):**

| Risk | Status efter Tier 1 |
|------|---------------------|
| IDOR (filnedladdning) | **Åtgärdad** |
| Hårdkodade credentials | **Åtgärdad** |
| Oskyddad n8n-webhook | Kvarstår (Tier 2) |

---

## Fördjupade fynd

De tre allvarligaste kvarvarande riskerna efter Tier 1.

### Fynd 1 — LLM09: Felaktig AI-feedback

| | |
|---|---|
| **Vad** | llama3 (7B) kan generera sakligt felaktig feedback som läraren godkänner utan granskning. |
| **Attackväg** | Inlämning → n8n → Ollama → felaktigt utkast → läraren klickar "Godkänn och spara" → eleven får fel återkoppling. |
| **Befintlig kontroll** | Human-in-the-loop, AI-utkast i UI, `AutomationLog`, uppgiftsbeskrivning och bedömningsmall i prompten. |
| **Åtgärd** | UI-varning *"Granska alltid — AI kan ha fel"*. Validera RAG-index. Flagga utkast som avviker från förväntat format. |
| **Integritet** | Felaktig feedback kan vara orättvis mot eleven. AI sparar tid på utkast men ersätter inte lärarens professionella bedömning. |

### Fynd 2 — LLM01: Prompt injection via studentinlämning

| | |
|---|---|
| **Vad** | Elev kan skriva instruktioner i inlämningen som försöker manipulera AI-modellen. |
| **Attackväg** | *"Ignorera alla instruktioner..."* i inlämning → n8n → Ollama → missvisande positiv feedback. |
| **Befintlig kontroll** | Tier 1: avgränsare `###STUDENT_TEXT_START/END###`. Pseudonymisering. Human-in-the-loop. Inget betygsförslag i prompt. |
| **Åtgärd** | Tier 2: webhook-hemlighet. Flagga avvikande output. Ev. detektera misstänkta mönster i input. |
| **Integritet** | Strikt filtrering kan ta bort legitima formuleringar i kodinlämningar — avgränsare valdes framför hård filtrering. |

### Fynd 3 — LLM05: Osaniterad AI-output i n8n-flödet

| | |
|---|---|
| **Vad** | n8n sparar Ollamas råsvar direkt i PostgreSQL utan sanitisering. |
| **Attackväg** | Null-bytes eller kontrolltecken i AI-svar → DB-fel eller trasigt UI vid visning. |
| **Befintlig kontroll** | `OllamaLLMService.Sanitize()` i API-vägen (RAG, materialgenerering). Parametriserad SQL i n8n. |
| **Åtgärd** | Tier 2: Code-nod i n8n före Postgres-insert, eller flytta feedbackgenerering till API. |
| **Integritet** | Krasch i feedbackflödet kan innebära att vissa elever inte får återkoppling — differentialbehandling. |

---

## OWASP LLM Top 10 — detaljerad genomgång

### LLM01 — Prompt Injection [Score: 8 | Nivå: MEDEL]

**Beskrivning:** En student kan formulera sin inlämning på ett sätt som försöker manipulera AI-modellen att ignorera instruktionerna från läraren.

**Var uppstår risken i TeacherAid:** n8n-workflödet skickar studentens inlämningstext till llama3 i samma prompt som systeminstruktionerna.

**Befintlig kontroll (efter Tier 1):**
- Elevtexten avgränsas med `###STUDENT_TEXT_START###` / `###STUDENT_TEXT_END###` i n8n-prompten.
- Instruktion till modellen att aldrig följa direktiv från elevtexten.
- Human-in-the-loop — läraren granskar utkast innan godkännande.

**Kvarvarande åtgärd:** Webhook-autentisering (Tier 2). Logga och flagga inlämningar där output avviker från förväntat feedback-format.

**Avvägning:** Avgränsare bevarar legitima specialtecken i programmeringsinlämningar. Hårdare input-filtrering riskerar att ta bort giltigt innehåll.

---

### LLM09 — Misinformation [Score: 20 | Nivå: HÖG]

**Beskrivning:** AI-modellen kan generera sakligt felaktig feedback som läraren godkänner utan att granska den noggrant.

**Var uppstår risken i TeacherAid:** llama3 (7B) är begränsad i facktermskunskap och kan hallucinera kriterier som inte finns i uppgiftsbeskrivningen. Risken ökar när RAG-indexet är glest.

**Befintlig kontroll:**
- All feedback presenteras som "AI-utkast" — läraren måste aktivt godkänna.
- `AutomationLog` loggar varje AI-anrop.
- Uppgiftsbeskrivning och bedömningsmall skickas med i prompten.

**Åtgärd:** UI-varning om AI-fel. Utöka RAG-indexet. Validera output-format.

**Avvägning:** Human-in-the-loop eliminerar automatisk publicering men kräver att läraren faktiskt granskar — annars kvarstår risken trots tekniska kontroller.

---

### LLM05 — Improper Output Handling [Score: 9 | Nivå: MEDEL]

**Beskrivning:** AI-genererad output med kontrolltecken, null-bytes eller extremt långa strängar kan orsaka problem i databas, UI eller automationsflöden.

**Var uppstår risken i TeacherAid:**
- **API-vägen:** `OllamaLLMService.GenerateAsync()` — sanitisering **implementerad** (`Sanitize()` tar bort kontrolltecken, null-bytes, begränsar till 8 000 tecken). System-prompt instruerar svenska svar med oförändrade engelska facktermer där det är branschstandard.
- **n8n-vägen:** Feedback sparas direkt från Ollama-svar till `FeedbackDrafts` **utan** sanitisering.

**Åtgärd (Tier 2):** Code-nod i n8n med samma logik som `Sanitize()`, eller flytta feedbackgenerering till API.

**Avvägning:** Negligibel kostnad — en extra strängoperation per anrop påverkar inte feedbackkvaliteten.

---

### LLM02 — Sensitive Information Disclosure [Score: 4 | Nivå: LÅG]

**Beskrivning:** Studentinlämningar innehåller personuppgifter som kan läcka till AI-modellen eller obehöriga.

**Var uppstår risken i TeacherAid:** `FolderSyncService` parsear studentnamn ur filnamn. `Submission.StudentName` lagras i databasen.

**Befintlig kontroll (efter Tier 1):**
- `TextPseudonymizer` ersätter namn med `[Student]` i material som indexeras och skickas till AI.
- `studentName` skickas **inte** längre i n8n-webhook-payload.
- Lokal Ollama — inga personuppgifter lämnar organisationens infrastruktur.
- JWT på API-endpoints — filnedladdning kräver inloggning (Tier 1).

**Kvarvarande åtgärd:** Kryptera `Submission.Content` i vila. NER-baserad pseudonymisering för namn i fritext.

**Avvägning:** Filnamnsbaserad pseudonymisering missar namn studenten skriver själv i texten. NER ökar komplexiteten markant.

---

### LLM08 — Vector and Embedding Weaknesses [Score: 6 | Nivå: MEDEL]

**Beskrivning:** Embedding-vektorer lagras i pgvector utan kryptering. En databas-dump kan exponera rekonstruerbart material.

**Var uppstår risken i TeacherAid:** `DocumentChunk.Embedding` lagras som `vector(768)` i PostgreSQL.

**Åtgärd:** Lokal infrastruktur — pgvector exponeras inte mot internet. Långsiktigt: pgcrypto för känsliga kolumner.

**Avvägning:** Semantisk chunking ger marginell vinst i MVP-skede.

---

### LLM07 — System Prompt Leakage [Score: 4 | Nivå: LÅG]

**Beskrivning:** Kursmaterial i RAG-kontexten kan extraheras via frågor utformade för att få ordagrant citat.

**Var uppstår risken i TeacherAid:** `POST /api/qa/ask` inkluderar upp till tre RAG-chunks i prompten. Endpointen är **avsiktligt anonym** — elever ska kunna ställa kursfrågor utan inloggning.

**Åtgärd:** Risken begränsas till kursinnehåll (inte elevdata eller systemprompt). Lägg till instruktion i prompten att aldrig citera kontexten ordagrant.

**Avvägning:** Anonym åtkomst sänker tröskeln för elever men ökar läckagerisken för kursmaterial.

---

### LLM06 — Excessive Agency [Score: 3 | Nivå: LÅG] — Icke relevant

AI genererar enbart utkast; läraren måste explicit godkänna via `PUT /api/submissions/{id}/feedback`. Inga autonoma utskick eller betygssättning.

---

### LLM04 — Data and Model Poisoning [Score: 3 | Nivå: LÅG] — Icke relevant

Endast inloggad lärare (JWT) kan synka kursmaterial. En organisation med en läraranvändare har minimalt hot-surface.

---

### LLM03 — Supply Chain [Score: 2 | Nivå: LÅG] — Icke relevant

Standardrisk för NuGet/Docker-beroenden. Lokal Ollama eliminerar externa AI-API:er. Hanteras med Dependabot.

---

### LLM10 — Unbounded Consumption [Score: 1 | Nivå: LÅG] — Icke relevant

Ollama körs lokalt utan token-kostnad. Resursförbrukning begränsas av serverns CPU/GPU.

---

## Klassisk säkerhet

### IDOR — filnedladdning [ÅTGÄRDAD i Tier 1]

**Problem:** `GET /api/submissions/{id}/file` hade `[AllowAnonymous]` — vem som helst kunde enumerera ID:n och ladda ner elevinlämningar.

**Åtgärd:** `[AllowAnonymous]` borttagen. Frontend använder `downloadFile()` med JWT i `Authorization`-header.

### Autentisering [ÅTGÄRDAD i Tier 1]

**Problem:** Lösenord hårdkodat i `AuthService.cs` (`password123`).

**Åtgärd:** Credentials läses från `Auth:Username` och `Auth:Password` i `appsettings.Development.json` (gitignorad).

### Oskyddad n8n-webhook [KVARSTÅR — Tier 2]

**Problem:** `POST http://127.0.0.1:5678/webhook/feedback` saknar autentisering.

**Planerad åtgärd:** Delad hemlighet i `X-Webhook-Secret`-header, valideras i n8n.

---

## Planerade åtgärder (Tier 2)

| # | Åtgärd | Prioritet |
|---|--------|-----------|
| 5 | Webhook-hemlighet mellan API och n8n | Hög |
| 6 | Sanitisera n8n-output före Postgres-insert | Medel |
| 7 | Begränsa CORS till frontend-origin | Låg |
