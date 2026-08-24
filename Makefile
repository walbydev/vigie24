# =====================================================================
# Vigie24 — Makefile
# Local environments (dev, mock) and deployment (staging, production)
#
# Usage:
#   make dev-run        Start the dev environment (real AI, port 5000) in the background
#   make dev-debug      Start the dev environment in the foreground (live logs, Ctrl+C)
#   make mock-run       Start the mock environment (simulated AI, port 5001) in the background
#   make mock-debug     Start the mock environment in the foreground (live logs, Ctrl+C)
#   make staging-pub    Deploy to the staging server (SSH alias, see Makefile.env)
#   make staging-ps     Container status on staging
#   make staging-logs   Follow the staging stack logs
#   make staging-down   Stop the staging stack
#   make run ENV=dev    Parameterized form: ENV = dev | mock (default: dev)
#   make debug ENV=mock Parameterized form: foreground launch
#   make stop ENV=dev   Stop the ENV stack (containers kept)
#   make down ENV=dev   Stop and remove containers + network of the ENV stack
#   make logs ENV=dev   Follow the logs of the ENV stack
#   make ps ENV=dev     Container status of the ENV stack
#   make release VERSION=x.y.z  Tag and push a major release (Gitea + GitHub)
#   make help           Show this help
# =====================================================================

SHELL := /bin/bash
ENV   ?= dev

# Map the legacy ENV name (dev) to the environments/ directory layout
ENV_DIR  = $(if $(filter dev,$(ENV)),development,$(ENV))
ENV_PATH = environments/$(ENV_DIR)
COMPOSE  = cd $(ENV_PATH) && docker compose -f docker-compose.yml --env-file .env

# Web URLs per environment
URL_dev  := http://localhost:5000
URL_mock := http://localhost:5001

# --- Deployment hosts (override in Makefile.env, gitignored) ---
# STAGING_HOST / PROD_HOST : SSH aliases defined in ~/.ssh/config
# STAGING_DIR  / PROD_DIR  : deployment directory on the remote host
-include Makefile.env
STAGING_HOST ?= vigie24-staging
STAGING_DIR  ?= vigie24
PROD_HOST    ?= vigie24-prod
PROD_DIR     ?= vigie24

STAGING_STACK = $(STAGING_DIR)/environments/staging
PROD_STACK    = $(PROD_DIR)/environments/production

.PHONY: help run debug stop down logs ps check-env \
        dev-run dev-debug mock-run mock-debug \
        staging-pub staging-ps staging-logs staging-down staging-status \
        prod-pub prod-ps prod-logs prod-down prod-status prod-backup prod-sync \
        staging-worker-pause staging-worker-resume \
        prod-worker-pause prod-worker-resume \
        release

help:
	@echo "Vigie24 — available targets:"
	@echo "  make dev-run       Start the dev environment (real AI, port 5000) in the background"
	@echo "  make dev-debug     Start the dev environment in the foreground, live logs (Ctrl+C)"
	@echo "  make mock-run      Start the mock environment (simulated AI, port 5001) in the background"
	@echo "  make mock-debug    Start the mock environment in the foreground, live logs (Ctrl+C)"
	@echo "  make staging-pub   Deploy to the staging server (SSH alias in Makefile.env)"
	@echo "  make staging-ps    Container status on staging"
	@echo "  make staging-logs  Follow the staging stack logs"
	@echo "  make staging-down  Stop the staging stack"
	@echo "  make run ENV=dev   Start ENV (dev|mock) in the background"
	@echo "  make debug ENV=dev Start ENV in the foreground, live logs (Ctrl+C to stop)"
	@echo "  make stop ENV=dev  Stop the ENV stack without removing containers"
	@echo "  make down ENV=dev  Stop and remove containers/network of the ENV stack"
	@echo "  make logs ENV=dev  Follow the logs of the ENV stack"
	@echo "  make ps ENV=dev    Container status of the ENV stack"
	@echo "  make release VERSION=x.y.z  Tag and push a major release (Gitea + GitHub)"

