# Vigie24 Debugging Guide in Rider

## 🚀 Option 1: Docker Compose Debugging (Recommended)

This method lets you debug **Web** and **Worker** simultaneously in Docker containers.

### Prerequisites
- Docker Desktop or Docker Engine installed
- Rider 2024.3+ with Docker support
- .NET SDK 10.0.11+ installed locally

### Setup

#### 1. Start PostgreSQL first
```bash
cd /home/philippe/RiderProjects/OpenSource/vigie24
docker compose -f docker-compose.debug.yml up -d postgres
```

#### 2. Configure Rider for Docker Compose

1. **Open** `Run → Edit Configurations...` (Shift+F10)
2. **Add** a new configuration → `Docker Compose`
3. **Configure**:
   - **Compose file**: `docker-compose.debug.yml`
   - **Services**: Check `vigie24-web` and `vigie24-worker`
   - **Startup action**: `Run`
   - **Debugger attach**: ✅ Check `Attach debugger to running containers`
   - **Working directory**: `/home/philippe/RiderProjects/OpenSource/vigie24`

4. **Apply** and **OK**

#### 3. Start debugging

1. Select the `docker-compose.debug.yml` configuration in the toolbar
2. Click on **Debug** (the green bug icon)
3. Rider will:
   - Launch the containers
   - Attach the debugger automatically
   - Let you set breakpoints

#### 4. Set breakpoints

- **Web**: `src/Vigie24.Web/Pages/*.razor`, `Program.cs`, `Pages/*.cshtml.cs`
- **Worker**: `src/Vigie24.Worker/Worker.cs`, `ProcessFeedsAsync()`, `FetchRssFeedAsync()`
- **Services**: `src/Vigie24.Services/*AiService.cs`
- **Infrastructure**: `src/Vigie24.Infrastructure/*Repository.cs`

#### 5. Test

- **Access the web UI**: http://localhost:5000
- **Watch the logs**: The `Run` window in Rider shows logs from both services
- **Debug**: Breakpoints will be hit in both services

### Ports used
- **Web**: `localhost:5000` (UI)
- **Worker**: No web UI, logs only
- **PostgreSQL**: `localhost:5432`
- **Web Debug**: `localhost:55555` (if manual remote debugging is needed)
- **Worker Debug**: `localhost:55556` (if manual remote debugging is needed)

---

## 🚀 Option 2: Local Debugging (Without Docker)

This method runs the services locally without Docker containers.

### Prerequisites
- Local PostgreSQL or Docker
- Environment variables configured

### Setup

#### 1. Start PostgreSQL (Docker)
```bash
docker run -d --name vigie24-db-local   -e POSTGRES_USER=postgres   -e POSTGRES_PASSWORD=postgres   -e POSTGRES_DB=vigie24   -p 5432:5432   postgres:16-alpine
```

#### 2. Configure User Secrets

**For Worker:**
```bash
cd src/Vigie24.Worker
dotnet user-secrets set "ConnectionStrings__Database" "Host=localhost;Database=vigie24;Username=postgres;Password=postgres"
dotnet user-secrets set "AiService__Provider" "infomaniak"
dotnet user-secrets set "AiService__ApiKey" "VOTRE_CLE_INFOMANIAK"
```

**For Web:**
```bash
cd src/Vigie24.Web
dotnet user-secrets set "ConnectionStrings__Database" "Host=localhost;Database=vigie24;Username=postgres;Password=postgres"
dotnet user-secrets set "AiService__Provider" "infomaniak"
dotnet user-secrets set "AiService__ApiKey" "VOTRE_CLE_INFOMANIAK"
```

#### 3. Configure Rider to launch both projects

**Method A: Multiple Startup Projects**

1. `Right-click the solution → Properties`
2. **Common Properties → Startup Project**: `Multiple startup projects`
3. Configure:
   - `Vigie24.Web` → `Action: Start`
   - `Vigie24.Worker` → `Action: Start`
4. **OK**

**Method B: Custom Run/Debug configuration**

