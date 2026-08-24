namespace Vigie24.Tests.Unit.Services;

using Microsoft.EntityFrameworkCore;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;
using Vigie24.Infrastructure.Queries;

/// <summary>
/// Tests for the ApplyFilterCriteria extension (shared UI + RSS filtering).
/// Uses the real in-memory DbContext (same model as the InMemory integration tests).
/// </summary>
public class ArticleFilteringTests
{
    private static async Task<Vigie24DbContext> SeedAsync(params Article[] articles)
    {
        var options = new DbContextOptionsBuilder<Vigie24DbContext>()
            .UseInMemoryDatabase($"ArticleFiltering_{Guid.NewGuid()}")
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
        var numerama = new Source { Name = "Numerama", CountryCode = "FR", SourceKind = "press", IngestionMethod = "rss" };
        var micode = new Source { Name = "Micode", CountryCode = "FR", SourceKind = "youtube", IngestionMethod = "youtube" };
        var tagIa = new Tag { Slug = "ia" };
        var tagDev = new Tag { Slug = "dev" };

        Article New(string title, decimal score, decimal rating, DateTime published, Source primary, Tag tag) =>
            new()
            {
                Title = title,
                Summary = $"Résumé de {title}",
                CanonicalUrl = $"https://x.test/{Guid.NewGuid():N}",
                RankingScore = score,
                VoteAverage = rating,
                PublishedAt = published,
                IsPublished = true,
                Sources = { new ArticleSource { Source = primary, IsPrimary = true, SourceArticleUrl = "https://x.test/a" } },
                Tags = { new ArticleTag { Tag = tag } }
            };

        return new[]
        {
            New("L'IA expliquée", 80, 4.2m, new DateTime(2026, 8, 20), leMonde, tagIa),
            New("Un nouveau GPU", 70, 3.5m, new DateTime(2026, 8, 15), rts, tagDev),
            New("Débat : les LLM", 55, 4.8m, new DateTime(2026, 8, 10), numerama, tagIa),
            New("Vlog hebdo", 90, 4.0m, new DateTime(2026, 8, 1), micode, tagDev)
        };
    }

    [Fact]
    public async Task EmptyCriteria_ReturnsAllArticles()
    {
        await using var db = await SeedAsync(BuildArticles());

        var result = await db.Articles.ApplyFilterCriteria(new FilterCriteria()).ToListAsync();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task MinScore_FiltersByRankingScore()
    {
        await using var db = await SeedAsync(BuildArticles());

        var result = await db.Articles.ApplyFilterCriteria(new FilterCriteria { MinScore = 60 }).ToListAsync();

        Assert.Equal(new[] { "L'IA expliquée", "Un nouveau GPU", "Vlog hebdo" }, result.Select(a => a.Title).OrderBy(t => t));
    }

    [Fact]
    public async Task CountryCode_UsesPrimarySource()
    {
        await using var db = await SeedAsync(BuildArticles());

        var result = await db.Articles.ApplyFilterCriteria(new FilterCriteria { CountryCode = "CH" }).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Un nouveau GPU", result[0].Title);
    }

    [Fact]
    public async Task SourceKind_FiltersBySourceKind()
    {
        await using var db = await SeedAsync(BuildArticles());

        var result = await db.Articles.ApplyFilterCriteria(new FilterCriteria { SourceKind = "youtube" }).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Vlog hebdo", result[0].Title);
    }

    [Fact]
    public async Task SelectedSources_FiltersBySourceNames()
    {
        await using var db = await SeedAsync(BuildArticles());

        var result = await db.Articles
            .ApplyFilterCriteria(new FilterCriteria { SelectedSources = new[] { "Numerama" } })
            .ToListAsync();

        Assert.Single(result);
        Assert.Equal("Débat : les LLM", result[0].Title);
    }

    [Fact]
    public async Task MinRating_FiltersByVoteAverage()
    {
        await using var db = await SeedAsync(BuildArticles());

        var result = await db.Articles.ApplyFilterCriteria(new FilterCriteria { MinRating = 4 }).ToListAsync();

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, a => a.Title == "Un nouveau GPU");
    }

    [Fact]
    public async Task Range_FiltersByPublishedAt()
    {
        await using var db = await SeedAsync(BuildArticles());

        var result = await db.Articles
            .ApplyFilterCriteria(new FilterCriteria
            {
                RangeFrom = new DateTime(2026, 8, 11),
                RangeTo = new DateTime(2026, 8, 21)
            })
            .ToListAsync();

        Assert.Equal(new[] { "L'IA expliquée", "Un nouveau GPU" }, result.Select(a => a.Title).OrderBy(t => t));
    }

    [Fact]
    public async Task CombinedCriteria_AreAnded()
    {
        await using var db = await SeedAsync(BuildArticles());

        var result = await db.Articles
            .ApplyFilterCriteria(new FilterCriteria { MinScore = 60, CountryCode = "FR" })
            .ToListAsync();

        Assert.Equal(new[] { "L'IA expliquée", "Vlog hebdo" }, result.Select(a => a.Title).OrderBy(t => t));
    }

    [Fact]
    public void SearchCriterion_BuildsIlIkeExpression()
    {
        // Full-text search relies on ILike (case-insensitive): we verify the
        // shape of the expression tree without enumerating it (only Npgsql translates ILike).
        var q = Array.Empty<Article>().AsQueryable()
            .ApplyFilterCriteria(new FilterCriteria { SearchText = "IA" });

        Assert.Contains("ILike", q.Expression.ToString());
    }
}
