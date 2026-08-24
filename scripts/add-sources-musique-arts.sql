-- ============================================================
-- Vigie24 — Music & Visual arts sources (from Philippe's YouTube
-- subscriptions, walbymedia account — verified 2026-08-24)
-- 26 channels: HTTP 200 + <entry> on feeds/videos.xml.
-- Idempotent: ON CONFLICT DO NOTHING.
-- The AI assigns categories (Musiques / Arts graphiques) at ingest time.
-- ============================================================
INSERT INTO sources ("Id", "Name", "WebsiteUrl", "FeedUrl", "CountryCode", "LanguageCode", "SourceKind", "IngestionMethod", "IsActive", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), 'The Supermen Lovers', 'https://www.youtube.com/channel/UCbY0IAS0Qf1FHObBaYvwCPQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCbY0IAS0Qf1FHObBaYvwCPQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'MOSIMANN', 'https://www.youtube.com/channel/UCtaRinTphVJqoLc0LpKhK2w', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCtaRinTphVJqoLc0LpKhK2w', 'CH', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Deftones', 'https://www.youtube.com/channel/UCQDsiRE--IQe5ycCLeetIxQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCQDsiRE--IQe5ycCLeetIxQ', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Alice In Chains', 'https://www.youtube.com/channel/UCK9X9JACEsonjbqaewUtICA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCK9X9JACEsonjbqaewUtICA', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'The HU', 'https://www.youtube.com/channel/UCs6vRDdkZ8bP8Xt6WHbvrwA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCs6vRDdkZ8bP8Xt6WHbvrwA', 'MN', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'w.o.d.', 'https://www.youtube.com/channel/UC9akgSn0CzVFhDyQpEQ02ZQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC9akgSn0CzVFhDyQpEQ02ZQ', 'JP', 'ja', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Jojo Plays Music', 'https://www.youtube.com/channel/UCtwq-WteM_z19HbtvNZbrMg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCtwq-WteM_z19HbtvNZbrMg', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Jazz & Bossa Collection', 'https://www.youtube.com/channel/UCqKLOX-B7H9djovoTzj-Wdg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCqKLOX-B7H9djovoTzj-Wdg', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Andryel guitar', 'https://www.youtube.com/channel/UCo3IZtT5dYfUBr_CegHNXvw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCo3IZtT5dYfUBr_CegHNXvw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'TheKillersMusic', 'https://www.youtube.com/channel/UCkhyoTaWKuB-Rdbb6Z3Z5DA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCkhyoTaWKuB-Rdbb6Z3Z5DA', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Two Door Cinema Club', 'https://www.youtube.com/channel/UC21j22BUI19MJ6F8IrxnjJg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC21j22BUI19MJ6F8IrxnjJg', 'GB', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Hoobastank', 'https://www.youtube.com/channel/UCityQM41pM2PchmqbpawEIQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCityQM41pM2PchmqbpawEIQ', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'AnnenMayKantereit', 'https://www.youtube.com/channel/UCheky_SBEExtbK_T0onuDwg', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCheky_SBEExtbK_T0onuDwg', 'DE', 'de', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'CharlesBerthoud', 'https://www.youtube.com/channel/UCAi_uNeDWRXj8C8yw358gWw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCAi_uNeDWRXj8C8yw358gWw', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Matador Records', 'https://www.youtube.com/channel/UCLGElt2TSebXf1VVJzhLQbw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCLGElt2TSebXf1VVJzhLQbw', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Adrien Ka - Infographiks', 'https://www.youtube.com/channel/UCf77f_QifIrd7tq4GqtrAYA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCf77f_QifIrd7tq4GqtrAYA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'TheCGBros', 'https://www.youtube.com/channel/UC-1rx8j9Ggp8mp4uD0ZdEIA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC-1rx8j9Ggp8mp4uD0ZdEIA', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Will Bottone', 'https://www.youtube.com/channel/UC_Jv52aYRZADZdNFa7fEcag', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC_Jv52aYRZADZdNFa7fEcag', 'US', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Marcello Barenghi', 'https://www.youtube.com/channel/UCcBnT6LsxANZjUWqpjR8Jpw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCcBnT6LsxANZjUWqpjR8Jpw', 'IT', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Jazza', 'https://www.youtube.com/channel/UCHu2KNu6TtJ0p4hpSW7Yv7Q', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCHu2KNu6TtJ0p4hpSW7Yv7Q', 'AU', 'en', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Pipoune', 'https://www.youtube.com/channel/UCRi-EiVLhiE31HSqwW9JRjQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCRi-EiVLhiE31HSqwW9JRjQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'peindre ? enfantin !', 'https://www.youtube.com/channel/UCvtb2iEG5wpX198-6wd5yXA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCvtb2iEG5wpX198-6wd5yXA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Meeea', 'https://www.youtube.com/channel/UCEEi1rclmzywXicV7JfPjWw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCEEi1rclmzywXicV7JfPjWw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Basti Ui', 'https://www.youtube.com/channel/UCnnIP34CpT7nIBZS5KuuhbA', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCnnIP34CpT7nIBZS5KuuhbA', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'Basti Sans MS', 'https://www.youtube.com/channel/UC6sYueSaa1TwougrKgyuLvQ', 'https://www.youtube.com/feeds/videos.xml?channel_id=UC6sYueSaa1TwougrKgyuLvQ', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now()),
(gen_random_uuid(), 'QUENTIN', 'https://www.youtube.com/channel/UCB0ffaItcXwkgbVu8Hvq3Tw', 'https://www.youtube.com/feeds/videos.xml?channel_id=UCB0ffaItcXwkgbVu8Hvq3Tw', 'FR', 'fr', 'Youtube', 'Rss', true, now(), now())
ON CONFLICT DO NOTHING;
