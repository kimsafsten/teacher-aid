# TeacherAid – AI-stöd för lärare

Ett RAG-baserat verktyg som hjälper lärare på Yrkesakademin att ge feedback på studentinlämningar och svara på kursfrågor med hjälp av lokal AI via Ollama.

---

## Arkitektur

```
React (Vite)  →  ASP.NET Core API  →  PostgreSQL + pgvector
                       ↓
                      n8n  →  Ollama (llama3 + nomic-embed-text)
```

- **Backend**: .NET 10 Web API med JWT-autentisering
- **Databas**: PostgreSQL med pgvector för vektorsökning (RAG)
- **AI**: Ollama kör lokalt i Docker — ingen data skickas externt
- **Automation**: n8n triggas via webhook för att generera feedbackutkast

### Kodkonventioner

- **Källkod på engelska** — identifierare, kommentarer och API-rutter (t.ex. `SyncSubmissions()`, `POST /api/sync/course-material`)
- **Meddelanden på svenska** — UI-texter, felmeddelanden till användare och LLM-prompter
- **Filsystem** — fysiska mappar (`kursmaterial/`, `inlamningar/`) och obligatoriska filnamn (`uppgiftsbeskrivning`, `bedömningsmall`) behåller svenska namn

---

## Förutsättningar

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (endast för frontend)

---

## Installation

### 1. Klona och starta infrastrukturen

```bash
git clone <repo-url>
cd teacher-aid
docker-compose up -d
```

### 2. Ladda ned AI-modellerna

```bash
docker exec -it $(docker ps --filter "ancestor=ollama/ollama" --format "{{.Names}}") ollama pull llama3
docker exec -it $(docker ps --filter "ancestor=ollama/ollama" --format "{{.Names}}") ollama pull nomic-embed-text
```

### 3. Konfigurera miljövariabler

Skapa `TeacherAid.Api/appsettings.Development.json`:

```json
{
  "Jwt": {
    "Key": "din-hemliga-nyckel-minst-32-tecken"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=teacheraiddb;Username=postgres;Password=postgres"
  },
  "FolderPaths": {
    "CourseMaterial": "../kursmaterial",
    "Submissions":    "../inlamningar",
    "Generated":      "../genererat"
  }
}
```

### 4. Starta API:et

```bash
cd TeacherAid.Api
dotnet run
```

API:et är tillgängligt på `http://localhost:5010`.  
Swagger UI: `http://localhost:5010/swagger`

### 5. Starta frontend (valfritt)

```bash
cd teacher-aid-frontend
npm install
npm run dev
```

---

## Inloggning

Standardkonto för Anna Lindqvist:

- Användarnamn: `anna`
- Lösenord: `password123`

> ⚠️ Byt ut hårdkodade credentials innan produktionssättning.

---

## API – Endpoints

| Metod | Endpoint | Beskrivning |
|-------|----------|-------------|
| POST | `/api/auth/login` | Logga in, få JWT |
| POST | `/api/submissions` | Skicka in studentarbete |
| POST | `/api/submissions/{id}/process` | Trigga AI-feedback via n8n |
| GET | `/api/submissions/{id}/feedback` | Hämta feedbackutkast |
| PUT | `/api/submissions/{id}/feedback` | Godkänn feedback |
| GET | `/api/submissions/all` | Lista inlämningar med feedbackstatus |
| GET | `/api/submissions/{id}/file` | Ladda ner originalfil (JWT) |
| GET | `/api/submissions/logs` | Se automationslogg |
| POST | `/api/sync/course-material` | Synka `kursmaterial/` till RAG-index |
| POST | `/api/sync/submissions` | Synka `inlamningar/` och trigga feedback |
| POST | `/api/qa/documents` | Ladda upp kursdokument |
| POST | `/api/qa/ask` | Ställ kursfråga (RAG) |
| POST | `/api/qa/generate-material` | Generera kursmaterial (sparas i `genererat/`) |
| GET | `/api/qa/generated` | Lista genererade filer |
| GET | `/api/qa/generated/{fileName}` | Hämta innehåll i en genererad fil |
| PUT | `/api/qa/generated/{fileName}` | Spara redigerat innehåll till fil |

<<<<<<< Updated upstream
=======
### Generera kursmaterial (lärargränssnitt)

