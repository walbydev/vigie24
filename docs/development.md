# Vigie24 - Local Development Guide

## Prerequisites
- .NET SDK 10.0.11+
- Docker & Docker Compose
- PostgreSQL 16 (optional, can use Docker)
- Rider 2024.3+ (recommended) or VS Code

## Initial Setup

### 1. Clone the repository
```bash
git clone <repo-url>
cd vigie24
```

### 2. Configure environment variables
Copy `.env.example` to `.env` and fill in the API keys:
```bash
cp .env.example .env
# Éditer .env avec vos clés API
```

**Configuration files:**
- `.env` → API keys (do not commit, already in .gitignore)
- `appsettings.json` → Base configuration
- `appsettings.Development.json` → Dev override

### 3. Restore dependencies
```bash
dotnet restore Vigie24.slnx
```

### 4. Start PostgreSQL (via Docker)
```bash
docker compose up -d postgres
```

Or use a local PostgreSQL on port 5432 with:
- User: `postgres`
- Password: `postgres`
- Database: `vigie24`

### 5. Initialize the database
```bash
# Depuis le projet Infrastructure
cd src/Vigie24.Infrastructure
dotnet ef database update
```

### 6. Run the application

**Option A: Via Docker Compose (recommended for a production-like setup)**
```bash
docker compose up -d
# Web: http://localhost:5000
```

**Option B: Via Rider (debugging)**
1. Open the `Vigie24.slnx` solution in Rider
2. Select the launch profile:
   - `Vigie24.Web` → Run only the frontend
   - `Vigie24.Worker` → Run only the worker
   - `Docker Compose` → Run both (recommended)
3. Press Run/Debug

**Option C: Via the dotnet CLI**
```bash
# Lancer le worker
cd src/Vigie24.Worker
dotnet run

# Lancer le web
cd src/Vigie24.Web
dotnet run
```

## Debugging

### Recommended breakpoints
1. **Worker**: `Vigie24Worker.ExecuteAsync()` - main entry point
2. **Worker**: `ProcessFeedsAsync()` - RSS feed processing
3. **Worker**: `FetchRssFeedAsync()` - fetching feeds
4. **Web**: `Pages/*.razor` - Blazor components
5. **Services**: `*AiService.cs` - AI calls

### Logs
Logs are configured at **Debug** level in `appsettings.Development.json`:
- `Microsoft.EntityFrameworkCore.Database.Command` → SQL queries
- `Vigie24` → Business logs (Debug level)

### Enabling SQL logging
Already enabled in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

## Tests

### Running the tests
```bash
dotnet test tests/Vigie24.Tests
```

### Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Project Architecture

```
vigie24/
├── src/
│   ├── Vigie24.Domain/        # Entités, interfaces, value objects
│   ├── Vigie24.Infrastructure # EF Core, repositories, migrations
│   ├── Vigie24.Services/      # Services IA, feed parsing, business logic
│   ├── Vigie24.Web/           # Blazor Server frontend
│   └── Vigie24.Worker/        # Background service (RSS polling)
├── tests/
│   └── Vigie24.Tests/         # Tests unitaires
├── scripts/                   # Scripts SQL, migrations
├── docker-compose.yml         # Orchestration locale
├── .env                       # Variables d'environnement (secrets)
└── AGENTS.md                  # Contexte pour assistants IA
```

## Important Notes

### 1. PostgreSQL ENUM
ENUM fields in the database are stored as `varchar`. In C#, use `string`, not `enum`.

### 2. AI Timeouts
AI calls have a 60s timeout. Always use `async/await`.

### 3. Worker Polling
- Default interval: 15 min
- In dev: 5 min (via `appsettings.Development.json`)
- Max concurrent requests: 5

### 4. Connection Strings
Docker format: `Host=postgres;Database=vigie24;Username=postgres;Password=postgres`
Local format: `Host=localhost;Database=vigie24;Username=postgres;Password=postgres`

## Useful Commands

```bash
# Voir les logs Docker
docker compose logs -f worker
docker compose logs -f web

# Redémarrer un service
docker compose restart worker
docker compose restart web

# Arrêter tout
docker compose down

# Reset DB
docker compose down -v
docker compose up -d postgres
dotnet ef database update

# Voir les migrations
dotnet ef migrations list

# Ajouter une migration
dotnet ef migrations add NomMigration --project src/Vigie24.Infrastructure --startup-project src/Vigie24.Web
```

## Troubleshooting

### The worker cannot connect to PostgreSQL
1. Check that PostgreSQL is running: `docker compose ps`
2. Check the logs: `docker compose logs postgres`
3. Restart the worker: `docker compose restart worker`

### EF Core migration error
```bash
dotnet ef database drop --force
dotnet ef database update
```

### API keys issue
Check that `.env` is properly configured and that the variables are exposed in `docker-compose.yml`.

## GitHub Copilot Integration

The `AGENTS.md` file contains the full project context for AI assistants. Make sure it is up to date.

For VS Code, the `.vscode/settings.json` file configures the development environment.
For Rider, the launch profiles are in `src/*/Properties/launchSettings.json`.
