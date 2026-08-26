# Release Process

## Model

- **Gitea is the primary source** (`origin`): every commit is pushed there, continuously.
- **GitHub hosts the public releases** (remote `github`): `master` + a semver tag are pushed to GitHub at release time — never on a daily basis.

## Versioning (SemVer)

| Increment | When | Pushed to |
|---|---|---|
| **Major** (`X.0.0`) | New major features or breaking changes | Gitea **and** GitHub |
| **Minor** (`0.Y.0`) | New features, backward compatible | Gitea **and** GitHub (on request) |
| **Patch** (`0.0.Z`) | Bug fixes | Gitea **and** GitHub (on request) |

> Daily commits stay on Gitea only. Every tagged release so far (v1.0.0, v1.0.1, v1.1.0) has also been published on GitHub.

## Releasing a version

```bash
# 1. Make sure master is up to date on Gitea
git push origin master

# 2. Tag + push to Gitea and GitHub (one command)
make release VERSION=1.1.0
```

The `release` Makefile target:
1. Fails if the working tree is not clean or `VERSION` is empty
2. Creates an annotated tag `v<VERSION>`
3. Pushes `master` + the **new tag only** to `origin` (Gitea) — old tags are never re-pushed (avoids "already exists" rejections)
4. Pushes `master` + the **new tag only** to `github` (GitHub)

### Release notes (FR + EN)

`make release` only creates tags. After the push, create the GitHub release notes
via the REST API (token: `COPILOT_GITHUB_TOKEN` in `/opt/data/.env`, scope
Contents:write, `Authorization: Bearer`):

```bash
curl -s -X POST -H "Authorization: Bearer $COPILOT_GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  https://api.github.com/repos/walbydev/vigie24/releases \
  -d '{"tag_name":"v1.1.0","name":"Vigie24 v1.1.0","body":"## 🇫🇷 …\n\n## 🇬🇧 …","draft":false,"prerelease":false}'
```

Body = 🇫🇷 section (Nouveautés / Améliorations / Corrections…) then 🇬🇧
(Highlights / Improvements / Fixes), written from
`git log --oneline <previous tag>..<tag>`. Releases attach to an existing tag →
they can be created or edited post-hoc without re-tagging. Verify afterwards:
`GET /releases/tags/v1.1.0` and check the FR/EN markers in the body.

## Checklist before a release

- [ ] `dotnet build Vigie24.slnx -c Debug` → 0 errors
- [ ] `dotnet test tests/Vigie24.Tests --filter "FullyQualifiedName!~Integration"` → all green
- [ ] Staging deployment validated (`make staging-pub` + site check)
- [ ] Public URLs unchanged (no SEO breakage)
- [ ] No secrets in the repo (`git ls-files | grep .env` → only `*.example`)
- [ ] Docs updated (README, docs/) and translated

## Future: CI on Gitea (optional)

A future Gitea Actions pipeline could run build + tests on every push and/or deploy to production. It is intentionally **out of scope** for now: deployment remains Makefile-driven (`make staging-pub` / `make prod-pub`). Any CI would live on Gitea (primary source), never on GitHub.
