# Lösningsbeskrivning – TeacherAid

## 01. Problemet kunden hade

Anna Lindqvist är lärare på Yrkesakademin med tre kurser per termin och cirka 30 studenter per klass. Hon hade tre sammanlänkade problem:

**Feedback på inlämningar** var det mest akuta. Med 30 studenter per kurs och begränsad tid fick de flesta studenter tre rader och en bokstav — inte den substantiella feedback de förtjänade. Anna upplevde själv att det var orättvist men såg ingen annan utväg utan att jobba helger.

**Repetitiva studentfrågor** tog oproportionerligt mycket tid. Samma frågor dök upp om och om igen i Slack och mejl, och varje svar krävde hennes uppmärksamhet trots att svaret var identiskt.

**Kursmaterialproduktion** skalade inte. Skolan skickade in nya YH-ansökningar kontinuerligt, och när en ansökan gick igenom skulle kursen byggas från grunden. Befintligt material kändes föråldrat innan Anna hunnit uppdatera det, och övningarna var i princip identiska år från år — bara siffrorna byttes ut.

Det underliggande problemet var att Anna inte kunde skala sin pedagogik utan att tappa kvalitet.

---

## 02. Vad lösningen gör

TeacherAid är ett AI-stött webbaserat verktyg för lärare på Yrkesakademin. Det låter läraren synka studentinlämningar från filsystemet, automatiskt generera feedbackutkast med hjälp av lokal AI, samt granska och godkänna utkast i lärargränssnittet innan det används.

Utöver feedbackhantering kan läraren synka kursdokument som indexeras för RAG-baserad frågehantering — elever kan ställa anonyma frågor om kursmaterialet och få svar genererade av AI, utan att behöva logga in. Läraren kan även generera nytt kursmaterial utifrån indexerat underlag.

---

## 03. Backend i korthet

Systemet består av fyra lager som samverkar:

```
React (Vite)  →  ASP.NET Core API  →  PostgreSQL + pgvector
                       ↓
                      n8n  →  Ollama (llama3 + nomic-embed-text)
```

**ASP.NET Core API (.NET 10)** hanterar all affärslogik och exponerar ett REST-API med JWT-autentisering för lärarens gränssnitt. Centrala tjänster:

- `RagService` — indexerar kursdokument i vektorformat och besvarar elevfrågor med RAG-mönstret (Retrieval-Augmented Generation)
- `OllamaLLMService` — kommunicerar med den lokala Ollama-instansen för både embedding och textgenerering (via `http://127.0.0.1:11434` på Windows för att undvika IPv6-problem med Docker). Saniterar AI-output i API-vägen (`Sanitize()`).
- `FolderSyncService` / `DocumentExtractorService` — synkar filer från `kursmaterial/` och `inlamningar/` (`SyncCourseMaterial()`, `SyncSubmissions()`)
- `TextPseudonymizer` — ersätter elevnamn med `[Student]` innan text skickas till AI
- `FeedbackWebhookTrigger` — skickar webhook-payload till n8n i bakgrunden (fire-and-forget)

**PostgreSQL med pgvector** används som databas. pgvector-tillägget möjliggör vektorsökning (L2-distans) direkt i databasen, vilket är kärnan i RAG-flödet. Kursdokument klassificeras med `DocumentType` (`CourseMaterial`, `AssignmentDescription`, `GradingRubric`).

**Ollama (Docker)** kör AI-modellerna lokalt — `llama3` för textgenerering och `nomic-embed-text` för embeddings. Ingen data skickas till externa tjänster. API-vägen instruerar modellen att svara på svenska men behålla etablerade facktermer på engelska (t.ex. *prompt*, *structure as code*).

**Materialgenerering i gränssnittet** — läraren anger kurs-ID och instruktion; AI:n använder indexerat kursmaterial som kontext. Resultatet sparas i `genererat/`, kan redigeras och sparas igen, och tidigare genereringar kan laddas från historik. Knappen *Rensa och generera nytt* nollställer utkast och instruktion för nästa generering utan att radera sparade filer.

**Mappstruktur för inlämningar** — `inlamningar/{kursId}/{uppgiftId}/` med filerna `uppgiftsbeskrivning` och `bedömningsmall` (valfritt filformat) samt elevfiler enligt `Förnamn_Efternamn_KursId.pdf`. Synkas via `POST /api/sync/submissions`. Uppgiftsbeskrivning och bedömningsmall indexeras som RAG-dokument och skickas med i n8n-prompten vid feedbackgenerering.

