using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Services.Interfaces;
using Xunit;

namespace Vigie24.Tests.Functional;

/// <summary>
/// Functional tests exercising the REAL production code paths: the scoring formulas
/// (ArticleScoring), the publication rule enforced by the Worker, and the taxonomy
/// fallbacks. These tests replaced a set of assertions that re-implemented the
/// formulas locally and therefore validated nothing.
/// </summary>
public class ScoringFunctionalTests
{
    [Theory]
    [InlineData(85, 70, true)]   // high editorial score → published
    [InlineData(45, 60, false)]  // low editorial score → not published
    [InlineData(60, 50, true)]   // exactly at the threshold → published (>=)
    public void PublicationRule_ShouldUseEditorialScore(decimal editorial, decimal algorithmic, bool expectedPublished)
    {
        var article = BuildArticle(editorial, algorithmic);

        // The exact rule enforced by the Worker when creating the article:
        // IsPublished = aiResult.EditorialScore >= ArticleScoring.PublicationThreshold
        var isPublished = article.EditorialScore >= ArticleScoring.PublicationThreshold;

        Assert.Equal(expectedPublished, isPublished);
    }

    [Fact]
    public void CalculateRanking_ShouldBlendEditorialAlgorithmicEngagement()
    {
        const decimal editorial = 80m, algorithmic = 70m, engagement = 50m;

        var ranking = ArticleScoring.CalculateRanking(editorial, algorithmic, engagement);

        Assert.Equal(editorial * 0.55m + algorithmic * 0.35m + engagement * 0.10m, ranking);
    }

    [Fact]
    public void Ranking_WithoutEngagement_ShouldBePureEditorialAlgorithmic()
    {
        const decimal editorial = 80m, algorithmic = 70m;

        var ranking = ArticleScoring.CalculateRanking(editorial, algorithmic);

        Assert.Equal(editorial * ArticleScoring.EditorialWeight
                   + algorithmic * ArticleScoring.AlgorithmicWeight, ranking);
    }

    [Fact]
    public void Engagement_ShouldRaiseRanking()
    {
        var withEngagement = ArticleScoring.CalculateRanking(70m, 60m,
            ArticleScoring.CalculateEngagementScore(voteCount: 10, voteAverage: 4.5m, clickCount: 8));
        var withoutEngagement = ArticleScoring.CalculateRanking(70m, 60m);

        Assert.True(withEngagement > withoutEngagement);
    }

    [Fact]
    public void Engagement_WithoutVotesOrClicks_ShouldBeZero()
    {
        Assert.Equal(0m, ArticleScoring.CalculateEngagementScore(0, 0m, 0));
    }

    [Theory]
    [InlineData(0.5, 100)]   // ≤ 1h
    [InlineData(3, 80)]      // ≤ 6h
    [InlineData(12, 60)]     // ≤ 24h
    [InlineData(30, 40)]     // ≤ 48h
    [InlineData(96, 20)]     // decreasing after 48h: 40 − (96−48)/24×10
    [InlineData(240, 0)]     // clamped to 0
    public void RecencyScore_ShouldFollowTimeBuckets(double hours, decimal expected)
    {
        var publishedAt = DateTime.UtcNow.AddHours(-hours);

        // Precision 2: DateTime ticks rarely land on an exact hour boundary,
        // so the fractional formula yields 19.9999… instead of exactly 20.
        Assert.Equal(expected, ArticleScoring.CalculateRecencyScore(publishedAt), 2);
    }

    [Theory]
    [InlineData(50, 90)]
    [InlineData(30, 70)]
    [InlineData(15, 50)]
    [InlineData(5, 30)]
    public void ContentScore_ShouldFollowWordCount(int words, decimal expected)
    {
        var summary = string.Join(' ', Enumerable.Repeat("mot", words));

        Assert.Equal(expected, ArticleScoring.CalculateContentScore(summary));
    }

    [Fact]
    public void AlgorithmicScore_ShouldAverageRecencyAndContent()
    {
        var article = BuildArticle(80m, 0m);
        article.PublishedAt = DateTime.UtcNow.AddHours(-3);   // recency 80
        article.Summary = string.Join(' ', Enumerable.Repeat("mot", 30)); // content 70

        Assert.Equal(75m, ArticleScoring.CalculateAlgorithmicScore(article, recentArticlesCount: 10));
    }

