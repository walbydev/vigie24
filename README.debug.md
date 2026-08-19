# Guide de Débogage Vigie24 dans Rider

## 🚀 Option 1 : Débogage Docker Compose (Recommandé)

Cette méthode permet de déboguer **Web** et **Worker** simultanément dans des conteneurs Docker.

### Prérequis
- Docker Desktop ou Docker Engine installé
- Rider 2024.3+ avec support Docker
- .NET SDK 10.0.11+ installé localement

### Configuration

#### 1. Lancer PostgreSQL en premier
```bash
cd /home/philippe/RiderProjects/OpenSource/vigie24
docker compose -f docker-compose.debug.yml up -d postgres
```

#### 2. Configurer Rider pour Docker Compose

1. **Ouvrir** `Run → Edit Configurations...` (Shift+F10)
2. **Ajouter** une nouvelle configuration → `Docker Compose`
3. **Configurer** :
   - **Compose file** : `docker-compose.debug.yml`
   - **Services** : Cocher `vigie24-web` et `vigie24-worker`
   - **Startup action** : `Run`
   - **Debugger attach** : ✅ Cocher `Attach debugger to running containers`
   - **Working directory** : `/home/philippe/RiderProjects/OpenSource/vigie24`

4. **Appliquer** et **OK**

#### 3. Démarrer le débogage

1. Sélectionner la configuration `docker-compose.debug.yml` dans la barre d'outils
2. Cliquer sur **Debug** (la tique verte avec la虫子)
3. Rider va :
   - Lancer les conteneurs
   - Attacher le débogueur automatiquement
   - Vous permettre de mettre des breakpoints

#### 4. Mettre des breakpoints

- **Web** : `src/Vigie24.Web/Pages/*.razor`, `Program.cs`, `Pages/*.cshtml.cs`
- **Worker** : `src/Vigie24.Worker/Worker.cs`, `ProcessFeedsAsync()`, `FetchRssFeedAsync()`
- **Services** : `src/Vigie24.Services/*AiService.cs`
- **Infrastructure** : `src/Vigie24.Infrastructure/*Repository.cs`

#### 5. Tester

- **Accéder à l'interface web** : http://localhost:5000
- **Observer les logs** : Fenêtre `Run` dans Rider montre les logs des deux services
- **Déboguer** : Les breakpoints seront levés dans les deux services

### Ports utilisés
- **Web** : `localhost:5000` (interface)
- **Worker** : Pas d'interface web, logs uniquement
- **PostgreSQL** : `localhost:5432`
- **Debug Web** : `localhost:55555` (si besoin de debug à distance manuel)
- **Debug Worker** : `localhost:55556` (si besoin de debug à distance manuel)

---

## 🚀 Option 2 : Débogage Local (Sans Docker)

Cette méthode lance les services localement sans conteneurs Docker.

### Prérequis
- PostgreSQL local ou Docker
- Variables d'environnement configurées

### Configuration

#### 1. Lancer PostgreSQL (Docker)
```bash
docker run -d --name vigie24-db-local   -e POSTGRES_USER=postgres   -e POSTGRES_PASSWORD=postgres   -e POSTGRES_DB=vigie24   -p 5432:5432   postgres:16-alpine
```

#### 2. Configurer les User Secrets

**Pour Worker :**
```bash
cd src/Vigie24.Worker
dotnet user-secrets set "ConnectionStrings__Database" "Host=localhost;Database=vigie24;Username=postgres;Password=postgres"
dotnet user-secrets set "AiService__Provider" "infomaniak"
dotnet user-secrets set "AiService__ApiKey" "VOTRE_CLE_INFOMANIAK"
```

**Pour Web :**
```bash
cd src/Vigie24.Web
dotnet user-secrets set "ConnectionStrings__Database" "Host=localhost;Database=vigie24;Username=postgres;Password=postgres"
dotnet user-secrets set "AiService__Provider" "infomaniak"
dotnet user-secrets set "AiService__ApiKey" "VOTRE_CLE_INFOMANIAK"
```

#### 3. Configurer Rider pour lancer les deux projets

**Méthode A : Multiple Startup Projects**

1. `Right-click sur la solution → Properties`
2. **Common Properties → Startup Project** : `Multiple startup projects`
3. Configurer :
   - `Vigie24.Web` → `Action: Start`
   - `Vigie24.Worker` → `Action: Start`
4. **OK**

**Méthode B : Configuration Run/Debug personnalisée**

1. `Run → Edit Configurations`
2. **Ajouter** une configuration `.NET` pour chaque projet
3. Configurer les deux avec `Environment variables` :
   ```
   ASPNETCORE_ENVIRONMENT=Development
   DOTNET_EnableDiagnostics=1
   ```
4. Activer **Multiple startup projects** dans les deux

#### 4. Lancer le débogage

1. Sélectionner la configuration (ou `All Projects`)
2. Cliquer sur **Debug**
3. Les deux services démarrent simultanément

### Ports utilisés (local)
- **Web** : `localhost:5198` (http) ou `localhost:7040` (https)
- **Worker** : Console, pas de port web
- **PostgreSQL** : `localhost:5432`

---

## 🐛 Points de Débogage Recommandés

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

## 📊 Vérification du Débogage

### 1. Vérifier que le débogueur est attaché
```bash
# Dans le conteneur
docker exec -it vigie24-web-debug ps aux | grep dotnet
# Devrait montrer le processus avec diagnostics activés
```

### 2. Tester un breakpoint
1. Mettre un breakpoint dans `Worker.cs` ligne 63
2. Attendre le prochain cycle de polling (5 min en debug)
3. Le débogueur devrait se suspendre

### 3. Voir les logs
- **Fenêtre Run** dans Rider affiche les logs combinés
- **Console Docker** : `docker compose -f docker-compose.debug.yml logs -f`

---

## 🔧 Dépannage

### Le débogueur ne s'attache pas
1. Vérifier `DOTNET_EnableDiagnostics=1` dans les variables d'environnement
2. Vérifier que les Dockerfiles `.Debug` sont utilisés
3. Redémarrer les conteneurs : `docker compose -f docker-compose.debug.yml down && up`

### Les breakpoints ne sont pas levés
1. Vérifier que le code source correspond exactement à la version buildée
2. Rebuild : `docker compose -f docker-compose.debug.yml build --no-cache`
3. Vérifier le mode Debug : `dotnet --version` et `build -c Debug`

### Erreur de connexion à PostgreSQL
1. Vérifier que PostgreSQL est en cours d'exécution : `docker compose ps`
2. Vérifier les logs : `docker compose logs postgres`
3. Tester la connexion : `docker exec -it vigie24-db-debug psql -U postgres`

---

## 🎯 Commandes Utiles

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

## 💡 Astuces

1. **Polling rapide** : En debug, le polling est à 5 min (vs 15 min en prod)
2. **Logs détaillés** : `appsettings.Development.json` a le logging SQL activé
3. **Hot Reload** : Rider supporte le hot reload pour Blazor (F5 pour rafraîchir)
4. **Database Explorer** : Utiliser l'outil Database Explorer de Rider pour inspecter PostgreSQL

---

## 📝 Résumé des Fichiers Créés

- `Dockerfile.Web.Debug` → Build en Debug avec diagnostics activés
- `Dockerfile.Worker.Debug` → Build en Debug avec diagnostics activés
- `docker-compose.debug.yml` → Configuration pour débogage simultané
- `README.debug.md` → Ce guide

Choisissez **Option 1 (Docker Compose)** pour une expérience de débogage la plus proche de la production.