# --- Combined targets: env + mode ---
dev-run:   ENV = dev
dev-run:   run

dev-debug: ENV = dev
dev-debug: debug

mock-run:  ENV = mock
mock-run:  run

mock-debug: ENV = mock
mock-debug: debug

# --- Environment validation ---
check-env:
	@case "$(ENV)" in dev|mock) ;; *) \
		echo "❌ Invalid environment: '$(ENV)' (expected: dev or mock)"; \
		exit 1 ;; \
	esac
	@test -f $(ENV_PATH)/docker-compose.yml || { \
		echo "❌ $(ENV_PATH)/docker-compose.yml not found"; \
		exit 1; \
	}
	@test -f $(ENV_PATH)/.env || { \
		echo "❌ $(ENV_PATH)/.env not found — copy $(ENV_PATH)/.env.example to $(ENV_PATH)/.env and fill in your secrets"; \
		exit 1; \
	}

# --- Launch modes ---
run: check-env
	$(COMPOSE) up -d --build
	@echo ""
	@echo "✅ Stack '$(ENV)' started — Web: $(URL_$(ENV))"
	@$(COMPOSE) ps

debug: check-env
	@echo "🔍 Debug mode '$(ENV)' — live logs (Ctrl+C to stop and clean up)"
	$(COMPOSE) up --build

stop: check-env
	$(COMPOSE) stop

down: check-env
	$(COMPOSE) down

logs: check-env
	$(COMPOSE) logs -f --tail=100

ps: check-env
	$(COMPOSE) ps

# --- Staging (pre-production) ---
# Pushes the solution to the Debian staging server, builds the Docker images
# on-site and starts the stack. Vigie24 is then reachable on the staging
# host (port 80).
staging-pub:
	@echo "🚀 Publishing to staging ($(STAGING_HOST))..."
	@ssh $(STAGING_HOST) "mkdir -p $(STAGING_DIR)"
	@tar czf - --exclude='.git' --exclude='bin' --exclude='obj' \
		--exclude='.idea' --exclude='.vscode' --exclude='.junie' \
		--exclude='*.bak' --exclude='*.local' --exclude='*.md~' \
		--exclude='.env*' --exclude='.hermes' --exclude='backups' \
		. | ssh $(STAGING_HOST) "tar xzf - -C $(STAGING_DIR)"
	@echo "🐳 Building images and starting containers on staging..."
	@ssh $(STAGING_HOST) "cd $(STAGING_STACK) && docker compose --env-file .env up -d --build"
	@echo ""
	@echo "✅ Staging deployed — Vigie24: http://$(STAGING_HOST)"
	@ssh $(STAGING_HOST) "cd $(STAGING_STACK) && docker compose --env-file .env ps --format 'table {{.Name}}\t{{.Status}}\t{{.Ports}}'"

staging-ps:
	@ssh $(STAGING_HOST) "cd $(STAGING_STACK) && docker compose --env-file .env ps --format 'table {{.Name}}\t{{.Status}}\t{{.Ports}}'"

staging-status: staging-ps

staging-logs:
	@ssh $(STAGING_HOST) "cd $(STAGING_STACK) && docker compose --env-file .env logs -f --tail=100"

staging-down:
	@ssh $(STAGING_HOST) "cd $(STAGING_STACK) && docker compose --env-file .env down"

