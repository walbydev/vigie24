# Schéma de base de données PostgreSQL — Vigie24

## 1. Introduction

Ce document décrit le schéma relationnel de Vigie24, agrégateur francophone de veille technologique (IA, Dev, Science, Cybersécurité et Hardware). Il couvre la collecte multi-sources, la déduplication d'un même événement, l'éditorialisation, le classement, les interactions lecteurs, les abonnements, les digests quotidiens et la traçabilité de l'ingestion.

Les descripteurs SQL (tables, colonnes, types, contraintes et index) sont en anglais. Les explications sont en français. Les noms sont en `snake_case`, au singulier pour les tables, et les dates sont stockées avec fuseau (`timestamptz`). Les identifiants utilisent des UUID générés côté PostgreSQL afin de rester non séquentiels et adaptés à une future architecture distribuée. Les montants de score utilisent `numeric` plutôt que `float` pour des résultats reproductibles.

Le DDL cible PostgreSQL 15+ et suppose que l'extension `pgcrypto` est disponible.

## 2. Diagramme conceptuel textuel

```text
users ──< votes >── articles ──< article_sources >── sources ──< ingestion_logs
  │                    │  │                           │
  │                    │  └──< article_authors >── authors
  │                    │
  │                    ├──< article_tags >── tags
  │                    ├── belongs_to ── categories (parent_category_id possible)
  │                    ├──< bookmarks >── users
  │                    └──< digest_articles >── daily_digests
  │
  └──< subscriptions

articles ──< article_translations >── locales
categories ──< category_translations >── locales
tags ──< tag_translations >── locales
sources ──< source_translations >── locales
```

Relations principales :

- Un `article` représente un événement éditorial canonique et peut être couvert par plusieurs `sources` via `article_sources`.
- Un article possède une catégorie principale, plusieurs `tags`, plusieurs `authors` et peut apparaître dans plusieurs `daily_digests`.
- Un `user` peut voter une seule fois par article, enregistrer plusieurs `bookmarks` et posséder plusieurs `subscriptions` (par exemple newsletter et RSS).
- Une `source` possède un pays, une langue, un type de collecte et plusieurs exécutions consignées dans `ingestion_logs`.
- Les tables de traduction permettent de publier progressivement les articles, catégories, tags et sources en plusieurs langues sans modifier les tables cœur.

## 3. DDL PostgreSQL complet

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;

CREATE TYPE source_kind AS ENUM ('press', 'blog_lab', 'youtube', 'reddit', 'other');
CREATE TYPE ingestion_method AS ENUM ('rss', 'api', 'scraping', 'youtube', 'reddit');
CREATE TYPE ingestion_status AS ENUM ('running', 'succeeded', 'partial', 'failed');
CREATE TYPE subscription_kind AS ENUM ('newsletter', 'rss');
CREATE TYPE subscription_status AS ENUM ('pending', 'active', 'paused', 'unsubscribed');
CREATE TYPE digest_status AS ENUM ('draft', 'published', 'archived');
CREATE TYPE vote_value AS ENUM ('one', 'two', 'three', 'four', 'five');

CREATE TABLE locales (
    code varchar(10) PRIMARY KEY,
    name varchar(100) NOT NULL,
    native_name varchar(100) NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT locales_code_format_chk CHECK (code ~ '^[a-z]{2,3}([_-][A-Z]{2})?$')
);

CREATE TABLE users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email citext NOT NULL UNIQUE,
    display_name varchar(120),
    password_hash text,
    avatar_url text,
    preferred_locale varchar(10) NOT NULL DEFAULT 'fr-FR' REFERENCES locales(code),
    is_active boolean NOT NULL DEFAULT true,
    email_verified_at timestamptz,
    last_login_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE sources (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(200) NOT NULL,
    country_code char(2),
    language_code varchar(10) NOT NULL DEFAULT 'fr' REFERENCES locales(code),
    source_kind source_kind NOT NULL DEFAULT 'other',
    ingestion_method ingestion_method NOT NULL,
    website_url text NOT NULL,
    feed_url text,
    is_active boolean NOT NULL DEFAULT true,
    last_ingested_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT sources_country_code_chk CHECK (country_code IS NULL OR country_code ~ '^[A-Z]{2}$'),
    CONSTRAINT sources_website_url_chk CHECK (website_url ~* '^https?://'),
    CONSTRAINT sources_feed_url_chk CHECK (feed_url IS NULL OR feed_url ~* '^https?://')
);

