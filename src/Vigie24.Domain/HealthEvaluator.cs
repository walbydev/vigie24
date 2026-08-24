namespace Vigie24.Domain;

/// <summary>Overall system health level.</summary>
public enum HealthLevel
{
    /// <summary>Everything works: recent ingestion and reachable AI provider.</summary>
    Ok,

    /// <summary>Degraded: stale ingestion (but not critical).</summary>
    Degraded,

    /// <summary>Offline: no ingestion for a long time or unreachable AI provider.</summary>
    Offline,
}

/// <summary>
/// Pure health evaluation logic (no infrastructure, testable).
/// </summary>
public static class HealthEvaluator
{
    /// <summary>
    /// Evaluates the health level from the freshness of ingestion and
    /// the availability of the AI provider.
    /// </summary>
    /// <param name="lastIngestionUtc">UTC date of the last ingested article (null = none).</param>
    /// <param name="providerHealthy">Is the AI provider responding?</param>
    /// <param name="okThreshold">Max age for an "Operational" state.</param>
    /// <param name="degradedThreshold">Max age for a "Degraded" state (beyond: offline).</param>
    public static HealthLevel Evaluate(
        DateTime? lastIngestionUtc,
        bool providerHealthy,
        TimeSpan okThreshold,
        TimeSpan degradedThreshold)
    {
        // Unreachable provider → offline, regardless of data freshness.
        if (!providerHealthy)
            return HealthLevel.Offline;

        // No article ever ingested → nothing proves the system works.
        if (lastIngestionUtc is not { } last)
            return HealthLevel.Offline;

        var age = DateTime.UtcNow - last;
        if (age > degradedThreshold)
            return HealthLevel.Offline;
        if (age > okThreshold)
            return HealthLevel.Degraded;
        return HealthLevel.Ok;
    }

    /// <summary>Formats an age in French: "il y a 12 min", "il y a 3 h 05", "il y a 2 j".</summary>
    public static string FormatAge(TimeSpan age)
    {
        if (age < TimeSpan.FromMinutes(1))
            return "à l'instant";
        if (age < TimeSpan.FromHours(1))
            return $"il y a {(int)age.TotalMinutes} min";
        if (age < TimeSpan.FromDays(1))
            return $"il y a {(int)age.TotalHours} h {age.Minutes:D2}";
        return $"il y a {(int)age.TotalDays} j";
    }
}
