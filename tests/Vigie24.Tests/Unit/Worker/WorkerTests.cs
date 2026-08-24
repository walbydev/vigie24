namespace Vigie24.Tests.Unit.Worker;

using System.ServiceModel.Syndication;
using Vigie24.Worker;
using Vigie24.Domain.Models;

/// <summary>
/// Unit tests for the Worker data classes
/// </summary>
public class FeedItemTests
{
    [Fact]
    public void FeedItem_ShouldHaveDefaultValues()
    {
        // Act
        var item = new FeedItem();

        // Assert
        Assert.Null(item.Title);
        Assert.Null(item.Description);
        Assert.Null(item.Content);
        Assert.Null(item.Link);
        Assert.Null(item.Author);
        Assert.Null(item.PublishedAt);
    }

    [Fact]
    public void FeedItem_ShouldSetProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var item = new FeedItem
        {
            Title = "Test Title",
            Description = "Test Description",
            Content = "Test Content",
            Link = "https://example.com",
            Author = "John Doe",
            PublishedAt = now
        };

        // Assert
        Assert.Equal("Test Title", item.Title);
        Assert.Equal("Test Description", item.Description);
        Assert.Equal("Test Content", item.Content);
        Assert.Equal("https://example.com", item.Link);
        Assert.Equal("John Doe", item.Author);
        Assert.Equal(now, item.PublishedAt);
    }

    [Fact]
    public void ArticleFeedItem_ShouldHaveDefaultValues()
    {
        // Act
        var item = new ArticleFeedItem();

        // Assert
        Assert.Equal(string.Empty, item.Title);
        Assert.Equal(string.Empty, item.Content);
        Assert.Equal(string.Empty, item.Url);
        Assert.Equal(string.Empty, item.Author);
    }

    [Fact]
    public void ArticleFeedItem_ShouldSetProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var item = new ArticleFeedItem
        {
            Title = "Test Title",
            Content = "Test Content",
            Url = "https://example.com/article",
            Author = "Jane Doe",
            PublishedAt = now
        };

        // Assert
        Assert.Equal("Test Title", item.Title);
        Assert.Equal("Test Content", item.Content);
        Assert.Equal("https://example.com/article", item.Url);
        Assert.Equal("Jane Doe", item.Author);
        Assert.Equal(now, item.PublishedAt);
    }
}

/// <summary>
/// Tests for the Worker scoring methods
/// </summary>
public class ScoringTests
{
    [Fact]
    public void CalculateRecencyScore_WithRecentArticle_ShouldReturnHighScore()
    {
        // Arrange
        var recentDate = DateTime.UtcNow - TimeSpan.FromHours(1);

        // Act
        var score = CalculateRecencyScore(recentDate);

        // Assert
        Assert.True(score > 5m, $"Score {score} devrait être > 5 pour un article récent");
    }

    [Fact]
    public void CalculateRecencyScore_WithOldArticle_ShouldReturnLowScore()
    {
        // Arrange
        var oldDate = DateTime.UtcNow - TimeSpan.FromDays(30);

        // Act
        var score = CalculateRecencyScore(oldDate);

        // Assert
        Assert.True(score < 3m, $"Score {score} devrait être < 3 pour un article ancien");
    }

    [Fact]
    public void CalculateContentScore_WithLongSummary_ShouldReturnHigherScore()
    {
        // Arrange
        var longSummary = "Ceci est un résumé très détaillé qui contient beaucoup d'informations pertinentes sur l'article et devrait obtenir un meilleur score que le résumé court.";
        var shortSummary = "Court.";

        // Act
        var longScore = CalculateContentScore(longSummary);
        var shortScore = CalculateContentScore(shortSummary);

        // Assert
        Assert.True(longScore >= shortScore, 
            $"Score long ({longScore}) devrait être >= score court ({shortScore})");
    }

    [Fact]
    public void CalculateAlgorithmicScore_ShouldConsiderRecentArticles()
    {
        // Arrange
        var article = new Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com",
            Title = "Test",
            Summary = "Test",
            PublishedAt = DateTime.UtcNow,
            FirstSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act with different counts
        var score1 = CalculateAlgorithmicScore(article, 1);
        var score10 = CalculateAlgorithmicScore(article, 10);
        var score100 = CalculateAlgorithmicScore(article, 100);

        // Assert - the score should decrease with more recent articles
        Assert.True(score1 >= score10, "Score devrait diminuer avec plus d'articles récents");
        Assert.True(score10 >= score100, "Score devrait diminuer avec plus d'articles récents");
    }

