using Microsoft.EntityFrameworkCore;
using Vigie24.Domain.Models;

namespace Vigie24.Infrastructure.Queries;

/// <summary>
/// Applies the filtering criteria (FilterCriteria) to an article query.
/// Single source of filtering: used by the UI (ArticleBrowser) and the RSS feeds
/// (Npgsql translation, including ILike for full-text search).
/// </summary>
public static class ArticleFiltering
{
    public static IQueryable<Article> ApplyFilterCriteria(this IQueryable<Article> q, FilterCriteria c)
    {
        var search = c.SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{search}%";
            q = q.Where(a => EF.Functions.ILike(a.Title, pattern)
                || EF.Functions.ILike(a.Summary, pattern)
                || a.Tags.Any(t => EF.Functions.ILike(t.Tag.Slug, pattern))
                || a.Sources.Any(s => EF.Functions.ILike(s.Source.Name, pattern)));
        }

        // Negative filter: any excluded word found in the title or the summary removes
        // the article. Words are matched case-insensitively, LIKE wildcards are escaped.
        foreach (var raw in c.ExcludeWords)
        {
            var word = raw.Trim();
            if (word.Length == 0)
                continue;

            var pattern = $"%{EscapeLike(word)}%";
            q = q.Where(a => !EF.Functions.ILike(a.Title, pattern, "\\")
                && (a.Summary == null || !EF.Functions.ILike(a.Summary, pattern, "\\")));
        }

        // The displayed country is that of the primary source
        if (!string.IsNullOrEmpty(c.CountryCode))
        {
            q = q.Where(a => a.Sources.Any(s => s.IsPrimary && s.Source.CountryCode == c.CountryCode));
        }

        // Source type: YouTube, Reddit, Press, Blogs (case-insensitive)
        if (!string.IsNullOrEmpty(c.SourceKind))
        {
            q = q.Where(a => a.Sources.Any(s => s.Source.SourceKind.ToLower() == c.SourceKind));
        }

        if (c.SelectedSources.Count > 0)
        {
            var selected = c.SelectedSources;
            q = q.Where(a => a.Sources.Any(s => selected.Contains(s.Source.Name)));
        }

        if (c.MinScore > 0)
        {
            q = q.Where(a => a.RankingScore >= c.MinScore);
        }

        if (c.MinRating > 0)
        {
            q = q.Where(a => a.VoteAverage >= c.MinRating);
        }

        // Anxiety cap: 10 = no filter. Articles without a score (null, not yet
        // evaluated) remain visible — only scored articles above the cap are hidden.
        if (c.MaxAnxiety < 10)
        {
            q = q.Where(a => a.AnxietyScore == null || a.AnxietyScore <= c.MaxAnxiety);
        }

        if (c.RangeFrom is { } from)
        {
            q = q.Where(a => a.PublishedAt >= from);
        }

        if (c.RangeTo is { } to)
        {
            q = q.Where(a => a.PublishedAt < to);
        }

        return q;
    }

    /// <summary>Escapes LIKE wildcards so an excluded word is matched literally.</summary>
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("%", "\\%", StringComparison.Ordinal)
             .Replace("_", "\\_", StringComparison.Ordinal);
}
