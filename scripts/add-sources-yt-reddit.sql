-- Ajout des sources YouTube (chaînes validées) et Reddit (subreddits francophones).
-- Idempotent : ON CONFLICT DO NOTHING (contraintes uniques WebsiteUrl/FeedUrl).
-- CountryCode mis à 'FR' par défaut (contenu francophone) — ajustable.
INSERT INTO sources ("Id", "Name", "WebsiteUrl", "FeedUrl", "CountryCode", "LanguageCode", "SourceKind", "IngestionMethod", "IsActive", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), 'Dr Firas', 'https://www.youtube.com/channel/UCriIQI8uaoEro5FEnOpeidQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCriIQI8uaoEro5FEnOpeidQ', 'TN', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Nico Decode', 'https://www.youtube.com/channel/UCmHtQ8dnZA8JgMcl2GW2Xfw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCmHtQ8dnZA8JgMcl2GW2Xfw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Shubham Sharma', 'https://www.youtube.com/channel/UCLKx4-_XO5sR0AO0j8ye7zQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCLKx4-_XO5sR0AO0j8ye7zQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Yassine Sdiri', 'https://www.youtube.com/channel/UC94UeaPuTt_L51RkDxj9d_w', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC94UeaPuTt_L51RkDxj9d_w', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Reddit — r/france', 'https://www.reddit.com/r/france/', 'https://www.reddit.com/r/france/.rss', 'FR', 'fr', 'Reddit', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Reddit — r/programmation', 'https://www.reddit.com/r/programmation/', 'https://www.reddit.com/r/programmation/.rss', 'FR', 'fr', 'Reddit', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Reddit — r/AskFrance', 'https://www.reddit.com/r/AskFrance/', 'https://www.reddit.com/r/AskFrance/.rss', 'FR', 'fr', 'Reddit', 'Rss', true, now(), now())
ON CONFLICT DO NOTHING;
