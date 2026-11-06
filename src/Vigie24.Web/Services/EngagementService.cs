using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;
using Vigie24.Services;

namespace Vigie24.Web.Services;

/// <summary>
/// Reader engagement (votes 0-5, clicks) with direct database access.
/// Runs inside the Blazor Server circuit: no HttpClient and no relative URL,
/// so no server-to-server HTTP request and no silent failures.
/// </summary>
public interface IEngagementService
{
    Task<EngagementState> GetStateAsync(Guid articleId, CancellationToken ct = default);
    Task<Dictionary<Guid, int>> GetMyVotesAsync(IReadOnlyList<Guid> articleIds, CancellationToken ct = default);
    Task<EngagementState> VoteAsync(Guid articleId, int value, CancellationToken ct = default);
    Task TrackClickAsync(Guid articleId, CancellationToken ct = default);
}

public sealed record EngagementState(
    int? MyVote, decimal VoteAverage, int VoteCount, int ClickCount, decimal RankingScore);

public sealed class EngagementService(
    Vigie24DbContext db,
    IHttpContextAccessor http,
    IJSRuntime js,
    EngagementRateLimiter limiter) : IEngagementService
{
    private const string VoterCookie = "voter_id";
    private string? _voterKey;

    private string? CookieVoterKey() => http.HttpContext?.Request.Cookies[VoterCookie];

    private string? GetVoterKey() => _voterKey ??= CookieVoterKey();

    /// <summary>
    /// Clé du visiteur. Lue dans le cookie au prerender (HttpContext disponible) ; sinon
    /// demandée à `vigie24.voterId()` côté navigateur, car dans un circuit Blazor SignalR
    /// il n'y a plus de HttpContext : écrire un cookie depuis le service y lève
    /// « Headers are read-only ».
    /// </summary>
    private async ValueTask<string?> ResolveVoterKeyAsync()
    {
        if (!string.IsNullOrEmpty(_voterKey)) return _voterKey;
        var fromCookie = CookieVoterKey();
        if (!string.IsNullOrEmpty(fromCookie)) return _voterKey = fromCookie;
        try
        {
            var fromJs = await js.InvokeAsync<string?>("vigie24.voterId");
            if (!string.IsNullOrEmpty(fromJs)) return _voterKey = fromJs;
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSDisconnectedException or JSException)
        {
            // prerender statique ou circuit coupé : pas encore d'identité, on réessaiera
        }
        return null;
    }

    /// <summary>Clé d'anti-abus : identité du voteur, à défaut l'adresse IP.</summary>
    private string ClientKey(string? voterKey) =>
        !string.IsNullOrEmpty(voterKey)
            ? $"v:{voterKey}"
            : http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public async Task<EngagementState> GetStateAsync(Guid articleId, CancellationToken ct = default)
    {
        var article = await db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == articleId, ct);
        if (article is null) return new EngagementState(null, 0, 0, 0, 0);

        int? myVote = null;
        // Cookie seulement : on ne crée pas d'identité pour un simple affichage.
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

    /// <summary>
    /// Votes of the current visitor for a list of articles (a single query for a whole page).
    /// </summary>
    public async Task<Dictionary<Guid, int>> GetMyVotesAsync(IReadOnlyList<Guid> articleIds, CancellationToken ct = default)
    {
        var key = GetVoterKey();
        if (string.IsNullOrEmpty(key) || articleIds.Count == 0)
            return new Dictionary<Guid, int>();

        return await db.ArticleVotes.AsNoTracking()
            .Where(v => v.VoterKey == key && articleIds.Contains(v.ArticleId))
            .ToDictionaryAsync(v => v.ArticleId, v => (int)v.VoteValue, ct);
    }

    public async Task<EngagementState> VoteAsync(Guid articleId, int value, CancellationToken ct = default)
    {
        if (value < 0 || value > 5)
            throw new ArgumentOutOfRangeException(nameof(value), "Le vote doit être entre 0 et 5.");

        var voterKey = await ResolveVoterKeyAsync();
        if (string.IsNullOrEmpty(voterKey))
            throw new InvalidOperationException("Identité de vote indisponible dans cette session.");

        // Anti-abus : fenêtre fixe par identité de voteur (30 votes/heure par défaut)
        if (!limiter.TryConsume($"vote:{ClientKey(voterKey)}"))
            throw new RateLimitExceededException("Trop de votes depuis cette adresse. Réessayez plus tard.");

        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == articleId, ct)
            ?? throw new KeyNotFoundException($"Article {articleId} introuvable.");

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

        // Aggregates + ranking score including engagement
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
        // Anti-abus : fenêtre fixe par identité (cohérent avec le endpoint HTTP /track)
        if (!limiter.TryConsume($"click:{ClientKey(GetVoterKey())}"))
            return;

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