1. `Run → Edit Configurations`
2. **Add** a `.NET` configuration for each project
3. Configure both with `Environment variables`:
   ```
   ASPNETCORE_ENVIRONMENT=Development
   DOTNET_EnableDiagnostics=1
   ```
4. Enable **Multiple startup projects** in both

#### 4. Launch debugging

1. Select the configuration (or `All Projects`)
2. Click on **Debug**
3. Both services start simultaneously

### Ports used (local)
- **Web**: `localhost:5198` (http) or `localhost:7040` (https)
- **Worker**: Console, no web port
- **PostgreSQL**: `localhost:5432`

---

## 🐛 Recommended Breakpoints

### Worker (`src/Vigie24.Worker/Worker.cs`)
```csharp
// Ligne 44 : ExecuteAsync - point d'entrée du worker
protected override async Task ExecuteAsync(CancellationToken stoppingToken)

// Ligne 63 : ProcessFeedsAsync - traitement des flux RSS
var sources = await _feedRepository.GetAllAsync();

// Ligne 120 : FetchRssFeedAsync - récupération des feeds
var response = await _httpClient.GetAsync(feedUrl);
```

### Web (`src/Vigie24.Web/`)
```csharp
// Program.cs - configuration de l'application
builder.Services.AddRazorPages();

// Pages/Index.razor - page d'accueil
@page "/"

// Services - appel IA
src/Vigie24.Services/InfomaniakAiService.cs
```

---

## 📊 Verifying the Debugging Session

### 1. Verify that the debugger is attached
```bash
# Dans le conteneur
docker exec -it vigie24-web-debug ps aux | grep dotnet
# Devrait montrer le processus avec diagnostics activés
```

### 2. Test a breakpoint
1. Set a breakpoint in `Worker.cs` line 63
2. Wait for the next polling cycle (5 min in debug)
3. The debugger should pause

### 3. View the logs
- **Run window** in Rider shows the combined logs
- **Docker Console**: `docker compose -f docker-compose.debug.yml logs -f`

---

## 🔧 Troubleshooting

### The debugger does not attach
1. Verify `DOTNET_EnableDiagnostics=1` in the environment variables
2. Verify that the `.Debug` Dockerfiles are used
3. Restart the containers: `docker compose -f docker-compose.debug.yml down && up`

### Breakpoints are not hit
1. Verify that the source code matches the built version exactly
2. Rebuild: `docker compose -f docker-compose.debug.yml build --no-cache`
3. Check Debug mode: `dotnet --version` and `build -c Debug`

### PostgreSQL connection error
1. Verify that PostgreSQL is running: `docker compose ps`
2. Check the logs: `docker compose logs postgres`
3. Test the connection: `docker exec -it vigie24-db-debug psql -U postgres`

---

## 🎯 Useful Commands

```bash
# Lancer tout en mode debug
docker compose -f docker-compose.debug.yml up

# Arrêter tout
docker compose -f docker-compose.debug.yml down

# Voir les logs
docker compose -f docker-compose.debug.yml logs -f

# Logs du worker uniquement
docker compose -f docker-compose.debug.yml logs -f vigie24-worker

# Rebuild sans cache
docker compose -f docker-compose.debug.yml build --no-cache

# Reset DB
docker compose -f docker-compose.debug.yml down -v
docker compose -f docker-compose.debug.yml up -d postgres
```

---

## 💡 Tips

1. **Fast polling**: In debug, polling is every 5 min (vs 15 min in prod)
2. **Detailed logs**: `appsettings.Development.json` has SQL logging enabled
3. **Hot Reload**: Rider supports hot reload for Blazor (F5 to refresh)
4. **Database Explorer**: Use Rider's Database Explorer tool to inspect PostgreSQL

---

## 📝 Summary of Created Files

- `Dockerfile.Web.Debug` → Debug build with diagnostics enabled
- `Dockerfile.Worker.Debug` → Debug build with diagnostics enabled
- `docker-compose.debug.yml` → Configuration for simultaneous debugging
- `docs/DEBUGGING.md` → This guide

Choose **Option 1 (Docker Compose)** for a debugging experience closest to production.
