using Vigie24.Domain;
using Xunit;

namespace Vigie24.Tests.Unit.Domain;

/// <summary>Contract tests for the « mémoire de la veille » window selection.</summary>
public class YearAgoTests
{
    [Fact]
    public void Window_ShouldSpanThreeDaysCenteredOnLastYear()
    {
        var (from, to) = YearAgo.Window(new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2025, 8, 25), from); // J-366
        Assert.Equal(new DateTime(2025, 8, 27), to);   // J-364 (exclusif)
    }

    [Fact]
    public void MonthsUntil_OneWeekOfData_ShouldReportElevenMonths()
    {
        var oldest = new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc); // ~7 jours de données
        var months = YearAgo.MonthsUntil(oldest, new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(11, months);
    }

    [Fact]
    public void MonthsUntil_FullYearOfData_ShouldReportZero()
    {
        var oldest = new DateTime(2025, 8, 25, 0, 0, 0, DateTimeKind.Utc);
        var months = YearAgo.MonthsUntil(oldest, new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(0, months);
    }

    [Fact]
    public void MonthsUntil_MoreThanAYear_ShouldClampToZero()
    {
        var oldest = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc); // plus d'un an de données
        var months = YearAgo.MonthsUntil(oldest, new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(0, months);
    }
}
