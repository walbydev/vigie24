using Vigie24.Domain;
using Xunit;

namespace Vigie24.Tests.Unit.Domain;

public class TimelineMathTests
{
    [Theory]
    [InlineData(0.0, 10, 1)]    // top of track → page 1
    [InlineData(0.5, 10, 6)]    // middle → page 6 (Floor(0.5*10)+1)
    [InlineData(0.99, 10, 10)]  // near bottom → page 10
    [InlineData(1.0, 10, 10)]   // clamp to max
    public void PageFromFraction_ShouldClampToValidPage(double fraction, int totalPages, int expected)
        => Assert.Equal(expected, TimelineMath.PageFromFraction(fraction, totalPages));

    [Theory]
    [InlineData(50, 5000, 4.0)]   // 50/5000 → 4 % (min 4)
    [InlineData(50, 200, 25.0)]   // 50/200 → 25 %
    [InlineData(50, 30, 100.0)]   // more articles than a full page → 100 % (max)
    [InlineData(50, 0, 100.0)]    // no articles → 100 %
    public void ThumbHeightPercent_ShouldReflectWindowSize(int pageSize, int totalCount, double expected)
        => Assert.Equal(expected, TimelineMath.ThumbHeightPercent(pageSize, totalCount), 1);

    [Theory]
    [InlineData(1, 10, 0.0)]
    [InlineData(3, 10, 20.0)]    // (3-1)*100/10
    [InlineData(10, 10, 90.0)]
    [InlineData(5, 1, 0.0)]      // single page → 0
    public void ThumbTopPercent_ShouldReflectCurrentPage(int page, int totalPages, double expected)
        => Assert.Equal(expected, TimelineMath.ThumbTopPercent(page, totalPages), 1);

    [Fact]
    public void BuildTicks_ShouldUseDays_ForShortRanges()
    {
        var dates = Enumerable.Range(0, 10)
            .Select(i => new DateTime(2026, 8, 25).AddDays(-i)).ToList();

        var ticks = TimelineMath.BuildTicks(dates);

        Assert.Contains(ticks, t => t.Start.Date == new DateTime(2026, 8, 25).Date);
        Assert.All(ticks, t => Assert.Equal(TimeSpan.FromDays(1), t.Step));
    }

    [Fact]
    public void BuildTicks_ShouldUseWeeks_ForMediumRanges()
    {
        var dates = Enumerable.Range(0, 200)
            .Select(i => new DateTime(2026, 1, 1).AddDays(i)).ToList();

        var ticks = TimelineMath.BuildTicks(dates);

        Assert.NotEmpty(ticks);
        Assert.All(ticks, t => Assert.Equal(TimeSpan.FromDays(7), t.Step));
    }

    [Fact]
    public void BuildTicks_ShouldUseMonths_ForLongRanges()
    {
        var dates = Enumerable.Range(0, 400)
            .Select(i => new DateTime(2025, 1, 1).AddDays(i)).ToList();

        var ticks = TimelineMath.BuildTicks(dates);

        Assert.NotEmpty(ticks);
        Assert.All(ticks, t => Assert.Equal(TimeSpan.FromDays(30), t.Step));
    }

    [Fact]
    public void BuildTicks_EmptyDates_ShouldReturnEmpty()
        => Assert.Empty(TimelineMath.BuildTicks(new List<DateTime>()));
}
