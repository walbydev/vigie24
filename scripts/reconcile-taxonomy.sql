-- ============================================================
-- Vigie24 — Réconciliation de la taxonomie (idempotent)
-- 1) Reclassement des articles des catégories « parallèles » (créées
--    librement par l'IA) vers les catégories officielles de la taxonomie.
-- 2) Tags : normalisation des slugs (minuscules, sans accents, tirets)
--    + fusion des doublons (cybersecurite/cybersécurité, promotion/promotions…).
--
-- À exécuter une fois après déploiement du code corrigé :
--   cat scripts/reconcile-taxonomy.sql | docker exec -i vigie24-db-staging \
--     psql -U postgres_staging -d vigie24_staging
-- ============================================================

-- 1) Catégories parallèles → officielles
DO $$
DECLARE m record;
BEGIN
  FOR m IN SELECT * FROM (VALUES
    ('electronique','hardware-electronique'), ('mobilite','hardware-mobilite'),
    ('composants','hardware-composants'), ('agents-ia','ia-agents'),
    ('modeles-llm','ia-modeles-llm'), ('outils-applications','ia-outils-applications'),
    ('vision-generatif','ia-vision-generatif'), ('web','dev-web'),
    ('mobile','dev-mobile'), ('devops-cloud','dev-devops-cloud'),
    ('open-source','dev-open-source'), ('backend','dev-backend'),
    ('menaces-acteurs','cyber-menaces-acteurs'), ('reglementation','cyber-reglementation'),
    ('vulnerabilites','cyber-vulnerabilites'), ('espace','science-espace'),
    ('recherche','science-recherche'), ('sante-biotech','science-sante-biotech')
  ) AS t(old_slug, new_slug) LOOP
    UPDATE articles a SET "CategoryId" = n."Id"
    FROM categories o, categories n
    WHERE o."Slug" = m.old_slug AND n."Slug" = m.new_slug AND a."CategoryId" = o."Id";
    DELETE FROM categories WHERE "Slug" = m.old_slug;
  END LOOP;
END $$;

-- 2) Tags — fonction de slugification (équivalent de VigieTaxonomy.Slugify)
CREATE OR REPLACE FUNCTION vigie_slugify(t text) RETURNS text LANGUAGE sql IMMUTABLE AS $$
  SELECT btrim(regexp_replace(
    translate(lower($1), 'àâäáãåæçèéêëìíîïñòóôöõùúûüýÿ', 'aaaaaaaceeeeiiiinooooouuuuyy'),
    '[^a-z0-9]+', '-', 'g'), '-');
$$;

-- 2a) Fusion des doublons créés par la normalisation (ex: cybersecurite / cybersécurité)
DO $$
DECLARE g record; k uuid; i int; dup uuid;
BEGIN
  FOR g IN
    SELECT vigie_slugify("Slug") AS ns, array_agg("Id" ORDER BY "CreatedAt") AS ids
    FROM tags GROUP BY vigie_slugify("Slug") HAVING count(*) > 1
  LOOP
    k := g.ids[1];
    FOR i IN 2..array_length(g.ids, 1) LOOP
      dup := g.ids[i];
      INSERT INTO article_tags ("ArticleId", "TagId", "CreatedAt")
        SELECT "ArticleId", k, now() FROM article_tags WHERE "TagId" = dup
        ON CONFLICT DO NOTHING;
      DELETE FROM article_tags WHERE "TagId" = dup;
      DELETE FROM tags WHERE "Id" = dup;
    END LOOP;
  END LOOP;
END $$;

-- 2b) Normalisation des slugs existants (retrait des accents, casse, ponctuation)
UPDATE tags SET "Slug" = vigie_slugify("Slug") WHERE "Slug" <> vigie_slugify("Slug");

-- 2c) Fusions sémantiques singulier/pluriel (vers le pluriel, sauf fuite-de-donnees)
DO $$
DECLARE m record;
BEGIN
  FOR m IN SELECT * FROM (VALUES
    ('promotion','promotions'),
    ('bon-plan','bons-plans'),
    ('fuites-de-donnees','fuite-de-donnees')
  ) AS t(dup_slug, keeper_slug) LOOP
    INSERT INTO article_tags ("ArticleId", "TagId", "CreatedAt")
      SELECT at."ArticleId", k."Id", now() FROM article_tags at
      JOIN tags d ON at."TagId" = d."Id"
      JOIN tags k ON k."Slug" = m.keeper_slug
      WHERE d."Slug" = m.dup_slug
      ON CONFLICT DO NOTHING;
    DELETE FROM article_tags WHERE "TagId" IN (SELECT "Id" FROM tags WHERE "Slug" = m.dup_slug);
    DELETE FROM tags WHERE "Slug" = m.dup_slug;
  END LOOP;
END $$;

-- Vérifications
SELECT '--- catégories restantes (parallèles attendues à 0) ---' AS info;
SELECT "Slug" FROM categories WHERE "Slug" NOT IN
  (SELECT "Slug" FROM (VALUES ('ia'),('ia-agents'),('ia-modeles-llm'),('ia-vision-generatif'),('ia-outils-applications'),
    ('developpement'),('dev-web'),('dev-backend'),('dev-mobile'),('dev-devops-cloud'),('dev-open-source'),
    ('cybersecurite'),('cyber-vulnerabilites'),('cyber-menaces-acteurs'),('cyber-reglementation'),
    ('science'),('science-recherche'),('science-espace'),('science-sante-biotech'),
    ('hardware'),('hardware-composants'),('hardware-electronique'),('hardware-mobilite')) AS t(s));
SELECT '--- doublons de tags restants ---' AS info;
SELECT "Slug", count(*) FROM tags GROUP BY "Slug" HAVING count(*) > 1;
SELECT '--- compteurs ---' AS info;
SELECT (SELECT count(*) FROM tags) AS tags, (SELECT count(*) FROM categories) AS categories;
