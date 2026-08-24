-- ============================================
-- Complete reset script for Vigie24
-- Date: 2026-08-18
-- Description: Deletes all data and re-inserts the base sources
-- ============================================

-- Commencer une transaction
BEGIN;

-- ============================================
-- 1. Delete all data (in dependency order)
-- ============================================

-- Delete junction table data first
DELETE FROM article_votes;
DELETE FROM article_tags;
DELETE FROM article_sources;
DELETE FROM article_authors;

-- Delete main table data (except sources, categories, tags)
DELETE FROM articles;
DELETE FROM authors;
DELETE FROM users;

-- Delete tags and categories
DELETE FROM tags;
DELETE FROM categories;

-- Delete sources (full reset)
DELETE FROM sources;

-- ============================================
-- 2. Insert reference categories
-- ============================================
-- Note: Les noms de colonnes utilisent PascalCase

INSERT INTO categories ("Slug", "DisplayOrder", "IsActive") VALUES
('technologie', 1, true),
('securite', 2, true),
('open-source', 3, true),
('cloud-devops', 4, true),
('data-ia', 5, true),
('programmation', 6, true),
('hardware', 7, true),
('startups', 8, true);

-- ============================================
-- 3. Insert reference tags
-- ============================================

INSERT INTO tags ("Slug") VALUES
('ia'),
('llm'),
('python'),
('javascript'),
('docker'),
('kubernetes'),
('linux'),
('git'),
('cloud'),
('securite'),
('api'),
('machine-learning'),
('web'),
('mobile'),
('blockchain');

-- ============================================
-- 4. Insertion des sources RSS de base
-- ============================================

-- French technology sources
INSERT INTO sources ("Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive") VALUES
-- General news
('01net', 'https://www.01net.com/feed/', 'https://www.01net.com/', 'Press', 'Rss', true),
('Numerama', 'https://www.numerama.com/feed/', 'https://www.numerama.com/', 'Press', 'Rss', true),
('Journal du Net', 'https://www.journaldunet.com/feed/', 'https://www.journaldunet.com/', 'Press', 'Rss', true),

-- Security
('SecurityWeek', 'https://www.securityweek.com/feed/', 'https://www.securityweek.com/', 'Press', 'Rss', true),
('The Hacker News', 'https://thehackernews.com/feeds/posts/default', 'https://thehackernews.com/', 'Press', 'Rss', true),

-- Open Source
('OpenSource.com', 'https://opensource.com/feed', 'https://opensource.com/', 'BlogLab', 'Rss', true),
('Framablog', 'https://framablog.org/feed', 'https://framablog.org/', 'BlogLab', 'Rss', true),

-- DevOps & Cloud
('DevOps.com', 'https://devops.com/feed/', 'https://devops.com/', 'Press', 'Rss', true),
('Cloud Native Computing', 'https://www.cncf.io/feed/', 'https://www.cncf.io/', 'BlogLab', 'Rss', true),

-- IA & Data
('AI Trends', 'https://aitrends.com/feed/', 'https://aitrends.com/', 'Press', 'Rss', true),
('Medium AI', 'https://medium.com/feed/@towardsdatascience', 'https://towardsdatascience.com/', 'BlogLab', 'Rss', true),

-- Programmation
('Dev.to', 'https://dev.to/feed', 'https://dev.to/', 'BlogLab', 'Rss', true),
('Stack Overflow Blog', 'https://stackoverflow.blog/feed/', 'https://stackoverflow.blog/', 'BlogLab', 'Rss', true),
('GitHub Blog', 'https://github.blog/feed/', 'https://github.blog/', 'BlogLab', 'Rss', true),

-- Hardware
('AnandTech', 'https://www.anandtech.com/feed', 'https://www.anandtech.com/', 'Press', 'Rss', true),
('Tom''s Hardware', 'https://www.tomshardware.com/feeds/all', 'https://www.tomshardware.com/', 'Press', 'Rss', true);

-- ============================================
-- 5. Verification
-- ============================================

-- Show the number of inserted items
SELECT 
    'Sources' as table_name, COUNT(*) as count FROM sources
UNION ALL
SELECT 
    'Categories', COUNT(*) FROM categories
UNION ALL
SELECT 
    'Tags', COUNT(*) FROM tags;

-- Show the categories
SELECT "Slug", "DisplayOrder", "IsActive" FROM categories ORDER BY "DisplayOrder";

-- Afficher les sources actives
SELECT 
    "Name", 
    "FeedUrl", 
    "SourceKind",
    CASE 
        WHEN "IsActive" THEN '✓ Actif'
        ELSE '✗ Inactif'
    END as status
FROM sources 
ORDER BY "Name";

-- Commit de la transaction
COMMIT;

-- Message de confirmation
SELECT '✓ Réinitialisation terminée avec succès !' as message;
