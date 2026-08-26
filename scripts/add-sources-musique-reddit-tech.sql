-- ============================================================
-- Vigie24 — Sources musique / MAO (YouTube) + Reddit tech FR
-- 2026-08-26 — chaînes demandées par Philippe (@Switch-Angel,
-- @dzodigital) + recherche francophone de production musicale
-- (MAO, techno/trance, beatmaking, home studio).
-- Vérifié : feeds/videos.xml HTTP 200 + 15 <entry> (11 chaînes).
-- Reddit : ajouté malgré le blocage temporaire de Reddit sur les
-- IP datacenter (429/Blocked au moment de l'ajout) — les 3 sous
-- existants reprendront à l'ingestion ; feed invalide = 404 bénin.
-- Idempotent : ON CONFLICT DO NOTHING (unique sur WebsiteUrl).
-- ============================================================
INSERT INTO sources ("Id", "Name", "WebsiteUrl", "FeedUrl", "CountryCode", "LanguageCode", "SourceKind", "IngestionMethod", "IsActive", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), 'Switch Angel', 'https://www.youtube.com/channel/UCmHz7DQbbkcY_UoWY8bBJLw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCmHz7DQbbkcY_UoWY8bBJLw', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'DZO DIGITAL', 'https://www.youtube.com/channel/UCY3y6kGLgimmxATfT25y4Gg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCY3y6kGLgimmxATfT25y4Gg', 'DZ', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Mao Maker (Tuto Mao)', 'https://www.youtube.com/channel/UCBZa-5NmwJX2VtWgxIOd3EA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCBZa-5NmwJX2VtWgxIOd3EA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Tutoriels MAO', 'https://www.youtube.com/channel/UC3qEkWa-xZ8JYrAVQ0KVUBw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC3qEkWa-xZ8JYrAVQ0KVUBw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'La Guerre Des Potards - DJing & M.A.O.', 'https://www.youtube.com/channel/UCOhMVIz7LzKuaUBxR3nNxdg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCOhMVIz7LzKuaUBxR3nNxdg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Forge du Son', 'https://www.youtube.com/channel/UCZl_ib7Eak8U1t1QJiCemsA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCZl_ib7Eak8U1t1QJiCemsA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'DRJ', 'https://www.youtube.com/channel/UCh1wJCTGj3583QsL7ZYyVVw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCh1wJCTGj3583QsL7ZYyVVw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Mxney flp', 'https://www.youtube.com/channel/UCvrPVwqOEzNut7QdhUn7noA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCvrPVwqOEzNut7QdhUn7noA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Alex Beat Genius', 'https://www.youtube.com/channel/UCPebw7l3tZ3Uncr_vTMcxnQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCPebw7l3tZ3Uncr_vTMcxnQ', 'BE', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Skull-T Beats', 'https://www.youtube.com/channel/UCn9OBkAJsluQ0wfUsKblI1Q', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCn9OBkAJsluQ0wfUsKblI1Q', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Projet Home Studio', 'https://www.youtube.com/channel/UCutVZXyXtRCpuN85iGUH5lQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCutVZXyXtRCpuN85iGUH5lQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Reddit — r/SysadminFr', 'https://www.reddit.com/r/SysadminFr/', 'https://www.reddit.com/r/SysadminFr/.rss', 'FR', 'fr', 'Reddit', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Reddit — r/actutech', 'https://www.reddit.com/r/actutech/', 'https://www.reddit.com/r/actutech/.rss', 'FR', 'fr', 'Reddit', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Reddit — r/QuebecTI', 'https://www.reddit.com/r/QuebecTI/', 'https://www.reddit.com/r/QuebecTI/.rss', 'CA', 'fr', 'Reddit', 'Rss', true, now(), now())
ON CONFLICT DO NOTHING;
