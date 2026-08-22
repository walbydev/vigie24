# Scripts Vigie24

Collection de scripts utilitaires pour la gestion de la base de données et les tests.

## Structure

```
scripts/
├── init-db.sql             # Schéma initial de la DB (auto au 1er démarrage Docker)
├── add-sources.sql         # Ajout de sources de test (7 sources)
├── add-all-sources.sql     # Ajout de 246+ sources complètes
├── add-all-sources.sh      # Wrapper shell pour add-all-sources.sql
├── add-sources-intl.sql    # Ajout de sources internationales (BE, CA, LU)
├── add-sources-yt-reddit.sql # Ajout de chaînes YouTube + subreddits Reddit
├── check_types.sql         # Vérification des types de colonnes
├── reconcile-taxonomy.sql  # Réconciliation catégories/tags (idempotent)
├── reset-database.sql      # Réinitialisation complète (SQL)
├── reset-db.sh             # Wrapper shell pour reset-database.sql
├── sync-articles-to-prod.py # Sync staging → production (idempotent, sans doublon)
├── test-api.sh             # Test de l'API Infomaniak
├── test-infomaniak.sh      # Test spécifique Infomaniak
└── README.md               # Ce fichier
```

---

## Scripts SQL

### `init-db.sql` - Schéma initial de la base de données

Définit la structure complète des tables PostgreSQL (création initiale).

**Usage :** Exécuté automatiquement lors du premier démarrage via Docker Compose.

---

### `add-all-sources.sql` - Ajout de 246+ sources RSS

Script complet ajoutant plus de 240 sources RSS technologiques françaises et internationales.

**Catégories incluses :**
- Actualités Générales (France) - 10 sources
- Sécurité Informatique - 12 sources
- Open Source - 13 sources
- DevOps & Cloud - 16 sources
- IA & Data - 17 sources
- Programmation - 34 sources
- Hardware & Tech - 23 sources
- Startups & Business - 28 sources
- Blockchain & Crypto - 33 sources
- YouTube Tech - 43 sources

**Total : 246 sources actives**

**Usage :**
```bash
# Via le wrapper shell (recommandé)
./scripts/add-all-sources.sh

# Ou manuellement
cat scripts/add-all-sources.sql | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24
```

**Répartition par type :**
- BlogLab : 117 sources
- Press : 84 sources
- Youtube : 43 sources
- Reddit : 2 sources

---

### `add-sources.sql` - Ajout de sources de test

Script léger pour ajouter rapidement 7 sources RSS de test dans la base.

**Usage :**
```bash
cat scripts/add-sources.sql | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24
```

---

### `reset-database.sql` - Réinitialisation complète

Supprime toutes les données et réinsère les données de référence (catégories, tags, sources).

**Usage :**
```bash
# Via le wrapper shell (recommandé)
./scripts/reset-db.sh

# Ou manuellement
cat scripts/reset-database.sql | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24
```

**Données réinsérées :**
- 8 catégories
- 15 tags
- 16 sources RSS (par défaut)

⚠️ **Attention :** Supprime TOUTES les données existantes.

---

### `check_types.sql` - Vérification des types de colonnes

Script utilitaire pour inspecter les types de données des colonnes dans les tables.

**Usage :**
```bash
cat scripts/check_types.sql | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24
```

---

## Scripts Shell

### `add-all-sources.sh` - Wrapper d'ajout de sources

Script shell qui exécute `add-all-sources.sql` avec vérifications préalables.

**Usage :**
```bash
./scripts/add-all-sources.sh
```

**Fonctionnalités :**
- Vérifie que le fichier SQL existe
- Vérifie que Docker Compose est disponible
- Vérifie que les conteneurs sont en ligne
- Affiche un résumé des données insérées
- Messages colorés et lisibles

---

### `reset-db.sh` - Wrapper de réinitialisation

Script shell qui exécute `reset-database.sql` avec vérifications préalables.

**Usage :**
```bash
./scripts/reset-db.sh
```

**Fonctionnalités :**
- Vérifie que le fichier SQL existe
- Vérifie que Docker Compose est disponible
- Affiche un résumé des données insérées
- Messages colorés et lisibles

---

### `test-api.sh` - Test de l'API Infomaniak

Teste la connectivité et l'authentification avec l'API Infomaniak.

**Usage :**
```bash
./scripts/test-api.sh
```

