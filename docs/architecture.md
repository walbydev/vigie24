# Vigie24 — Architecture

> Companion document: [algorithms.md](algorithms.md) describes every scoring and
> matching formula in detail. The database schema is in
> [design/database-schema.md](design/database-schema.md).

## 1. System overview

Vigie24 is an intelligent RSS monitoring system for French-speaking technology
news. It aggregates 240+ vetted sources (press, blogs, YouTube channels, Reddit
communities), analyzes each article with an LLM, publishes the best content on a
Blazor Server website, and exposes granular RSS feeds — all in French, the product
language.

```
┌────────────┐   ┌──────────────┐   ┌───────────┐   ┌──────────────┐
│  RSS/Atom  │──▶│              │──▶│           │──▶│  PostgreSQL  │
│  YouTube   │──▶│    Worker    │   │  AI APIs  │   │     16       │
│  Reddit    │──▶│ (15 min loop)│   │ Infomaniak│   └──────┬───────┘
└────────────┘   │              │   │ DeepSeek  │          │
                 └──────────────┘   └───────────┘          ▼
                                              ┌─────────────────────┐
                                              │  Web (Blazor Server)│
                                              │  + /feed RSS + sitemap
                                              └─────────────────────┘
```

## 2. Solution layout

| Project | Responsibility |
|---|---|
| `Vigie24.Domain` | Entities, scoring formulas, taxonomy, debate engine, RSS/sitemap builders, cost & health evaluation — **no infrastructure dependencies** |
| `Vigie24.Infrastructure` | EF Core + Npgsql, `DbContext`, migrations, queries (filtering, RSS service, cost reports) |
| `Vigie24.Services` | AI abstraction (`IAiService`), providers (Infomaniak, DeepSeek, mock), factory |
| `Vigie24.Web` | Blazor Server frontend (SSR prerender, SEO, filters, engagement endpoints) |
| `Vigie24.Worker` | `BackgroundService`: RSS polling + AI processing pipeline |
| `tests/Vigie24.Tests` | Unit, functional and integration tests |

## 3. Runtime components

### 3.1 Worker — the ingestion pipeline

A `BackgroundService` that runs every `FeedReader:PollingIntervalMinutes` (default
**15 min**, 5 min in production). Each cycle:

1. Loads active sources from the database (tracked query — `LastIngestedAt` must be persisted).
2. For each source, fetches its feed (RSS/Atom via `SyndicationFeed`, or YouTube/Reddit via their APIs), capped by `FeedReader:MaxItemsPerFeed` (default 15).
3. Processes each item: exact dedup by canonical URL → semantic dedup → AI analysis → persistence (see [algorithms.md](algorithms.md)).
4. Updates `LastIngestedAt`, then rebuilds the debates.

The worker is resilient: a failing source is logged and skipped, a failing article
is skipped, and embedding failures are non-blocking warnings.

### 3.2 Web — Blazor Server

ASP.NET Core Blazor Server with `ServerPrerendered` rendering (indexable initial
HTML). Key surfaces:

- **Pages**: `/` (article browser), `/categories`, `/themes`, `/debats`, `/debats/candidats`, `/articles/{id}` (reading mode), `/sources`, `/projet`, `/charte-graphique`, `/couts`, `/licence`.
- **RSS endpoints**: `/feed` (global) and `/categories/{slug}/feed` (per category) — built by `RssFeedService`, `Cache-Control: public, max-age=300`.
- **SEO**: dynamic `sitemap.xml`, `robots.txt`, Open Graph / Twitter cards, JSON-LD (WebSite, Organization, BreadcrumbList).
- **Engagement**: anonymous votes (0–5) and click tracking, persisted and blended into ranking.

### 3.3 Database — PostgreSQL 16

EF Core + Npgsql, tables created by migrations. Main aggregates: `articles`
(feeds + AI results + scores), `sources`, `categories`, `tags` + `article_tags`,
`article_embeddings` (semantic vectors), `debates` + `debate_articles`,
`authors`, engagement metrics and per-article token usage. Full schema:
[design/database-schema.md](design/database-schema.md).

## 4. Configuration model

Golden rule:

- **`appsettings.json` + `appsettings.{Env}.json`** — all non-sensitive configuration (polling, scores, models, timeouts, URLs). Versioned.
- **`environments/<env>/.env`** — secrets only (API keys, DB password) + compose substitution variables. Never committed; `.env.example` templates are versioned.
- Per-environment overrides are **volume-mounted** into the containers (no image rebuild required to change config).

Environments: **Development** (real AI, port 5000), **Mock** (simulated AI, tests), **Staging** (pre-production on a LAN host), **Production** (public VPS). Each lives in a self-contained `environments/<env>/` folder with its own `docker-compose.yml`.

## 5. AI provider abstraction

`IAiService` (in `Vigie24.Services`) defines: `ProcessArticleAsync` (summary,
editorial score, themes, categories, keywords, topic, stance, anxiety score),
`IsDuplicateAsync` (semantic dedup), `GetEmbeddingAsync` (multilingual vector),
`IsHealthyAsync`. The factory selects the provider from `AiService:Provider`:

| Provider | Role | Model |
|---|---|---|
| Infomaniak (primary) | OpenAI-compatible, Swiss datacenters | `google/gemma-4-31B-it` |
| DeepSeek | Fallback | deepseek model |
| Mock | Tests / mock environment | — |

Embeddings: `bge-multilingual-gemma2`, 3584 dimensions (via the primary provider).

## 6. Deployment model

- **Staging / Production**: Docker Compose stacks (postgres + worker + web) deployed with `make staging-pub` / `make prod-pub` — the repo is packed with `tar` (excluding `.git`, `bin`, `obj`, `.env*`) and streamed over SSH; remote files are **not** deleted (renamed/removed files must be cleaned manually).
- **Web**: Kestrel serves HTTP directly (staging), Caddy reverse proxy with TLS on production.
- **Database**: PostgreSQL 16 container, volume-persisted; `make prod-backup` for dumps.

## 7. Testing strategy

- Unit tests for Domain logic (scoring, taxonomy, debate engine, cost, health) — no infrastructure.
- Functional tests for the web layer.
- Integration tests requiring Docker (excluded from the default run via `--filter "FullyQualifiedName!~Integration"`).
- EF queries are tested with a directly-constructed `InMemory` context (Npgsql + InMemory in one DI container are incompatible).

## 8. Public surface & release process

- **Repo**: Gitea is the primary source (all commits); GitHub (`walbydev/vigie24`) receives major releases via `make release VERSION=x.y.z` (annotated tag pushed to both remotes).
- **Website**: vigie24.info (production), 192.168.30.160 (staging).
- **Contact**: contact@vigie24.info.
