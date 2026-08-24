namespace Vigie24.Tests.Unit.Services;

using Microsoft.EntityFrameworkCore;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;
using Vigie24.Infrastructure.Queries;

/// <summary>
/// Tests for AI cost aggregation (CostReportService): token sums per day/week,
/// CHF conversion (AiCost) and time window. InMemory context built directly.
/// </summary>
public class CostReportServiceTests
{
    private const decimal InPrice = 0.20m;
    private const decimal OutPrice = 0.40m;

    private static async Task<Vigie24DbContext> SeedAsync(params Article[] articles)
    {
        var options = new DbContextOptionsBuilder<Vigie24DbContext>()
            .UseInMemoryDatabase($"CostReport_{Guid.NewGuid()}")
            .Options;
        var db = new Vigie24DbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Articles.AddRange(articles);
        await db.SaveChangesAsync();
        return db;
    }

    private static Article New(string title, int? prompt, int? completion, DateTime updatedAt) => new()
    {
        Title = title,
        CanonicalUrl = $"https://x.test/{Guid.NewGuid():N}",
        PromptTokens = prompt,
        CompletionTokens = completion,
        UpdatedAt = updatedAt,
        CreatedAt = updatedAt
    };

    [Fact]
    public async Task Daily_7Days_SumsTokensPerDay()
    {
        var today = DateTime.UtcNow.Date;
        await using var db = await SeedAsync(
            New("hier 1", 1000, 100, today.AddDays(-1)),
            New("hier 2", 500, 50, today.AddDays(-1).AddHours(5)),
            New("aujourd'hui", 200, 20, today.AddHours(2)),
            New("hors fenêtre", 999, 99, today.AddDays(-8)));

        var points = await CostReportService.GetAsync(db, 7, CostGrouping.Daily, InPrice, OutPrice);

        Assert.Equal(2, points.Count);
        Assert.Equal(1500, points[0].PromptTokens);
        Assert.Equal(150, points[0].CompletionTokens);
        Assert.Equal(1650, points[0].TotalTokens);
        Assert.Equal(200, points[1].PromptTokens);
        Assert.Equal(20, points[1].CompletionTokens);
    }

    [Fact]
    public async Task Daily_ReturnsPointsOrderedByDateAsc()
    {
        var today = DateTime.UtcNow.Date;
        await using var db = await SeedAsync(
            New("aujourd'hui", 200, 20, today.AddHours(2)),
            New("hier", 1000, 100, today.AddDays(-1)));

        var points = await CostReportService.GetAsync(db, 7, CostGrouping.Daily, InPrice, OutPrice);

        Assert.Equal(2, points.Count);
        Assert.True(points[0].Date < points[1].Date);
    }

    [Fact]
    public async Task Weekly_365Days_GroupsByWeekStartingMonday()
    {
        var today = DateTime.UtcNow.Date;
        // last week's Thursday and this week's Monday → 2 distinct groups
        var lastWeekThursday = today.AddDays(-((int)today.DayOfWeek + 6) % 7 - 3);
        var thisWeekMonday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        await using var db = await SeedAsync(
            New("semaine passée", 1000, 100, lastWeekThursday),
            New("cette semaine", 500, 50, thisWeekMonday));

        var points = await CostReportService.GetAsync(db, 365, CostGrouping.Weekly, InPrice, OutPrice);

        Assert.Equal(2, points.Count);
        Assert.Equal(DateOnly.FromDateTime(thisWeekMonday), points[^1].Date);
        Assert.Equal(500, points[^1].PromptTokens);
    }

    [Fact]
    public async Task Cost_IsComputedWithConfiguredPrices()
    {
        var today = DateTime.UtcNow.Date;
        await using var db = await SeedAsync(
            New("un article", 1_000_000, 500_000, today.AddHours(1)));

        var points = await CostReportService.GetAsync(db, 7, CostGrouping.Daily, InPrice, OutPrice);

        // 1M × 0.20/1M + 500k × 0.40/1M = 0.20 + 0.20 = 0.40 CHF
        Assert.Single(points);
        Assert.Equal(0.40m, points[0].CostChf);
    }

    [Fact]
    public async Task Excludes_NullTokensArticles()
    {
        var today = DateTime.UtcNow.Date;
        await using var db = await SeedAsync(
            New("avec tokens", 100, 10, today.AddHours(1)),
            New("sans tokens", null, null, today.AddHours(2)));

        var points = await CostReportService.GetAsync(db, 7, CostGrouping.Daily, InPrice, OutPrice);

        Assert.Single(points);
        Assert.Equal(100, points[0].PromptTokens);
    }
}
