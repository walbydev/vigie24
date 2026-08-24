# Vigie24 - Configuration des Environnements

Ce document décrit les 4 environnements disponibles pour le projet Vigie24.

## 📋 Table des Environnements

| Environnement | Description | IA | Port Web | Port DB |
|--------------|-------------|-----|----------|---------|
| **Development** | Développement actif avec IA réelles | ✅ Réelles | 5000 | 5432 |
| **Mock** | Tests & démonstration sans APIs | ❌ Mockées | 5001 | 5433 |
| **Staging** | Préproduction (serveur Debian <STAGING_HOST>) | ✅ Réelles | 80 | interne |
| **Production** | VPS public — [vigie24.info](https://vigie24.info) | ✅ Réelles | 443 (HTTPS) | interne |

---

## 🔧 1. Environnement DEVELOPPEMENT

### Objectif
Développement actif avec les vraies APIs IA (Infomaniak/DeepSeek) et débogage complet.

### Fichiers de configuration
- `environments/development/.env` - Variables d'environnement (secrets, gitignoré ; template : `environments/development/.env.example`)
- `appsettings.Development.json` - Configuration .NET
- `docker-compose.yml` - Configuration Docker

### Lancement
```bash
make dev-run      # arrière-plan
make dev-debug    # avant-plan (logs live)
```

### Caractéristiques
- **IA**: Réelles (Infomaniak ou DeepSeek)
- **Logging**: Debug (très verbeux)
- **Polling RSS**: 5 minutes
- **Temperature IA**: 0.7
- **Port Web**: http://localhost:5000
- **Port PostgreSQL**: localhost:5432

---

## 🧪 2. Environnement MOCK

### Objectif
Tests & démonstration sans aucun appel aux APIs IA — génération d'articles factices (MockAiService).

### Fichiers de configuration
- `environments/mock/.env` - Variables d'environnement (pas de secrets requis)
- `appsettings.Mock.json` - Configuration .NET
- `docker-compose.yml` - Configuration Docker

### Lancement
```bash
make mock-run      # arrière-plan
make mock-debug    # avant-plan (logs live)
```

### Caractéristiques
- **IA**: Mockées (MockAiService) — aucun appel réseau
- **Logging**: Information
- **Polling RSS**: 15 minutes
- **Temperature IA**: 0.5
- **Port Web**: http://localhost:5001
- **Port PostgreSQL**: localhost:5433

---

## 🟡 3. Environnement STAGING (préproduction)

### Objectif
Préproduction sur un serveur dédié du réseau local (Debian, <STAGING_HOST>) — valide le déploiement Docker complet avant la production. IA réelles, base de données isolée.

### Fichiers de configuration
- `environments/staging/.env` - Variables d'environnement (secrets, gitignoré ; template : `environments/staging/.env.example`)
- `appsettings.Staging.json` - Configuration .NET
- `docker-compose.yml` - Configuration Docker

### Publication
```bash
make staging-pub   # rsync de la solution + build + démarrage Docker sur <STAGING_HOST>
```

Prérequis : compte de déploiement (`vigie24deploy`) créé sur le serveur, clé SSH
installée, alias `vigie24-staging` dans `~/.ssh/config` (voir `docs/deployment.fr.md` ou le README racine).

### Opérations
```bash
make staging-ps      # état des conteneurs
make staging-logs    # logs live
make staging-down    # arrêt de la stack
```

### Caractéristiques
- **IA**: Réelles (Infomaniak)
- **Logging**: Information
- **Polling RSS**: 15 minutes
- **Temperature IA**: 0.4
- **URL**: http://<STAGING_HOST> (port 80 → 8080 interne)
- **Port PostgreSQL**: interne (réseau Docker uniquement)
- **Base**: `vigie24_staging` (volume dédié)

---

## 🔴 4. Environnement PRODUCTION

### Objectif
Déploiement final sur VPS public, HTTPS — le site vigie24.info est en production.

### Fichiers de configuration
- `environments/production/.env` - Variables d'environnement (secrets, gitignoré ; template : `environments/production/.env.example`)
- `appsettings.Production.json` - Configuration .NET
- `docker-compose.yml` - Configuration Docker

### Caractéristiques
- **IA**: Réelles (Infomaniak)
- **Logging**: Warning (sobre)
- **Polling RSS**: 15 minutes
- **Temperature IA**: 0.3
- **Port Web**: 443 (HTTPS, via reverse proxy)
- **Port PostgreSQL**: interne
