-- Flag sources whose content is commercial (buying guides, affiliate reviews,
-- provider "avis"/"tests") as excluded from ingestion.
--
-- Why a column and not IsActive = false: the worker's 24 h recovery probe
-- re-activates auto-disabled sources, so an editorial exclusion must be a
-- separate, durable signal (see Vigie24.Domain.SourceEditorialPolicy).
--
-- Values: 'Standard' (ingested, default) | 'Commercial' (never ingested).
--
-- Usage (prod):
--   ssh vigie24-prod "docker exec -i vigie24-db-prod psql -U postgres_prod -d vigie24_prod" \
--     < scripts/exclude-commercial-sources.sql
-- Adjust DB_USER / DB_NAME per environment (postgres_staging / vigie24_staging on staging).
-- The web UI (/sources) then shows a "⛔ Écartée" badge; no restart needed.

-- 1) Mark a source as commercial (idempotent, matched on the unique WebsiteUrl).
UPDATE sources
SET "EditorialPolicy" = 'Commercial',
    "UpdatedAt"      = now()
WHERE "WebsiteUrl" IN (
    'https://example-commercial.example'   -- ← replace with the real site URLs
)
  AND "EditorialPolicy" <> 'Commercial';

-- 2) Review: every excluded source, with the publication volume it produced
--    before the exclusion (useful to spot a source wrongly flagged).
SELECT s."Name",
       s."WebsiteUrl",
       s."EditorialPolicy",
       s."IsActive",
       count(a."Id") AS "Articles"
FROM sources s
LEFT JOIN article_sources link ON link."SourceId" = s."Id"
LEFT JOIN articles a ON a."Id" = link."ArticleId"
WHERE s."EditorialPolicy" = 'Commercial'
GROUP BY s."Name", s."WebsiteUrl", s."EditorialPolicy", s."IsActive"
ORDER BY "Articles" DESC;

-- 3) Undo (source becomes ingestible again on the next cycle):
-- UPDATE sources SET "EditorialPolicy" = 'Standard', "UpdatedAt" = now()
-- WHERE "WebsiteUrl" = 'https://example-commercial.example';
