# =====================================================================
# Vigie24 — Makefile
# Lancement des environnements dev, mock et publication staging
#
# Usage :
#   make dev-run        Démarre l'environnement dev  (IA réelles, port 5000) en arrière-plan
#   make dev-debug      Démarre l'environnement dev  en avant-plan (logs live, Ctrl+C)
#   make mock-run       Démarre l'environnement mock (IA simulées, port 5001) en arrière-plan
#   make mock-debug     Démarre l'environnement mock en avant-plan (logs live, Ctrl+C)
#   make staging-pub    Publie la solution sur le serveur de staging (192.168.30.160, port 80)
#   make staging-ps     État des conteneurs sur le staging
#   make staging-logs   Suit les logs de la stack staging
#   make staging-down   Arrête la stack staging
#   make run ENV=dev    Forme paramétrée : ENV = dev | mock (défaut : dev)
#   make debug ENV=mock Forme paramétrée : lancement en avant-plan
#   make stop ENV=dev   Arrête la stack ENV (conteneurs conservés)
#   make down ENV=dev   Arrête et supprime conteneurs + réseau de la stack ENV
#   make logs ENV=dev   Suit les logs de la stack ENV
#   make ps ENV=dev     État des conteneurs de la stack ENV
#   make help           Affiche cette aide
# =====================================================================

SHELL := /bin/bash
ENV   ?= dev
COMPOSE = docker compose -f docker-compose.$(ENV).yml

# URLs web selon l'environnement
URL_dev  := http://localhost:5000
URL_mock := http://localhost:5001

# --- Staging (préproduction) : serveur Debian 192.168.30.160 ---
# STAGING_HOST : alias SSH défini dans ~/.ssh/config ; STAGING_DIR : dossier de déploiement (~/vigie24)
STAGING_HOST ?= vigie24-staging
STAGING_DIR ?= vigie24

# --- Production (VPS) ---
# PROD_HOST : alias SSH défini dans ~/.ssh/config (ex: vigie24-prod) ; PROD_DIR : dossier de déploiement
PROD_HOST ?= vigie24-prod
PROD_DIR ?= vigie24

.PHONY: help run debug stop down logs ps check-env \
        dev-run dev-debug mock-run mock-debug \
        staging-pub staging-ps staging-logs staging-down staging-status \
        prod-pub prod-ps prod-logs prod-down prod-status prod-backup

help:
	@echo "Vigie24 — cibles disponibles :"
	@echo "  make dev-run       Démarre l'environnement dev  (IA réelles, port 5000) en arrière-plan"
	@echo "  make dev-debug     Démarre l'environnement dev  en avant-plan avec logs live (Ctrl+C)"
	@echo "  make mock-run      Démarre l'environnement mock (IA simulées, port 5001) en arrière-plan"
	@echo "  make mock-debug    Démarre l'environnement mock en avant-plan avec logs live (Ctrl+C)"
	@echo "  make staging-pub   Publie la solution sur le staging (192.168.30.160, http://192.168.30.160)"
	@echo "  make staging-ps    État des conteneurs sur le staging"
	@echo "  make staging-logs  Suit les logs de la stack staging"
	@echo "  make staging-down  Arrête la stack staging"
	@echo "  make run ENV=dev   Démarre ENV (dev|mock) en arrière-plan"
	@echo "  make debug ENV=dev Démarre ENV en avant-plan, logs live (Ctrl+C pour arrêter)"
	@echo "  make stop ENV=dev  Arrête la stack ENV sans supprimer les conteneurs"
	@echo "  make down ENV=dev  Arrête et supprime conteneurs/réseau de la stack ENV"
	@echo "  make logs ENV=dev  Suit les logs de la stack ENV"
	@echo "  make ps ENV=dev    État des conteneurs de la stack ENV"

# --- Cibles combinées : env + mode ---
dev-run:   ENV = dev
dev-run:   run

dev-debug: ENV = dev
dev-debug: debug

mock-run:  ENV = mock
mock-run:  run

mock-debug: ENV = mock
mock-debug: debug

# --- Validation de l'environnement ---
check-env:
	@case "$(ENV)" in dev|mock) ;; *) \
		echo "❌ Environnement invalide : '$(ENV)' (attendu : dev ou mock)"; \
		exit 1 ;; \
	esac
	@test -f docker-compose.$(ENV).yml || { \
		echo "❌ Fichier docker-compose.$(ENV).yml introuvable à la racine"; \
		exit 1; \
	}

# --- Modes de lancement ---
run: check-env
	$(COMPOSE) up -d --build
	@echo ""
	@echo "✅ Stack '$(ENV)' démarrée — Web : $(URL_$(ENV))"
	@$(COMPOSE) ps