# --- Production (VPS): full deployment (code + images + containers) ---
prod-pub:
	@echo "🚀 Publishing to production ($(PROD_HOST))..."
	@ssh $(PROD_HOST) "mkdir -p $(PROD_DIR)"
	@tar czf - --exclude='.git' --exclude='bin' --exclude='obj' \
		--exclude='.idea' --exclude='.vscode' --exclude='.junie' \
		--exclude='*.bak' --exclude='*.local' --exclude='*.md~' \
		--exclude='.env*' --exclude='.hermes' --exclude='backups' \
		. | ssh $(PROD_HOST) "tar xzf - -C $(PROD_DIR)"
	@echo "🐳 Building images and starting containers on the VPS..."
	@ssh $(PROD_HOST) "cd $(PROD_STACK) && docker compose --env-file .env up -d --build"
	@echo ""
	@ssh $(PROD_HOST) "echo '✅ Production deployed — access: ' && grep -E '^VIGIE24_DOMAIN=' $(PROD_STACK)/.env | cut -d= -f2"
	@ssh $(PROD_HOST) "cd $(PROD_STACK) && docker compose --env-file .env ps --format 'table {{.Name}}\t{{.Status}}\t{{.Ports}}'"

prod-ps:
	@ssh $(PROD_HOST) "cd $(PROD_STACK) && docker compose --env-file .env ps --format 'table {{.Name}}\t{{.Status}}\t{{.Ports}}'"

prod-status: prod-ps

prod-logs:
	@ssh $(PROD_HOST) "cd $(PROD_STACK) && docker compose --env-file .env logs -f --tail=100"

prod-down:
	@ssh $(PROD_HOST) "cd $(PROD_STACK) && docker compose --env-file .env down"

prod-backup:
	@mkdir -p backups
	@ssh $(PROD_HOST) "cd $(PROD_STACK) && docker compose --env-file .env exec -T postgres pg_dump -U postgres_prod -d vigie24_prod" > backups/vigie24-prod-$$(date +%Y%m%d-%H%M%S).sql
	@echo "✅ PostgreSQL backup saved in backups/"

# Syncs articles from staging to production (idempotent, no duplicates)
prod-sync:
	@python3 scripts/sync-articles-to-prod.py

# --- Staging: pause/resume the worker (AI token savings) ---
staging-worker-pause:
	@ssh $(STAGING_HOST) "cd $(STAGING_STACK) && docker compose --env-file .env stop vigie24-worker"
	@echo "⏸️ Staging worker stopped — no AI tokens spent. Resume: make staging-worker-resume"

staging-worker-resume:
	@ssh $(STAGING_HOST) "cd $(STAGING_STACK) && docker compose --env-file .env start vigie24-worker"
	@echo "▶️ Staging worker restarted."

# --- Production: pause/resume the worker (AI token savings) ---
prod-worker-pause:
	@ssh $(PROD_HOST) "cd $(PROD_STACK) && docker compose --env-file .env stop vigie24-worker"
	@echo "⏸️ Production worker stopped — no AI tokens spent. Resume: make prod-worker-resume"

prod-worker-resume:
	@ssh $(PROD_HOST) "cd $(PROD_STACK) && docker compose --env-file .env start vigie24-worker"
	@echo "▶️ Production worker restarted."

# --- Public release (Gitea = primary source, GitHub = major releases only) ---
# Auth: GITEA_TOKEN / GITHUB_TOKEN read from Makefile.env (gitignored) when set
release:
	@test -n "$(VERSION)" || { echo "❌ Usage: make release VERSION=x.y.z"; exit 1; }
	@test -z "$$(git status --porcelain)" || { echo "❌ Working tree is not clean — commit or stash first"; exit 1; }
	@git tag -a "v$(VERSION)" -m "Release v$(VERSION)"
	@if [ -n "$(GITEA_TOKEN)" ]; then git -c http.extraHeader="Authorization: token $(GITEA_TOKEN)" push origin master --tags; else git push origin master --tags; fi
	@if [ -n "$(GITHUB_TOKEN)" ]; then \
		GH_BASIC="$$(printf 'x-access-token:%s' "$(GITHUB_TOKEN)" | base64 | tr -d '\n')"; \
		git -c "http.extraHeader=Authorization: Basic $$GH_BASIC" push github master --tags; \
	else git push github master --tags; fi
	@echo "✅ Release v$(VERSION) pushed to Gitea (origin) and GitHub (github)"