CREATE TABLE source_translations (
    source_id uuid NOT NULL REFERENCES sources(id) ON DELETE CASCADE,
    locale_code varchar(10) NOT NULL REFERENCES locales(code),
    localized_name varchar(200) NOT NULL,
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (source_id, locale_code)
);

CREATE TABLE categories (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    slug varchar(100) NOT NULL UNIQUE,
    parent_category_id uuid REFERENCES categories(id) ON DELETE SET NULL,
    display_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT categories_slug_chk CHECK (slug ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$')
);

CREATE TABLE category_translations (
    category_id uuid NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    locale_code varchar(10) NOT NULL REFERENCES locales(code),
    name varchar(120) NOT NULL,
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (category_id, locale_code)
);

CREATE TABLE tags (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    slug varchar(100) NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT tags_slug_chk CHECK (slug ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$')
);

CREATE TABLE tag_translations (
    tag_id uuid NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    locale_code varchar(10) NOT NULL REFERENCES locales(code),
    name varchar(120) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (tag_id, locale_code)
);

CREATE TABLE authors (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(200) NOT NULL,
    profile_url text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE articles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id uuid NOT NULL REFERENCES categories(id),
    canonical_url text NOT NULL,
    title text NOT NULL,
    summary text NOT NULL,
    image_url text,
    image_alt_text text,
    language_code varchar(10) NOT NULL DEFAULT 'fr' REFERENCES locales(code),
    published_at timestamptz NOT NULL,
    first_seen_at timestamptz NOT NULL DEFAULT now(),
    editorial_score numeric(8,4) NOT NULL DEFAULT 0,
    algorithmic_score numeric(8,4) NOT NULL DEFAULT 0,
    ranking_score numeric(8,4) NOT NULL DEFAULT 0,
    vote_count integer NOT NULL DEFAULT 0,
    vote_average numeric(4,3) NOT NULL DEFAULT 0,
    cross_source_count integer NOT NULL DEFAULT 0,
    is_published boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT articles_canonical_url_chk CHECK (canonical_url ~* '^https?://'),
    CONSTRAINT articles_editorial_score_chk CHECK (editorial_score >= 0),
    CONSTRAINT articles_algorithmic_score_chk CHECK (algorithmic_score >= 0),
    CONSTRAINT articles_ranking_score_chk CHECK (ranking_score >= 0),
    CONSTRAINT articles_vote_count_chk CHECK (vote_count >= 0),
    CONSTRAINT articles_vote_average_chk CHECK (vote_average BETWEEN 0 AND 5),
    CONSTRAINT articles_cross_source_count_chk CHECK (cross_source_count >= 0)
);

CREATE TABLE article_translations (
    article_id uuid NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
    locale_code varchar(10) NOT NULL REFERENCES locales(code),
    title text NOT NULL,
    summary text NOT NULL,
    image_alt_text text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (article_id, locale_code)
);

CREATE TABLE article_sources (
    article_id uuid NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
    source_id uuid NOT NULL REFERENCES sources(id) ON DELETE RESTRICT,
    source_article_url text NOT NULL,
    source_title text,
    source_published_at timestamptz,
    source_author_text text,
    is_primary boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (article_id, source_id),
    CONSTRAINT article_sources_url_chk CHECK (source_article_url ~* '^https?://')
);

CREATE TABLE article_authors (
    article_id uuid NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
    author_id uuid NOT NULL REFERENCES authors(id) ON DELETE RESTRICT,
    author_order smallint NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (article_id, author_id),
    CONSTRAINT article_authors_order_chk CHECK (author_order >= 0)
);

CREATE TABLE article_tags (
    article_id uuid NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
    tag_id uuid NOT NULL REFERENCES tags(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (article_id, tag_id)
);

CREATE TABLE votes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    article_id uuid NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
    value vote_value NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (user_id, article_id)
);

CREATE TABLE bookmarks (
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    article_id uuid NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, article_id)
);

CREATE TABLE subscriptions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    kind subscription_kind NOT NULL,
    address citext NOT NULL,
    locale_code varchar(10) NOT NULL DEFAULT 'fr-FR' REFERENCES locales(code),
    status subscription_status NOT NULL DEFAULT 'pending',
    confirmation_token_hash text,
    confirmed_at timestamptz,
    unsubscribed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (kind, address)
);

