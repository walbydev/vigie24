# Vigie24 Scripts

Collection of utility scripts for database management and testing.

## Structure

```
scripts/
├── init-db.sql             # Initial DB schema (automatic on first Docker start)
├── add-sources.sql         # Ajout de sources de test (7 sources)
├── add-all-sources.sql     # Add 246+ complete sources
├── add-all-sources.sh      # Wrapper shell pour add-all-sources.sql
├── add-sources-intl.sql    # Ajout de sources internationales (BE, CA, LU)
├── add-sources-yt-reddit.sql # Add YouTube channels + Reddit subreddits
├── check_types.sql         # Column type checks
├── reconcile-taxonomy.sql  # Category/tag reconciliation (idempotent)
├── reset-database.sql      # Full reset (SQL)
├── reset-db.sh             # Wrapper shell pour reset-database.sql
├── sync-articles-to-prod.py # Sync staging → production (idempotent, sans doublon)
├── minify-assets.py        # Minify CSS/JS (make assets)
├── test-api.sh             # Test de l'API Infomaniak
├── test-infomaniak.sh      # Infomaniak-specific test
└── README.md               # Ce fichier
```

---

## SQL Scripts

### `init-db.sql` - Initial database schema

Defines the complete PostgreSQL table structure (initial creation).

**Usage:** Executed automatically on first startup via Docker Compose.

---

### `add-all-sources.sql` - Adding 246+ RSS sources

Complete script adding more than 240 French and international tech RSS sources.

**Included categories:**
- General News (France) - 10 sources
- IT Security - 12 sources
- Open Source - 13 sources
- DevOps & Cloud - 16 sources
- AI & Data - 17 sources
- Programming - 34 sources
- Hardware & Tech - 23 sources
- Startups & Business - 28 sources
- Blockchain & Crypto - 33 sources
- YouTube Tech - 43 sources

**Total: 246 active sources**

**Usage:**
```bash
# Via the shell wrapper (recommended)
./scripts/add-all-sources.sh

# Ou manuellement
cat scripts/add-all-sources.sql | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24
```

**Breakdown by type:**
- BlogLab: 117 sources
- Press: 84 sources
- YouTube: 43 sources
- Reddit: 2 sources

---

### `add-sources.sql` - Adding test sources

Lightweight script to quickly add 7 test RSS sources to the database.

**Usage:**
```bash
cat scripts/add-sources.sql | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24
```

---

### `reset-database.sql` - Full reset

Deletes all data and re-inserts the reference data (categories, tags, sources).

**Usage:**
```bash
# Via the shell wrapper (recommended)
./scripts/reset-db.sh

# Ou manuellement
cat scripts/reset-database.sql | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24
```

**Re-inserted data:**
- 8 categories
- 15 tags
- 16 RSS sources (default)

⚠️ **Warning:** Deletes ALL existing data.

---

### `check_types.sql` - Column type verification

Utility script to inspect the data types of columns in the tables.

**Usage:**
```bash
cat scripts/check_types.sql | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24
```

---

## Shell Scripts

### `add-all-sources.sh` - Source addition wrapper

Shell script that runs `add-all-sources.sql` with preliminary checks.

**Usage:**
```bash
./scripts/add-all-sources.sh
```

**Features:**
- Checks that the SQL file exists
- Checks that Docker Compose is available
- Checks that the containers are up
- Displays a summary of the inserted data
- Colorful, readable messages

---

### `reset-db.sh` - Reset wrapper

Shell script that runs `reset-database.sql` with preliminary checks.

**Usage:**
```bash
./scripts/reset-db.sh
```

**Features:**
- Checks that the SQL file exists
- Checks that Docker Compose is available
- Displays a summary of the inserted data
- Colorful, readable messages

---

### `test-api.sh` - Infomaniak API test

Tests connectivity and authentication with the Infomaniak API.

**Usage:**
```bash
./scripts/test-api.sh
```

**Prerequisites:**
- `INFOMANIAK_API_KEY` environment variable set in `.env`

---

### `test-infomaniak.sh` - Infomaniak-specific test

More complete test including a real call to the AI model.

**Usage:**
```bash
./scripts/test-infomaniak.sh
```

