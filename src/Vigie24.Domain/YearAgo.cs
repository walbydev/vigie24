namespace Vigie24.Domain;

/// <summary>
/// « La mémoire de la veille » : selects what the watch said one year ago.
/// Window = [today − 366 days, today − 364 days) (the same date last year, ±1 day).
/// </summary>
public static class YearAgo
{
    public const int TakeCount = 20;

    /// <summary>Returns the (inclusive start, exclusive end) window for "one year ago".</summary>
    public static (DateTime From, DateTime To) Window(DateTime todayUtc)
    {
        var today = todayUtc.Date;
        return (today.AddDays(-366), today.AddDays(-364));
    }

    /// <summary>
    /// Whole months remaining before the archive holds a full year of data.
    /// oldestUtc = date of the oldest article in the database (UTC).
    /// </summary>
    public static int MonthsUntil(DateTime oldestUtc, DateTime todayUtc)
    {
        var days = (oldestUtc.Date - todayUtc.Date.AddDays(-365)).Days;
        return Math.Max(0, (int)Math.Floor(days / 30.4));
    }
}
