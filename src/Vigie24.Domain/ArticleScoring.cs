using Vigie24.Domain.Models;

namespace Vigie24.Domain;

/// <summary>
/// Calcul des scores Vigie24 — partagé entre le Worker (calcul) et le Web
/// (hint « calcul précis » affiché sur chaque carte article).
/// Les formules ci-dessous sont la référence unique.
/// </summary>
public static class ArticleScoring
{
    public const decimal EditorialWeight = 0.6m;
    public const decimal AlgorithmicWeight = 0.4m;

    /// <summary>Récence : 100 (≤1h), 80 (≤6h), 60 (≤24h), 40 (≤48h), puis décroissance</summary>
    public static decimal CalculateRecencyScore(DateTime publishedAt)
    {
        var hoursOld = (decimal)(DateTime.UtcNow - publishedAt).TotalHours;
        if (hoursOld <= 1) return 100;
        if (hoursOld <= 6) return 80;
        if (hoursOld <= 24) return 60;
        if (hoursOld <= 48) return 40;
        return Math.Max(0, 40 - (hoursOld - 48) / 24 * 10);
    }

    /// <summary>Densité d'information du résumé : 90 (≥50 mots), 70 (≥30), 50 (≥15), 30</summary>
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

    public static decimal CalculateRanking(decimal editorialScore, decimal algorithmicScore)
        => editorialScore * EditorialWeight + algorithmicScore * AlgorithmicWeight;
}
