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
