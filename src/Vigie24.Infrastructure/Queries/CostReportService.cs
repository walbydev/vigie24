using Microsoft.EntityFrameworkCore;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;

namespace Vigie24.Infrastructure.Queries;

/// <summary>Granularité des points d'un rapport de coûts IA.</summary>
public enum CostGrouping
{
    /// <summary>Un point par jour.</summary>
    Daily,

    /// <summary>Un point par semaine (groupée à partir du lundi).</summary>
    Weekly
}

/// <summary>Point de coût : consommation et coût d'une période (jour ou semaine).</summary>
public sealed record CostPoint(DateOnly Date, long PromptTokens, long CompletionTokens, decimal CostChf, int ArticleCount)
{
    public long TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Agrégation de la consommation IA (tokens + CHF) par période, pour la page /couts.
/// Les tokens sont capturés par analyse (champ usage des APIs OpenAI-compatible) sur
/// <see cref="Article.UpdatedAt"/> (moment de l'analyse). Les articles sans tokens (NULL,
/// antérieurs à la migration de suivi) sont ignorés — documenté sur la page.
/// </summary>
public static class CostReportService
{
    public static async Task<List<CostPoint>> GetAsync(
        Vigie24DbContext db,
        int days,
        CostGrouping grouping,
        decimal inputPricePerMillion,
        decimal outputPricePerMillion,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var rows = await db.Articles.AsNoTracking()
            .Where(a => a.PromptTokens != null && a.UpdatedAt >= cutoff)
            .Select(a => new
            {
                Day = a.UpdatedAt.Date,
                Prompt = (long)a.PromptTokens!,
                Completion = (long)a.CompletionTokens!
            })
            .ToListAsync(ct);

        var points = rows
            .GroupBy(r => grouping == CostGrouping.Weekly
                ? MondayOf(DateOnly.FromDateTime(r.Day))
                : DateOnly.FromDateTime(r.Day))
            .Select(g => new CostPoint(
                g.Key,
                g.Sum(r => r.Prompt),
                g.Sum(r => r.Completion),
                CostChf(g.Sum(r => r.Prompt), g.Sum(r => r.Completion), inputPricePerMillion, outputPricePerMillion),
                g.Count()))
            .OrderBy(p => p.Date)
            .ToList();

        return points;
    }

    /// <summary>Lundi de la semaine ISO contenant la date (les barres « semaine » sont datées du lundi).</summary>
    private static DateOnly MondayOf(DateOnly date)
        => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static decimal CostChf(long prompt, long completion, decimal inPerM, decimal outPerM)
        => prompt * inPerM / 1_000_000m + completion * outPerM / 1_000_000m;
}
