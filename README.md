# Vigie24 — Intelligent French-speaking Tech Watch

**Vigie24** is an intelligent RSS monitoring system for French-speaking technology news. It aggregates feeds from 214 vetted sources (208 active — press, blogs, YouTube channels, Reddit communities), analyzes each article with an LLM (summary, editorial score, anxiety score, categories, themes, keywords, debate stances), detects duplicates and related stories, and publishes the best content on a Blazor Server website with granular RSS feeds.

🌐 **Website**: [vigie24.info](https://vigie24.info) — the product is **French by design** (UI copy, public URLs, AI-generated content)  
📡 **RSS feeds**: global `/feed` + per-category `/categories/{slug}/feed`  
📄 **License**: MIT

---

## ✨ Features

- **RSS aggregation** — 214 vetted sources (208 active) across 8 parent categories (AI, Development, Cybersecurity, Science, Hardware, Entertainment, Music, Visual Arts), French-speaking first (CH > FR > BE > QC > EU > World), open to international coverage
- **AI analysis** — per article: concise French summary, editorial score (0–100), anxiety score (0–10), category hierarchy, themes, keywords, debate topic + stance — with a **full analysis trace** (justification, model reasoning, exact LLM used)
- **AI transparency** — "Réflexion & analyse" block on every article page; AI Editor's Journal (`/journal`): explained rejections, weekly picks, categorization hesitations, notable reflections
- **Calmer reading** — Anxiety Barometer (`/barometre`): daily index, 30-day curve, per category/country, antidotes; zen RSS feed `/feed/zen`; global **Zen mode** in the navigation (hides anxiety-inducing articles site-wide)
- **Living dossiers** — `/debats/{id}`: stance timeline, coverage intensity, position evolution, media map, first coverage; per-source scoops on `/sources`
- **Daily pages** — Morning Briefing (`/briefing`), Hot Topics Radar (`/radar`), Community Top (`/top`), One Year Ago (`/il-y-a-un-an`), showcase homepage (masonry layers + article preview) and a dedicated Articles page
- **AI providers** — Infomaniak (primary, Switzerland-based) with DeepSeek as fallback, via a pluggable factory (`IAiService`)
- **Smart publishing** — articles are published when the editorial score ≥ 60; duplicates are detected semantically (embedding-based)
- **Debates** — articles covering the same topic with opposing stances are grouped into "debates" (`/debats`, `/debats/candidats`), powered by multilingual embeddings (bge-multilingual-gemma2, 3584 dims)
- **Engagement** — anonymous votes (0–5) and click tracking, blended into the ranking score
- **Source resilience** — only permanent failures (404/410, invalid XML, dead DNS) can disable a source; transient ones (429/403/5xx/timeout) never do; exponential backoff (5 min → 6 h), automatic reactivation after 24 h via a no-AI-cost probe
- **Security & performance** — hardened Caddy headers (CSP, HSTS, frame/nosniff policies), anti-abuse rate limits, secure cookies; minified CSS/JS assets with immutable 1-year cache (`make assets`)
- **Cost transparency** — `/couts` page with per-day/week charts of token consumption and CHF cost
- **SEO** — French URLs (`/articles`, `/categories`, `/themes`, `/debats`, `/projet`, `/charte-graphique`, `/couts`), XML sitemap, BreadcrumbList rich snippets, RSS feeds

## 🏗️ Architecture

| Component | Technology |
|---|---|
| Frontend | Blazor Server (ASP.NET Core, .NET 10) |
| Worker | `BackgroundService` — RSS polling every 15 min (5 min in production) + AI processing |
| Database | PostgreSQL 16 (Npgsql + EF Core 10) |
| AI | Infomaniak API (OpenAI-compatible) + DeepSeek fallback |
| Deployment | Docker Compose (one stack per environment) + Caddy reverse proxy (prod) |

```
src/
├── Vigie24.Domain/          # Entities, scoring, taxonomy, RSS/sitemap builders
├── Vigie24.Infrastructure/  # EF Core, PostgreSQL, queries, filters
├── Vigie24.Services/        # AI services (Infomaniak, DeepSeek, mock), factory
├── Vigie24.Web/             # Blazor Server UI
└── Vigie24.Worker/          # RSS polling + AI processing pipeline
tests/Vigie24.Tests/         # Unit, functional and integration tests
```

## 📂 Repository Layout

```
├── environments/<env>/      # One self-contained folder per environment (development,
│                            # mock, staging, production): docker-compose.yml,
│                            # .env.example (secrets template) + appsettings override
├── docs/                    # Documentation — English by default (*.md),
│                            # French versions as *.fr.md
├── scripts/                 # Dev/ops tools (SQL, shell, Python)
├── src/                     # Solution projects
├── tests/                   # Tests
├── Makefile                 # Dev + deployment entry point
└── Vigie24.slnx
```

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Docker + Docker Compose (for the containerized stacks)
- An AI provider API key (Infomaniak or DeepSeek)

### Option A — Docker Compose (recommended)

```bash
git clone https://github.com/walbydev/vigie24.git
cd vigie24

# Development environment (real AI, port 5000)
cp environments/development/.env.example environments/development/.env
# fill in your API keys in environments/development/.env
make dev-run

# Mock environment (simulated AI, no API key needed, port 5001)
cp environments/mock/.env.example environments/mock/.env
make mock-run
```

### Option B — Local .NET run (without Docker)

```bash
dotnet restore Vigie24.slnx
cd src/Vigie24.Worker
dotnet user-secrets set "AiService__ApiKey" "your-key"
dotnet user-secrets set "AiService__Provider" "infomaniak"
cd ../../src/Vigie24.Web
dotnet run
```

PostgreSQL must be reachable (see `docs/development.md`).

### Configuration golden rule
- **`appsettings.json`** (per project) + **`environments/<env>/appsettings.<Env>.json`** (volume-mounted overrides) → all non-sensitive config, versioned in git
- **`environments/<env>/.env`** → secrets only (API keys, DB passwords), never committed; `.env.example` templates are versioned
- All config is injected in containers via `env_file` + environment overrides — no secret is baked into images

## 📡 Automatic Telegram report (every 72 h)

The repository ships a **self-contained Python pipeline** that discovers new tech
sources (RSS, YouTube, Reddit, Bluesky) and sends Philippe a **10-20 source report,
mostly French-speaking**, to Telegram **every 3 days**.

```
src/discovery.py   multi-source collection -> candidates.jsonl (gitignored)
src/report.py      relevance filter + dedup + seen state -> Markdown report
src/telegram.py    Telegram sending: send_report() / send_error() (stdlib only)
run_pipeline.py    orchestrator: discovery -> report -> send (or alert)
scheduler.py       portable APScheduler scheduler (IntervalTrigger(days=3))
scripts/systemd/   sample systemd units (timer OnUnitActiveSec=3d)
```

**Scheduling method retained.** On the homelab container (no systemd), scheduling
is handled by the **Hermes scheduler** (cron `vigie24-rapport-3j`, every 72 h,
supervised by the `main-hermes` service, a robust equivalent of an
`IntervalTrigger(days=3)`) which runs the wrapper
`/opt/data/scripts/vigie24_report_3d.sh`. For a standalone host the repo provides
`scheduler.py` (APScheduler `IntervalTrigger(days=3)`, run with
`uv run --with apscheduler python3 scheduler.py`) and the sample systemd units
`scripts/systemd/vigie24-report.{service,timer}` (`OnUnitActiveSec=3d`).

**Secrets — never versioned.** Bot token and chat id are read from the
`VIGIE24_TELEGRAM_BOT_TOKEN` / `VIGIE24_TELEGRAM_CHAT_ID` environment variables
(template: `.env.example`). On the homelab container they live in
`/opt/data/vigie24_telegram.env` (0600, outside the repo), loaded by the wrapper
and by the systemd units via `EnvironmentFile`.

**Commands**

```bash
python3 run_pipeline.py                        # full pipeline + Telegram send
python3 run_pipeline.py --dry-run              # pipeline without sending, state untouched
python3 run_pipeline.py --send-report report.md   # manual send test
python3 run_pipeline.py --send-error "message"    # manual alert test
python3 -m src.telegram --send-report report.md   # direct variant
```

**Behavior.** Each run produces `report_YYYY-MM-DD_HHMM.md` (gitignored), logs to
`logs/pipeline.log` (gitignored) and updates `state/seen_sources.json` (an already
reported source is never proposed again). If a step fails, a `⚠️ Vigie24 — …` alert
goes to Telegram and the script exits non-zero (1 = discovery/report, 3 = send) —
**the scheduler is never blocked** (non-zero exit is logged, next trigger unchanged).

**Python tests** (offline): `python3 -m unittest discover -s tests -p "test_*.py"`

## 🧪 Tests

```bash
dotnet test tests/Vigie24.Tests --filter "FullyQualifiedName!~Integration"
```
218 unit/functional tests (integration tests require a PostgreSQL container — see `docs/testing.md`).

## 📖 Documentation

| Doc | English | Français |
|---|---|---|
| Development | `docs/development.md` | `docs/development.fr.md` |
| Debugging | `docs/debugging.md` | `docs/debugging.fr.md` |
| Environments | `docs/environments.md` | `docs/environments.fr.md` |
| Deployment | `docs/deployment.md` | `docs/deployment.fr.md` |
| Production | `docs/production.md` | `docs/production.fr.md` |
| Scripts | `docs/scripts.md` | `docs/scripts.fr.md` |
| Testing | `docs/testing.md` | `docs/testing.fr.md` |
| Releases | `docs/releases.md` | `docs/releases.fr.md` |
| Architecture | `docs/architecture.md` | `docs/architecture.fr.md` |
| Algorithms | `docs/algorithmes.md` | `docs/algorithmes.fr.md` |
| Design docs | `docs/design/*.md` | `docs/design/*.fr.md` |

## 🔄 Release Policy

- **Gitea is the primary source** (`origin`) — every commit lives there
- **GitHub hosts public major releases only** (`github` remote), via `make release VERSION=x.y.z` (semver tag pushed to both remotes)
- Daily commits are never pushed to GitHub manually

## 📝 License

MIT — see [LICENSE](LICENSE). Copyright (c) 2026 Walby Developments.

## 📞 Contact

- Author: Philippe Ryf — philippe@walby.dev
- Website: [vigie24.info](https://vigie24.info) — contact@vigie24.info

**Vigie24** — *La veille technologique, enfin claire.*
