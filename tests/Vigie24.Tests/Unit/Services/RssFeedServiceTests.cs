namespace Vigie24.Tests.Unit.Services;

using Microsoft.EntityFrameworkCore;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;
using Vigie24.Infrastructure.Queries;

/// <summary>
/// Tests for RSS feed construction (RssFeedService): filters, sorting, category scope,
/// limite de 50 articles. Le contexte est construit directement en InMemory (pas de DI),
/// same pattern as the existing InMemory integration tests.
/// </summary>
public class RssFeedServiceTests
{
    private static async Task<Vigie24DbContext> SeedAsync(params Article[] articles)
    {
        var options = new DbContextOptionsBuilder<Vigie24DbContext>()
            .UseInMemoryDatabase($"RssFeed_{Guid.NewGuid()}")
            .Options;
        var db = new Vigie24DbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Articles.AddRange(articles);
        await db.SaveChangesAsync();
        return db;
    }

    private static Article[] BuildArticles()
    {
        var leMonde = new Source { Name = "Le Monde", CountryCode = "FR", SourceKind = "press", IngestionMethod = "rss" };
        var rts = new Source { Name = "RTS", CountryCode = "CH", SourceKind = "press", IngestionMethod = "rss" };
        var tech = new Category { Slug = "tech", DisplayOrder = 1, IsActive = true };
        var ia = new Category { Slug = "ia", DisplayOrder = 1, IsActive = true, ParentCategory = tech };

        Article New(string title, decimal score, DateTime publishedAt, Source primary, Category? category = null, bool isPublished = true) =>
            new()
            {
                Title = title,
                Summary = $"Résumé de {title}",
                CanonicalUrl = $"https://x.test/{Guid.NewGuid():N}",
                RankingScore = score,
                PublishedAt = publishedAt,
                IsPublished = isPublished,
                Category = category,
                CategoryId = category?.Id ?? Guid.Empty,
                Sources = { new ArticleSource { Source = primary, IsPrimary = true, SourceArticleUrl = "https://x.test/a" } }
            };

        return new[]
        {
            New("L'IA expliquée", 80, new DateTime(2026, 8, 20), leMonde, ia),
            New("Un nouveau GPU", 70, new DateTime(2026, 8, 15), rts, ia),
            New("Débat : les LLM", 55, new DateTime(2026, 8, 10), leMonde),
            New("Vlog hebdo", 90, new DateTime(2026, 8, 1), leMonde),
            New("Brouillon non publié", 99, new DateTime(2026, 8, 25), leMonde, isPublished: false)
        };
    }

    [Fact]
    public async Task Feed_ReturnsPublishedArticlesOrderedByDateDesc()
    {
        await using var db = await SeedAsync(BuildArticles());

        var items = await RssFeedService.GetItemsAsync(db, new FilterCriteria());

        // 5 seeded articles, 1 unpublished → 4, sorted from newest to oldest
        Assert.Equal(4, items.Count);
        Assert.Equal(new[]
        {
            "L'IA expliquée", "Un nouveau GPU", "Débat : les LLM", "Vlog hebdo"
        }, items.Select(i => i.Title));
    }

    [Fact]
    public async Task Feed_WithCriteria_FiltersItems()
    {
        await using var db = await SeedAsync(BuildArticles());

        var items = await RssFeedService.GetItemsAsync(db, new FilterCriteria { MinScore = 80 });

        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.Title == "L'IA expliquée");
        Assert.Contains(items, i => i.Title == "Vlog hebdo");
    }

    [Fact]
    public async Task CategoryFeed_ScopesToCategoryIncludingSubcategories()
    {
        await using var db = await SeedAsync(BuildArticles());

        var parentItems = await RssFeedService.GetItemsAsync(db, new FilterCriteria(), "tech");
        var leafItems = await RssFeedService.GetItemsAsync(db, new FilterCriteria(), "ia");

        // "ia" is a subcategory of "tech": both "ia" articles surface in "tech"
        Assert.Equal(2, parentItems.Count);
        Assert.Equal(2, leafItems.Count);
        Assert.Equal(parentItems.Select(i => i.Title).OrderBy(t => t), leafItems.Select(i => i.Title).OrderBy(t => t));
    }

    [Fact]
    public async Task CategoryFeed_WithCriteria_CombinesScopeAndFilters()
    {
        await using var db = await SeedAsync(BuildArticles());

        var items = await RssFeedService.GetItemsAsync(db, new FilterCriteria { MinScore = 80 }, "ia");

        Assert.Single(items);
        Assert.Equal("L'IA expliquée", items[0].Title);
    }

    [Fact]
    public async Task CategoryFeed_UnknownCategory_ReturnsEmpty()
    {
        await using var db = await SeedAsync(BuildArticles());

        var items = await RssFeedService.GetItemsAsync(db, new FilterCriteria(), "inconnue");

        Assert.Empty(items);
    }

    [Fact]
    public async Task Feed_IsCappedAtFiftyItems()
    {
        var leMonde = new Source { Name = "Le Monde", CountryCode = "FR", SourceKind = "press", IngestionMethod = "rss" };
        var articles = Enumerable.Range(1, 55)
            .Select(i => new Article
            {
                Title = $"Article {i}",
                CanonicalUrl = $"https://x.test/{i}",
                RankingScore = 50,
                PublishedAt = new DateTime(2026, 1, 1).AddDays(i),
                IsPublished = true,
                Sources = { new ArticleSource { Source = leMonde, IsPrimary = true, SourceArticleUrl = "https://x.test/a" } }
            })
            .ToArray();
        await using var db = await SeedAsync(articles);

        var items = await RssFeedService.GetItemsAsync(db, new FilterCriteria());

        Assert.Equal(RssFeedService.RssFeedSize, items.Count);
        Assert.Equal("Article 55", items[0].Title);
    }
}
