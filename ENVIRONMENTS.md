# Vigie24 - Configuration des Environnements

Ce document décrit les 3 environnements disponibles pour le projet Vigie24.

## 📋 Table des Environnements

| Environnement | Description | IA | Port Web | Port DB |
|--------------|-------------|-----|----------|---------|
| **Development** | Développement actif avec IA réelles | ✅ Réelles | 5000 | 5432 |
| **Mock** | Tests & démonstration sans APIs | ❌ Mockées | 5001 | 5433 |
| **Production** | Déploiement VPS (futur) | ✅ Réelles | 443 (HTTPS) | interne |

---

## 🔧 1. Environnement DEVELOPPEMENT

### Objectif
Développement actif avec les vraies APIs IA (Infomaniak/DeepSeek) et débogage complet.

### Fichiers de configuration
-  - Variables d'environnement
-  - Configuration .NET
-  - Configuration Docker

### Lancement


### Caractéristiques
- **IA**: Réelles (Infomaniak ou DeepSeek)
- **Logging**: Debug (très verbeux)
- **Polling RSS**: 5 minutes
- **Temperature IA**: 0.7
- **Port Web**: http://localhost:5000
- **Port PostgreSQL**: localhost:5432
