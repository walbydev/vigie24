-- Remove LGBT-themed sources from Vigie24 (blog feeds out of scope for the
-- French tech-watch platform).
--
-- Idempotent: safe to run on any environment. These sources were added
-- manually to the prod database (not present in versioned seed scripts).
--
-- Sources removed (all BlogLab, no linked articles):
--   - Gay Cultes        (http://gaycultes.blogspot.com)
--   - Gayming Magazine  (https://gaymingmag.com)
--   - The Gay Passport  (https://thegaypassport.com)

BEGIN;

DELETE FROM sources
WHERE "Name" IN ('Gay Cultes', 'Gayming Magazine', 'The Gay Passport');

COMMIT;
