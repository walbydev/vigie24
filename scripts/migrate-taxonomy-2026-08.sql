-- ============================================================
-- Vigie24 — Taxonomy migration 2026-08: Musiques, Arts graphiques,
-- Divertissements rework, Politique removal (idempotent)
--
-- Order: create → reclassify → unpublish → delete
-- Run once per environment (staging first, then production):
--   cat scripts/migrate-taxonomy-2026-08.sql | docker exec -i <db> \
--     psql -U <user> -d <db>
-- ============================================================

-- 0. Fix DisplayOrder of existing parents (idempotent)
UPDATE categories SET "DisplayOrder" = v.order
FROM (VALUES ('ia',1), ('developpement',2), ('cybersecurite',3),
             ('science',4), ('hardware',5), ('divertissements',6)) AS v(slug, order)
WHERE categories."Slug" = v.slug;

-- 1. Create new parent categories
INSERT INTO categories ("Id", "Slug", "DisplayOrder", "IsActive")
SELECT gen_random_uuid(), 'musiques', 7, true
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE "Slug" = 'musiques');

INSERT INTO categories ("Id", "Slug", "DisplayOrder", "IsActive")
SELECT gen_random_uuid(), 'arts-graphiques', 8, true
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE "Slug" = 'arts-graphiques');

-- 2. Create new subcategories (children of their parents)
-- Musiques
INSERT INTO categories ("Id", "Slug", "ParentCategoryId", "IsActive")
SELECT gen_random_uuid(), s.slug, p."Id", true
FROM (VALUES
  ('musiques-electronique-techno'), ('musiques-metal-philharmonique'),
  ('musiques-jazz-blues'), ('musiques-hiphop-rnb'), ('musiques-pop-variete')
) AS s(slug)
JOIN categories p ON p."Slug" = 'musiques'
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE "Slug" = s.slug);

-- Arts graphiques
INSERT INTO categories ("Id", "Slug", "ParentCategoryId", "IsActive")
SELECT gen_random_uuid(), s.slug, p."Id", true
FROM (VALUES
  ('arts-graphiques-manga-anime'), ('arts-graphiques-infographie-2d-3d'),
  ('arts-graphiques-illustration-bd'), ('arts-graphiques-design-ui-ux')
) AS s(slug)
JOIN categories p ON p."Slug" = 'arts-graphiques'
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE "Slug" = s.slug);

-- Divertissements subcategories
INSERT INTO categories ("Id", "Slug", "ParentCategoryId", "IsActive")
SELECT gen_random_uuid(), s.slug, p."Id", true
FROM (VALUES
  ('divertissements-cinema-series'), ('divertissements-streaming'),
  ('divertissements-culture-medias')
) AS s(slug)
JOIN categories p ON p."Slug" = 'divertissements'
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE "Slug" = s.slug);

-- 3. Reclassify articles from AI-created parallel categories
DO $$
DECLARE m record;
BEGIN
  FOR m IN SELECT * FROM (VALUES
    ('cinema',   'divertissements-cinema-series'),
    ('streaming','divertissements-streaming'),
    ('culture',  'divertissements-culture-medias'),
    ('medias',   'divertissements-culture-medias'),
    ('musique',  'musiques-pop-variete')
  ) AS t(old_slug, new_slug) LOOP
    UPDATE articles a SET "CategoryId" = n."Id"
    FROM categories o, categories n
    WHERE o."Slug" = m.old_slug AND n."Slug" = m.new_slug AND a."CategoryId" = o."Id";
  END LOOP;
END $$;

-- 4. Unpublish out-of-scope articles (politics, people, misc news)
UPDATE articles a SET "IsPublished" = false
FROM categories c
WHERE a."CategoryId" = c."Id"
  AND c."Slug" IN ('elections', 'suisse', 'people', 'actualites');

-- 5. Delete the parallel categories (now empty of published articles;
--    ON DELETE SET NULL on articles.CategoryId handles any leftover)
DELETE FROM categories WHERE "Slug" IN (
  'politique', 'divers', 'musique', 'people', 'cinema', 'streaming',
  'culture', 'medias', 'actualites', 'elections', 'suisse'
);

-- 6. Verification: no parallel slug left, no published article without a category
DO $$
DECLARE n_orphans int; n_parallel int;
BEGIN
  SELECT COUNT(*) INTO n_orphans FROM articles
    WHERE "IsPublished" = true AND "CategoryId" IS NULL;
  SELECT COUNT(*) INTO n_parallel FROM categories
    WHERE "Slug" IN ('politique','divers','musique','people','cinema','streaming',
                     'culture','medias','actualites','elections','suisse');
  IF n_orphans > 0 OR n_parallel > 0 THEN
    RAISE EXCEPTION 'Migration incomplete: % orphan(s), % parallel slug(s) left', n_orphans, n_parallel;
  END IF;
END $$;

-- ============================================================
-- Done. Expected new categories: musiques (+5), arts-graphiques (+4),
-- divertissements-cinema-series, divertissements-streaming,
-- divertissements-culture-medias. Politique/divers/musique/people removed.
-- ============================================================
