namespace Vigie24.Tests.Integration;

using Vigie24.Infrastructure.Data;
using Vigie24.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

/// <summary>
/// Integration tests for the DbContext with an in-memory database
/// </summary>
public class Vigie24DbContextInMemoryTests : IAsyncLifetime
{
    private Vigie24DbContext _context = null!;
    private DbContextOptions<Vigie24DbContext> _options = null!;

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<Vigie24DbContext>()
            .UseInMemoryDatabase(databaseName: $"Vigie24IntegrationTest_{Guid.NewGuid()}")
            .Options;

        _context = new Vigie24DbContext(_options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context.Dispose();
    }

    [Fact]
    public async Task SaveAndRetrieveArticle_ShouldPersistData()
    {
        // Arrange
        var article = new Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com/article",
            Title = "Test Article",
            Summary = "Test summary",
            LanguageCode = "fr",
            PublishedAt = DateTime.UtcNow - TimeSpan.FromHours(1),
            FirstSeenAt = DateTime.UtcNow,
            EditorialScore = 8.5m,
            AlgorithmicScore = 7.2m,
            RankingScore = 8.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        // Assert
        var savedArticle = await _context.Articles.FindAsync(article.Id);
        Assert.NotNull(savedArticle);
        Assert.Equal(article.Title, savedArticle.Title);
        Assert.Equal(article.CanonicalUrl, savedArticle.CanonicalUrl);
        Assert.Equal(8.5m, savedArticle.EditorialScore);
    }

    [Fact]
    public async Task GetActiveSources_ShouldReturnOnlyActiveSources()
    {
        // Arrange
        var activeSource = new Source
        {
            Id = Guid.NewGuid(),
            Name = "Active",
            FeedUrl = "https://active.com/feed",
            WebsiteUrl = "https://active.com",
            SourceKind = "Press",
            IngestionMethod = "Rss",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveSource = new Source
        {
            Id = Guid.NewGuid(),
            Name = "Inactive",
            FeedUrl = "https://inactive.com/feed",
            WebsiteUrl = "https://inactive.com",
            SourceKind = "Press",
            IngestionMethod = "Rss",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sources.AddRange(activeSource, inactiveSource);
        await _context.SaveChangesAsync();

        // Act
        var activeSources = await _context.Sources
            .Where(s => s.IsActive)
            .ToListAsync();

        // Assert
        Assert.Single(activeSources);
        Assert.Equal("Active", activeSources[0].Name);
    }

    [Fact]
    public async Task GetArticlesByCategory_ShouldReturnCorrectArticles()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        
        var article1 = new Article
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            CanonicalUrl = "https://example.com/1",
            Title = "Article 1",
            Summary = "Test",
            PublishedAt = DateTime.UtcNow,
            FirstSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var article2 = new Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com/2",
            Title = "Article 2",
            Summary = "Test",
            PublishedAt = DateTime.UtcNow,
            FirstSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Articles.AddRange(article1, article2);
        await _context.SaveChangesAsync();

        // Act
        var articles = await _context.Articles
            .Where(a => a.CategoryId == categoryId)
            .ToListAsync();

        // Assert
        Assert.Single(articles);
        Assert.Equal("Article 1", articles[0].Title);
    }

    [Fact]
    public async Task GetRecentArticles_ShouldOrderByPublishedAtDescending()
    {
        // Arrange
        var articles = new List<Article>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                CanonicalUrl = "https://example.com/1",
                Title = "Oldest",
                Summary = "Test",
                PublishedAt = DateTime.UtcNow - TimeSpan.FromHours(3),
                FirstSeenAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                CanonicalUrl = "https://example.com/2",
                Title = "Newest",
                Summary = "Test",
                PublishedAt = DateTime.UtcNow - TimeSpan.FromHours(1),
                FirstSeenAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Articles.AddRange(articles);
        await _context.SaveChangesAsync();

        // Act
        var recentArticles = await _context.Articles
            .OrderByDescending(a => a.PublishedAt)
            .Take(2)
            .ToListAsync();

        // Assert
        Assert.Equal(2, recentArticles.Count);
        Assert.Equal("Newest", recentArticles[0].Title);
    }

    [Fact]
    public async Task BulkInsertArticles_ShouldHandleMultipleArticles()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var articles = new List<Article>();
        for (int i = 0; i < 50; i++)
        {
            articles.Add(new Article
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                CanonicalUrl = $"https://example.com/{i}",
                Title = $"Article {i}",
                Summary = $"Summary {i}",
                PublishedAt = DateTime.UtcNow,
                FirstSeenAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Act
        _context.Articles.AddRange(articles);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.Articles.CountAsync();
        Assert.Equal(50, count);
    }

    [Fact]
    public async Task GetArticlesWithHighScore_ShouldFilterCorrectly()
    {
        // Arrange
        var articles = new List<Article>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                CanonicalUrl = "https://example.com/1",
                Title = "High Score",
                Summary = "Test",
                PublishedAt = DateTime.UtcNow,
                FirstSeenAt = DateTime.UtcNow,
                EditorialScore = 85m,
                AlgorithmicScore = 80m,
                RankingScore = 82m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                CanonicalUrl = "https://example.com/2",
                Title = "Low Score",
                Summary = "Test",
                PublishedAt = DateTime.UtcNow,
                FirstSeenAt = DateTime.UtcNow,
                EditorialScore = 45m,
                AlgorithmicScore = 40m,
                RankingScore = 42m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Articles.AddRange(articles);
        await _context.SaveChangesAsync();

        // Act
        var highScoreArticles = await _context.Articles
            .Where(a => a.RankingScore >= 70m)
            .ToListAsync();

        // Assert
        Assert.Single(highScoreArticles);
        Assert.Equal("High Score", highScoreArticles[0].Title);
    }
}