**Prérequis :**
- Variables d'environnement `INFOMANIAK_API_KEY` définies dans `.env`

---

### `test-infomaniak.sh` - Test spécifique Infomaniak

Test plus complet incluant un appel réel au modèle IA.

**Usage :**
```bash
./scripts/test-infomaniak.sh
```

**Prérequis :**
- Variables d'environnement `INFOMANIAK_API_KEY` définies dans `.env`
- Modèle spécifié (ex: `Qwen/Qwen3.5-122B-A10B-FP8`)

---

### `sync-articles-to-prod.py` - Synchronisation staging → production

Importe en production **uniquement les articles absents** (déduplication sur la
contrainte unique `CanonicalUrl` — aucun doublon possible). Les catégories sont
mappées par slug, les tags manquants sont créés, les liens `article_tags`
reproduits. Idempotent : peut être relancé sans risque.

**Usage :**
```bash
make prod-sync                                # synchronisation réelle
python3 scripts/sync-articles-to-prod.py --dry-run   # aperçu sans rien écrire
```

**Prérequis :** alias SSH `vigie24-staging` et `vigie24-prod` configurés.
Aucun secret en clair (tout passe par SSH).

---

## Commandes utiles

### Vérifier le contenu de la base

```bash
# Nombre d'éléments dans chaque table
docker compose exec postgres psql -U postgres -d vigie24 -c "
  SELECT 'Sources' as table_name, COUNT(*) as count FROM sources
  UNION ALL SELECT 'Categories', COUNT(*) FROM categories
  UNION ALL SELECT 'Tags', COUNT(*) FROM tags;
"

# Liste des sources
docker compose exec postgres psql -U postgres -d vigie24 -c "SELECT Name, FeedUrl, IsActive FROM sources ORDER BY Name;"

# Liste des catégories
docker compose exec postgres psql -U postgres -d vigie24 -c "SELECT Slug, DisplayOrder FROM categories ORDER BY DisplayOrder;"

# Répartition par type
docker compose exec postgres psql -U postgres -d vigie24 -c "SELECT \"SourceKind\", COUNT(*) FROM sources WHERE \"IsActive\" = true GROUP BY \"SourceKind\" ORDER BY COUNT(*) DESC;"
```

### Examiner les logs du Worker

```bash
docker compose logs --tail=50 vigie24-worker
```

### Redémarrer un service

```bash
docker compose restart vigie24-worker
docker compose restart vigie24-web
docker compose restart vigie24-db
```

---

## Bonnes pratiques

1. **Ne jamais exécuter `reset-database.sql` en production** sans confirmation explicite
2. **Sauvegarder les données importantes** avant toute réinitialisation
3. **Utiliser les wrappers shell** (`add-all-sources.sh`, `reset-db.sh`) plutôt que d'exécuter le SQL directement
4. **Tester les scripts SQL** dans un environnement de développement avant production
5. **Garder les scripts à jour** lorsque le schéma de la base évolue
6. **Vérifier les doublons** avant d'ajouter de nouvelles sources

---

## Dépannage

### Erreur de connexion à la base
```bash
# Vérifier que PostgreSQL est en ligne
docker compose ps

# Tester la connexion
docker compose exec postgres psql -U postgres -c "SELECT version();"
```

### Script SQL non trouvé
```bash
# Vérifier l'emplacement
ls -la scripts/*.sql

# Vérifier les permissions
chmod 644 scripts/*.sql
```

### Worker ne voit pas les nouvelles sources
```bash
# Redémarrer le Worker pour vider le cache
docker compose restart vigie24-worker
```

### Erreur de syntaxe SQL
```bash
# Vérifier le fichier SQL
cat scripts/nom_du_fichier.sql | head -20

# Tester avec psql en mode verbose
docker compose exec postgres psql -U postgres -d vigie24 -E -f scripts/nom_du_fichier.sql
```

### Doublons dans la base
```bash
# Vérifier les doublons
docker compose exec postgres psql -U postgres -d vigie24 -c "SELECT \"FeedUrl\", COUNT(*) FROM sources GROUP BY \"FeedUrl\" HAVING COUNT(*) > 1;"

# Supprimer les doublons (garder le premier)
docker compose exec postgres psql -U postgres -d vigie24 -c "DELETE FROM sources a USING sources b WHERE a.ctid < b.ctid AND a.\"FeedUrl\" = b.\"FeedUrl\";"
```
