# Processus de release

## Modèle

- **Gitea est la source principale** (`origin`) : chaque commit y est poussé en continu.
- **GitHub n'héberge que les versions majeures publiques** (remote `github`) : `master` + un tag semver y sont poussés au moment de la release — jamais au quotidien.

## Versionnage (SemVer)

| Incrément | Quand | Poussé vers |
|---|---|---|
| **Majeure** (`X.0.0`) | Nouvelles fonctionnalités majeures ou breaking changes | Gitea **et** GitHub |
| **Mineure** (`0.Y.0`) | Nouvelles fonctionnalités, rétro-compatibles | Gitea uniquement |
| **Correctif** (`0.0.Z`) | Corrections de bugs | Gitea uniquement |

## Publier une version majeure

```bash
# 1. S'assurer que master est à jour sur Gitea
git push origin master

# 2. Tag + push vers Gitea et GitHub (une seule commande)
make release VERSION=1.1.0
```

La cible `release` du Makefile :
1. Échoue si l'arbre de travail n'est pas propre ou si `VERSION` est vide
2. Crée un tag annoté `v<VERSION>`
3. Pousse `master` + tags vers `origin` (Gitea)
4. Pousse `master` + tags vers `github` (GitHub)

## Liste de contrôle avant une release

- [ ] `dotnet build Vigie24.slnx -c Debug` → 0 erreur
- [ ] `dotnet test tests/Vigie24.Tests --filter "FullyQualifiedName!~Integration"` → tout vert
- [ ] Déploiement staging validé (`make staging-pub` + vérification du site)
- [ ] URLs publiques inchangées (pas de rupture SEO)
- [ ] Aucun secret dans le dépôt (`git ls-files | grep .env` → uniquement `*.example`)
- [ ] Docs à jour (README, docs/) et traduites

## Futur : CI sur Gitea (optionnel)

Un futur pipeline Gitea Actions pourrait exécuter build + tests à chaque push et/ou déployer vers la production. C'est volontairement **hors périmètre** pour l'instant : le déploiement reste piloté par le Makefile (`make staging-pub` / `make prod-pub`). Toute CI vivrait sur Gitea (source principale), jamais sur GitHub.
