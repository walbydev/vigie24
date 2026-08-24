# Déploiement

Vigie24 se déploie via Docker Compose, une stack auto-contenue par environnement dans `environments/<env>/`. Chaque stack construit les images sur la machine cible depuis la racine du dépôt (`build.context: ../..`) et lit ses secrets dans `environments/<env>/.env` (jamais transféré par le déploiement : le `.env` distant survit).

Le déploiement est piloté par le Makefile. Les hôtes distants sont des alias SSH définis dans `~/.ssh/config` (ou dans `Makefile.env`, gitignoré — gabarit dans `Makefile.env.example`).

## Staging

```bash
make staging-pub
```

Ce que ça fait :
1. `mkdir -p <STAGING_DIR>` sur l'hôte de staging
2. `tar` du dépôt (excluant `.git`, `bin`, `obj`, `.env*`, backups, dossiers IDE) via SSH vers `<STAGING_DIR>`
3. `cd <STAGING_DIR>/environments/staging && docker compose --env-file .env up -d --build`

Ensuite :
```bash
make staging-ps     # état des conteneurs
make staging-logs   # suivi des logs
make staging-down   # arrêt de la stack
```

**Important** :
- Le vrai `environments/staging/.env` sur le serveur n'est **jamais écrasé** (le tar exclut `.env*`) — il doit exister une fois (copie depuis `.env.example` + secrets).
- Première migration vers le layout `environments/` : déplacer l'ancien fichier une seule fois :
  `ssh <STAGING_HOST> "mkdir -p ~/vigie24/environments/staging && mv ~/vigie24/.env.staging ~/vigie24/environments/staging/.env"`
- La surcharge `appsettings.<Env>.json` est montée en volume lecture seule — mettez-la à jour puis redéployez pour appliquer.

## Production

```bash
make prod-pub
```

Mêmes mécaniques sur le VPS (`<PROD_DIR>/environments/production`), plus le reverse proxy Caddy (HTTPS automatique via Let's Encrypt quand `VIGIE24_DOMAIN` est un vrai domaine ; HTTP simple quand c'est une IP). Le firewall doit exposer 80/443.

Cibles utiles : `make prod-ps`, `make prod-logs`, `make prod-down`, `make prod-backup` (dump PostgreSQL dans `backups/`), `make prod-sync` (sync idempotente des articles staging → production), `make staging-worker-pause|resume`, `make prod-worker-pause|resume` (économie de tokens IA).

## Liste de contrôle du déploiement

- [ ] Build + tests locaux verts
- [ ] `make staging-pub` → `curl` du site (200)
- [ ] Logs propres (`docker compose logs` — pas de rafales « A second operation » de concurrence Blazor ; recharger la page 3-4× avant de conclure)
- [ ] Les warnings `libgssapi_krb5.so.2` dans les logs conteneurs sont bénins (probe Kerberos) — vérifier `RestartCount` et le site à la place
