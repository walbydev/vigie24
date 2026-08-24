# Release Process

## Model

- **Gitea is the primary source** (`origin`): every commit is pushed there, continuously.
- **GitHub hosts public major releases only** (remote `github`): `master` + a semver tag are pushed to GitHub at release time — never on a daily basis.

## Versioning (SemVer)

| Increment | When | Pushed to |
|---|---|---|
| **Major** (`X.0.0`) | New major features or breaking changes | Gitea **and** GitHub |
| **Minor** (`0.Y.0`) | New features, backward compatible | Gitea only |
| **Patch** (`0.0.Z`) | Bug fixes | Gitea only |

## Releasing a major version

```bash
# 1. Make sure master is up to date on Gitea
git push origin master

# 2. Tag + push to Gitea and GitHub (one command)
make release VERSION=1.1.0
```

The `release` Makefile target:
1. Fails if the working tree is not clean or `VERSION` is empty
2. Creates an annotated tag `v<VERSION>`
3. Pushes `master` + tags to `origin` (Gitea)
4. Pushes `master` + tags to `github` (GitHub)

## Checklist before a release

- [ ] `dotnet build Vigie24.slnx -c Debug` → 0 errors
- [ ] `dotnet test tests/Vigie24.Tests --filter "FullyQualifiedName!~Integration"` → all green
- [ ] Staging deployment validated (`make staging-pub` + site check)
- [ ] Public URLs unchanged (no SEO breakage)
- [ ] No secrets in the repo (`git ls-files | grep .env` → only `*.example`)
- [ ] Docs updated (README, docs/) and translated

## Future: CI on Gitea (optional)

A future Gitea Actions pipeline could run build + tests on every push and/or deploy to production. It is intentionally **out of scope** for now: deployment remains Makefile-driven (`make staging-pub` / `make prod-pub`). Any CI would live on Gitea (primary source), never on GitHub.
