# Vigie24 - Project Context for AI Assistants

## Project Overview
Vigie24 est un système de surveillance RSS intelligent avec traitement IA, déployé en .NET 10 avec Blazor Server et Worker Services.

## Architecture
- **Vigie24.Web**: Blazor Server frontend (port 5000 en production, 5198/7040 en local)
- **Vigie24.Worker**: Background service pour polling RSS et traitement IA (cycle 15 min)
- **Vigie24.Infrastructure**: EF Core, PostgreSQL, repositories
- **Vigie24.Services**: Services IA (Infomaniak, DeepSeek), feed parsing
- **Vigie24.Domain**: Entités, interfaces, modèles

## Stack Technique
- **.NET 10** (SDK 10.0.11)
- **PostgreSQL 16** (via Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0)
- **Blazor Server** pour le frontend
- **BackgroundService** pour le worker
- **Docker Compose** pour l'orchestration locale
- **Infomaniak AI Service** (provider principal) et **DeepSeek** (fallback)

## Configuration Locale (Développement)

### 1. Solution File
Le projet utilise `Vigie24.slnx` (nouveau format .NET 10). Rider devrait le reconnaître automatiquement.
Si problèmes, générer un `.sln` classique :
```bash
dotnet new sln -n Vigie24
dotnet sln add src/Vigie24.Domain/Vigie24.Domain.csproj
dotnet sln add src/Vigie24.Infrastructure/Vigie24.Infrastructure.csproj
dotnet sln add src/Vigie24.Services/Vigie24.Services.csproj
dotnet sln add src/Vigie24.Web/Vigie24.Web.csproj
dotnet sln add src/Vigie24.Worker/Vigie24.Worker.csproj
```

### 2. Variables d'Environnement
Fichier `.env` à la racine contient les clés API. Pour le développement local :

**Option A: User Secrets (recommandé)**
```bash
cd src/Vigie24.Worker
dotnet user-secrets set "AiService__Provider" "infomaniak"
dotnet user-secrets set "AiService__ApiUrl" "https://api.infomaniak.com"
dotnet user-secrets set "AiService__ApiKey" "VOTRE_CLE_INFOMANIAK"
dotnet user-secrets set "ConnectionStrings__Database" "Host=localhost;Database=vigie24;Username=postgres;Password=postgres"
```

**Option B: appsettings.Development.json**
Déjà configuré avec `Logging` en Debug et `FeedReader__PollingIntervalMinutes: 5` pour tests rapides.

### 3. PostgreSQL Local
Pour déboguer sans Docker, lancer PostgreSQL localement :
```bash
docker run -d --name vigie24-db-local \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=vigie24 \
  -p 5432:5432 \
  postgres:16-alpine
```

Ou utiliser le Docker Compose :
```bash
docker compose up -d postgres
```

### 4. Profils de Lancement (Rider)
Rider détecte automatiquement `launchSettings.json` :

**Pour Vigie24.Web :**
- Profile: `http` → http://localhost:5198
- Profile: `https` → https://localhost:7040

**Pour Vigie24.Worker :**
- Profile: `Vigie24.Worker` → Console app, pas de URL

### 5. Dépendances NuGet
Le projet utilise `NuGet.config` à la racine. Vérifier que toutes les packages sont restaurées :
```bash
dotnet restore Vigie24.slnx
```

## Points Critiques de Configuration

### EF Core & PostgreSQL
- **ENUM mapping**: Les champs ENUM en DB PostgreSQL sont stockés en `varchar`, donc utiliser `string` en C# (pas `enum`)
- **Table names**: EF Core utilise lowercase par défaut (`articles`, `sources`, `categories`)
- **Migration**: `dotnet ef migrations add NomMigration` depuis le projet Infrastructure

### AI Service Abstraction
Le projet implémente un pattern Factory pour switcher entre providers :
- `IAiService` interface
- `InfomaniakAiService` et `DeepSeekAiService` implémentations
- `AiServiceFactory` pour l'injection

Configuration via environment variables ou appsettings :
```json
{
  "AiService": {
    "Provider": "infomaniak",  // ou "deepseek"
    "ApiUrl": "https://api.infomaniak.com",
    "ApiKey": "xxx",
    "Model": "Qwen/Qwen3.5-122B-A10B-FP8",
    "TimeoutSeconds": 60,
    "Temperature": 0.3,
    "MaxTokens": 2048
  }
}
```

### Worker Polling
- Intervalle par défaut: 15 minutes (configurable via `FeedReader__PollingIntervalMinutes`)
- En dev: 5 minutes dans `appsettings.Development.json`
- Max concurrent requests: 5 (évite la surcharge des APIs)

## Scripts Utiles
```bash
# Initialiser la DB
dotnet ef database update --project src/Vigie24.Infrastructure

# Seed data
dotnet run --project scripts/seed-data

# Tests
dotnet test tests/Vigie24.Tests
```

## Débogage
1. **Worker**: Mettre des breakpoints dans `Vigie24Worker.ExecuteAsync()` et `ProcessFeedsAsync()`
2. **Web**: Debugger les RazorComponents et `Program.cs`
3. **DB**: Activer SQL logging dans `appsettings.Development.json` :
   ```json
   "Logging": {
     "LogLevel": {
       "Microsoft.EntityFrameworkCore.Database.Command": "Information"
     }
   }
   ```

## GitHub Copilot / AI Assistant Tips
- Le projet suit Clean Architecture avec séparation Domain/Infrastructure/Services/Web/Worker
- Utiliser des `string` pour les champs ENUM PostgreSQL
- Les timeouts IA sont longs (60s) - prévoir async/await partout
- Le worker utilise `IHttpClientFactory` pour gérer les connexions HTTP
- Les erreurs de feed sont catchées et loguées, ne crashent pas le worker

## Fichiers de Configuration Clés
- `.env` → Clés API (ne pas commiter)
- `appsettings.json` → Configuration de base
- `appsettings.Development.json` → Override pour dev (polling 5 min)
- `docker-compose.yml` → Orchestration locale
- `src/*/Properties/launchSettings.json` → Profils Rider/VS

## Notes Spécifiques .NET 10
- `.slnx` est le nouveau format XML de solution (support Rider en cours)
- SDK 10.0.11 utilisé
- EF Core 10.0.0
- Npgsql 10.0.0
