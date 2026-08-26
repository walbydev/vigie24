namespace Vigie24.Domain;

/// <summary>Topic momentum: observed 7-day activity vs the 28-day baseline.</summary>
public record TopicMomentum(string Slug, int Count7d, int Count28d, double Score);

/// <summary>
/// Computes topic momentum (« radar des sujets chauds »): for each tag,
/// score = count7d / (count28d / 4) — the 7-day count compared to what a
/// uniform 28-day activity would produce. &gt; 1 = rising, &lt; 1 = falling.
/// Pure computation, no DB dependency.
/// </summary>
public static class TopicMomentumCalculator
{
    /// <summary>Score above which a topic is flagged "rising" (hausse).</summary>
    public const double RisingThreshold = 1.2;

    /// <summary>Score below which a topic is flagged "falling" (baisse).</summary>
    public const double FallingThreshold = 0.8;

    public static List<TopicMomentum> Compute(IEnumerable<(string Slug, int Count7d, int Count28d)> rows)
        => rows
            .Select(r => new TopicMomentum(r.Slug, r.Count7d, r.Count28d, Score(r.Count7d, r.Count28d)))
            .OrderByDescending(t => t.Score)
            .ToList();

    /// <summary>7-day count vs the 28-day baseline (÷4), guarded against division by zero.</summary>
    public static double Score(int count7d, int count28d)
    {
        var baseline = Math.Max(count28d / 4.0, 0.5);
        return count7d / baseline;
    }
}
