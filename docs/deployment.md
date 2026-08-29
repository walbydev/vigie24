# Deployment

Vigie24 deploys via Docker Compose, one self-contained stack per environment in `environments/<env>/`. Each stack builds the images on the target host from the repository root (`build.context: ../..`) and reads its secrets from `environments/<env>/.env` (never transferred by the deployment: the remote `.env` survives).

Deployment is Makefile-driven. Remote hosts are SSH aliases defined in `~/.ssh/config` (or in `Makefile.env`, gitignored — template in `Makefile.env.example`).

## Staging

```bash
make staging-pub
```

What it does:
1. `mkdir -p <STAGING_DIR>` on the staging host
2. `tar` the repository (excluding `.git`, `bin`, `obj`, `.env*`, backups, IDE folders) over SSH into `<STAGING_DIR>`
3. `cd <STAGING_DIR>/environments/staging && docker compose --env-file .env up -d --build`

Then:
```bash
make staging-ps     # container status
make staging-logs   # follow logs
make staging-down   # stop the stack
```

**Important**:
- The real `environments/staging/.env` on the server is **never overwritten** (the tar excludes `.env*`) — it must exist once (copy from `.env.example` and fill in the secrets).
- First migration to the `environments/` layout: move the legacy file once:
  `ssh <STAGING_HOST> "mkdir -p ~/vigie24/environments/staging && mv ~/vigie24/.env.staging ~/vigie24/environments/staging/.env"`
- The per-environment `appsettings.<Env>.json` is mounted as a read-only volume — update it and redeploy to apply changes.

## Production

```bash
make prod-pub
```

Same mechanics on the VPS (`<PROD_DIR>/environments/production`), plus the Caddy reverse proxy (automatic HTTPS via Let's Encrypt when `VIGIE24_DOMAIN` is a real domain; plain HTTP when it is an IP). Firewall must expose 80/443.

Useful targets: `make prod-ps`, `make prod-logs`, `make prod-down`, `make prod-backup` (PostgreSQL dump into `backups/`), `make prod-sync` (idempotent article sync staging → production), `make staging-worker-pause|resume`, `make prod-worker-pause|resume` (AI token savings).

## Static assets

CSS/JS live as readable sources in `wwwroot/css/` + `wwwroot/js/`; the site serves the minified `.min.*` copies (committed). Run `make assets` (uv + rcssmin/jsmin via `scripts/minify-assets.py`) after any CSS/JS edit — the Docker build on the server needs no tooling. Minified URLs carry `?v=` hashes (`asp-append-version`) and are served with `Cache-Control: public, max-age=31536000, immutable`.

## Database migrations

- `dotnet ef migrations add` uses `DesignTimeDbContextFactory` (Infrastructure) — the Web host is not built in design-time, so no `--startup-project` is needed.
- In production the **worker applies pending migrations at startup** ("Database migrated successfully"). On environments where the worker is paused (e.g. staging), apply the migration manually (ALTER TABLE + `__EFMigrationsHistory` insert with a copied `ProductVersion`).

## Deploy checklist

- [ ] Local build + tests green
- [ ] `make staging-pub` → `curl` the site (200)
- [ ] Logs clean (`docker compose logs` — no `A second operation` Blazor concurrency bursts; reload the page 3-4× before concluding)
- [ ] `libgssapi_krb5.so.2` warnings in container logs are benign (Kerberos probe) — check `RestartCount` and the site instead

## Known pitfall: tar deploys never delete removed files

The tar-based deploy overlays files but **never removes files that were deleted/renamed in git**. After a rename or delete in the repo, the stale file stays on the remote host and can break the build (e.g. a renamed `.razor` page caused an ambiguous-route 500). Fix once on the remote host:

```bash
ssh <STAGING_HOST> "find ~/vigie24 -name '<OldFileName>' -delete"
make staging-pub
```

## More deployment pitfalls

- **`header_log` replaces the logged headers** — in the Caddyfile, `header_log headers ...` overrides the default header set: always list `User-Agent` explicitly (the traffic report's bot filter relies on it).
- **`prod-pub` does not reload Caddy on Caddyfile-only changes** — if only the Caddyfile changed, the image is unchanged and `docker compose up` will not restart the container. Hot-reload instead: `docker exec vigie24-caddy-prod caddy reload --config /etc/caddy/Caddyfile`.
