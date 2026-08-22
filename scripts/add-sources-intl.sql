-- ============================================================
-- Vigie24 — Ajout de sources RSS francophones hors France
-- Suisse (CH), Belgique (BE), Canada/Québec (CA), Luxembourg (LU)
-- Flux vérifiés (HTTP 200, RSS/Atom valide, dates parsables) le 2026-08-22.
-- Idempotent : ON CONFLICT ("WebsiteUrl") DO NOTHING
-- ============================================================

INSERT INTO sources ("Id", "Name", "WebsiteUrl", "FeedUrl", "CountryCode", "LanguageCode", "SourceKind", "IngestionMethod", "IsActive", "CreatedAt", "UpdatedAt") VALUES
-- Suisse
(gen_random_uuid(), 'Le Temps',            'https://www.letemps.ch',          'https://www.letemps.ch/articles.rss',                     'CH', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'watson',              'https://www.watson.ch',           'https://www.watson.ch/fr/api/2.0/rss/index.xml?tag=Front', 'CH', 'fr', 'Press',  'Rss', true, now(), now()),
(gen_random_uuid(), 'Heidi.news',          'https://www.heidi.news',          'https://www.heidi.news/articles.rss',                      'CH', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), '20 minutes',          'https://www.20min.ch',            'https://partner-feeds.20min.ch/rss/20minutes/suisse-romande', 'CH', 'fr', 'Press', 'Rss', true, now(), now()),
-- Belgique
(gen_random_uuid(), 'Le Soir',             'https://www.lesoir.be',           'https://www.lesoir.be/rss.xml',                            'BE', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'La Libre',            'https://www.lalibre.be',          'https://www.lalibre.be/arc/outboundfeeds/rss/?outputType=xml', 'BE', 'fr', 'Press', 'Rss', true, now(), now()),
(gen_random_uuid(), '7sur7',               'https://www.7sur7.be',            'https://www.7sur7.be/home/rss.xml',                         'BE', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'Le Vif',              'https://www.levif.be',            'https://www.levif.be/feed/',                                'BE', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'DataNews',            'https://datanews.levif.be',       'https://datanews.levif.be/feed/',                           'BE', 'fr', 'Press',    'Rss', true, now(), now()),
-- Canada / Québec
(gen_random_uuid(), 'La Presse',           'https://www.lapresse.ca',         'https://www.lapresse.ca/actualites/rss',                    'CA', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'Journal de Montréal', 'https://www.journaldemontreal.com','https://www.journaldemontreal.com/rss.xml',               'CA', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'Journal de Québec',   'https://www.journaldequebec.com', 'https://www.journaldequebec.com/rss.xml',                   'CA', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'TVA Nouvelles',       'https://www.tvanouvelles.ca',     'https://www.tvanouvelles.ca/rss.xml',                       'CA', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'Les Affaires',        'https://www.lesaffaires.com',     'https://www.lesaffaires.com/feed/',                         'CA', 'fr', 'Press',    'Rss', true, now(), now()),
(gen_random_uuid(), 'Infobref',            'https://infobref.com',            'https://infobref.com/feed/',                                'CA', 'fr', 'BlogLab',  'Rss', true, now(), now()),
(gen_random_uuid(), 'Branchez-vous',       'https://branchez-vous.com',       'https://branchez-vous.com/feed/',                           'CA', 'fr', 'BlogLab',  'Rss', true, now(), now()),
-- Luxembourg
(gen_random_uuid(), 'L''essentiel',        'https://www.lessentiel.lu',       'https://partner-feeds.lessentiel.lu/rss/lessentiel-fr',      'LU', 'fr', 'Press',    'Rss', true, now(), now())
ON CONFLICT ("WebsiteUrl") DO NOTHING;

-- Vérifications
SELECT "CountryCode", count(*) AS sources FROM sources WHERE "IsActive" = true GROUP BY "CountryCode" ORDER BY "CountryCode";
SELECT "Name", "CountryCode", "FeedUrl" FROM sources WHERE "CountryCode" <> 'FR' ORDER BY "CountryCode", "Name";