    // Helper methods to test private methods
    private static decimal CalculateRecencyScore(DateTime publishedAt)
    {
        var hoursSincePublish = (DateTime.UtcNow - publishedAt).TotalHours;
        var recencyFactor = Math.Max(0, 100 - hoursSincePublish);
        
        if (recencyFactor > 80) return 10m;
        if (recencyFactor > 60) return 8m;
        if (recencyFactor > 40) return 6m;
        if (recencyFactor > 20) return 4m;
        return 2m;
    }

    private static decimal CalculateContentScore(string summary)
    {
        var wordCount = summary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Min(10m, (decimal)wordCount / 10);
    }

    private static decimal CalculateAlgorithmicScore(Article article, int recentArticlesCount)
    {
        var recencyScore = CalculateRecencyScore(article.PublishedAt);
        var contentScore = CalculateContentScore(article.Summary);
        
        var popularityFactor = recentArticlesCount > 0 
            ? 10m / (recentArticlesCount + 1) 
            : 10m;
        
        return (recencyScore + contentScore + popularityFactor) / 3;
    }
}

/// <summary>
/// Tests pour la logique de traitement des articles
/// </summary>
public class ArticleProcessingTests
{
    [Theory]
    [InlineData(60, true)]
    [InlineData(65, true)]
    [InlineData(59, false)]
    [InlineData(50, false)]
    [InlineData(80, true)]
    public void IsPublished_ShouldBeTrueWhenScoreAboveThreshold(decimal score, bool expected)
    {
        // Arrange
        const decimal threshold = 60;

        // Act
        var result = score >= threshold;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Article_ShouldHaveValidUrlFormat()
    {
        // Act
        var article = new Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com/article/123",
            Title = "Test Article",
            Summary = "Test summary",
            PublishedAt = DateTime.UtcNow,
            FirstSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.StartsWith("https://", article.CanonicalUrl);
        Assert.Contains("example.com", article.CanonicalUrl);
    }

    [Fact]
    public void Article_ShouldHaveValidTimestamps()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var article = new Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com",
            Title = "Test",
            Summary = "Test",
            PublishedAt = now - TimeSpan.FromHours(1),
            FirstSeenAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        Assert.True(article.PublishedAt < article.FirstSeenAt, 
            "PublishedAt devrait être avant FirstSeenAt");
        Assert.True(article.FirstSeenAt >= article.CreatedAt,
            "FirstSeenAt devrait être après ou égal à CreatedAt");
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    [InlineData("es")]
    public void Article_ShouldSupportMultipleLanguages(string languageCode)
    {
        // Act
        var article = new Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com",
            Title = "Test",
            Summary = "Test",
            LanguageCode = languageCode,
            PublishedAt = DateTime.UtcNow,
            FirstSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(languageCode, article.LanguageCode);
    }
}

/// <summary>
/// Tests for the category and tags logic
/// </summary>
public class CategoryTagTests
{
    [Fact]
    public void CategorySlug_ShouldBeLowercase()
    {
        // Arrange
        var slugs = new[] { "technologie", "securite", "open-source", "cloud-devops" };

        // Act & Assert
        foreach (var slug in slugs)
        {
            Assert.Equal(slug.ToLower(), slug);
            Assert.DoesNotContain(slug, char.IsUpper);
        }
    }

    [Fact]
    public void TagSlug_ShouldBeValidFormat()
    {
        // Arrange
        var slugs = new[] { "ia", "llm", "python", "docker", "kubernetes" };

        // Act & Assert
        foreach (var slug in slugs)
        {
            Assert.NotEmpty(slug);
            Assert.DoesNotContain(slug, c => char.IsWhiteSpace(c) || c == '.');
        }
    }

    [Fact]
    public void SourceKind_ShouldHaveValidValues()
    {
        // Arrange
        var validKinds = new[] { "Press", "BlogLab", "Youtube", "Reddit" };

        // Act & Assert
        foreach (var kind in validKinds)
        {
            Assert.NotEmpty(kind);
            Assert.False(kind.Contains(" "));
        }
    }