På fliken **Kursmaterial** kan läraren synka filer från `kursmaterial/`, ange kurs-ID och instruktion, och generera nytt material. Resultatet sparas automatiskt i `genererat/` och kan redigeras och sparas igen i webbläsaren. **Rensa och generera nytt** tömmer utkastet och instruktionen (kurs-ID behålls) så att en ny generering kan startas — tidigare filer finns kvar i historikpanelen.

### Synka inlämningar (lärargränssnitt)

På fliken **Inlämningar** synkas filer från `inlamningar/` med `POST /api/sync/submissions`. Förväntad struktur:

```
inlamningar/{kursId}/{uppgiftId}/
  uppgiftsbeskrivning.pdf   (eller .docx / .txt)
  bedömningsmall.pdf
  Förnamn_Efternamn_KursId.pdf
```

Uppgiftsbeskrivning och bedömningsmall indexeras som `AssignmentDescription` respektive `GradingRubric`. Övriga filer behandlas som elevinlämningar och triggar automatiskt n8n-webhooken med `assignmentDescription` och `gradingRubric` i payloaden.

AI-svar på svenska; etablerade facktermer (t.ex. *structure as code*, *prompt*) behålls på engelska när det är branschstandard.

>>>>>>> Stashed changes
---

## n8n-workflow

<<<<<<< Updated upstream
Importera `n8n-workflow.json` från repots rot i n8n-gränssnittet (`http://localhost:5678`).
=======
Importera `n8n-workflow.json` från repots rot i n8n-gränssnittet (`http://127.0.0.1:5678`). På Windows med Docker + WSL måste både webbläsaren **och** n8n-containern använda `127.0.0.1` — annars laddas UI:t men interna anrop (t.ex. telemetry) går fortfarande till `localhost` och ger `ERR_CONNECTION_RESET` via IPv6. `docker-compose.yml` sätter detta via `N8N_HOST` och `N8N_EDITOR_BASE_URL`. API:ets `OllamaLLMService` använder samma adress (`http://127.0.0.1:11434`).
>>>>>>> Stashed changes

Workflowen tar emot studentdata via webhook (`submissionId`, `courseId`, `assignmentId`, `content`, `assignmentDescription`, `gradingRubric`), anropar Ollama för feedbackgenerering och sparar utkastet i databasen.

---

## Docker-tjänster

| Tjänst | Port | Beskrivning |
|--------|------|-------------|
| PostgreSQL + pgvector | 5434 | Databas |
| n8n | 5678 | Automationsplattform |
| Ollama | 11434 | Lokal LLM-runtime |

---

## Kända begränsningar

- **Chunking delar inte på meningar** — `TextChunker.Chunk()` splittar enbart på radbrytningar. En lång mening som inte innehåller `\n` hamnar i ett enda chunk som kan överskrida maxstorleken på 500 tecken.
- **Endast tre chunks används per fråga** — RAG-sökningen hämtar alltid exakt tre närmaste chunks oavsett relevans. Vid glest indexerat material kan svaren bli missvisande.
- **Embeddings cachas inte** — varje fråga och varje chunk vid indexering genererar ett nytt embedding-anrop till Ollama. Vid stora dokumentvolymer blir detta en flaskhals.
- **Hårdkodad användare** — autentiseringen är avsedd för en enskild lärare (Anna Lindqvist). Stöd för flera användare eller roller saknas.
- **Ingen felhantering för Ollama-timeout** — om Ollama är under uppstart eller överbelastad kastar API:et ett ohanterat undantag utan meningsfull felrespons till klienten.
- **Feedback visas inte på inlämningen** — AI-genererat feedbackutkast dyker inte upp direkt under inlämningen i gränssnittet. Läraren måste manuellt kopiera över feedbacken.
- **Kursmaterial kan inte redigeras i gränssnittet** — det finns inget sätt att uppdatera eller ta bort uppladdade kursdokument via frontend.
- **Feedback kan inte redigeras direkt på sidan** — läraren kan inte justera AI-feedbacken inline utan måste hantera det utanför systemet.
- **Frontend stöder i dagsläget endast en kurs** — backend hanterar flera kurser via courseId, men frontend har kurs-ID:t förifyllt med `SYS25D`. Elever kan inte välja kurs i gränssnittet.
- **Elevfrågor är begränsade till 400 tecken** — för att motverka att elever klistrar in hela uppgifter och för att minska risken för prompt injection. Validering sker i både frontend och backend.
- **Elever loggar inte in (by design)** — elever ställer anonyma frågor utan autentisering, eftersom funktionen är begränsad till generella kursfrågor. Allt som kräver inloggning är förbehållet läraren.
