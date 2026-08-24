namespace Vigie24.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the domain models
/// </summary>
public class ArticleTests
{
    [Fact]
    public void CreateArticle_WithValidData_ShouldSucceed()
    {
        // Act
        var article = new Vigie24.Domain.Models.Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com/article",
            Title = "Test Article",
            Summary = "Test summary",
            LanguageCode = "fr",
            PublishedAt = DateTime.UtcNow,
            FirstSeenAt = DateTime.UtcNow,
            EditorialScore = 8.5m,
            AlgorithmicScore = 7.2m,
            RankingScore = 8.0m,
            
        };

        // Assert
        Assert.Equal("Test Article", article.Title);
        Assert.Equal("Test summary", article.Summary);
        Assert.Equal("https://example.com/article", article.CanonicalUrl);
        Assert.Equal("fr", article.LanguageCode);
        Assert.Equal(8.5m, article.EditorialScore);
    }

    [Fact]
    public void Article_ShouldHaveDefaultValues()
    {
        // Act
        var article = new Vigie24.Domain.Models.Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com",
            Title = "Test",
            Summary = "Test",
            PublishedAt = DateTime.UtcNow,
            
        };

        // Assert
        Assert.Null(article.ImageUrl);
        Assert.Null(article.ImageAltText);
        Assert.Equal("fr", article.LanguageCode);
        Assert.Equal(0, article.VoteCount);
        Assert.False(article.IsPublished);
    }
}

/// <summary>
/// Tests for the Source model
/// </summary>
public class SourceTests
{
    [Fact]
    public void CreateSource_WithValidData_ShouldSucceed()
    {
        // Act
        var source = new Vigie24.Domain.Models.Source
        {
            Id = Guid.NewGuid(),
            Name = "Test Source",
            FeedUrl = "https://example.com/feed",
            WebsiteUrl = "https://example.com",
            SourceKind = "Press",
            IngestionMethod = "Rss",
            IsActive = true,
            
        };

        // Assert
        Assert.Equal("Test Source", source.Name);
        Assert.Equal("https://example.com/feed", source.FeedUrl);
        Assert.True(source.IsActive);
        Assert.Equal("Press", source.SourceKind);
        Assert.Equal("Rss", source.IngestionMethod);
    }

    [Fact]
    public void Source_ShouldHaveDefaultIsActiveTrue()
    {
        // Act
        var source = new Vigie24.Domain.Models.Source
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            FeedUrl = "https://example.com/feed",
            WebsiteUrl = "https://example.com",
            SourceKind = "Press",
            IngestionMethod = "Rss",
            
        };

        // Assert
        Assert.True(source.IsActive);
    }
}

/// <summary>
/// Tests for the Category model
/// </summary>
public class CategoryTests
{
    [Fact]
    public void CreateCategory_WithValidData_ShouldSucceed()
    {
        // Act
        var category = new Vigie24.Domain.Models.Category
        {
            Id = Guid.NewGuid(),
            Slug = "technologie",
            DisplayOrder = 1,
            IsActive = true
        };

        // Assert
        Assert.Equal("technologie", category.Slug);
        Assert.Equal(1, category.DisplayOrder);
        Assert.True(category.IsActive);
    }

    [Fact]
    public void Category_ShouldHaveDefaultValues()
    {
        // Act
        var category = new Vigie24.Domain.Models.Category
        {
            Id = Guid.NewGuid(),
            Slug = "test",
            
        };

        // Assert
        Assert.Null(category.ParentCategoryId);
        Assert.Equal(0, category.DisplayOrder);
        Assert.True(category.IsActive);
        Assert.Empty(category.SubCategories);
    }
}

/// <summary>
/// Tests for the Tag model
/// </summary>
public class TagTests
{
    [Fact]
    public void CreateTag_WithValidData_ShouldSucceed()
    {
        // Act
        var tag = new Vigie24.Domain.Models.Tag
        {
            Id = Guid.NewGuid(),
            Slug = "ia",
            
        };

        // Assert
        Assert.Equal("ia", tag.Slug);
        Assert.Empty(tag.Articles);
    }

    [Fact]
    public void Tag_ShouldHaveDefaultTimestamps()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var tag = new Vigie24.Domain.Models.Tag
        {
            Id = Guid.NewGuid(),
            Slug = "test",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        Assert.Equal(now, tag.CreatedAt);
        Assert.Equal(now, tag.UpdatedAt);
    }
}
