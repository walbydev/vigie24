# Vigie24 — Documentation

> English is the default documentation language (`*.md`). French versions are provided as `*.fr.md`.

## Guides

| Topic | English | Français |
|---|---|---|
| Development | [development.md](development.md) | [development.fr.md](development.fr.md) |
| Debugging | [debugging.md](debugging.md) | [debugging.fr.md](debugging.fr.md) |
| Environments | [environments.md](environments.md) | [environments.fr.md](environments.fr.md) |
| Deployment | [deployment.md](deployment.md) | [deployment.fr.md](deployment.fr.md) |
| Production | [production.md](production.md) | [production.fr.md](production.fr.md) |
| Scripts | [scripts.md](scripts.md) | [scripts.fr.md](scripts.fr.md) |
| Testing | [testing.md](testing.md) | [testing.fr.md](testing.fr.md) |
| Releases | [releases.md](releases.md) | [releases.fr.md](releases.fr.md) |
| Architecture | [architecture.md](architecture.md) | [architecture.fr.md](architecture.fr.md) |
| Algorithms | [algorithmes.md](algorithmes.md) | [algorithmes.fr.md](algorithmes.fr.md) |

## Design documents (specs)

| Topic | English | Français |
|---|---|---|
| Overview / specification | [design/overview.md](design/overview.md) | [design/overview.fr.md](design/overview.fr.md) |
| Database schema | [design/database-schema.md](design/database-schema.md) | [design/database-schema.fr.md](design/database-schema.fr.md) |
| Visual mockup | [design/mockup.md](design/mockup.md) | [design/mockup.fr.md](design/mockup.fr.md) |
| Sources list | [design/sources.md](design/sources.md) | [design/sources.fr.md](design/sources.fr.md) |

## Conventions

- **Language**: code identifiers, comments, logs and docs in English; UI copy, public URLs, AI prompts and mock data in French (product language).
- **Config**: non-sensitive config in `appsettings*.json` (versioned); secrets in `environments/<env>/.env` (never committed, `.env.example` templates versioned).
- **Release**: Gitea is the primary source; GitHub receives major releases only (`make release VERSION=x.y.z`) — see [releases.md](releases.md).
