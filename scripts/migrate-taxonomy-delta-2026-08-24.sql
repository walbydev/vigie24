-- ============================================================
-- Vigie24 — Taxonomy delta 2026-08-24 (idempotent)
-- Add: Divertissements > Bushcraft, Bricolage maison
-- Remove: Musiques > Hip-Hop & R&B
-- ============================================================

-- Add Bushcraft + Bricolage maison under Divertissements
INSERT INTO categories ("Id", "Slug", "ParentCategoryId", "IsActive")
SELECT gen_random_uuid(), s.slug, p."Id", true
FROM (VALUES ('divertissements-bushcraft'), ('divertissements-bricolage-maison')) AS s(slug)
JOIN categories p ON p."Slug" = 'divertissements'
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE "Slug" = s.slug);

-- Remove Hip-Hop & R&B (no articles expected; safe-guard: only if empty)
DELETE FROM categories
WHERE "Slug" = 'musiques-hiphop-rnb'
  AND NOT EXISTS (SELECT 1 FROM articles WHERE "CategoryId" = categories."Id");
