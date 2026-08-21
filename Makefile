# =====================================================================
# Vigie24 — Makefile
# Lancement des environnements dev et mock (modes run / debug)
#
# Usage :
#   make dev-run        Démarre l'environnement dev  (IA réelles, port 5000) en arrière-plan
#   make dev-debug      Démarre l'environnement dev  en avant-plan (logs live, Ctrl+C)
#   make mock-run       Démarre l'environnement mock (IA simulées, port 5001) en arrière-plan
#   make mock-debug     Démarre l'environnement mock en avant-plan (logs live, Ctrl+C)
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

.PHONY: help run debug stop down logs ps check-env \
        dev-run dev-debug mock-run mock-debug

help:
	@echo "Vigie24 — cibles disponibles :"
	@echo "  make dev-run       Démarre l'environnement dev  (IA réelles, port 5000) en arrière-plan"
	@echo "  make dev-debug     Démarre l'environnement dev  en avant-plan avec logs live (Ctrl+C)"
	@echo "  make mock-run      Démarre l'environnement mock (IA simulées, port 5001) en arrière-plan"
	@echo "  make mock-debug    Démarre l'environnement mock en avant-plan avec logs live (Ctrl+C)"
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