**Prerequisites:**
- `INFOMANIAK_API_KEY` environment variable set in `.env`
- Model specified (e.g. `google/gemma-4-31B-it`)

---

### `sync-articles-to-prod.py` - Staging → production synchronization

Imports into production **only the articles that are missing** (deduplication on the
unique `CanonicalUrl` constraint — no duplicates possible). Categories are
mapped by slug, missing tags are created, `article_tags` links are
reproduced. Idempotent: can safely be re-run.

**Usage:**
```bash
make prod-sync                                # real synchronization
python3 scripts/sync-articles-to-prod.py --dry-run   # preview without writing anything
```

**Prerequisites:** SSH aliases `vigie24-staging` and `vigie24-prod` configured.
No secrets in plain text (everything goes through SSH).

---

### `minify-assets.py` - CSS/JS minification

Minifies `wwwroot/css/site.css`, `wwwroot/css/radar.css` and
`wwwroot/js/vigie24.js` into committed `.min.*` copies (rcssmin/jsmin via uv).

**Usage:**
```bash
make assets     # uv run --with rcssmin --with jsmin python3 scripts/minify-assets.py
```

Run it after every edit of a CSS/JS source — the site serves the `.min.*` files,
and a stale minified file silently serves old styles.

---

## Useful commands

### Check the database contents

```bash
# Number of items in each table
docker compose exec postgres psql -U postgres -d vigie24 -c "
  SELECT 'Sources' as table_name, COUNT(*) as count FROM sources
  UNION ALL SELECT 'Categories', COUNT(*) FROM categories
  UNION ALL SELECT 'Tags', COUNT(*) FROM tags;
"

# Liste des sources
docker compose exec postgres psql -U postgres -d vigie24 -c "SELECT Name, FeedUrl, IsActive FROM sources ORDER BY Name;"

# List of categories
docker compose exec postgres psql -U postgres -d vigie24 -c "SELECT Slug, DisplayOrder FROM categories ORDER BY DisplayOrder;"

# Breakdown by type
docker compose exec postgres psql -U postgres -d vigie24 -c "SELECT \"SourceKind\", COUNT(*) FROM sources WHERE \"IsActive\" = true GROUP BY \"SourceKind\" ORDER BY COUNT(*) DESC;"
```

### Inspect the Worker logs

```bash
docker compose logs --tail=50 vigie24-worker
```

### Restart a service

```bash
docker compose restart vigie24-worker
docker compose restart vigie24-web
docker compose restart vigie24-db
```

---

## Best practices

1. **Never run `reset-database.sql` in production** without explicit confirmation
2. **Back up important data** before any reset
3. **Use the shell wrappers** (`add-all-sources.sh`, `reset-db.sh`) instead of running the SQL directly
4. **Test SQL scripts** in a development environment before production
5. **Keep the scripts up to date** when the database schema changes
6. **Check for duplicates** before adding new sources

---

## Troubleshooting

### Database connection error
```bash
# Check that PostgreSQL is up
docker compose ps

# Tester la connexion
docker compose exec postgres psql -U postgres -c "SELECT version();"
```

### SQL script not found
```bash
# Check the location
ls -la scripts/*.sql

# Check permissions
chmod 644 scripts/*.sql
```

### Worker does not see new sources
```bash
# Restart the Worker to clear the cache
docker compose restart vigie24-worker
```

### SQL syntax error
```bash
# Check the SQL file
cat scripts/nom_du_fichier.sql | head -20

# Tester avec psql en mode verbose
docker compose exec postgres psql -U postgres -d vigie24 -E -f scripts/nom_du_fichier.sql
```

### Duplicates in the database
```bash
# Check for duplicates
docker compose exec postgres psql -U postgres -d vigie24 -c "SELECT \"FeedUrl\", COUNT(*) FROM sources GROUP BY \"FeedUrl\" HAVING COUNT(*) > 1;"

# Supprimer les doublons (garder le premier)
docker compose exec postgres psql -U postgres -d vigie24 -c "DELETE FROM sources a USING sources b WHERE a.ctid < b.ctid AND a.\"FeedUrl\" = b.\"FeedUrl\";"
```
