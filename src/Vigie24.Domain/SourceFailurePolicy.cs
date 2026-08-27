namespace Vigie24.Domain;

/// <summary>
/// Pure decision logic for per-source fetch-failure handling
/// (consecutive-failure counter and auto-disable threshold).
/// No infrastructure — unit-testable.
/// </summary>
public static class SourceFailurePolicy
{
    /// <summary>Default number of consecutive fetch failures before a source is auto-disabled (FeedReader:MaxFetchFailures).</summary>
    public const int DefaultMaxFailures = 4;

    /// <summary>Counter value after one more consecutive failure.</summary>
    public static int NextFailureCount(int current) => current + 1;

    /// <summary>True when the source must be disabled (maxFailures &lt;= 0 disables the rule).</summary>
    public static bool ShouldDisable(int failureCount, int maxFailures)
        => maxFailures > 0 && failureCount >= maxFailures;

    /// <summary>French reason recorded on the source when auto-disabled (product language).</summary>
    public static string DisableReason(int failureCount)
        => $"Désactivée automatiquement après {failureCount} échecs de récupération consécutifs";
}
