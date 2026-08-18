-- ============================================
-- Script de création des données de test
-- Date: 2026-08-18
-- Description: Insère des données de test pour les tests unitaires et d'intégration
-- ============================================

BEGIN;

-- Supprimer les données existantes (pour éviter les doublons)
DELETE FROM articles;
DELETE FROM source_tag;
DELETE FROM source_category;
DELETE FROM articles_tag;
DELETE FROM articles_category;
DELETE FROM tags;
DELETE FROM categories;
DELETE FROM sources;
DELETE FROM authors;

-- ============================================
-- AUTHORS
-- ============================================
INSERT INTO authors ("Id", "Name", "Bio") VALUES
('a1111111-1111-1111-1111-111111111101', 'Jean Dupont', 'Journaliste tech spécialisé en IA'),
('a1111111-1111-1111-1111-111111111102', 'Marie Martin', 'Expert en sécurité informatique'),
('a1111111-1111-1111-1111-111111111103', 'Pierre Bernard', 'Développeur full-stack'),
('a1111111-1111-1111-1111-111111111104', 'Sophie Thomas', 'Data scientist'),
('a1111111-1111-1111-1111-111111111105', 'Luc Petit', 'Architecte cloud');

-- ============================================
-- CATEGORIES
-- ============================================
INSERT INTO categories ("Id", "Slug", "DisplayName", "Description", "DisplayOrder", "IsActive") VALUES
('c2222222-2222-2222-2222-222222222201', 'technologie', 'Technologie', 'Actualités technologiques générales', 1, true),
('c2222222-2222-2222-2222-222222222202', 'securite', 'Sécurité', 'Cybersécurité et protection des données', 2, true),
('c2222222-2222-2222-2222-222222222203', 'open-source', 'Open Source', 'Logiciels libres et communautés', 3, true),
('c2222222-2222-2222-2222-222222222204', 'cloud-devops', 'Cloud & DevOps', 'Infrastructure et déploiement', 4, true),
('c2222222-2222-2222-2222-222222222205', 'data-ia', 'Data & IA', 'Intelligence artificielle et données', 5, true),
('c2222222-2222-2222-2222-222222222206', 'programmation', 'Programmation', 'Développement logiciel', 6, true),
('c2222222-2222-2222-2222-222222222207', 'hardware', 'Hardware', 'Matériel et composants', 7, true),
('c2222222-2222-2222-2222-222222222208', 'startups', 'Startups', 'Écosystème startup', 8, true);

-- ============================================
-- TAGS
-- ============================================
INSERT INTO tags ("Id", "Slug", "DisplayName", "Description", "IsActive") VALUES
('t3333333-3333-3333-3333-333333333301', 'ia', 'IA', 'Intelligence artificielle', true),
('t3333333-3333-3333-3333-333333333302', 'llm', 'LLM', 'Large Language Models', true),
('t3333333-3333-3333-3333-333333333303', 'python', 'Python', 'Langage Python', true),
('t3333333-3333-3333-3333-333333333304', 'javascript', 'JavaScript', 'Langage JavaScript', true),
('t3333333-3333-3333-3333-333333333305', 'docker', 'Docker', 'Conteneurisation', true),
('t3333333-3333-3333-3333-333333333306', 'kubernetes', 'Kubernetes', 'Orchestration de conteneurs', true),
('t3333333-3333-3333-3333-333333333307', 'linux', 'Linux', 'Système d\'exploitation Linux', true),
('t3333333-3333-3333-3333-333333333308', 'git', 'Git', 'Gestion de version', true),
('t3333333-3333-3333-3333-333333333309', 'cloud', 'Cloud', 'Cloud computing', true),
('t3333333-3333-3333-3333-333333333310', 'securite', 'Sécurité', 'Cybersécurité', true),
('t3333333-3333-3333-3333-333333333311', 'api', 'API', 'Interfaces de programmation', true),
('t3333333-3333-3333-3333-333333333312', 'machine-learning', 'Machine Learning', 'Apprentissage automatique', true),
('t3333333-3333-3333-3333-333333333313', 'web', 'Web', 'Développement web', true),
('t3333333-3333-3333-3333-333333333314', 'mobile', 'Mobile', 'Développement mobile', true),
('t3333333-3333-3333-3333-333333333315', 'blockchain', 'Blockchain', 'Technologie blockchain', true);