CREATE TABLE daily_digests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    digest_date date NOT NULL,
    locale_code varchar(10) NOT NULL DEFAULT 'fr-FR' REFERENCES locales(code),
    title varchar(255) NOT NULL,
    intro text,
    status digest_status NOT NULL DEFAULT 'draft',
    published_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (digest_date, locale_code)
);

CREATE TABLE digest_articles (
    digest_id uuid NOT NULL REFERENCES daily_digests(id) ON DELETE CASCADE,
    article_id uuid NOT NULL REFERENCES articles(id) ON DELETE RESTRICT,
    position smallint NOT NULL,
    editorial_note text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (digest_id, article_id),
    UNIQUE (digest_id, position),
    CONSTRAINT digest_articles_position_chk CHECK (position > 0)
);

CREATE TABLE ingestion_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    source_id uuid NOT NULL REFERENCES sources(id) ON DELETE CASCADE,
    method ingestion_method NOT NULL,
    status ingestion_status NOT NULL DEFAULT 'running',
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    items_seen integer NOT NULL DEFAULT 0,
    items_created integer NOT NULL DEFAULT 0,
    items_updated integer NOT NULL DEFAULT 0,
    items_skipped integer NOT NULL DEFAULT 0,
    error_count integer NOT NULL DEFAULT 0,
    error_message text,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ingestion_logs_counts_chk CHECK (items_seen >= 0 AND items_created >= 0 AND items_updated >= 0 AND items_skipped >= 0 AND error_count >= 0),
    CONSTRAINT ingestion_logs_finished_chk CHECK (finished_at IS NULL OR finished_at >= started_at)
);

CREATE INDEX idx_articles_published_ranking ON articles (published_at DESC, ranking_score DESC) WHERE is_published;
CREATE INDEX idx_articles_category_published ON articles (category_id, published_at DESC) WHERE is_published;
CREATE INDEX idx_articles_filter_scores ON articles (ranking_score DESC, cross_source_count DESC, vote_average DESC) WHERE is_published;
CREATE UNIQUE INDEX idx_articles_canonical_url ON articles (canonical_url);
CREATE INDEX idx_article_sources_source ON article_sources (source_id, created_at DESC);
CREATE UNIQUE INDEX idx_article_sources_primary ON article_sources (article_id) WHERE is_primary;
CREATE INDEX idx_article_tags_tag ON article_tags (tag_id, article_id);
CREATE INDEX idx_sources_filters ON sources (country_code, source_kind, is_active);
CREATE INDEX idx_sources_last_ingested ON sources (last_ingested_at NULLS FIRST) WHERE is_active;
CREATE INDEX idx_votes_article ON votes (article_id, created_at DESC);
CREATE INDEX idx_bookmarks_article ON bookmarks (article_id);
CREATE INDEX idx_subscriptions_delivery ON subscriptions (kind, status, locale_code);
CREATE INDEX idx_digests_published ON daily_digests (digest_date DESC, locale_code) WHERE status = 'published';
CREATE INDEX idx_ingestion_logs_source_time ON ingestion_logs (source_id, started_at DESC);
CREATE INDEX idx_ingestion_logs_failures ON ingestion_logs (status, started_at DESC) WHERE status IN ('failed', 'partial');
CREATE INDEX idx_ingestion_logs_metadata ON ingestion_logs USING gin (metadata);

-- Maintient automatiquement updated_at pour toutes les tables qui le possèdent.
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

DO $$
DECLARE
    table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'locales', 'users', 'sources', 'source_translations', 'categories',
        'category_translations', 'tags', 'tag_translations', 'authors', 'articles',
        'article_translations', 'article_sources', 'article_authors', 'article_tags',
        'votes', 'bookmarks', 'subscriptions', 'daily_digests', 'digest_articles',
        'ingestion_logs'
    ] LOOP
        EXECUTE format('CREATE TRIGGER %I_set_updated_at BEFORE UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION set_updated_at()', table_name, table_name);
    END LOOP;