---

## 04. n8n-workflow — vad det automatiserar

n8n-workflowet (`n8n-workflow.json`) triggas automatiskt när läraren synkar inlämningar från filsystemet.

1. **Vid synk (huvudflöde i frontend)** — läraren klickar *Synka inlämningar* (`POST /api/sync/submissions`). `FolderSyncService` skapar nya `Submission`-poster och triggar webhook för varje ny inlämning via `FeedbackWebhookTrigger`.
2. **Regenerering (reserverad)** — `POST /api/submissions/{id}/process` finns i API:et för att kunna trigga om feedback, t.ex. om n8n-genereringen misslyckades. Endpointen är **inte** kopplad till nuvarande lärargränssnitt ännu — planerad framtida funktion.

API:et skickar payload till n8n i bakgrunden utan att vänta på svar (Ollama kan ta flera minuter). `SyncPanel` pollar `GET /api/submissions/{id}/feedback` tills utkastet finns. Varje anrop loggas i `AutomationLog`.

Workflowen:

1. Tar emot data via POST till `/webhook/feedback` (`submissionId`, `courseId`, `assignmentId`, `content`, `assignmentDescription`, `gradingRubric`) — **utan** `studentName`
2. Skickar innehållet till Ollama (`llama3`) med prompt som inkluderar uppgiftsbeskrivning, bedömningsmall och elevtext avgränsad med `###STUDENT_TEXT_START/END###`
3. Sparar AI-svaret i `FeedbackDrafts` med parameteriserad SQL (`Approved = false`)

Läraren granskar utkastet i gränssnittet och godkänner via `PUT /api/submissions/{id}/feedback`.

---

## 05. Diagram

### Systemflöde (n8n-workflow)
![n8n Feedback Workflow](flowchart.svg)

### RAG-frågeflöde (sekvensdiagram)
![Sequence Diagram – RAG Query Flow](sequence-diagram.svg)

### Datamodell (ER-diagram)
![ER Diagram – TeacherAid Data Model](er-diagram.svg)

---

## Val och avgränsningar

**Lokal AI med Ollama istället för molntjänst** — Ollama valdes framför t.ex. OpenAI av kostnadsskäl. Eftersom all AI-inferens körs lokalt i Docker tillkommer inga löpande API-kostnader, vilket är avgörande för en lärare med begränsad budget.

**En kurs (by design)** — Lösningen är medvetet begränsad till en enda kurs för att testa konceptet på en kontrollerad mängd data innan en fullskalig lösning med flera kurser och lärare övervägs.

**Anonyma elevfrågor utan inloggning** — Elevernas frågor är generella kursfrågor och ingen personlig data kopplas till dem. Det finns därmed inget behov av autentisering, och att slippa inloggning sänker tröskel för eleverna att använda tjänsten.

**Vad som valdes bort** — En funktion identifierades som viktig men lämnades utanför scope i denna version (MVP): ordentlig bedömningsfeedback kopplad till kurskriterier (där AI specificerar hur varje elev uppnått respektive bedömningskriterium). Planerat som nästa steg efter MVP — se `docs/Inlamning2_TeacherAid_KimSafsten.md`, avsnitt *Nästa steg*.

---

## Kända begränsningar

- **Feedback visas inte på inlämningen** — AI-genererat feedbackutkast dyker inte upp direkt under inlämningen. Läraren måste manuellt kopiera över feedbacken.
- **Kursmaterial kan inte redigeras i gränssnittet** — det finns inget sätt att uppdatera eller ta bort uppladdade kursdokument via frontend.
- **En kurs stöds (by design)** — systemet är avsiktligt begränsat till en enda kurs i nuvarande version.
- **Elever loggar inte in (by design)** — elever ställer anonyma frågor utan autentisering.
- **Chunking delar inte på meningar** — `TextChunker.Chunk()` splittar enbart på radbrytningar och kan producera chunks som överskrider maxstorleken.
- **Endast tre chunks används per fråga** — RAG-sökningen hämtar alltid exakt tre närmaste chunks oavsett relevans.
- **Embeddings cachas inte** — varje anrop genererar ett nytt embedding-anrop till Ollama.
- **Oskyddad n8n-webhook** — `POST /webhook/feedback` saknar autentisering.
- **Osaniterad AI-output i n8n-vägen** — feedback sparas direkt från Ollama utan `Sanitize()` (API-vägen saniterar).
- **Ingen felhantering för Ollama-timeout** — om Ollama är under uppstart kastar API:et ett ohanterat undantag.
