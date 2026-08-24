# Vigie24 PostgreSQL database schema

## 1. Introduction

This document describes the relational schema of Vigie24, a French-language technology watch aggregator (AI, Dev, Science, Cybersecurity and Hardware). It covers multi-source collection, deduplication of the same event, editorialization, ranking, reader interactions, subscriptions, daily digests and ingestion traceability.

SQL descriptors (tables, columns, types, constraints and indexes) are in English. The explanations are in English. Names use `snake_case`, singular for tables, and dates are stored with timezone (`timestamptz`). Identifiers use PostgreSQL-generated UUIDs to remain non-sequential and suited to a future distributed architecture. Score amounts use `numeric` rather than `float` for reproducible results.

The DDL targets PostgreSQL 15+ and assumes the `pgcrypto` extension is available.

## 2. Textual conceptual diagram

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

Main relationships:

- An `article` represents a canonical editorial event and may be covered by several `sources` via `article_sources`.
- An article has a main category, several `tags`, several `authors` and may appear in several `daily_digests`.
- A `user` may vote once per article, save several `bookmarks` and hold several `subscriptions` (e.g. newsletter and RSS).
- A `source` has a country, a language, a collection type and several runs logged in `ingestion_logs`.
- Translation tables allow articles, categories, tags and sources to be progressively published in several languages without modifying the core tables.

## 3. Complete PostgreSQL DDL

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

-- Automatically maintains updated_at for every table that has it.
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

### Recommended initial values

The DDL does not enforce business content so it stays reusable. At deployment, insert at least the `fr-FR`, `en-US` and `fr` locales, then the five root categories: `ia`, `dev`, `science`, `cybersecurite`, `hardware`. Categories and their display labels must be populated in `category_translations`.

## 4. Table-by-table description

### `locales`
Reference of supported languages/locales. `code` is the stable key used by translated content and user preferences. `is_active` allows preparing a language without exposing it immediately.

### `users`
Reader account. `email` is case-insensitive thanks to `citext` and unique. `password_hash` may remain null if authentication is delegated to an external provider. `preferred_locale` prepares newsletters and the international interface.

### `sources`
Reference of media, blogs/labs, YouTube channels or Reddit communities. `source_kind` serves the editorial filter, while `ingestion_method` describes the technical mechanism. `country_code` enables the country/region filter (ISO 3166-1 alpha-2), `language_code` the publication language, and `last_ingested_at` operational tracking.

### `source_translations`
Localized names and descriptions of a source. The base name in `sources.name` acts as a fallback when no translation is available.

### `categories` and `category_translations`
`categories` carries the slug, the optional hierarchy and the display order. It covers the main category of each article. The translation table avoids duplicating a category per language.

### `tags` and `tag_translations`
Reusable free-form tags such as `#LLM` or `#Mistral`. The canonical `slug` guarantees that a case variant does not create a duplicate; the `#` prefix may only be added in the UI.

### `authors` and `article_authors`
An article may have several authors, with a display order. `source_author_text` in `article_sources` also keeps the original wording when author normalization is uncertain.

### `articles`
Canonical editorial entity, independent of any particular source URL. `canonical_url` is Vigie24's reference URL; `title`, `summary`, `image_url` and `image_alt_text` feed the displayed card. `published_at` is used for temporal sorting. The `editorial_score`, `algorithmic_score`, `ranking_score`, `vote_count`, `vote_average` and `cross_source_count` fields are materialized values to speed up ranking pages; they must be recomputed by a job or transactional triggers.

### `article_translations`
Localized version of the title, summary and alternative text. The base version in `articles` may be the French version at launch; another language is added by insertion, without column migrations.

### `article_sources`
Many-to-many join table essential to the "3 crossed sources" notion. It keeps the URL, title, date and author as observed in each source. Only one primary source is allowed per article via the partial index.

### `article_authors` and `article_tags`
Normalized join tables. Their primary keys prevent duplicates and their reverse indexes speed up author and tag pages.

### `votes`
Star vote from 1 to 5. The enum type stays readable in SQL; the application converts its value to an integer to compute the average. The `UNIQUE (user_id, article_id)` constraint guarantees one vote per reader and article; a change replaces the existing vote.

### `bookmarks`
A reader's favorite. The composite key makes the operation idempotent and prevents the same article from being saved twice by a user.

### `subscriptions`
Newsletter or RSS subscriptions. `address` holds the delivery address or RSS subscription identifier; `status` allows confirmation, pausing and unsubscribing. The confirmation token is stored hashed, never in plain text.

### `daily_digests` and `digest_articles`
A digest is unique per date and locale. `digest_articles.position` sets the editorial order and `editorial_note` allows a specific hook in "Le 24 de Vigie24".

### `ingestion_logs`
Per-source execution log: status, duration, volumes, errors and JSON metadata. It makes it possible to diagnose a broken feed and to explain when an article was seen, created, updated or skipped. The `started_at`/`finished_at` dates are distinct from the audit timestamps.

## 5. Mixed ranking calculation

The persisted fields allow fast filtering and sorting without recomputing all relationships on every query. An initial implementation can normalize each component within `[0, 1]`:

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

The Bayesian average prevents an article receiving a single 5-star rating from artificially overtaking an article with many votes. The weights and caps are business parameters, not structural constraints. A scoring job updates the materialized fields, notably after a vote, the addition of a cross-referenced source or an editorial correction. The `idx_articles_published_ranking` and `idx_articles_filter_scores` indexes serve the ranking pages; country and type filters go through `article_sources -> sources`.

## 6. Internationalization

- Variable descriptive content is separated into `article_translations`, `category_translations`, `tag_translations` and `source_translations`, with an `(entity_id, locale_code)` key.
- Core columns keep a fallback version: the initial article in `articles`, the invariant slug in `categories`/`tags`, and the technical attributes of `sources`.
- `locales` centralizes enabled languages and user, digest and subscription preferences.
- Text fields use PostgreSQL `text`/`varchar` in Unicode; characters must not be limited to the Latin alphabet.
- A future digest localization is done via a `daily_digests` row with another locale, without modifying `articles`. For a very large number of languages, a generic translation table could be introduced later, but the current typed model keeps strong constraints and simple queries.

## 7. Scalability and maintenance notes

1. **Deduplication:** `canonical_url` blocks the exact duplicate. In production, add a normalized content/title fingerprint and a semantic reconciliation step before insertion to group different URLs covering the same event.
2. **Materialized counters:** recompute `vote_count`, `vote_average`, `cross_source_count` and `ranking_score` in a transaction or an idempotent worker. A control view can compare the counters against the real aggregates.
3. **Ingestion:** use `ingestion_logs` as an append-only history; keep the detailed error in an observability system if it contains sensitive data. Set up alerts on `failed`/`partial` and on active sources whose `last_ingested_at` is too old.
4. **Deletion:** accounts, votes and bookmarks follow the user in cascade. Articles referenced in a digest and sources referenced by an article are protected by `RESTRICT` to preserve editorial history.
5. **Security:** encrypt backups, never store passwords in plain text, limit the ingestion account's privileges and validate URLs on the application side despite the DDL's minimal constraints.
6. **Performance:** partition `ingestion_logs` by month if volume grows significantly; consider a time-based partition of `articles` only after measuring. Use `EXPLAIN (ANALYZE, BUFFERS)` before adding indexes.
7. **Search:** for full-text search, add a `search_vector tsvector` column that is generated or maintained by a trigger, with a GIN index; this option is deliberately kept separate from the relational core.
8. **Business evolution:** if anonymous votes are required, add a sessions table or a device hash subject to a privacy policy rather than weakening the uniqueness of authenticated votes.
