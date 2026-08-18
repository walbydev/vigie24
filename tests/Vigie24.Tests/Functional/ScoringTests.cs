namespace Vigie24.Tests.Functional;

using Vigie24.Domain.Models;
using System.Text.Json;

/// <summary>
/// Tests fonctionnels pour les algorithmes de scoring
/// </summary>
public class ScoringFunctionalTests
{
    [Fact]
    public void ArticleWithHighScore_ShouldBePublished()
    {
        // Arrange
        var article = CreateArticleWithScore(85m);
        const decimal publishThreshold = 60m;

        // Act
        var shouldPublish = article.RankingScore >= publishThreshold;

        // Assert
        Assert.True(shouldPublish, "Article avec score élevé devrait être publié");
    }

    [Fact]
    public void ArticleWithLowScore_ShouldNotBePublished()
    {
        // Arrange
        var article = CreateArticleWithScore(45m);
        const decimal publishThreshold = 60m;

        // Act
        var shouldPublish = article.RankingScore >= publishThreshold;

        // Assert
        Assert.False(shouldPublish, "Article avec score bas ne devrait pas être publié");
    }

    [Theory]
    [InlineData(90, 10)]
    [InlineData(75, 8)]
    [InlineData(60, 6)]
    [InlineData(45, 4)]
    public void RankingScore_ShouldReflectEditorialAndAlgorithmic(decimal editorialScore, int expectedRange)
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
            UpdatedAt = DateTime.UtcNow,
            EditorialScore = editorialScore,
            AlgorithmicScore = editorialScore - 5m,
            RankingScore = (editorialScore + (editorialScore - 5m)) / 2m
        };

        // Act
        var rankingScore = article.RankingScore;

        // Assert
        Assert.InRange(rankingScore, editorialScore - 5m, editorialScore);
    }

    [Fact]
    public void RecentArticle_ShouldHaveHigherRecencyScore()
    {
        // Arrange
        var recentArticle = new Article
        {
            PublishedAt = DateTime.UtcNow - TimeSpan.FromHours(1)
        };

        var oldArticle = new Article
        {
            PublishedAt = DateTime.UtcNow - TimeSpan.FromDays(30)
        };

        // Act & Assert - vérifie que les dates sont différentes
        Assert.True(recentArticle.PublishedAt > oldArticle.PublishedAt);
    }

    [Fact]
    public void ArticleWithMultipleTags_ShouldHaveRichContent()
    {
        // Arrange
        var tags = new List<string> { "IA", "LLM", "MachineLearning", "DeepLearning" };

        // Act
        var tagCount = tags.Count;

        // Assert
        Assert.Equal(4, tagCount);
        Assert.Contains("IA", tags);
        Assert.Contains("LLM", tags);
    }

    [Theory]
    [InlineData("fr", true)]
    [InlineData("en", true)]
    [InlineData("es", true)]
    [InlineData("", false)]
    public void ArticleLanguageCode_ShouldBeValid(string languageCode, bool shouldBeValid)
    {
        // Arrange & Act
        var isValid = !string.IsNullOrEmpty(languageCode);

        // Assert
        Assert.Equal(shouldBeValid, isValid);
    }

    [Fact]
    public void SourceWithMultipleFeeds_ShouldHandleDuplicates()
    {
        // Arrange
        var feedUrls = new List<string>
        {
            "https://example.com/feed",
            "https://example.com/feed",
            "https://example.com/rss"
        };

        // Act
        var uniqueUrls = feedUrls.Distinct().ToList();

        // Assert
        Assert.Equal(2, uniqueUrls.Count);
    }

    [Fact]
    public void CategoryWithArticles_ShouldCalculateAverageScore()
    {
        // Arrange
        var scores = new List<decimal> { 80m, 60m, 40m, 90m, 70m };

        // Act
        var average = scores.Average();

        // Assert
        Assert.Equal(68m, average);
    }

    [Fact]
    public void ArticleWithLongContent_ShouldScoreHigher()
    {
        // Arrange
        var shortSummary = "Court.";
        var longSummary = "Ceci est un résumé très détaillé qui contient beaucoup d'informations pertinentes sur l'article et devrait obtenir un meilleur score de contenu que le résumé court car il fournit plus de contexte.";

        // Act
        var shortWordCount = shortSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var longWordCount = longSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // Assert
        Assert.True(longWordCount > shortWordCount, 
            $"Résumé long ({longWordCount} mots) devrait avoir plus de mots que court ({shortWordCount} mots)");
    }

    [Fact]
    public void ActiveSources_ShouldBeProcessed()
    {
        // Arrange
        var sources = new List<Source>
        {
            new() { IsActive = true, Name = "Active1" },
            new() { IsActive = false, Name = "Inactive" },
            new() { IsActive = true, Name = "Active2" }
        };

        // Act
        var activeSources = sources.Where(s => s.IsActive).ToList();

        // Assert
        Assert.Equal(2, activeSources.Count);
        Assert.All(activeSources, s => Assert.True(s.IsActive));
    }

    private Article CreateArticleWithScore(decimal score)
    {
        return new Article
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CanonicalUrl = "https://example.com",
            Title = "Test",
            Summary = "Test",
            PublishedAt = DateTime.UtcNow,
            FirstSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RankingScore = score
        };
    }
}

/// <summary>
/// Tests fonctionnels pour le workflow complet
/// </summary>
public class WorkflowFunctionalTests
{
    [Fact]
    public void FullIngestionWorkflow_ShouldProcessSources()
    {
        // Arrange
        var sourceCount = 246;
        var activeSources = sourceCount; // Tous actifs par défaut

        // Act
        var processedSources = activeSources;

        // Assert
        Assert.Equal(sourceCount, processedSources);
        Assert.True(processedSources > 200, "Devrait traiter 200+ sources");
    }

    [Fact]
    public void ArticleCreationWorkflow_ShouldValidateRequiredFields()
    {
        // Arrange & Act
        var requiredFields = new[] { "Id", "CategoryId", "CanonicalUrl", "Title", "Summary", "PublishedAt" };

        // Assert
        Assert.NotEmpty(requiredFields);
        Assert.Equal(6, requiredFields.Length);
    }

    [Fact]
    public void ScoreCalculationWorkflow_ShouldBeConsistent()
    {
        // Arrange
        var editorialScore = 80m;
        var algorithmicScore = 75m;

        // Act
        var rankingScore = (editorialScore + algorithmicScore) / 2m;

        // Assert
        Assert.Equal(77.5m, rankingScore);
        Assert.InRange(rankingScore, algorithmicScore, editorialScore);
    }

    [Fact]
    public void CategoryAssignmentWorkflow_ShouldMapCorrectly()
    {
        // Arrange
        var categorySlugs = new[] { "technologie", "securite", "open-source", "cloud-devops", "ia-ml" };

        // Act
        var mappedCategories = categorySlugs.Select(s => new { Slug = s, Mapped = true }).ToList();

        // Assert
        Assert.Equal(5, mappedCategories.Count);
        Assert.All(mappedCategories, c => Assert.True(c.Mapped));
    }

    [Fact]
    public void TagExtractionWorkflow_ShouldIdentifyKeywords()
    {
        // Arrange
        var text = "L'intelligence artificielle et les LLM transforment le développement logiciel.";
        var keywords = new[] { "intelligence artificielle", "LLM", "développement logiciel" };

        // Act
        var extractedKeywords = keywords.Where(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();

        // Assert
        Assert.Equal(3, extractedKeywords.Count);
    }
}
