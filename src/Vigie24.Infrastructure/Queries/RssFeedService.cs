using Microsoft.EntityFrameworkCore;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;

namespace Vigie24.Infrastructure.Queries;

/// <summary>
/// Builds RSS 2.0 feeds: query shared between /feed and /categories/{slug}/feed.
/// Applies the same filter criteria as the ArticleBrowser UI, sorts by date
/// descending, and caps the result at <see cref="RssFeedSize"/> articles.
/// </summary>
public static class RssFeedService
{
    public const int RssFeedSize = 50;

    public static async Task<List<RssItem>> GetItemsAsync(
        Vigie24DbContext db,
        FilterCriteria criteria,
        string? categorySlug = null,
        CancellationToken ct = default)
    {
        var q = db.Articles.AsNoTracking().Where(a => a.IsPublished);

        if (!string.IsNullOrEmpty(categorySlug))
        {
            var slug = categorySlug;
            q = q.Where(a => a.Category != null
                && (a.Category.Slug == slug
                    || (a.Category.ParentCategory != null && a.Category.ParentCategory.Slug == slug)));
        }

        return await q.ApplyFilterCriteria(criteria)
            .OrderByDescending(a => a.PublishedAt)
            .Take(RssFeedSize)
            .Select(a => new RssItem(a.Title, a.CanonicalUrl, a.Summary, a.PublishedAt, a.CanonicalUrl))
            .ToListAsync(ct);
    }
}
