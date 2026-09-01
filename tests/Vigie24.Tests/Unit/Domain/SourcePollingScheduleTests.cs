namespace Vigie24.Tests.Unit.Domain;

using Vigie24.Domain;

/// <summary>
/// Unit tests for SourcePollingSchedule (per-kind polling schedule:
/// RSS/Atom on a regular interval, YouTube/Reddit at fixed UTC slots 08:00/20:00).
/// </summary>
public class SourcePollingScheduleTests
{
    private static readonly TimeSpan Rss20 = TimeSpan.FromMinutes(20);

    // --- NextSlotAfter ---

    [Fact]
    public void NextSlotAfter_MorningSlot_ReturnsEveningSlot()
    {
        var last = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc), SourcePollingSchedule.NextSlotAfter(last));
    }

    [Fact]
    public void NextSlotAfter_BeforeMorningSlot_ReturnsSameDayMorningSlot()
    {
        var last = new DateTime(2026, 9, 1, 7, 59, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc), SourcePollingSchedule.NextSlotAfter(last));
    }

    [Fact]
    public void NextSlotAfter_AfterEveningSlot_ReturnsNextDayMorningSlot()
    {
        var last = new DateTime(2026, 9, 1, 20, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Utc), SourcePollingSchedule.NextSlotAfter(last));
    }

    // --- IsDue: never fetched → always due (catch-up, e.g. freshly reactivated) ---

    [Theory]
    [InlineData("Youtube")]
    [InlineData("Reddit")]
    [InlineData("Rss")]
    public void IsDue_NeverFetched_IsDue(string kind)
        => Assert.True(SourcePollingSchedule.IsDue(kind, null, new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), Rss20));

    // --- IsDue: YouTube/Reddit fixed slots ---

    [Fact]
    public void IsDue_YoutubeMorningSlotReached_IsDue()
    {
        var last = new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc); // previous slot
        var now = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Utc);

        Assert.True(SourcePollingSchedule.IsDue("Youtube", last, now, Rss20));
    }

    [Fact]
    public void IsDue_YoutubeJustFetched_NotDueAgainWithinSameSlot()
    {
        var last = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 9, 1, 8, 30, 0, DateTimeKind.Utc);

        Assert.False(SourcePollingSchedule.IsDue("Youtube", last, now, Rss20));
    }

    [Fact]
    public void IsDue_YoutubeMidDay_NotDue()
    {
        var last = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.False(SourcePollingSchedule.IsDue("Youtube", last, now, Rss20));
    }

    [Fact]
    public void IsDue_YoutubeMissedMorningSlot_CatchesUpLaterSameDay()
    {
        var last = new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc); // last night
        var now = new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);  // morning slot missed (worker was down)

        Assert.True(SourcePollingSchedule.IsDue("Youtube", last, now, Rss20));
    }

    [Fact]
    public void IsDue_YoutubeKind_IsCaseInsensitive()
        => Assert.False(SourcePollingSchedule.IsDue("youtube",
            new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), Rss20));

    [Fact]
    public void IsDue_RedditEveningSlotReached_IsDue()
    {
        var last = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc);

        Assert.True(SourcePollingSchedule.IsDue("Reddit", last, now, Rss20));
    }

    // --- IsDue: RSS/Atom regular interval ---

    [Fact]
    public void IsDue_RssExactlyAtInterval_IsDue()
    {
        var last = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        Assert.True(SourcePollingSchedule.IsDue("Rss", last, last.Add(Rss20), Rss20));
    }

    [Fact]
    public void IsDue_RssIntervalNotReached_NotDue()
    {
        var last = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 9, 1, 8, 19, 59, DateTimeKind.Utc);

        Assert.False(SourcePollingSchedule.IsDue("Rss", last, now, Rss20));
    }

    [Fact]
    public void IsDue_RssIntervalReached_IsDue()
    {
        var last = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 9, 1, 8, 21, 0, DateTimeKind.Utc);

        Assert.True(SourcePollingSchedule.IsDue("Rss", last, now, Rss20));
    }

    // --- IsDue: unknown kinds fall back to the RSS interval ---

    [Fact]
    public void IsDue_UnknownKind_UsesRssInterval()
        => Assert.False(SourcePollingSchedule.IsDue("Press",
            new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 8, 10, 0, DateTimeKind.Utc), Rss20));

    [Fact]
    public void IsDue_NullKind_UsesRssInterval()
        => Assert.False(SourcePollingSchedule.IsDue(null,
            new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 8, 10, 0, DateTimeKind.Utc), Rss20));
}
