-- Add test RSS sources
INSERT INTO sources (
    "Name", 
    "WebsiteUrl", 
    "FeedUrl", 
    "CountryCode", 
    "LanguageCode", 
    "SourceKind", 
    "IngestionMethod", 
    "IsActive"
) VALUES 
    ('Le Monde Informatique', 'https://www.lemondeinformatique.fr', 'https://www.lemondeinformatique.fr/actualites/rss', 'FR', 'fr', 'Press', 'Rss', true),
    ('Developpez.com', 'https://developpez.com', 'https://www.developpez.com/actualites/rss/', 'FR', 'fr', 'Press', 'Rss', true),
    ('Hacker News', 'https://news.ycombinator.com', 'https://hnrss.org/frontpage', 'US', 'en', 'Other', 'Rss', true),
    ('ZDNet France', 'https://www.zdnet.fr', 'https://www.zdnet.fr/actualites/rss.xml', 'FR', 'fr', 'Press', 'Rss', true),
    ('Next INpact', 'https://www.nextinpact.com', 'https://www.nextinpact.com/rss.xml', 'FR', 'fr', 'Press', 'Rss', true),
    ('Thurium', 'https://thurium.fr', 'https://thurium.fr/feed.rss', 'FR', 'fr', 'Press', 'Rss', true),
    ('01net', 'https://www.01net.com', 'https://www.01net.com/astuces/rss.xml', 'FR', 'fr', 'Press', 'Rss', true)
ON CONFLICT DO NOTHING;
