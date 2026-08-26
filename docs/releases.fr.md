# Processus de release

## Modèle

- **Gitea est la source principale** (`origin`) : chaque commit y est poussé en continu.
- **GitHub héberge les versions publiques** (remote `github`) : `master` + un tag semver y sont poussés au moment de la release — jamais au quotidien.

## Versionnage (SemVer)

| Incrément | Quand | Poussé vers |
|---|---|---|
| **Majeure** (`X.0.0`) | Nouvelles fonctionnalités majeures ou breaking changes | Gitea **et** GitHub |
| **Mineure** (`0.Y.0`) | Nouvelles fonctionnalités, rétro-compatibles | Gitea **et** GitHub (sur demande) |
| **Correctif** (`0.0.Z`) | Corrections de bugs | Gitea **et** GitHub (sur demande) |

> Les commits quotidiens restent sur Gitea uniquement. Chaque version taguée à ce jour
> (v1.0.0, v1.0.1, v1.1.0) a aussi été publiée sur GitHub.

## Publier une version

```bash
# 1. S'assurer que master est à jour sur Gitea
git push origin master

# 2. Tag + push vers Gitea et GitHub (une seule commande)
make release VERSION=1.1.0
```

La cible `release` du Makefile :
1. Échoue si l'arbre de travail n'est pas propre ou si `VERSION` est vide
2. Crée un tag annoté `v<VERSION>`
3. Pousse `master` + **uniquement le nouveau tag** vers `origin` (Gitea) — les anciens
   tags ne sont jamais re-poussés (évite les rejets « already exists »)
4. Pousse `master` + **uniquement le nouveau tag** vers `github` (GitHub)

### Notes de release (FR + EN)

`make release` ne crée que des tags. Après le push, créer les notes de release GitHub
via l'API REST (token : `COPILOT_GITHUB_TOKEN` dans `/opt/data/.env`, scope
Contents:write, `Authorization: Bearer`) :

```bash
curl -s -X POST -H "Authorization: Bearer $COPILOT_GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  https://api.github.com/repos/walbydev/vigie24/releases \
  -d '{"tag_name":"v1.1.0","name":"Vigie24 v1.1.0","body":"## 🇫🇷 …\n\n## 🇬🇧 …","draft":false,"prerelease":false}'
```

Body = section 🇫🇷 (Nouveautés / Améliorations / Corrections…) puis 🇬🇧
(Highlights / Improvements / Fixes), rédigée depuis
`git log --oneline <tag précédent>..<tag>`. Les releases s'attachent à un tag
existant → créables/modifiables POST-HOC sans re-tagger. Vérifier ensuite :
`GET /releases/tags/v1.1.0` et contrôler la présence des marqueurs FR/EN dans le body.

## Liste de contrôle avant une release

- [ ] `dotnet build Vigie24.slnx -c Debug` → 0 erreur
- [ ] `dotnet test tests/Vigie24.Tests --filter "FullyQualifiedName!~Integration"` → tout vert
- [ ] Déploiement staging validé (`make staging-pub` + vérification du site)
- [ ] URLs publiques inchangées (pas de rupture SEO)
- [ ] Aucun secret dans le dépôt (`git ls-files | grep .env` → uniquement `*.example`)
- [ ] Docs à jour (README, docs/) et traduites

## Futur : CI sur Gitea (optionnel)

Un futur pipeline Gitea Actions pourrait exécuter build + tests à chaque push et/ou déployer vers la production. C'est volontairement **hors périmètre** pour l'instant : le déploiement reste piloté par le Makefile (`make staging-pub` / `make prod-pub`). Toute CI vivrait sur Gitea (source principale), jamais sur GitHub.
