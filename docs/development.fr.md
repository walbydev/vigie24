# Vigie24 - Guide de Développement Local

## Prérequis
- .NET SDK 10.0.11+
- Docker & Docker Compose
- PostgreSQL 16 (optionnel, peut utiliser Docker)
- Rider 2024.3+ (recommandé) ou VS Code

## Première Configuration

### 1. Cloner le dépôt
```bash
git clone <repo-url>
cd vigie24
```

### 2. Configurer les variables d'environnement
Copier `.env.example` vers `.env` et remplir les clés API :
```bash
cp .env.example .env
# Éditer .env avec vos clés API
```

**Fichiers de configuration :**
- `.env` → Clés API (ne pas commiter, déjà dans .gitignore)
- `appsettings.json` → Configuration de base
- `appsettings.Development.json` → Override pour dev

### 3. Restaurer les dépendances
```bash
dotnet restore Vigie24.slnx
```

### 4. Lancer PostgreSQL (via Docker)
```bash
docker compose up -d postgres
```

Ou utiliser PostgreSQL local sur le port 5432 avec :
- User: `postgres`
- Password: `postgres`
- Database: `vigie24`

### 5. Initialiser la base de données
```bash
# Depuis le projet Infrastructure
cd src/Vigie24.Infrastructure
dotnet ef database update
```

### 6. Lancer l'application

**Option A: Via Docker Compose (recommandé pour production-like)**
```bash
docker compose up -d
# Web: http://localhost:5000
```

**Option B: Via Rider (débogage)**
1. Ouvrir la solution `Vigie24.slnx` dans Rider
2. Sélectionner le profil de lancement :
   - `Vigie24.Web` → Lancer uniquement le frontend
   - `Vigie24.Worker` → Lancer uniquement le worker
   - `Docker Compose` → Lancer les deux (recommandé)
3. Appuyer sur Run/Debug

**Option C: Via dotnet CLI**
```bash
# Lancer le worker
cd src/Vigie24.Worker
dotnet run

# Lancer le web
cd src/Vigie24.Web
dotnet run
```

## Débogage

### Points de breakpoint recommandés
1. **Worker**: `Vigie24Worker.ExecuteAsync()` - entrée principale
2. **Worker**: `ProcessFeedsAsync()` - traitement des flux RSS
3. **Worker**: `FetchRssFeedAsync()` - récupération des feeds
4. **Web**: `Pages/*.razor` - composants Blazor
5. **Services**: `*AiService.cs` - appels IA

### Logs
Les logs sont configurés en **Debug** dans `appsettings.Development.json` :
- `Microsoft.EntityFrameworkCore.Database.Command` → SQL queries
- `Vigie24` → Logs métier (Debug level)

### Activer le SQL logging
Déjà activé dans `appsettings.Development.json` :
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

### Exécuter les tests
```bash
dotnet test tests/Vigie24.Tests
```

### Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture du Projet

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

## Points d'Attention

### 1. ENUM PostgreSQL
Les champs ENUM en DB sont stockés en `varchar`. En C#, utiliser `string`, pas `enum`.

### 2. Timeouts IA
Les appels IA ont un timeout de 60s. Toujours utiliser `async/await`.

### 3. Polling Worker
- Intervalle par défaut: 15 min
- En dev: 5 min (via `appsettings.Development.json`)
- Max concurrent requests: 5

### 4. Connection Strings
Format Docker: `Host=postgres;Database=vigie24;Username=postgres;Password=postgres`
Format local: `Host=localhost;Database=vigie24;Username=postgres;Password=postgres`

## Commandes Utiles

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

# Minifier les assets CSS/JS après toute édition wwwroot (le site sert les .min.*)
make assets

# Voir les migrations
dotnet ef migrations list

# Ajouter une migration (DesignTimeDbContextFactory — pas de --startup-project Web requis)
dotnet ef migrations add NomMigration --project src/Vigie24.Infrastructure
```

## Dépannage

### Le worker ne se connecte pas à PostgreSQL
1. Vérifier que PostgreSQL est en cours d'exécution: `docker compose ps`
2. Vérifier les logs: `docker compose logs postgres`
3. Redémarrer le worker: `docker compose restart worker`

### Erreur de migration EF Core
```bash
dotnet ef database drop --force
dotnet ef database update
```

### Problème de clés API
Vérifier que `.env` est bien configuré et que les variables sont exposées dans `docker-compose.yml`.

## Intégration avec GitHub Copilot

Le fichier `AGENTS.md` contient le contexte complet du projet pour les assistants IA. Assurez-vous qu'il est à jour.

Pour VS Code, le fichier `.vscode/settings.json` configure l'environnement de développement.
Pour Rider, les profils de lancement sont dans `src/*/Properties/launchSettings.json`.