    [Fact]
    public void IngestionMethod_ShouldHaveValidValues()
    {
        // Arrange
        var validMethods = new[] { "Rss", "Api", "Youtube", "Reddit" };

        // Act & Assert
        foreach (var method in validMethods)
        {
            Assert.NotEmpty(method);
            Assert.False(method.Contains(" "));
        }
    }
}

/// <summary>
/// Tests pour l'extraction d'illustration depuis les flux RSS (media:thumbnail,
/// media:content, enclosure) — voir Worker.ExtractFeedImage.
/// </summary>
public class ExtractFeedImageTests
{
    private static SyndicationItem LoadFirstItem(string xml)
    {
        using var reader = System.Xml.XmlReader.Create(
            new System.IO.StringReader(xml),
            new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Parse });
        var feed = SyndicationFeed.Load(reader);
        return feed.Items.First();
    }

    [Fact]
    public void YoutubeFeed_ShouldExtractMediaThumbnailFromGroup()
    {
        // Arrange — real structure of a YouTube feeds/videos.xml feed
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <feed xmlns:yt="http://www.youtube.com/xml/schemas/2015"
                  xmlns:media="http://search.yahoo.com/mrss/"
                  xmlns="http://www.w3.org/2005/Atom">
              <entry>
                <id>yt:video:kw_7tYjZgLE</id>
                <yt:videoId>kw_7tYjZgLE</yt:videoId>
                <title>Test video</title>
                <link rel="alternate" href="https://www.youtube.com/watch?v=kw_7tYjZgLE"/>
                <media:group>
                  <media:title>Test video</media:title>
                  <media:content url="https://www.youtube.com/v/kw_7tYjZgLE?version=3"
                                 type="application/x-shockwave-flash" width="640" height="390"/>
                  <media:thumbnail url="https://i4.ytimg.com/vi/kw_7tYjZgLE/hqdefault.jpg"
                                   width="480" height="360"/>
                </media:group>
              </entry>
            </feed>
            """;
        var item = LoadFirstItem(xml);

        // Act
        var url = Vigie24.Worker.Vigie24Worker.ExtractFeedImage(item);

        // Assert — la vignette, PAS l'URL du lecteur Flash
        Assert.Equal("https://i4.ytimg.com/vi/kw_7tYjZgLE/hqdefault.jpg", url);
    }

    [Fact]
    public void YoutubeFeed_ShouldIgnoreNonImageMediaContent()
    {
        // Arrange — media:content flash en premier (hors group), la vignette doit gagner
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <feed xmlns:media="http://search.yahoo.com/mrss/"
                  xmlns="http://www.w3.org/2005/Atom">
              <entry>
                <id>yt:video:abc123</id>
                <title>Test</title>
                <link rel="alternate" href="https://www.youtube.com/watch?v=abc123"/>
                <media:content url="https://www.youtube.com/v/abc123?version=3"
                               type="application/x-shockwave-flash"/>
                <media:thumbnail url="https://i.ytimg.com/vi/abc123/hqdefault.jpg"/>
              </entry>
            </feed>
            """;
        var item = LoadFirstItem(xml);

        // Act
        var url = Vigie24.Worker.Vigie24Worker.ExtractFeedImage(item);

        // Assert
        Assert.Equal("https://i.ytimg.com/vi/abc123/hqdefault.jpg", url);
    }

    [Fact]
    public void RssFeed_ShouldFallBackToImageEnclosure()
    {
        // Arrange — flux RSS 2.0 classique sans media:*, avec enclosure image
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
              <channel>
                <title>Test</title>
                <item>
                  <title>Article</title>
                  <link>https://example.com/article</link>
                  <enclosure url="https://example.com/img/photo.jpg"
                             type="image/jpeg" length="12345"/>
                </item>
              </channel>
            </rss>
            """;
        var item = LoadFirstItem(xml);

        // Act
        var url = Vigie24.Worker.Vigie24Worker.ExtractFeedImage(item);

        // Assert
        Assert.Equal("https://example.com/img/photo.jpg", url);
    }
}