-- ============================================
-- SOURCES
-- ============================================
INSERT INTO sources ("Id", "Name", "FeedUrl", "WebsiteUrl", "SourceKind", "IngestionMethod", "IsActive", "LastFetchAt", "NextFetchAt", "FetchIntervalMinutes", "LastError", "ErrorCount", "CreatedAt", "UpdatedAt") VALUES
('s4444444-4444-4444-4444-444444444401', '01net', 'https://www.01net.com/feed/', 'https://www.01net.com/', 'Press', 'Rss', true, NOW() - INTERVAL '1 hour', NOW() + INTERVAL '15 minutes', 15, NULL, 0, NOW(), NOW()),
('s4444444-4444-4444-4444-444444444402', 'Numerama', 'https://www.numerama.com/feed/', 'https://www.numerama.com/', 'Press', 'Rss', true, NOW() - INTERVAL '2 hours', NOW() + INTERVAL '15 minutes', 15, NULL, 0, NOW(), NOW()),
('s4444444-4444-4444-4444-444444444403', 'SecurityWeek', 'https://www.securityweek.com/feed/', 'https://www.securityweek.com/', 'Press', 'Rss', true, NOW() - INTERVAL '30 minutes', NOW() + INTERVAL '15 minutes', 15, NULL, 0, NOW(), NOW()),
('s4444444-4444-4444-4444-444444444404', 'OpenSource.com', 'https://opensource.com/feed', 'https://opensource.com/', 'BlogLab', 'Rss', true, NOW() - INTERVAL '45 minutes', NOW() + INTERVAL '15 minutes', 15, NULL, 0, NOW(), NOW()),
('s4444444-4444-4444-4444-444444444405', 'DevOps.com', 'https://devops.com/feed/', 'https://devops.com/', 'Press', 'Rss', true, NOW() - INTERVAL '20 minutes', NOW() + INTERVAL '15 minutes', 15, NULL, 0, NOW(), NOW());

-- ============================================
-- RELATIONS SOURCE-CATEGORY
-- ============================================
INSERT INTO source_category ("SourceId", "CategoryId") VALUES
('s4444444-4444-4444-4444-444444444401', 'c2222222-2222-2222-2222-222222222201'), -- 01net -> technologie
('s4444444-4444-4444-4444-444444444402', 'c2222222-2222-2222-2222-222222222205'), -- Numerama -> data-ia
('s4444444-4444-4444-4444-444444444403', 'c2222222-2222-2222-2222-222222222202'), -- SecurityWeek -> securite
('s4444444-4444-4444-4444-444444444404', 'c2222222-2222-2222-2222-222222222203'), -- OpenSource.com -> open-source
('s4444444-4444-4444-4444-444444444405', 'c2222222-2222-2222-2222-222222222204'); -- DevOps.com -> cloud-devops

