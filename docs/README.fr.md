# Vigie24 — Documentation

> L'anglais est la langue par défaut de la documentation (`*.md`). Les versions françaises sont fournies en `*.fr.md`.

## Guides

| Sujet | English | Français |
|---|---|---|
| Développement | [development.md](development.md) | [development.fr.md](development.fr.md) |
| Débogage | [debugging.md](debugging.md) | [debugging.fr.md](debugging.fr.md) |
| Environnements | [environments.md](environments.md) | [environments.fr.md](environments.fr.md) |
| Déploiement | [deployment.md](deployment.md) | [deployment.fr.md](deployment.fr.md) |
| Production | [production.md](production.md) | [production.fr.md](production.fr.md) |
| Scripts | [scripts.md](scripts.md) | [scripts.fr.md](scripts.fr.md) |
| Tests | [testing.md](testing.md) | [testing.fr.md](testing.fr.md) |
| Releases | [releases.md](releases.md) | [releases.fr.md](releases.fr.md) |
| Architecture | [architecture.md](architecture.md) | [architecture.fr.md](architecture.fr.md) |
| Algorithmes | [algorithmes.md](algorithmes.md) | [algorithmes.fr.md](algorithmes.fr.md) |

## Documents de design (spécifications)

| Sujet | English | Français |
|---|---|---|
| Vue d'ensemble / spécification | [design/overview.md](design/overview.md) | [design/overview.fr.md](design/overview.fr.md) |
| Schéma de base de données | [design/database-schema.md](design/database-schema.md) | [design/database-schema.fr.md](design/database-schema.fr.md) |
| Mockup visuel | [design/mockup.md](design/mockup.md) | [design/mockup.fr.md](design/mockup.fr.md) |
| Liste des sources | [design/sources.md](design/sources.md) | [design/sources.fr.md](design/sources.fr.md) |

## Conventions

- **Langue** : identifiants, commentaires, logs et docs du code en anglais ; textes d'interface, URLs publiques, prompts IA et données mock en français (langue du produit).
- **Configuration** : config non sensible dans `appsettings*.json` (versionné) ; secrets dans `environments/<env>/.env` (jamais commités, gabarits `.env.example` versionnés).
- **Release** : Gitea est la source principale ; GitHub ne reçoit que les versions majeures (`make release VERSION=x.y.z`) — voir [releases.fr.md](releases.fr.md).
