using Vigie24.Domain;
using Xunit;

namespace Vigie24.Tests.Unit.Domain;

/// <summary>Contract tests for the daily briefing builder.</summary>
public class DailyBriefingTests
{
    private static BriefingArticle Article(Guid id, decimal score, int votes = 0, decimal avg = 0, int clicks = 0)
        => new(id, $"Article {id}", score, clicks, votes, avg, new DateTime(2026, 8, 26, 8, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Build_ShouldPickTop5ByScoreThenEngagement()
    {
        var articles = new[]
        {
            Article(Guid.NewGuid(), 50m, clicks: 9),
            Article(Guid.NewGuid(), 90m),
            Article(Guid.NewGuid(), 80m, votes: 4, avg: 4.5m),
            Article(Guid.NewGuid(), 80m, votes: 2, avg: 3m),
            Article(Guid.NewGuid(), 70m),
            Article(Guid.NewGuid(), 60m)
        };

        var briefing = DailyBriefingBuilder.Build(articles, Array.Empty<(string, int)>(), 2);

        Assert.Equal(5, briefing.TopArticles.Count);
        Assert.Equal(90m, briefing.TopArticles[0].EditorialScore);
        Assert.Equal(80m, briefing.TopArticles[1].EditorialScore);
        Assert.Equal(4.5m, briefing.TopArticles[1].VoteAverage); // 80/4,5 avant 80/3
        Assert.Equal(60m, briefing.TopArticles[4].EditorialScore);
        Assert.Equal(6, briefing.ArticlesLast24h);
        Assert.Equal(2, briefing.DebateCount);
    }

    [Fact]
    public void Build_ShouldPickMostFrequentTheme()
    {
        var themes = new[] { ("IA", 12), ("Sécurité", 5), ("IA", 3) };

        var briefing = DailyBriefingBuilder.Build(Array.Empty<BriefingArticle>(), themes, 0);

        Assert.NotNull(briefing.TopTheme);
        Assert.Equal("IA", briefing.TopTheme!.TagSlug);
        Assert.Equal(15, briefing.TopTheme.ArticleCount);
    }

    [Fact]
    public void Build_Empty_ShouldReturnEmptyBriefing()
    {
        var briefing = DailyBriefingBuilder.Build(Array.Empty<BriefingArticle>(), Array.Empty<(string, int)>(), 0);

        Assert.Empty(briefing.TopArticles);
        Assert.Null(briefing.TopTheme);
        Assert.Equal(0, briefing.ArticlesLast24h);
    }

    [Fact]
    public void Build_TieTheme_ShouldBeDeterministic()
    {
        var themes = new[] { ("Zeta", 5), ("Alpha", 5) };

        var briefing = DailyBriefingBuilder.Build(Array.Empty<BriefingArticle>(), themes, 0);

        Assert.Equal("Alpha", briefing.TopTheme!.TagSlug);
    }
}
