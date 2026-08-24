# Vigie24 — Veille technologique francophone intelligente

**Vigie24** est un système intelligent de surveillance RSS dédié à l'actualité technologique francophone. Il agrège 240+ sources vérifiées (presse, blogs, chaînes YouTube, communautés Reddit), analyse chaque article par IA (résumé, score éditorial, catégories, thèmes, mots-clés, positions de débat), détecte les doublons et sujets connexes, et publie le meilleur contenu sur un site Blazor Server avec des flux RSS granulaires.

🌐 **Site web** : [vigie24.info](https://vigie24.info) — le produit est **francophone par conception** (textes d'interface, URLs publiques, contenus générés par IA)  
📡 **Flux RSS** : global `/feed` + par catégorie `/categories/{slug}/feed`  
📄 **Licence** : MIT

---

## ✨ Fonctionnalités

- **Agrégation RSS** — 240+ sources vérifiées dans 5 catégories (IA, Dev, Science, Cybersécurité, Hardware), priorité francophone (CH > FR > BE > QC > Europe > Monde)
- **Analyse IA** — par article : résumé concis en français, score éditorial (0–100), hiérarchie de catégories, thèmes, mots-clés, sujet et position de débat (pour / contre / nuance)
- **Fournisseurs IA** — Infomaniak (principal, basé en Suisse) avec DeepSeek en secours, via une factory interchangeable (`IAiService`)
- **Publication intelligente** — les articles sont publiés si le score éditorial ≥ 60 ; les doublons sont détectés sémantiquement (embeddings)
- **Débats** — les articles traitant du même sujet avec des positions opposées sont regroupés en « débats » (`/debats`, `/debats/candidats`), via des embeddings multilingues (bge-multilingual-gemma2, 3584 dimensions)
- **Engagement** — votes anonymes (0–5) et suivi des clics, intégrés au score de classement
- **Transparence des coûts** — page `/couts` avec graphiques quotidiens/hebdomadaires de consommation de tokens et coût en CHF
- **Health check** — fraîcheur de l'ingestion + disponibilité du fournisseur IA sur la page d'accueil
- **SEO** — URLs françaises (`/categories`, `/themes`, `/debats`, `/projet`, `/charte-graphique`, `/couts`), sitemap XML, rich snippets BreadcrumbList, flux RSS

## 🏗️ Architecture

| Composant | Technologie |
|---|---|
| Frontend | Blazor Server (ASP.NET Core, .NET 10) |
| Worker | `BackgroundService` — polling RSS toutes les 15 min + traitement IA |
| Base de données | PostgreSQL 16 (Npgsql + EF Core 10) |
| IA | API Infomaniak (compatible OpenAI) + fallback DeepSeek |
| Déploiement | Docker Compose (une stack par environnement) + reverse proxy Caddy (prod) |

```
src/
├── Vigie24.Domain/          # Entités, scoring, taxonomie, builders RSS/sitemap
├── Vigie24.Infrastructure/  # EF Core, PostgreSQL, requêtes, filtres
├── Vigie24.Services/        # Services IA (Infomaniak, DeepSeek, mock), factory
├── Vigie24.Web/             # UI Blazor Server
└── Vigie24.Worker/          # Pipeline polling RSS + traitement IA
tests/Vigie24.Tests/         # Tests unitaires, fonctionnels et d'intégration
```

## 📂 Structure du dépôt

```
├── environments/<env>/      # Un dossier auto-contenu par environnement (development,
│                            # mock, staging, production) : docker-compose.yml,
│                            # .env.example (gabarit de secrets) + surcharge appsettings
├── docs/                    # Documentation — anglais par défaut (*.md),
│                            # versions françaises en *.fr.md
├── scripts/                 # Outils dev/ops (SQL, shell, Python)
├── src/                     # Projets de la solution
├── tests/                   # Tests
├── Makefile                 # Point d'entrée dev + déploiement
└── Vigie24.slnx
```

## 🚀 Démarrage rapide

### Prérequis
- SDK .NET 10
- Docker + Docker Compose (pour les stacks conteneurisées)
- Une clé API de fournisseur IA (Infomaniak ou DeepSeek)

### Option A — Docker Compose (recommandé)

```bash
git clone https://github.com/walbydev/vigie24.git
cd vigie24

# Environnement de développement (IA réelles, port 5000)
cp environments/development/.env.example environments/development/.env
# renseigner vos clés API dans environments/development/.env
make dev-run

# Environnement mock (IA simulées, aucune clé requise, port 5001)
cp environments/mock/.env.example environments/mock/.env
make mock-run
```

### Option B — Exécution .NET locale (sans Docker)

```bash
dotnet restore Vigie24.slnx
cd src/Vigie24.Worker
dotnet user-secrets set "AiService__ApiKey" "votre-cle"
dotnet user-secrets set "AiService__Provider" "infomaniak"
cd ../../src/Vigie24.Web
dotnet run
```

PostgreSQL doit être joignable (voir `docs/development.fr.md`).

### Règle d'or de configuration
- **`appsettings.json`** (par projet) + **`environments/<env>/appsettings.<Env>.json`** (surcharges montées en volume) → toute la config non sensible, versionnée dans git
- **`environments/<env>/.env`** → secrets uniquement (clés API, mots de passe DB), jamais commités ; les gabarits `.env.example` sont versionnés
- Toute la config est injectée dans les conteneurs via `env_file` + surcharges d'environnement — aucun secret n'est intégré aux images

## 🧪 Tests

```bash
dotnet test tests/Vigie24.Tests --filter "FullyQualifiedName!~Integration"
```
125 tests unitaires/fonctionnels (les tests d'intégration requièrent un conteneur PostgreSQL — voir `docs/testing.fr.md`).

## 📖 Documentation

| Doc | English | Français |
|---|---|---|
| Développement | `docs/development.md` | `docs/development.fr.md` |
| Débogage | `docs/debugging.md` | `docs/debugging.fr.md` |
| Environnements | `docs/environments.md` | `docs/environments.fr.md` |
| Déploiement | `docs/deployment.md` | `docs/deployment.fr.md` |
| Production | `docs/production.md` | `docs/production.fr.md` |
| Scripts | `docs/scripts.md` | `docs/scripts.fr.md` |
| Tests | `docs/testing.md` | `docs/testing.fr.md` |
| Releases | `docs/releases.md` | `docs/releases.fr.md` |
| Docs de design | `docs/design/*.md` | `docs/design/*.fr.md` |

## 🔄 Politique de release

- **Gitea est la source principale** (`origin`) — chaque commit y vit
- **GitHub héberge uniquement les versions majeures publiques** (remote `github`), via `make release VERSION=x.y.z` (tag semver poussé sur les deux remotes)
- Les commits quotidiens ne sont jamais poussés vers GitHub à la main

## 📝 Licence

MIT — voir [LICENSE](LICENSE). Copyright (c) 2026 Walby Developments.

## 📞 Contact

- Auteur : Philippe Ryf — philippe@walby.dev
- Site web : [vigie24.info](https://vigie24.info) — contact@vigie24.info

**Vigie24** — *La veille technologique, enfin claire.*
