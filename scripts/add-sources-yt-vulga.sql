-- Adds science-communication YouTube channels (science, tech, design, homelab).
-- All channel_id values were resolved via the <link rel="canonical"> of the channel page
-- AND verified with the feeds/videos.xml feed (feed title = expected channel), August 2026.
-- Idempotent : ON CONFLICT DO NOTHING (contraintes uniques WebsiteUrl/FeedUrl).
-- CountryCode: 'FR' for French-speaking channels, 'US' for English-speaking ones (homelab).
INSERT INTO sources ("Id", "Name", "WebsiteUrl", "FeedUrl", "CountryCode", "LanguageCode", "SourceKind", "IngestionMethod", "IsActive", "CreatedAt", "UpdatedAt") VALUES
-- Vulgarisation scientifique (FR)
(gen_random_uuid(), 'ScienceEtonnante', 'https://www.youtube.com/channel/UCaNlbnghtwlsGF-KzAFThqA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCaNlbnghtwlsGF-KzAFThqA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Astronogeek', 'https://www.youtube.com/channel/UC5X4e8ScZI2AFd_vkjSoyoQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC5X4e8ScZI2AFd_vkjSoyoQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Le Journal de l''Espace', 'https://www.youtube.com/channel/UCypzG0nv9B65DwOLJ578rZQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCypzG0nv9B65DwOLJ578rZQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'DirtyBiology', 'https://www.youtube.com/channel/UCtqICqGbPSbTN09K1_7VZ3Q', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCtqICqGbPSbTN09K1_7VZ3Q', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Passé Sauvage', 'https://www.youtube.com/channel/UCLhOJJbPciPdocXTaAk2SdA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCLhOJJbPciPdocXTaAk2SdA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Balade Mentale', 'https://www.youtube.com/channel/UCS_7tplUgzJG4DhA16re5Yg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCS_7tplUgzJG4DhA16re5Yg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Fouloscopie', 'https://www.youtube.com/channel/UCLXDNUOO3EQ80VmD9nQBHPg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCLXDNUOO3EQ80VmD9nQBHPg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'ScienceClic', 'https://www.youtube.com/channel/UCV6uM3y-8TeO7JlCtcEN-Bg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCV6uM3y-8TeO7JlCtcEN-Bg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Le Réveilleur', 'https://www.youtube.com/channel/UC1EacOJoqsKaYxaDomTCTEQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC1EacOJoqsKaYxaDomTCTEQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'La Tronche en Biais', 'https://www.youtube.com/channel/UCq-8pBMM3I40QlrhM9ExXJQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCq-8pBMM3I40QlrhM9ExXJQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
-- Dev / informatique (FR)
(gen_random_uuid(), 'Underscore_', 'https://www.youtube.com/channel/UCWedHS9qKebauVIK2J7383g', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCWedHS9qKebauVIK2J7383g', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Benjamin Code', 'https://www.youtube.com/channel/UCLOAPb7ATQUs_nDs9ViLcMw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCLOAPb7ATQUs_nDs9ViLcMw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Grafikart', 'https://www.youtube.com/channel/UCj_iGliGCkLcHSZ8eqVNPDQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCj_iGliGCkLcHSZ8eqVNPDQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Développeur Libre', 'https://www.youtube.com/channel/UCjDg7LaaMzmuhj6I1jTzb1Q', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCjDg7LaaMzmuhj6I1jTzb1Q', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Micode', 'https://www.youtube.com/channel/UCYnvxJ-PKiGXo_tYXpWAC-w', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCYnvxJ-PKiGXo_tYXpWAC-w', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Xavki', 'https://www.youtube.com/channel/UCs_AZuYXi6NA9tkdbhjItHQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCs_AZuYXi6NA9tkdbhjItHQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
-- Graphisme / design (FR)
(gen_random_uuid(), 'Léo Duff', 'https://www.youtube.com/channel/UCNiauGTV7XhkOpUAIXod4xA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCNiauGTV7XhkOpUAIXod4xA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Samuel T', 'https://www.youtube.com/channel/UCuTvuo6CHP9IcMxYJ6VXvaA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCuTvuo6CHP9IcMxYJ6VXvaA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
-- Homelab / bricolage (EN)
(gen_random_uuid(), 'Craft Computing', 'https://www.youtube.com/channel/UCp3yVOm6A55nx65STpm3tXQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCp3yVOm6A55nx65STpm3tXQ', 'US', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Techno Tim', 'https://www.youtube.com/channel/UCOk-gHyjcWZNj3Br4oxwh0A', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCOk-gHyjcWZNj3Br4oxwh0A', 'US', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Wolfgang''s Channel', 'https://www.youtube.com/channel/UCsnGwSIHyoYN0kiINAGUKxg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCsnGwSIHyoYN0kiINAGUKxg', 'US', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Jeff Geerling', 'https://www.youtube.com/channel/UCR-DXc1voovS8nhAvccRZhg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCR-DXc1voovS8nhAvccRZhg', 'US', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'NetworkChuck', 'https://www.youtube.com/channel/UC9x0AN7BWHpCDHSm9NiJFJQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC9x0AN7BWHpCDHSm9NiJFJQ', 'US', 'fr', 'Youtube', 'Rss', true, now(), now())
ON CONFLICT DO NOTHING;
