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
  }
}
```

### 4. Starta API:et

```bash
cd TeacherAid.Api
dotnet run
```

API:et är tillgängligt på `http://localhost:5000`.  
Swagger UI: `http://localhost:5000/swagger`

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
| GET | `/api/submissions/logs` | Se automationslogg |
| POST | `/api/qa/documents` | Ladda upp kursdokument |
| POST | `/api/qa/ask` | Ställ kursfråga (RAG) |
| POST | `/api/qa/generate-material` | Generera kursmaterial |

---

## n8n-workflow

Importera `n8n-workflow.json` från repots rot i n8n-gränssnittet (`http://localhost:5678`).

Workflowen tar emot studentdata via webhook, anropar Ollama för feedbackgenerering och sparar utkastet i databasen.

---

## Docker-tjänster

| Tjänst | Port | Beskrivning |
|--------|------|-------------|
| PostgreSQL + pgvector | 5434 | Databas |
| n8n | 5678 | Automationsplattform |
| Ollama | 11434 | Lokal LLM-runtime |

---

## Kända begränsningar

- **Chunking delar inte på meningar** — `ChunkText` splittar enbart på radbrytningar. En lång mening som inte innehåller `\n` hamnar i ett enda chunk som kan överskrida maxstorleken på 500 tecken.
- **Endast tre chunks används per fråga** — RAG-sökningen hämtar alltid exakt tre närmaste chunks oavsett relevans. Vid glest indexerat material kan svaren bli missvisande.
- **Embeddings cachas inte** — varje fråga och varje chunk vid indexering genererar ett nytt embedding-anrop till Ollama. Vid stora dokumentvolymer blir detta en flaskhals.
- **Hårdkodad användare** — autentiseringen är avsedd för en enskild lärare (Anna Lindqvist). Stöd för flera användare eller roller saknas.
- **Ingen felhantering för Ollama-timeout** — om Ollama är under uppstart eller överbelastad kastar API:et ett ohanterat undantag utan meningsfull felrespons till klienten.
- **Feedback visas inte på inlämningen** — AI-genererat feedbackutkast dyker inte upp direkt under inlämningen i gränssnittet. Läraren måste manuellt kopiera över feedbacken.
- **Kursmaterial kan inte redigeras i gränssnittet** — det finns inget sätt att uppdatera eller ta bort uppladdade kursdokument via frontend.
- **Feedback kan inte redigeras direkt på sidan** — läraren kan inte justera AI-feedbacken inline utan måste hantera det utanför systemet.
- **Genererat kursmaterial kan inte exporteras** — när AI genererat kursmaterial finns inget sätt att spara det direkt som ett dokument. Läraren måste manuellt kopiera innehållet till en extern dokumenthanterare.
- **En kurs stöds (by design)** — systemet är i nuvarande version avsiktligt begränsat till en enda kurs. Elever kan inte välja kurs eller ställa frågor om annat kursmaterial.
- **Elever loggar inte in (by design)** — elever ställer anonyma frågor utan autentisering, eftersom funktionen är begränsad till generella kursfrågor. Allt som kräver inloggning är förbehållet läraren.