-- ============================================
-- ARTICLES
-- ============================================
INSERT INTO articles ("Id", "Title", "Summary", "Content", "Url", "PublishedAt", "SourceId", "AuthorId", "CreatedAt", "UpdatedAt", "IsProcessed", "ProcessedAt", "AiAnalysis", "AiScore", "AiTags") VALUES
('a5555555-5555-5555-5555-555555555501', 'Nouvelle avancée en IA générative', 'Les chercheurs présentent un nouveau modèle de langage plus efficace', 'Contenu détaillé de l''article sur l''IA...', 'https://example.com/ai-advance', NOW() - INTERVAL '1 hour', 's4444444-4444-4444-4444-444444444401', 'a1111111-1111-1111-1111-111111111101', NOW(), NOW(), true, NOW(), '{"summary": "Avancée majeure", "sentiment": "positive", "keywords": ["IA", "LLM", "recherche"]}', 8.5, '{"ia", "llm", "machine-learning"}'),
('a5555555-5555-5555-5555-555555555502', 'Faille de sécurité critique découverte', 'Une vulnérabilité majeure affecte des millions d''appareils', 'Analyse détaillée de la faille...', 'https://example.com/security-flaw', NOW() - INTERVAL '2 hours', 's4444444-4444-4444-4444-444444444403', 'a1111111-1111-1111-1111-111111111102', NOW(), NOW(), true, NOW(), '{"summary": "Sécurité compromise", "sentiment": "negative", "keywords": ["sécurité", "faille", "patch"]}', 9.2, '{"securite", "api", "cloud"}'),
('a5555555-5555-5555-5555-555555555503', 'Kubernetes 1.29 publié', 'Nouvelles fonctionnalités et améliorations de performance', 'Détails de la nouvelle version...', 'https://example.com/k8s-release', NOW() - INTERVAL '3 hours', 's4444444-4444-4444-4444-444444444405', 'a1111111-1111-1111-1111-111111111103', NOW(), NOW(), false, NULL, NULL, NULL, NULL),
('a5555555-5555-5555-5555-555555555504', 'Python 3.13 en bêta', 'Nouvelles syntaxes et optimisations', 'Présentation des nouveautés...', 'https://example.com/python-beta', NOW() - INTERVAL '4 hours', 's4444444-4444-4444-4444-444444444404', 'a1111111-1111-1111-1111-111111111103', NOW(), NOW(), true, NOW(), '{"summary": "Mise à jour majeure", "sentiment": "positive", "keywords": ["Python", "développement", "performance"]}', 7.8, '{"python", "programmation", "web"}'),
('a5555555-5555-5555-5555-555555555505', 'Startup française lève 50M€', 'Financement pour développer sa plateforme d''IA', 'Histoire de la startup...', 'https://example.com/startup-funding', NOW() - INTERVAL '5 hours', 's4444444-4444-4444-4444-444444444402', 'a1111111-1111-1111-1111-111111111105', NOW(), NOW(), false, NULL, NULL, NULL, NULL);

-- ============================================
-- RELATIONS ARTICLE-TAG
-- ============================================
INSERT INTO articles_tag ("ArticleId", "TagId") VALUES
('a5555555-5555-5555-5555-555555555501', 't3333333-3333-3333-3333-333333333301'), -- IA
('a5555555-5555-5555-5555-555555555501', 't3333333-3333-3333-3333-333333333302'), -- LLM
('a5555555-5555-5555-5555-555555555501', 't3333333-3333-3333-3333-333333333312'), -- ML
('a5555555-5555-5555-5555-555555555502', 't3333333-3333-3333-3333-333333333310'), -- Sécurité
('a5555555-5555-5555-5555-555555555502', 't3333333-3333-3333-3333-333333333311'), -- API
('a5555555-5555-5555-5555-555555555502', 't3333333-3333-3333-3333-333333333309'), -- Cloud
('a5555555-5555-5555-5555-555555555503', 't3333333-3333-3333-3333-333333333306'), -- Kubernetes
('a5555555-5555-5555-5555-555555555503', 't3333333-3333-3333-3333-333333333305'), -- Docker
('a5555555-5555-5555-5555-555555555504', 't3333333-3333-3333-3333-333333333303'), -- Python
('a5555555-5555-5555-5555-555555555505', 't3333333-3333-3333-3333-333333333301'); -- IA

-- ============================================
-- RELATIONS ARTICLE-CATEGORY
-- ============================================
INSERT INTO articles_category ("ArticleId", "CategoryId") VALUES
('a5555555-5555-5555-5555-555555555501', 'c2222222-2222-2222-2222-222222222205'), -- IA -> Data & IA
('a5555555-5555-5555-5555-555555555502', 'c2222222-2222-2222-2222-222222222202'), -- Sécurité
('a5555555-5555-5555-5555-555555555503', 'c2222222-2222-2222-2222-222222222204'), -- DevOps
('a5555555-5555-5555-5555-555555555504', 'c2222222-2222-2222-2222-222222222206'), -- Programmation
('a5555555-5555-5555-5555-555555555505', 'c2222222-2222-2222-2222-222222222208'); -- Startups

COMMIT;

-- Vérification
SELECT 'Authors: ' || COUNT(*) FROM authors
UNION ALL SELECT 'Categories: ' || COUNT(*) FROM categories
UNION ALL SELECT 'Tags: ' || COUNT(*) FROM tags
UNION ALL SELECT 'Sources: ' || COUNT(*) FROM sources
UNION ALL SELECT 'Articles: ' || COUNT(*) FROM articles;