END $$;
```

### Valeurs initiales recommandées

Le DDL ne force pas le contenu métier afin de rester réutilisable. Au déploiement, insérer au minimum les locales `fr-FR`, `en-US` et `fr`, puis les cinq catégories racines : `ia`, `dev`, `science`, `cybersecurite`, `hardware`. Les catégories et leurs libellés affichés doivent être alimentés dans `category_translations`.

## 4. Description table par table

### `locales`
Référentiel des langues/locales supportées. `code` est la clé stable utilisée par les contenus traduits et les préférences utilisateur. `is_active` permet de préparer une langue sans l'exposer immédiatement.

### `users`
Compte lecteur. `email` est insensible à la casse grâce à `citext` et unique. `password_hash` peut rester nul si l'authentification est déléguée à un fournisseur externe. `preferred_locale` prépare les newsletters et l'interface internationale.

### `sources`
Référentiel des médias, blogs/labos, chaînes YouTube ou communautés Reddit. `source_kind` sert au filtre éditorial, tandis que `ingestion_method` décrit le mécanisme technique. `country_code` permet le filtre par pays/région (ISO 3166-1 alpha-2), `language_code` la langue de publication, et `last_ingested_at` le suivi opérationnel.

### `source_translations`
Noms et descriptions localisés d'une source. Le nom de base dans `sources.name` sert de repli si aucune traduction n'est disponible.

### `categories` et `category_translations`
`categories` porte le slug, la hiérarchie facultative et l'ordre d'affichage. Elle couvre la catégorie principale de chaque article. La table de traduction évite de dupliquer une catégorie par langue.

### `tags` et `tag_translations`
Tags libres réutilisables tels que `#LLM` ou `#Mistral`. Le `slug` canonique garantit qu'une variante de casse ne crée pas un doublon ; le préfixe `#` peut être ajouté uniquement dans l'interface.

### `authors` et `article_authors`
Un article peut avoir plusieurs auteurs, avec un ordre d'affichage. `source_author_text` dans `article_sources` conserve aussi la formulation originale quand la normalisation d'un auteur n'est pas certaine.

### `articles`
Entité éditoriale canonique, indépendante d'une URL source particulière. `canonical_url` est l'URL de référence de Vigie24 ; `title`, `summary`, `image_url` et `image_alt_text` alimentent la fiche affichée. `published_at` sert au tri temporel. Les champs `editorial_score`, `algorithmic_score`, `ranking_score`, `vote_count`, `vote_average` et `cross_source_count` sont des valeurs matérialisées pour accélérer les pages de classement ; ils doivent être recalculés par un job ou des triggers transactionnels.

### `article_translations`
Version localisée du titre, résumé et texte alternatif. La version de base dans `articles` peut être la version française au lancement ; une autre langue est ajoutée par insertion, sans migration de colonnes.

### `article_sources`
Table de liaison many-to-many essentielle à la notion de « 3 sources croisées ». Elle conserve l'URL, le titre, la date et l'auteur tels qu'observés dans chaque source. Une seule source primaire est autorisée par article via l'index partiel.

### `article_authors` et `article_tags`
Tables de liaison normalisées. Leurs clés primaires empêchent les doublons et leurs index inverses accélèrent les pages d'auteur et de tag.

### `votes`
Vote étoilé de 1 à 5. Le type enum reste lisible en SQL ; l'application convertit sa valeur en entier pour calculer la moyenne. La contrainte `UNIQUE (user_id, article_id)` garantit un vote par lecteur et article ; une modification remplace le vote existant.

### `bookmarks`
Favori d'un lecteur. La clé composite rend l'opération idempotente et empêche un même article d'être enregistré deux fois par un utilisateur.

### `subscriptions`
Abonnements newsletter ou RSS. `address` contient l'adresse de livraison ou l'identifiant d'abonnement RSS ; `status` permet la confirmation, la pause et le désabonnement. Le token de confirmation est stocké haché, jamais en clair.

### `daily_digests` et `digest_articles`
Un digest est unique par date et locale. `digest_articles.position` fixe l'ordre éditorial et `editorial_note` permet une accroche spécifique dans « Le 24 de Vigie24 ».