debug: check-env
	@echo "🔍 Mode debug '$(ENV)' — logs live (Ctrl+C pour arrêter et nettoyer)"
	$(COMPOSE) up --build

stop: check-env
	$(COMPOSE) stop

down: check-env
	$(COMPOSE) down

logs: check-env
	$(COMPOSE) logs -f --tail=100

ps: check-env
	$(COMPOSE) ps

# --- Staging (préproduction) ---
# Publie la solution sur le serveur Debian 192.168.30.160, construit les images
# Docker sur place et démarre la stack. Vigie24 est alors accessible sur
# http://192.168.30.160 (port 80).
staging-pub:
	@echo "🚀 Publication de la solution vers le staging ($(STAGING_HOST))..."
	@ssh $(STAGING_HOST) "mkdir -p $(STAGING_DIR)"
	@tar czf - --exclude='.git' --exclude='bin' --exclude='obj' \
		--exclude='.idea' --exclude='.vscode' --exclude='.junie' \
		--exclude='*.bak' --exclude='*.local' --exclude='*.md~' \
		--exclude='.env*' . | ssh $(STAGING_HOST) "tar xzf - -C $(STAGING_DIR)"
	@echo "🐳 Construction des images et démarrage des conteneurs sur le staging..."
	@ssh $(STAGING_HOST) "cd $(STAGING_DIR) && docker compose --env-file .env.staging -f docker-compose.staging.yml up -d --build"
	@echo ""
	@echo "✅ Staging déployé — Vigie24 : http://192.168.30.160"
	@ssh $(STAGING_HOST) "cd $(STAGING_DIR) && docker compose --env-file .env.staging -f docker-compose.staging.yml ps --format 'table {{.Name}}	{{.Status}}	{{.Ports}}'"

staging-ps:
	@ssh $(STAGING_HOST) "cd $(STAGING_DIR) && docker compose --env-file .env.staging -f docker-compose.staging.yml ps --format 'table {{.Name}}	{{.Status}}	{{.Ports}}'"

staging-status: staging-ps

staging-logs:
	@ssh $(STAGING_HOST) "cd $(STAGING_DIR) && docker compose --env-file .env.staging -f docker-compose.staging.yml logs -f --tail=100"

staging-down:
	@ssh $(STAGING_HOST) "cd $(STAGING_DIR) && docker compose --env-file .env.staging -f docker-compose.staging.yml down"

# --- Production (VPS) : déploiement complet (code + images + conteneurs) ---
prod-pub:
	@echo "🚀 Publication de la solution vers la production ($(PROD_HOST))..."
	@ssh $(PROD_HOST) "mkdir -p $(PROD_DIR)"
	@tar czf - --exclude='.git' --exclude='bin' --exclude='obj' \
		--exclude='.idea' --exclude='.vscode' --exclude='.junie' \
		--exclude='*.bak' --exclude='*.local' --exclude='*.md~' \
		--exclude='.env*' . | ssh $(PROD_HOST) "tar xzf - -C $(PROD_DIR)"
	@echo "🐳 Construction des images et démarrage des conteneurs sur le VPS..."
	@ssh $(PROD_HOST) "cd $(PROD_DIR) && docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build"
	@echo ""
	@echo "✅ Production déployée — accès : https://$$(ssh $(PROD_HOST) \"grep -E '^VIGIE24_DOMAIN=' $(PROD_DIR)/.env.production | cut -d= -f2\" 2>/dev/null)"
	@ssh $(PROD_HOST) "cd $(PROD_DIR) && docker compose --env-file .env.production -f docker-compose.prod.yml ps --format 'table {{.Name}}	{{.Status}}	{{.Ports}}'"

prod-ps:
	@ssh $(PROD_HOST) "cd $(PROD_DIR) && docker compose --env-file .env.production -f docker-compose.prod.yml ps --format 'table {{.Name}}	{{.Status}}	{{.Ports}}'"

prod-status: prod-ps

prod-logs:
	@ssh $(PROD_HOST) "cd $(PROD_DIR) && docker compose --env-file .env.production -f docker-compose.prod.yml logs -f --tail=100"

prod-down:
	@ssh $(PROD_HOST) "cd $(PROD_DIR) && docker compose --env-file .env.production -f docker-compose.prod.yml down"

prod-backup:
	@mkdir -p backups
	@ssh $(PROD_HOST) "cd $(PROD_DIR) && docker compose --env-file .env.production -f docker-compose.prod.yml exec -T postgres pg_dump -U postgres_prod -d vigie24_prod" > backups/vigie24-prod-$$(date +%Y%m%d-%H%M%S).sql
	@echo "✅ Sauvegarde PostgreSQL effectuée dans backups/"
