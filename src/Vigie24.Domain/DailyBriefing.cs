namespace Vigie24.Domain;

/// <summary>One "essential" article of the daily briefing.</summary>
public record BriefingArticle(
    Guid Id,
    string Title,
    decimal EditorialScore,
    int ClickCount,
    int VoteCount,
    decimal VoteAverage,
    DateTime PublishedAt);

/// <summary>Dominant theme of the week (tag slug + article count).</summary>
public record BriefingTheme(string TagSlug, int ArticleCount);

/// <summary>Computed daily briefing (pure aggregation, no DB dependency).</summary>
public record DailyBriefing(
    IReadOnlyList<BriefingArticle> TopArticles,
    BriefingTheme? TopTheme,
    int ArticlesLast24h,
    int DebateCount);

/// <summary>
/// Builds the daily briefing (« Le briefing du matin ») from raw rows:
/// the top 5 articles of the last 24 h (editorial score, then engagement),
/// the dominant theme of the last 7 days, and headline counts.
/// </summary>
public static class DailyBriefingBuilder
{
    public const int TopCount = 5;

    public static DailyBriefing Build(
        IEnumerable<BriefingArticle> last24hArticles,
        IEnumerable<(string TagSlug, int Count)> themeCounts7d,
        int debateCount)
    {
        var articles = last24hArticles
            .OrderByDescending(a => a.EditorialScore)
            .ThenByDescending(a => a.VoteAverage)
            .ThenByDescending(a => a.VoteCount)
            .ThenByDescending(a => a.ClickCount)
            .Take(TopCount)
            .ToList();

        var themes = themeCounts7d
            .GroupBy(t => t.TagSlug)
            .Select(g => new BriefingTheme(g.Key, g.Sum(t => t.Count)))
            .OrderByDescending(t => t.ArticleCount)
            .ThenBy(t => t.TagSlug) // deterministe en cas d'égalité
            .ToList();

        BriefingTheme? topTheme = themes.FirstOrDefault();

        return new DailyBriefing(articles, topTheme, last24hArticles.Count(), debateCount);
    }
}