### `ingestion_logs`
Journal d'exécution par source : statut, durée, volumes, erreurs et métadonnées JSON. Il permet de diagnostiquer un flux en panne et d'expliquer quand un article a été vu, créé, mis à jour ou ignoré. Les dates `started_at`/`finished_at` sont distinctes des timestamps d'audit.

## 5. Calcul du ranking mixte

Les champs persistés permettent de filtrer et trier rapidement sans recalculer toutes les relations à chaque requête. Une implémentation initiale peut normaliser chaque composante dans `[0, 1]` :

```text
source_factor   = min(cross_source_count / 3.0, 1.0)
vote_factor     = bayesian_average(vote_average, vote_count, prior_mean = 3.5, prior_count = 20)
editorial_factor = min(editorial_score / 100.0, 1.0)
algorithm_factor = min(algorithmic_score / 100.0, 1.0)

ranking_score = 0.40 * editorial_factor
              + 0.25 * algorithm_factor
              + 0.20 * vote_factor
              + 0.15 * source_factor
```

La moyenne bayésienne évite qu'un article recevant une seule note de 5 étoiles dépasse artificiellement un article ayant beaucoup de votes. Le poids et les plafonds sont des paramètres métier, pas des contraintes structurelles. Un job de scoring met à jour les champs matérialisés, notamment après un vote, l'ajout d'une source croisée ou une correction éditoriale. Les index `idx_articles_published_ranking` et `idx_articles_filter_scores` servent les pages de classement ; les filtres par pays et type passent par `article_sources -> sources`.

## 6. Internationalisation

- Les contenus descriptifs variables sont séparés dans `article_translations`, `category_translations`, `tag_translations` et `source_translations`, avec une clé `(entity_id, locale_code)`.
- Les colonnes cœur conservent une version de repli : l'article initial dans `articles`, le slug invariant dans `categories`/`tags`, et les attributs techniques de `sources`.
- `locales` centralise les langues activées et les préférences utilisateur, de digest et d'abonnement.
- Les champs textuels utilisent `text`/`varchar` PostgreSQL en Unicode ; il ne faut pas limiter les caractères à l'alphabet latin.
- Une future localisation d'un digest se fait par une ligne `daily_digests` avec une autre locale, sans modifier `articles`. Pour de très nombreuses langues, une table générique de traductions peut être introduite ultérieurement, mais le modèle typé actuel conserve des contraintes fortes et des requêtes simples.

## 7. Notes d'évolutivité et de maintenance

1. **Déduplication :** `canonical_url` bloque le doublon exact. En production, ajouter une empreinte normalisée du contenu/titre et une étape de rapprochement sémantique avant insertion pour regrouper des URLs différentes couvrant le même événement.
2. **Compteurs matérialisés :** recalculer `vote_count`, `vote_average`, `cross_source_count` et `ranking_score` dans une transaction ou un worker idempotent. Une vue de contrôle peut comparer les compteurs aux agrégats réels.
3. **Ingestion :** utiliser `ingestion_logs` comme historique append-only ; conserver l'erreur détaillée dans un système d'observabilité si elle contient des données sensibles. Mettre en place une alerte sur `failed`/`partial` et sur les sources actives dont `last_ingested_at` est trop ancien.
4. **Suppression :** les comptes, votes et bookmarks suivent l'utilisateur en cascade. Les articles référencés dans un digest et les sources référencées par un article sont protégés par `RESTRICT` afin de préserver l'historique éditorial.
5. **Sécurité :** chiffrer les sauvegardes, ne jamais stocker de mot de passe en clair, limiter les droits du compte d'ingestion et valider les URLs côté application malgré les contraintes minimales du DDL.
6. **Performance :** partitionner `ingestion_logs` par mois si le volume devient important ; envisager une partition temporelle d'`articles` uniquement après mesure. Utiliser `EXPLAIN (ANALYZE, BUFFERS)` avant d'ajouter des index.
7. **Recherche :** pour une recherche plein texte, ajouter une colonne `search_vector tsvector` générée ou entretenue par trigger, avec un index GIN ; cette option est volontairement séparée du cœur relationnel.
8. **Évolutions métier :** si les votes anonymes sont requis, ajouter une table de sessions ou un hash d'appareil soumis à une politique de confidentialité plutôt que de fragiliser l'unicité des votes authentifiés.
