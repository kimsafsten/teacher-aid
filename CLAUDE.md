# TeacherAid – CLAUDE.md

## Projektöversikt

RAG-baserat verktyg för lärare på Yrkesakademin. Lärare kan ge feedback på studentinlämningar och svara på kursfrågor via lokal AI (Ollama).

**Stack:** React (Vite) → ASP.NET Core (.NET 10) → PostgreSQL + pgvector → n8n → Ollama

## Mappstruktur

```
TeacherAid.Api/        # Backend – ASP.NET Core Web API
TeacherAid.Tests/      # Enhetstester
teacher-aid-frontend/  # Frontend – React/Vite
docs/                  # Dokumentation
```

### Testdata (ej källkod)

Följande mappar innehåller **ingen källkod** – de är testdata som appen läser från filsystemet under körning:

- `kursmaterial/` – kursdokument som indexeras i RAG-systemet
- `inlamningar/` – studentinlämningar som triggar AI-feedbackgenerering
- `genererat/` – AI-genererat kursmaterial, skrivs av appen vid körning

Dessa mappar ska ignoreras vid kodanalys och refaktorering.

## Kodkonventioner

- **Källkod på engelska** – identifierare, kommentarer, API-rutter
- **Meddelanden på svenska** – UI-texter, felmeddelanden, LLM-prompter
- Fysiska mappar och obligatoriska filnamn (`uppgiftsbeskrivning`, `bedömningsmall`) behåller svenska namn

## Köra lokalt

```bash
docker-compose up -d                          # Starta PostgreSQL, n8n, Ollama
cd TeacherAid.Api && dotnet run               # API på http://localhost:5010
cd teacher-aid-frontend && npm install && npm run dev  # Frontend
```

Kräver `TeacherAid.Api/appsettings.Development.json` med JWT-nyckel och connection string (se README.md).

## Standardkonto

- Användarnamn: `anna` / Lösenord: `password123`