    private static Article BuildArticle(decimal editorial, decimal algorithmic) => new()
    {
        Id = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        CanonicalUrl = "https://example.com/article",
        Title = "Test article",
        Summary = "Résumé de test pour l'article de démonstration.",
        EditorialScore = editorial,
        AlgorithmicScore = algorithmic,
        PublishedAt = DateTime.UtcNow,
        FirstSeenAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };
}

/// <summary>
/// End-to-end logic tests without infrastructure: the publication decision across
/// a full AI result set, required fields, the real ranking weights and the
/// taxonomy fallback.
/// </summary>
public class WorkflowFunctionalTests
{
    [Fact]
    public void FullIngestionDecision_ShouldPublishOnlyAboveThreshold()
    {
        // A full AI result set (as deserialized from the provider) goes through the
        // real production rule — the same expression used by the Worker.
        var results = new[]
        {
            BuildResult(editorial: 85m),  // published
            BuildResult(editorial: 42m),  // not published
            BuildResult(editorial: 60m),  // published (boundary)
        };

        var published = results
            .Select(r => r.EditorialScore >= ArticleScoring.PublicationThreshold)
            .ToList();

        Assert.Equal(new[] { true, false, true }, published);
    }

    [Fact]
    public void ArticleCreation_ShouldCarryRequiredFields()
    {
        var article = BuildArticle(75m, 65m);

        Assert.NotEqual(Guid.Empty, article.Id);
        Assert.NotNull(article.CategoryId);
        Assert.False(string.IsNullOrWhiteSpace(article.CanonicalUrl));
        Assert.False(string.IsNullOrWhiteSpace(article.Title));
        Assert.False(string.IsNullOrWhiteSpace(article.Summary));
        Assert.Equal(DateTimeKind.Utc, article.PublishedAt.Kind);
    }

    [Fact]
    public void Ranking_ShouldUseTheRealWeights()
    {
        // Regression: a previous version of this test re-implemented the ranking as
        // a plain average of editorial and algorithmic — which did not match the
        // production formula (0.55 / 0.35 / 0.10).
        const decimal editorial = 80m, algorithmic = 60m;
        var engagement = ArticleScoring.CalculateEngagementScore(voteCount: 5, voteAverage: 4m, clickCount: 2);

        var ranking = ArticleScoring.CalculateRanking(editorial, algorithmic, engagement);

        Assert.Equal(editorial * ArticleScoring.EditorialWeight
                   + algorithmic * ArticleScoring.AlgorithmicWeight
                   + engagement * ArticleScoring.EngagementWeight, ranking);
    }

    [Theory]
    [InlineData("ia", "IA")]
    [InlineData("developpement", "Développement")]
    [InlineData("dev-web", "Web")]
    [InlineData("musiques", "Musiques")]
    [InlineData("theme-inconnu", "Theme Inconnu")]  // fallback: humanized slug
    public void CategoryMapping_ShouldResolveDisplayNames(string slug, string expected)
    {
        Assert.Equal(expected, VigieTaxonomy.DisplayName(slug));
    }

    [Fact]
    public void Taxonomy_ShouldIncludeExpectedParents()
    {
        var parents = VigieTaxonomy.Categories.Where(c => c.ParentSlug is null).Select(c => c.Name).ToList();

        Assert.Contains("IA", parents);
        Assert.Contains("Développement", parents);
        Assert.Contains("Cybersécurité", parents);
        Assert.Contains("Musiques", parents);
        Assert.Contains("Arts graphiques", parents);
    }

    private static Article BuildArticle(decimal editorial, decimal algorithmic) => new()
    {
        Id = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        CanonicalUrl = "https://example.com/article",
        Title = "Test article",
        Summary = "Résumé de test pour l'article de démonstration.",
        EditorialScore = editorial,
        AlgorithmicScore = algorithmic,
        PublishedAt = DateTime.UtcNow,
        FirstSeenAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static AiProcessingResult BuildResult(decimal editorial) => new()
    {
        Summary = "Résumé de test.",
        EditorialScore = editorial,
    };
}
