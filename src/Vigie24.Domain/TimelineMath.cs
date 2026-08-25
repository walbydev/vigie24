namespace Vigie24.Domain;

/// <summary>
/// Pure geometry for the vertical article timeline (no UI dependency):
/// fraction→page mapping, thumb size/position, date graduation ticks.
/// </summary>
public static class TimelineMath
{
    /// <summary>Fraction (0..1) of the track → page number (1..totalPages).</summary>
    public static int PageFromFraction(double fraction, int totalPages)
        => Math.Clamp((int)Math.Floor(fraction * totalPages) + 1, 1, Math.Max(1, totalPages));

    /// <summary>Thumb height in % of the track (clamped 4..100).</summary>
    public static double ThumbHeightPercent(int pageSize, int totalCount)
        => totalCount <= 0 ? 100 : Math.Clamp(pageSize * 100.0 / totalCount, 4, 100);

    /// <summary>Thumb top offset in % of the track (start of the visible window).</summary>
    public static double ThumbTopPercent(int page, int totalPages)
        => totalPages <= 1 ? 0 : (page - 1) * 100.0 / totalPages;

    public sealed record Tick(DateTime Start, TimeSpan Step, string Label);

    /// <summary>
    /// Graduation ticks covering [min, max] of the dates:
    /// daily (span ≤ 45 days), weekly (≤ 320 days), monthly otherwise.
    /// </summary>
    public static List<Tick> BuildTicks(List<DateTime> dates)
    {
        if (dates.Count == 0) return new List<Tick>();
        var min = dates.Min().Date;
        var max = dates.Max().Date;
        var span = (max - min).Days;
        var step = span <= 45 ? TimeSpan.FromDays(1)
                 : span <= 320 ? TimeSpan.FromDays(7)
                 : TimeSpan.FromDays(30);
        var ticks = new List<Tick>();
        for (var d = min; d <= max; d += step)
            ticks.Add(new Tick(d, step, d.ToString(step.Days >= 30 ? "MMM yyyy" : "dd/MM")));
        return ticks;
    }
}
