-- YouTube channels requested by Philippe (cocoadmin, b21, SKY BIDOU, Koinsky)
-- + French-speaking gaming/devops/homelab selection. channel_id resolved via canonical
-- link and verified on the feeds/videos.xml feed (August 2026). Idempotent.
INSERT INTO sources ("Id", "Name", "WebsiteUrl", "FeedUrl", "CountryCode", "LanguageCode", "SourceKind", "IngestionMethod", "IsActive", "CreatedAt", "UpdatedAt") VALUES
-- Channels requested by Philippe
(gen_random_uuid(), 'cocoadmin', 'https://www.youtube.com/channel/UCIxwXQzgefPtQCkvL9eOzxg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCIxwXQzgefPtQCkvL9eOzxg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'B21', 'https://www.youtube.com/channel/UCS8ESog1SAy-BfXuJv5kGUg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCS8ESog1SAy-BfXuJv5kGUg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'SKY BIDOU', 'https://www.youtube.com/channel/UCCauNMRaGxJE95HauLjq0bg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCCauNMRaGxJE95HauLjq0bg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Koinsky', 'https://www.youtube.com/channel/UCBCFP-2BBIrVMi_XBO5x2ow', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCBCFP-2BBIrVMi_XBO5x2ow', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
-- Gaming FR
(gen_random_uuid(), 'Squeezie', 'https://www.youtube.com/channel/UCWeg2Pkate69NFdBeuRFTAw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCWeg2Pkate69NFdBeuRFTAw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Gotaga', 'https://www.youtube.com/channel/UCCFqUJYKT97UerMmb6DM0bw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCCFqUJYKT97UerMmb6DM0bw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'AmineMaTue', 'https://www.youtube.com/channel/UCNigJTVnMU8F3n08pYz0UYw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCNigJTVnMU8F3n08pYz0UYw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Michou', 'https://www.youtube.com/channel/UCo3i0nUzZjjLuM7VjAVz4zA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCo3i0nUzZjjLuM7VjAVz4zA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Inoxtag', 'https://www.youtube.com/channel/UCL9aTJb0ur4sovxcppAopEw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCL9aTJb0ur4sovxcppAopEw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'ZeratoR', 'https://www.youtube.com/channel/UCZ_oIYI9ZNpOfWbpZxWNuRQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCZ_oIYI9ZNpOfWbpZxWNuRQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'LeBouseuh', 'https://www.youtube.com/channel/UCUl7mwOyySfZzUkq4H29nug', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCUl7mwOyySfZzUkq4H29nug', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Aypierre', 'https://www.youtube.com/channel/UCA5sfitizqs1oEbB5KY4uKQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCA5sfitizqs1oEbB5KY4uKQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Furious Jumper', 'https://www.youtube.com/channel/UCLMKLU-ZuDQIsbjMvR3bbog', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCLMKLU-ZuDQIsbjMvR3bbog', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Ponce', 'https://www.youtube.com/channel/UCQNvLb5XLkpm2knLFzY4InA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCQNvLb5XLkpm2knLFzY4InA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
-- Devops / homelab FR
(gen_random_uuid(), 'IT-Connect', 'https://www.youtube.com/channel/UCIJTq0nGrgkGMy1xJL_YppQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCIJTq0nGrgkGMy1xJL_YppQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Sébastien WIERTZ', 'https://www.youtube.com/channel/UCSLSYROw0TsVMyOM0DiuOjw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCSLSYROw0TsVMyOM0DiuOjw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Le Homelab FR', 'https://www.youtube.com/channel/UCPxbGWCVFYvVPDJ7UWCJxFw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCPxbGWCVFYvVPDJ7UWCJxFw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'FrenchCoder', 'https://www.youtube.com/channel/UCqs3WY0xH9q0foAjNS0h6Qg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCqs3WY0xH9q0foAjNS0h6Qg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'LDLC', 'https://www.youtube.com/channel/UCIu36vEY96BCwfYvz5ccucg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCIu36vEY96BCwfYvz5ccucg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now())
ON CONFLICT DO NOTHING;
