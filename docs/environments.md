# Vigie24 - Environment Configuration

This document describes the 4 environments available for the Vigie24 project.

## 📋 Environments Overview

| Environment | Description | AI | Web Port | DB Port |
|-------------|-------------|-----|----------|---------|
| **Development** | Active development with real AI APIs | ✅ Real | 5000 | 5432 |
| **Mock** | Testing & demos without API calls | ❌ Mocked | 5001 | 5433 |
| **Staging** | Pre-production (Debian server `<STAGING_HOST>`) | ✅ Real | 80 | internal |
| **Production** | VPS deployment (future) | ✅ Real | 443 (HTTPS) | internal |

---

## 🔧 1. DEVELOPMENT Environment

### Purpose
Active development with the real AI APIs (Infomaniak/DeepSeek) and full debugging.

### Configuration files
- `environments/development/.env` - Environment variables (secrets, gitignored; template: `environments/development/.env.example`)
- `environments/development/appsettings.Development.json` - .NET configuration
- `environments/development/docker-compose.yml` - Docker configuration

### Launch
```bash
make dev-run      # background
make dev-debug    # foreground (live logs)
```

### Characteristics
- **AI**: Real (Infomaniak or DeepSeek)
- **Logging**: Debug (very verbose)
- **RSS polling**: 5 minutes
- **AI temperature**: 0.7
- **Web port**: http://localhost:5000
- **PostgreSQL port**: localhost:5432

---

## 🧪 2. MOCK Environment

### Purpose
Testing & demos with no AI API calls — generates fake articles (Bogus).

### Configuration files
- `environments/mock/.env` - Environment variables (no secrets required)
- `environments/mock/appsettings.Mock.json` - .NET configuration
- `environments/mock/docker-compose.yml` - Docker configuration

### Launch
```bash
make mock-run      # background
make mock-debug    # foreground (live logs)
```

### Characteristics
- **AI**: Mocked (Bogus) — no network calls
- **Logging**: Information
- **RSS polling**: 15 minutes
- **AI temperature**: 0.5
- **Web port**: http://localhost:5001
- **PostgreSQL port**: localhost:5433

---

## 🟡 3. STAGING Environment (pre-production)

### Purpose
Pre-production on a dedicated server on the local network (Debian, `<STAGING_HOST>`) — validates the full Docker deployment before production. Real AI, isolated database.

### Configuration files
- `environments/staging/.env` - Environment variables (secrets, gitignored; template: `environments/staging/.env.example`)
- `environments/staging/appsettings.Staging.json` - .NET configuration
- `environments/staging/docker-compose.yml` - Docker configuration

### Publish
```bash
make staging-pub   # publish the solution + build + start Docker on <STAGING_HOST>
```

Prerequisites: deployment account (`vigie24deploy`) created on the server, SSH key
installed, SSH alias `vigie24-staging` in `~/.ssh/config` or `Makefile.env`
(see `docs/plan` or README).

### Operations
```bash
make staging-ps      # container status
make staging-logs    # live logs
make staging-down    # stop the stack
```

### Characteristics
- **AI**: Real (Infomaniak)
- **Logging**: Information
- **RSS polling**: 15 minutes
- **AI temperature**: 0.4
- **URL**: http://<STAGING_HOST> (port 80 → 8080 internal)
- **PostgreSQL port**: internal (Docker network only)
- **Database**: `vigie24_staging` (dedicated volume)

---

## 🔴 4. PRODUCTION Environment

### Purpose
Final deployment on a VPS (future), HTTPS.

### Configuration files
- `environments/production/.env` - Environment variables (secrets, gitignored; template: `environments/production/.env.example`)
- `environments/production/appsettings.Production.json` - .NET configuration
- `environments/production/docker-compose.yml` - Docker configuration

### Characteristics
- **AI**: Real (Infomaniak)
- **Logging**: Warning (minimal)
- **RSS polling**: 15 minutes
- **AI temperature**: 0.3
- **Web port**: 443 (HTTPS, via reverse proxy)
- **PostgreSQL port**: internal
