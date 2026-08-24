using Microsoft.EntityFrameworkCore;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;

namespace Vigie24.Infrastructure.Queries;

/// <summary>Granularity of the points in an AI cost report.</summary>
public enum CostGrouping
{
    /// <summary>One point per day.</summary>
    Daily,

    /// <summary>One point per week (grouped starting from Monday).</summary>
    Weekly
}

/// <summary>Cost point: consumption and cost of a period (day or week).</summary>
public sealed record CostPoint(DateOnly Date, long PromptTokens, long CompletionTokens, decimal CostChf, int ArticleCount)
{
    public long TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Aggregates AI consumption (tokens + CHF) per period, for the /couts page.
/// Tokens are captured per analysis (usage field of OpenAI-compatible APIs) on
/// <see cref="Article.UpdatedAt"/> (time of analysis). Articles without tokens (NULL,
/// prior to the tracking migration) are ignored — documented on the page.
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

    /// <summary>Monday of the ISO week containing the date (the "week" bars are dated on Monday).</summary>
    private static DateOnly MondayOf(DateOnly date)
        => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static decimal CostChf(long prompt, long completion, decimal inPerM, decimal outPerM)
        => prompt * inPerM / 1_000_000m + completion * outPerM / 1_000_000m;
}
