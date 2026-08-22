using Vigie24.Domain.Models;

namespace Vigie24.Domain;

/// <summary>
/// Calcul des scores Vigie24 — partagé entre le Worker (calcul) et le Web
/// (hint « calcul précis » affiché sur chaque carte article).
/// Les formules ci-dessous sont la référence unique.
/// Score de classement = Éditorial × 0,55 + Algorithmique × 0,35 + Engagement × 0,10
/// (l'engagement — votes 0-5 et clics — entre ainsi dans le classement).
/// </summary>
public static class ArticleScoring
{
    public const decimal EditorialWeight = 0.55m;
    public const decimal AlgorithmicWeight = 0.35m;
    public const decimal EngagementWeight = 0.10m;

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

    /// <summary>
    /// Score d'engagement 0-100 à partir des votes (échelle 0-5) et des clics.
    /// Sans vote ni clic → 0 (le classement reste inchangé).
    /// Composantes (0-100) : votes = moyenne / 5 × 100 ; clics = min(100, clics × 10).
    /// Mélange pondéré : engagement = 0,6 × votes + 0,4 × clics (les votes pèsent plus).
    /// </summary>
    public static decimal CalculateEngagementScore(int voteCount, decimal voteAverage, int clickCount)
    {
        if (voteCount <= 0 && clickCount <= 0) return 0;
        var voteScore = voteCount > 0 ? Math.Clamp(voteAverage / 5m * 100m, 0m, 100m) : 0m;
        var clickScore = Math.Min(100m, clickCount * 10m);
        return voteScore * 0.6m + clickScore * 0.4m;
    }

    /// <summary>Score de classement final (engagement optionnel, 0 par défaut)</summary>
    public static decimal CalculateRanking(decimal editorialScore, decimal algorithmicScore, decimal engagementScore = 0)
        => editorialScore * EditorialWeight + algorithmicScore * AlgorithmicWeight + engagementScore * EngagementWeight;
}
