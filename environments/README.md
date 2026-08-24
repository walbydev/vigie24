# Environments

One self-contained folder per environment. Each folder contains everything needed to run that stack:

```
environments/
├── development/   # Real AI (Infomaniak/DeepSeek), port 5000
├── mock/          # Simulated AI (no API key), port 5001
├── staging/       # Pre-production on a Debian server, port 80
└── production/    # Public VPS, HTTPS via Caddy
```

## Per-folder contents

| File | Purpose |
|---|---|
| `docker-compose.yml` | The stack (postgres + worker + web, + caddy for production). Build context is the repo root (`../..`) |
| `docker-compose.debug.yml` | (development only) Debug override with diagnostic ports — `docker compose -f docker-compose.yml -f docker-compose.debug.yml up` |
| `.env.example` | **Template** of the secrets (API keys, DB credentials) — versioned in git |
| `.env` | **Real secrets** — gitignored, never committed, never transferred by deploys |
| `appsettings.<Env>.json` | Non-sensitive per-environment config overrides, mounted read-only into the containers |
| `Caddyfile` | (production only) Reverse proxy / HTTPS |

## Usage

All commands are wrapped by the Makefile at the repo root — you rarely need docker compose directly:

```bash
make dev-run        # development stack (background)
make dev-debug      # development stack, live logs
make mock-run       # mock stack (background)
make staging-pub    # deploy to staging
make prod-pub       # deploy to production
make run ENV=dev    # parameterized form (dev | mock)
```

Direct docker compose (from the environment folder):

```bash
cd environments/development
docker compose --env-file .env up -d --build
```

## Setup (first time)

```bash
# Copy the template and fill in your secrets (API keys, DB password)
cp environments/development/.env.example environments/development/.env
# non-sensitive config lives in appsettings*.json — no need to edit .env for it
```

Environment-specific non-sensitive overrides (`appsettings.<Env>.json`) are committed and volume-mounted; edit them and redeploy to apply.

> **Language note**: comments and docs in English; the product (UI, URLs, AI output) is French.
