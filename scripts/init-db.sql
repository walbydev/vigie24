-- Vigie24 Database Schema
-- PostgreSQL 16+

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Categories table
CREATE TABLE categories (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Slug" VARCHAR(100) NOT NULL UNIQUE,
    "ParentCategoryId" UUID REFERENCES categories("Id") ON DELETE SET NULL,
    "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Sources table
CREATE TABLE sources (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(255) NOT NULL,
    "CountryCode" VARCHAR(2),
    "LanguageCode" VARCHAR(5) NOT NULL DEFAULT 'fr',
    "SourceKind" VARCHAR(20) NOT NULL CHECK ("SourceKind" IN ('Press', 'BlogLab', 'Youtube', 'Reddit', 'Other')),
    "IngestionMethod" VARCHAR(20) NOT NULL CHECK ("IngestionMethod" IN ('Rss', 'Api', 'Scraping', 'Youtube', 'Reddit')),
    "WebsiteUrl" TEXT NOT NULL,
    "FeedUrl" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "LastIngestedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Authors table
CREATE TABLE authors (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(255) NOT NULL,
    "ProfileUrl" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tags table
CREATE TABLE tags (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Slug" VARCHAR(100) NOT NULL UNIQUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Users table
CREATE TABLE users (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Email" VARCHAR(500) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(500) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Articles table
CREATE TABLE articles (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Title" VARCHAR(500) NOT NULL,
    "Summary" TEXT NOT NULL,
    "Content" TEXT,
    "CanonicalUrl" TEXT NOT NULL UNIQUE,
    "WebsiteUrl" TEXT,
    "SourceUrl" TEXT,
    "PublishedAt" TIMESTAMP NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CategoryId" UUID REFERENCES categories("Id") ON DELETE SET NULL,
    "RankingScore" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "AlgorithmicScore" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "EditorialScore" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "CrossSourceCount" INTEGER NOT NULL DEFAULT 0,
    "FirstSeenAt" TIMESTAMP,
    "ImageUrl" TEXT,
    "ImageAltText" TEXT,
    "IsPublished" BOOLEAN NOT NULL DEFAULT false,
    "LanguageCode" VARCHAR(10),
    "VoteAverage" DECIMAL(3,2) NOT NULL DEFAULT 0,
    "VoteCount" INTEGER NOT NULL DEFAULT 0,
    "IsProcessed" BOOLEAN NOT NULL DEFAULT false
);

-- ArticleSources junction table
CREATE TABLE article_sources (
    "ArticleId" UUID NOT NULL REFERENCES articles("Id") ON DELETE CASCADE,
    "SourceId" UUID NOT NULL REFERENCES sources("Id") ON DELETE CASCADE,
    "SourceArticleUrl" TEXT NOT NULL,
    "SourceTitle" TEXT,
    "SourcePublishedAt" TIMESTAMP WITHOUT TIME ZONE,
    "SourceAuthorText" TEXT,
    "IsPrimary" BOOLEAN NOT NULL DEFAULT false,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("ArticleId", "SourceId")
);

-- ArticleAuthors junction table
CREATE TABLE article_authors (
    "ArticleId" UUID NOT NULL REFERENCES articles("Id") ON DELETE CASCADE,
    "AuthorId" UUID NOT NULL REFERENCES authors("Id") ON DELETE CASCADE,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("ArticleId", "AuthorId")
);

-- ArticleTags junction table
CREATE TABLE article_tags (
    "ArticleId" UUID NOT NULL REFERENCES articles("Id") ON DELETE CASCADE,
    "TagId" UUID NOT NULL REFERENCES tags("Id") ON DELETE CASCADE,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("ArticleId", "TagId")
);

-- ArticleVotes table
CREATE TABLE article_votes (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId" UUID NOT NULL REFERENCES users("Id") ON DELETE CASCADE,
    "ArticleId" UUID NOT NULL REFERENCES articles("Id") ON DELETE CASCADE,
    "VoteType" INTEGER NOT NULL CHECK ("VoteType" IN (-1, 1)),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE ("UserId", "ArticleId")
);

-- Indexes
CREATE INDEX idx_articles_category ON articles("CategoryId");
CREATE INDEX idx_articles_published_at ON articles("PublishedAt" DESC);
CREATE INDEX idx_articles_ranking_score ON articles("RankingScore" DESC);
CREATE INDEX idx_articles_canonical_url ON articles("CanonicalUrl");
CREATE INDEX idx_articles_is_processed ON articles("IsProcessed") WHERE "IsProcessed" = false;
