# Environnements

Un dossier auto-contenu par environnement. Chaque dossier contient tout ce qui est nécessaire pour exécuter sa stack :

```
environments/
├── development/   # IA réelles (Infomaniak/DeepSeek), port 5000
├── mock/          # IA simulées (aucune clé API), port 5001
├── staging/       # Préproduction sur serveur Debian, port 80
└── production/    # VPS public, HTTPS via Caddy
```

## Contenu de chaque dossier

| Fichier | Rôle |
|---|---|
| `docker-compose.yml` | La stack (postgres + worker + web, + caddy en production). Le contexte de build est la racine du dépôt (`../..`) |
| `docker-compose.debug.yml` | (development uniquement) Override de debug avec ports de diagnostic — `docker compose -f docker-compose.yml -f docker-compose.debug.yml up` |
| `.env.example` | **Gabarit** des secrets (clés API, identifiants DB) — versionné dans git |
| `.env` | **Vrais secrets** — gitignoré, jamais commité, jamais transféré par les déploiements |
| `appsettings.<Env>.json` | Surcharges de configuration non sensibles par environnement, montées en lecture seule dans les conteneurs |
| `Caddyfile` | (production uniquement) Reverse proxy / HTTPS |

## Utilisation

Toutes les commandes sont encapsulées par le Makefile à la racine du dépôt — vous n'avez rarement besoin de docker compose directement :

```bash
make dev-run        # stack development (arrière-plan)
make dev-debug      # stack development, logs live
make mock-run       # stack mock (arrière-plan)
make staging-pub    # déploiement staging
make prod-pub       # déploiement production
make run ENV=dev    # forme paramétrée (dev | mock)
```

Docker compose direct (depuis le dossier de l'environnement) :

```bash
cd environments/development
docker compose --env-file .env up -d --build
```

## Configuration (première fois)

```bash
# Copier le gabarit et renseigner vos secrets (clés API, mot de passe DB)
cp environments/development/.env.example environments/development/.env
# la config non sensible vit dans appsettings*.json — pas besoin d'éditer .env pour ça
```

Les surcharges non sensibles par environnement (`appsettings.<Env>.json`) sont commitées et montées en volume ; modifiez-les puis redéployez pour appliquer.

> **Note de langue** : commentaires et docs en anglais ; le produit (UI, URLs, sorties IA) en français.
