# Vigie24 — Veille technologique francophone intelligente

**Vigie24** est un système intelligent de surveillance RSS dédié à l'actualité technologique francophone. Il agrège 214 sources vérifiées (208 actives — presse, blogs, chaînes YouTube, communautés Reddit), analyse chaque article par IA (résumé, score éditorial, score d'anxiété, catégories, thèmes, mots-clés, positions de débat), détecte les doublons et sujets connexes, et publie le meilleur contenu sur un site Blazor Server avec des flux RSS granulaires.

🌐 **Site web** : [vigie24.info](https://vigie24.info) — le produit est **francophone par conception** (textes d'interface, URLs publiques, contenus générés par IA)  
📡 **Flux RSS** : global `/feed` + par catégorie `/categories/{slug}/feed`  
📄 **Licence** : MIT

---

## ✨ Fonctionnalités

- **Agrégation RSS** — 214 sources vérifiées (208 actives) réparties dans 8 catégories parentes (IA, Développement, Cybersécurité, Science, Hardware, Divertissements, Musiques, Arts graphiques), priorité francophone (CH > FR > BE > QC > Europe > Monde), ouverte sur l'actualité internationale
- **Analyse IA** — par article : résumé concis en français, score éditorial (0–100), score d'anxiété (0–10), hiérarchie de catégories, thèmes, mots-clés, sujet et position de débat — avec une **trace complète de l'analyse** (justification, raisonnement du modèle, LLM exact utilisé)
- **Transparence IA** — bloc « Réflexion & analyse » sur chaque fiche article ; journal du rédacteur IA (`/journal`) : refusés expliqués, coups de cœur, hésitations de catégorisation, réflexions marquantes
- **Lecture sereine** — baromètre de l'anxiété (`/barometre`) : indice quotidien, courbe 30 jours, par catégorie et pays, antidotes ; flux RSS zen `/feed/zen` ; **mode zen global** dans la navigation (masque les articles anxiogènes sur tout le site)
- **Dossiers vivants** — `/debats/{id}` : timeline des positions, intensité de couverture, évolution des positions, carte des médias, premier sur le sujet ; scoops par média sur `/sources`
- **Pages quotidiennes** — briefing du matin (`/briefing`), radar des sujets chauds (`/radar`), top de la communauté (`/top`), il y a un an (`/il-y-a-un-an`), accueil vitrine (maçonnerie + aperçu des articles) et page Articles dédiée
- **Fournisseurs IA** — Infomaniak (principal, basé en Suisse) avec DeepSeek en secours, via une factory interchangeable (`IAiService`)
- **Publication intelligente** — les articles sont publiés si le score éditorial ≥ 60 ; les doublons sont détectés sémantiquement (embeddings)
- **Débats** — les articles traitant du même sujet avec des positions opposées sont regroupés en « débats » (`/debats`, `/debats/candidats`), via des embeddings multilingues (bge-multilingual-gemma2, 3584 dimensions)
- **Engagement** — votes anonymes (0–5) et suivi des clics, intégrés au score de classement
- **Résilience des sources** — seuls les échecs définitifs (404/410, XML invalide, DNS mort) peuvent désactiver une source ; les transitoires (429/403/5xx/timeout) jamais ; backoff exponentiel (5 min → 6 h), réactivation automatique après 24 h via une sonde sans coût IA
- **Sécurité & performance** — en-têtes Caddy durcis (CSP, HSTS, politiques frame/nosniff), anti-abus sur les votes, cookies sécurisés ; assets CSS/JS minifiés avec cache immuable 1 an (`make assets`)
- **Transparence des coûts** — page `/couts` avec graphiques quotidiens/hebdomadaires de consommation de tokens et coût en CHF
- **SEO** — URLs françaises (`/articles`, `/categories`, `/themes`, `/debats`, `/projet`, `/charte-graphique`, `/couts`), sitemap XML, rich snippets BreadcrumbList, flux RSS

## 🏗️ Architecture

| Composant | Technologie |
|---|---|
| Frontend | Blazor Server (ASP.NET Core, .NET 10) |
| Worker | `BackgroundService` — polling RSS toutes les 15 min (5 min en production) + traitement IA |
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

## 📡 Rapport Telegram automatique (toutes les 72 h)

Le dépôt embarque un **pipeline Python autonome** qui découvre de nouvelles sources
technologiques (RSS, YouTube, Reddit, Bluesky) et envoie à Philippe un rapport de
**10-20 sources majoritairement francophones** sur Telegram, **tous les 3 jours**.

```
src/discovery.py   collecte multi-sources -> candidates.jsonl (gitignoré)
src/report.py      filtrage pertinence + dédup + état vu -> rapport Markdown
src/telegram.py    envoi Telegram : send_report() / send_error() (stdlib only)
run_pipeline.py    orchestrateur : discovery -> report -> envoi (ou alerte)
scheduler.py       planificateur APScheduler portable (IntervalTrigger(days=3))
scripts/systemd/   unités systemd d'exemple (timer OnUnitActiveSec=3d)
```

**Méthode retenue pour la planification.** Sur le conteneur homelab (pas de
systemd), la planification est portée par le **scheduler Hermes** (cron
`vigie24-rapport-3j`, toutes les 72 h, supervisé par le service `main-hermes`,
équivalent robuste d'un `IntervalTrigger(days=3)`) qui exécute le wrapper
`/opt/data/scripts/vigie24_report_3d.sh`. Pour un hôte autonome, le dépôt fournit
`scheduler.py` (APScheduler `IntervalTrigger(days=3)`, lancer avec
`uv run --with apscheduler python3 scheduler.py`) et les unités systemd
`scripts/systemd/vigie24-report.{service,timer}` (`OnUnitActiveSec=3d`).

**Secrets — jamais versionnés.** Le jeton du bot et le chat_id se lisent dans les
variables d'environnement `VIGIE24_TELEGRAM_BOT_TOKEN` et `VIGIE24_TELEGRAM_CHAT_ID`
(gabarit : `.env.example`). Sur le conteneur homelab, ils vivent dans
`/opt/data/vigie24_telegram.env` (0600, hors dépôt), chargé par le wrapper et par
les unités systemd via `EnvironmentFile`.

**Commandes**

```bash
python3 run_pipeline.py                        # pipeline complet + envoi Telegram
python3 run_pipeline.py --dry-run              # pipeline sans envoi, état non modifié
python3 run_pipeline.py --send-report report.md   # test manuel de l'envoi
python3 run_pipeline.py --send-error "message"    # test manuel de l'alerte
python3 -m src.telegram --send-report report.md   # variante directe
```

**Comportement.** Chaque exécution produit `report_AAAA-MM-JJ_HHMM.md` (gitignoré),
journalise dans `logs/pipeline.log` (gitignoré) et met à jour `state/seen_sources.json`
(une source déjà signalée n'est jamais re-proposée). En cas d'échec d'une étape, une
alerte `⚠️ Vigie24 — …` part sur Telegram et le script sort en erreur (1 = discovery/
report, 3 = envoi) — **le planificateur n'est jamais bloqué** (code de sortie non nul
journalisé, prochain déclenchement inchangé).

**Tests Python** (hors réseau) : `python3 -m unittest discover -s tests -p "test_*.py"`

## 🧪 Tests

```bash
dotnet test tests/Vigie24.Tests --filter "FullyQualifiedName!~Integration"
```
218 tests unitaires/fonctionnels (les tests d'intégration requièrent un conteneur PostgreSQL — voir `docs/testing.fr.md`).

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
