namespace Vigie24.Domain;

/// <summary>One daily bucket of the anxiety index.</summary>
public record AnxietyDay(DateTime Day, double Average, int ArticleCount);

/// <summary>Anxiety average over a label (category, country…).</summary>
public record AnxietyCategory(string Label, double Average, int ArticleCount);

/// <summary>
/// Result of the anxiety barometer computation over the last 30 days.
/// "Today" = the most recent day that carries articles; the baseline is the
/// average of the previous days (up to 29). Delta = today − baseline.
/// </summary>
public record AnxietySummary(
    double TodayAverage,
    double BaselineAverage,
    double Delta,
    double DeltaPercent,
    IReadOnlyList<AnxietyDay> Days,
    IReadOnlyList<AnxietyCategory> ByCategory,
    IReadOnlyList<AnxietyCategory> ByCountry);

/// <summary>
/// Pure computation of the anxiety barometer (Vigie24 signature feature):
/// aggregates per-article anxiety scores (0-10) into a daily index and compares
/// the latest day against the 30-day baseline. No DB dependency — testable.
/// </summary>
public static class AnxietyBarometer
{
    /// <summary>Threshold above which the daily index is flagged "anxiogène".</summary>
    public const double AnxietyThreshold = 6.0;

    /// <summary>Threshold below which the daily index is "calme".</summary>
    public const double CalmThreshold = 3.0;

    /// <summary>
    /// Computes the barometer from raw article rows (published only).
    /// </summary>
    /// <param name="rows">(PublishedAt, Anxiety) for the last 30 days — anxiety 0-10.</param>
    /// <param name="byCategory">(label, anxiety) rows for the per-category breakdown (optional).</param>
    /// <param name="byCountry">(label, anxiety) rows for the per-country breakdown (optional).</param>
    public static AnxietySummary Compute(
        IReadOnlyList<(DateTime PublishedAt, decimal Anxiety)> rows,
        IReadOnlyList<(string Label, decimal Anxiety)>? byCategory = null,
        IReadOnlyList<(string Label, decimal Anxiety)>? byCountry = null)
    {
        var days = rows
            .GroupBy(r => r.PublishedAt.Date)
            .Select(g => new AnxietyDay(g.Key, g.Average(r => (double)r.Anxiety), g.Count()))
            .OrderBy(d => d.Day)
            .ToList();

        var categories = GroupByLabel(byCategory ?? []);
        var countries = GroupByLabel(byCountry ?? []);

        if (days.Count == 0)
            return new AnxietySummary(0, 0, 0, 0, days, categories, countries);

        var today = days[^1];
        var baselineDays = days.Take(days.Count - 1).ToList();
        var baseline = baselineDays.Count > 0
            ? baselineDays.Average(d => d.Average)
            : today.Average; // single day of data: no baseline yet → delta 0

        var delta = today.Average - baseline;
        var deltaPercent = baseline > 0 ? delta / baseline * 100.0 : 0;

        return new AnxietySummary(
            today.Average,
            baseline,
            delta,
            deltaPercent,
            days,
            categories,
            countries);
    }

    /// <summary>Groups (label, anxiety) rows into labeled averages, ordered by average desc.</summary>
    public static List<AnxietyCategory> GroupByLabel(IEnumerable<(string Label, decimal Anxiety)> rows)
        => rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Label))
            .GroupBy(r => r.Label)
            .Select(g => new AnxietyCategory(g.Key, g.Average(r => (double)r.Anxiety), g.Count()))
            .OrderByDescending(c => c.Average)
            .ToList();
}
