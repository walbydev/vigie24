using Vigie24.Domain;
using Xunit;

namespace Vigie24.Tests.Unit.Domain;

/// <summary>
/// Contract tests for the anxiety barometer (signature feature): daily index,
/// 30-day baseline comparison, per-label breakdowns.
/// </summary>
public class AnxietyBarometerTests
{
    private static (DateTime, decimal) Row(int dayOffset, decimal anxiety)
        => (new DateTime(2026, 8, 26, 10, 0, 0, DateTimeKind.Utc).AddDays(-dayOffset), anxiety);

    [Fact]
    public void Compute_MultiDay_ShouldCompareTodayToBaseline()
    {
        var rows = new[]
        {
            Row(2, 2m), Row(2, 2m),   // jour J-2 : moyenne 2
            Row(1, 3m), Row(1, 5m),   // jour J-1 : moyenne 4
            Row(0, 8m), Row(0, 8m)    // jour J   : moyenne 8
        };

        var summary = AnxietyBarometer.Compute(rows);

        Assert.Equal(8.0, summary.TodayAverage);
        Assert.Equal(3.0, summary.BaselineAverage); // (2 + 4) / 2
        Assert.Equal(5.0, summary.Delta);
        Assert.Equal(5.0 / 3.0 * 100.0, summary.DeltaPercent, 6);
        Assert.Equal(3, summary.Days.Count);
        Assert.Equal(2, summary.Days[0].ArticleCount);
    }

    [Fact]
    public void Compute_Empty_ShouldReturnZeros()
    {
        var summary = AnxietyBarometer.Compute(Array.Empty<(DateTime, decimal)>());

        Assert.Equal(0, summary.TodayAverage);
        Assert.Equal(0, summary.DeltaPercent);
        Assert.Empty(summary.Days);
    }

    [Fact]
    public void Compute_SingleDay_ShouldHaveZeroDelta()
    {
        var rows = new[] { Row(0, 6m), Row(0, 4m) };

        var summary = AnxietyBarometer.Compute(rows);

        Assert.Equal(5.0, summary.TodayAverage);
        Assert.Equal(5.0, summary.BaselineAverage); // pas de baseline → delta 0
        Assert.Equal(0, summary.Delta);
        Assert.Equal(0, summary.DeltaPercent);
    }

    [Fact]
    public void Compute_WithBreakdowns_ShouldGroupByLabel()
    {
        var rows = new[] { Row(0, 5m) };
        var byCategory = new[]
        {
            ("IA", 8m), ("IA", 6m),
            ("Science", 2m),
            ("", 9m) // label vide → ignoré
        };
        var byCountry = new[]
        {
            ("France", 7m),
            ("Suisse", 3m), ("Suisse", 5m)
        };

        var summary = AnxietyBarometer.Compute(rows, byCategory, byCountry);

        Assert.Equal(2, summary.ByCategory.Count);
        Assert.Equal("IA", summary.ByCategory[0].Label);
        Assert.Equal(7.0, summary.ByCategory[0].Average);
        Assert.Equal(2, summary.ByCategory[0].ArticleCount);
        Assert.Equal("Science", summary.ByCategory[1].Label);

        Assert.Equal(2, summary.ByCountry.Count);
        Assert.Equal("France", summary.ByCountry[0].Label);
        Assert.Equal(7.0, summary.ByCountry[0].Average);
        Assert.Equal("Suisse", summary.ByCountry[1].Label);
        Assert.Equal(4.0, summary.ByCountry[1].Average);
    }

    [Fact]
    public void GroupByLabel_ShouldOrderByAverageDescAndFilterEmpty()
    {
        var result = AnxietyBarometer.GroupByLabel(new[]
        {
            ("B", 4m),
            ("A", 9m),
            ("", 9m),
            ("B", 6m)
        });

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Label);
        Assert.Equal(9.0, result[0].Average);
        Assert.Equal("B", result[1].Label);
        Assert.Equal(5.0, result[1].Average);
        Assert.Equal(2, result[1].ArticleCount);
    }
}
