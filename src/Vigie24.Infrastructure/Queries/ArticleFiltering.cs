using Microsoft.EntityFrameworkCore;
using Vigie24.Domain.Models;

namespace Vigie24.Infrastructure.Queries;

/// <summary>
/// Application des critères de filtrage (FilterCriteria) à une requête d'articles.
/// Source unique du filtrage : utilisée par l'UI (ArticleBrowser) et les flux RSS
/// (traduction Npgsql, y compris ILike pour la recherche plein-texte).
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

        // Le pays affiché est celui de la source primaire
        if (!string.IsNullOrEmpty(c.CountryCode))
        {
            q = q.Where(a => a.Sources.Any(s => s.IsPrimary && s.Source.CountryCode == c.CountryCode));
        }

        // Type de source : YouTube, Reddit, Presse, Blogs (insensible à la casse)
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
}
