# Vigie24 — Project Context for AI Assistants

## Project Overview
Vigie24 is an intelligent RSS monitoring system with AI processing, built with .NET 10 (Blazor Server + Worker Services). The product is **French-speaking by design**: UI copy, public URLs and AI-generated content stay in French. The codebase itself (identifiers, comments, logs, docs) is in English.

## Product surfaces (August 2026)

- **Home**: showcase — hero + AI status card (provider, model, ingestion, cost) + masonry layers (briefing top 3, barometer, debates, journal, community top, hot-topic radar, "Parcourir Vigie24" nav) + last-6-articles preview
- **Content pages**: `/articles` (full browser with filters), `/categories`, `/themes`, `/debats` + `/debats/{id}` (living dossiers), `/briefing`, `/radar`, `/top`, `/sources` (with scoops), `/il-y-a-un-an`
- **Signature pages**: `/barometre` (anxiety), `/journal` (AI editor transparency), `/projet` (public + technical parts), `/couts`, `/charte-graphique`
- **Feeds**: `/feed`, `/categories/{slug}/feed`, `/feed/zen` (anxiety ≤ 6)
- **Global Zen mode**: nav toggle (localStorage `vigie24.zen`) caps anxiety ≤ 6 on content surfaces — see `Vigie24.Web.Services.ZenModeService`
- Navigation: two levels (identity nav + content subnav, "⋯ More" panel ≤ 720px)

## Architecture
- **Vigie24.Web**: Blazor Server frontend (port 5000 in dev, 8080 in containers)
- **Vigie24.Worker**: Background service for RSS polling and AI processing (15 min cycle)
- **Vigie24.Infrastructure**: EF Core, PostgreSQL, repositories
- **Vigie24.Services**: AI services (Infomaniak, DeepSeek), feed parsing
- **Vigie24.Domain**: Entities, interfaces, models

## Tech Stack
- **.NET 10** (SDK 10.0.x)
- **PostgreSQL 16** (via Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0)
- **Blazor Server** frontend
- **BackgroundService** worker
- **Docker Compose** orchestration (see `environments/`)
- **Infomaniak AI Service** (primary provider) and **DeepSeek** (fallback)

## Repository Layout
- `src/` — solution projects (Vigie24.Domain, Infrastructure, Services, Web, Worker)
- `tests/Vigie24.Tests` — unit, functional and integration tests
- `environments/<env>/` — one self-contained folder per environment (dev, mock, staging, production): `docker-compose.yml`, `.env.example` (secrets template), `appsettings.<Env>.json` override (mounted as a volume, not baked into images)
- `docs/` — documentation, English by default (`*.md`), French as `*.fr.md`
- `scripts/` — dev/ops tools (SQL, shell, Python)
- `Makefile` — local dev and deployment entry point (`make dev-run`, `make staging-pub`, …); deployment hosts come from `Makefile.env` (gitignored, template in `Makefile.env.example`)

## Configuration: appsettings vs .env (golden rule)

- **`appsettings.json` + `appsettings.{Env}.json`** — ALL non-sensitive configuration (scores, timeouts, models, URLs, polling, user-agent). Versioned in git. Per-environment overrides live in `environments/<env>/appsettings.<Env>.json` and are volume-mounted into the containers.
- **`.env`** (in each `environments/<env>/`) — **secrets only** (API keys, DB password) + substitution variables used by docker-compose (`AI_PROVIDER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`). Never committed; `environments/<env>/.env.example` templates are versioned.
- **User Secrets** — recommended for local dev without Docker:

**Option A: User Secrets (recommended)**
```bash
cd src/Vigie24.Worker
dotnet user-secrets set "AiService__Provider" "infomaniak"
dotnet user-secrets set "AiService__ApiUrl" "https://api.infomaniak.com"
dotnet user-secrets set "AiService__ApiKey" "VOTRE_CLE_INFOMANIAK"
dotnet user-secrets set "ConnectionStrings__Database" "Host=localhost;Database=vigie24;Username=postgres;Password=postgres"
```

