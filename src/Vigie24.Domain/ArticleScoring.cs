using Vigie24.Domain.Models;

namespace Vigie24.Domain;

/// <summary>
/// Vigie24 score computation — shared between the Worker (computation) and the Web
/// ("precise score" hint displayed on each article card).
/// The formulas below are the single source of truth.
/// Ranking score = Editorial × 0.55 + Algorithmic × 0.35 + Engagement × 0.10
/// (engagement — 0-5 votes and clicks — thus contributes to the ranking).
/// </summary>
public static class ArticleScoring
{
    public const decimal EditorialWeight = 0.55m;
    public const decimal AlgorithmicWeight = 0.35m;
    public const decimal EngagementWeight = 0.10m;

    /// <summary>Recency: 100 (≤1h), 80 (≤6h), 60 (≤24h), 40 (≤48h), then decreasing</summary>
    public static decimal CalculateRecencyScore(DateTime publishedAt)
    {
        var hoursOld = (decimal)(DateTime.UtcNow - publishedAt).TotalHours;
        if (hoursOld <= 1) return 100;
        if (hoursOld <= 6) return 80;
        if (hoursOld <= 24) return 60;
        if (hoursOld <= 48) return 40;
        return Math.Max(0, 40 - (hoursOld - 48) / 24 * 10);
    }

    /// <summary>Summary information density: 90 (≥50 words), 70 (≥30), 50 (≥15), 30</summary>
    public static decimal CalculateContentScore(string summary)
    {
        var wordCount = summary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount >= 50) return 90;
        if (wordCount >= 30) return 70;
        if (wordCount >= 15) return 50;
        return 30;
    }

    public static decimal CalculateAlgorithmicScore(Article article, int recentArticlesCount)
        => (CalculateRecencyScore(article.PublishedAt) + CalculateContentScore(article.Summary)) / 2;

    /// <summary>
    /// Engagement score 0-100 from votes (0-5 scale) and clicks.
    /// No votes and no clicks → 0 (ranking unchanged).
    /// Components (0-100): votes = average / 5 × 100; clicks = min(100, clicks × 10).
    /// Weighted blend: engagement = 0.6 × votes + 0.4 × clicks (votes weigh more).
    /// </summary>
    public static decimal CalculateEngagementScore(int voteCount, decimal voteAverage, int clickCount)
    {
        if (voteCount <= 0 && clickCount <= 0) return 0;
        var voteScore = voteCount > 0 ? Math.Clamp(voteAverage / 5m * 100m, 0m, 100m) : 0m;
        var clickScore = Math.Min(100m, clickCount * 10m);
        return voteScore * 0.6m + clickScore * 0.4m;
    }

    /// <summary>Final ranking score (engagement optional, 0 by default)</summary>
    public static decimal CalculateRanking(decimal editorialScore, decimal algorithmicScore, decimal engagementScore = 0)
        => editorialScore * EditorialWeight + algorithmicScore * AlgorithmicWeight + engagementScore * EngagementWeight;
}
