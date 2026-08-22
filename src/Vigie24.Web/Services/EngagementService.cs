using Microsoft.EntityFrameworkCore;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;

namespace Vigie24.Web.Services;

/// <summary>
/// Engagement des lecteurs (votes 0-5, clics) en accès direct à la base.
/// Exécuté dans le circuit Blazor Server : pas de HttpClient ni d'URL relative,
/// donc pas de requête HTTP serveur→serveur ni d'échec silencieux.
/// </summary>
public interface IEngagementService
{
    Task<EngagementState> GetStateAsync(Guid articleId, CancellationToken ct = default);
    Task<EngagementState> VoteAsync(Guid articleId, int value, CancellationToken ct = default);
    Task TrackClickAsync(Guid articleId, CancellationToken ct = default);
}

public sealed record EngagementState(
    int? MyVote, decimal VoteAverage, int VoteCount, int ClickCount, decimal RankingScore);

public sealed class EngagementService(Vigie24DbContext db, IHttpContextAccessor http) : IEngagementService
{
    private const string VoterCookie = "voter_id";

    private string? GetVoterKey() => http.HttpContext?.Request.Cookies[VoterCookie];

    private string EnsureVoterKey()
    {
        var key = GetVoterKey();
        if (!string.IsNullOrEmpty(key)) return key;

        key = Guid.NewGuid().ToString("N");
        http.HttpContext?.Response.Cookies.Append(VoterCookie, key, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365)
        });
        return key;
    }

    public async Task<EngagementState> GetStateAsync(Guid articleId, CancellationToken ct = default)
    {
        var article = await db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == articleId, ct);
        if (article is null) return new EngagementState(null, 0, 0, 0, 0);

        int? myVote = null;
        var key = GetVoterKey();
        if (!string.IsNullOrEmpty(key))
        {
            myVote = await db.ArticleVotes.AsNoTracking()
                .Where(v => v.ArticleId == articleId && v.VoterKey == key)
                .Select(v => (int?)v.VoteValue)
                .FirstOrDefaultAsync(ct);
        }

        return new EngagementState(myVote, article.VoteAverage, article.VoteCount, article.ClickCount, article.RankingScore);
    }

    public async Task<EngagementState> VoteAsync(Guid articleId, int value, CancellationToken ct = default)
    {
        if (value < 0 || value > 5)
            throw new ArgumentOutOfRangeException(nameof(value), "Le vote doit être entre 0 et 5.");

        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == articleId, ct)
            ?? throw new KeyNotFoundException($"Article {articleId} introuvable.");

        var voterKey = EnsureVoterKey();

        var existing = await db.ArticleVotes
            .FirstOrDefaultAsync(v => v.ArticleId == articleId && v.VoterKey == voterKey, ct);
        if (existing is null)
        {
            db.ArticleVotes.Add(new ArticleVote { ArticleId = articleId, VoterKey = voterKey, VoteValue = (short)value });
        }
        else
        {
            existing.VoteValue = (short)value;
        }
        await db.SaveChangesAsync(ct);

        // Agrégats + score de classement avec engagement
        var votes = await db.ArticleVotes.Where(v => v.ArticleId == articleId).ToListAsync(ct);
        article.VoteCount = votes.Count;
        article.VoteAverage = votes.Count > 0 ? votes.Average(v => (decimal)v.VoteValue) : 0;
        article.RankingScore = ArticleScoring.CalculateRanking(
            article.EditorialScore, article.AlgorithmicScore,
            ArticleScoring.CalculateEngagementScore(article.VoteCount, article.VoteAverage, article.ClickCount));
        article.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new EngagementState(value, article.VoteAverage, article.VoteCount, article.ClickCount, article.RankingScore);
    }

    public async Task TrackClickAsync(Guid articleId, CancellationToken ct = default)
    {
        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == articleId, ct);
        if (article is null) return;

        article.ClickCount++;
        article.RankingScore = ArticleScoring.CalculateRanking(
            article.EditorialScore, article.AlgorithmicScore,
            ArticleScoring.CalculateEngagementScore(article.VoteCount, article.VoteAverage, article.ClickCount));
        article.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