**Option B: appsettings.Development.json**
Already configured with `Logging` in Debug and `FeedReader__PollingIntervalMinutes: 5` for fast tests.

### Local PostgreSQL
To debug without Docker, run PostgreSQL locally:
```bash
docker run -d --name vigie24-db-local \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=vigie24 \
  -p 5432:5432 \
  postgres:16-alpine
```

Or use the dev stack:
```bash
make dev-run
```

### Launch Profiles (Rider)
Rider picks up `launchSettings.json` automatically:

**Vigie24.Web:**
- Profile: `http` → http://localhost:5198
- Profile: `https` → https://localhost:7040

**Vigie24.Worker:**
- Profile: `Vigie24.Worker` → Console app, no URL

### NuGet
The project uses `NuGet.config` + `Directory.Packages.props` (Central Package Management) at the root:
```bash
dotnet restore Vigie24.slnx
```

## Critical Configuration Points

### EF Core & PostgreSQL
- **ENUM mapping**: PostgreSQL enum fields are stored as `varchar`, so use `string` in C# (not `enum`)
- **Table names**: EF Core uses lowercase by default (`articles`, `sources`, `categories`)
- **Migration**: `dotnet ef migrations add NomMigration` from the Infrastructure project

### AI Service Abstraction
Factory pattern to switch providers:
- `IAiService` interface
- `InfomaniakAiService` and `DeepSeekAiService` implementations
- `AiServiceFactory` for injection

Configuration via environment variables or appsettings:
```json
{
  "AiService": {
    "Provider": "infomaniak",  // or "deepseek"
    "ApiUrl": "https://api.infomaniak.com",
    "ApiKey": "xxx",
    "Model": "google/gemma-4-31B-it",
    "TimeoutSeconds": 60,
    "Temperature": 0.3,
    "MaxTokens": 2048
  }
}
```

### Worker Polling
- Default interval: 15 minutes (configurable via `FeedReader__PollingIntervalMinutes`)
- Dev: 5 minutes in `appsettings.Development.json`
- Max concurrent requests: 5 (avoids API overload)

## Useful Scripts
```bash
# Initialize the DB
dotnet ef database update --project src/Vigie24.Infrastructure

# Seed initial sources (see docs/scripts.md)
./scripts/add-all-sources.sh   # 246+ complete sources (or scripts/add-sources.sql for 7 test ones)

# Tests
dotnet test tests/Vigie24.Tests
```

## Debugging
1. **Worker**: breakpoints in `Vigie24Worker.ExecuteAsync()` and `ProcessFeedsAsync()`
2. **Web**: debug the Razor components and `Program.cs`
3. **DB**: enable SQL logging in `appsettings.Development.json`:
   ```json
   "Logging": {
     "LogLevel": {
       "Microsoft.EntityFrameworkCore.Database.Command": "Information"
     }
   }
   ```

## Coding Guidelines (AI assistants)
- Clean Architecture with Domain/Infrastructure/Services/Web/Worker separation
- Use `string` for PostgreSQL ENUM fields
- AI timeouts are long (60s) — use async/await everywhere
- The worker uses `IHttpClientFactory` for HTTP connections
- Feed errors are caught and logged, they never crash the worker
- **Language rule**: code identifiers, comments, logs and docs in English; UI copy, public URLs, AI prompts and mock data in French (product language)

## Key Configuration Files
- `environments/*/.env` → Secrets only (API keys, DB_PASSWORD) — never committed; templates are `.env.example`
- `appsettings.json` → Base configuration (scores, timeouts, models, URLs)
- `environments/*/appsettings.<Env>.json` → Per-environment overrides (volume-mounted)
- `src/*/Properties/launchSettings.json` → Rider/VS profiles

## .NET 10 Notes
- `.slnx` is the new XML solution format (Rider support in progress)
- SDK 10.0.x used
- EF Core 10.0.0
- Npgsql 10.0.0
