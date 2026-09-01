namespace Vigie24.Domain;

/// <summary>
/// Per-source-kind polling schedule.
/// RSS/Atom feeds are polled on a regular interval (defaults to
/// FeedReader:PollingIntervalMinutes); YouTube and Reddit are polled at fixed
/// UTC slots (08:00 / 20:00) — twice a day — to cut down on rate-limited
/// fetches (Reddit throttles datacenter IPs, YouTube throttles unauthenticated
/// access) while still catching morning and evening publications.
/// Pure logic — no infrastructure, unit-testable.
/// </summary>
public static class SourcePollingSchedule
{
    /// <summary>Fixed UTC polling times for the low-frequency kinds (2× per day).</summary>
    public static readonly TimeSpan[] FixedPollingTimes =
    [
        TimeSpan.FromHours(8),
        TimeSpan.FromHours(20)
    ];

    /// <summary>Source kinds polled at the fixed slots (case-insensitive; "Youtube" is the seeded casing).</summary>
    private static readonly string[] LowFrequencyKinds = ["Youtube", "Reddit"];

    /// <summary>
    /// Next fixed slot STRICTLY after <paramref name="last"/>. A source fetched at
    /// 08:00 is not due again until 20:00 (the same-day slot is excluded, so a
    /// just-fetched source is never fetched twice within the same slot).
    /// </summary>
    public static DateTime NextSlotAfter(DateTime last)
    {
        var day = last.Date;
        foreach (var time in FixedPollingTimes)
        {
            var slot = day.Add(time);
            if (slot > last)
                return slot;
        }
        return day.AddDays(1).Add(FixedPollingTimes[0]);
    }

    /// <summary>
    /// Whether a source is due for a fetch at <paramref name="utcNow"/>.
    /// A source never fetched yet (e.g. freshly reactivated) is always due — catch-up.
    /// </summary>
    public static bool IsDue(string? sourceKind, DateTime? lastFetchAttemptAt, DateTime utcNow, TimeSpan rssInterval)
    {
        if (lastFetchAttemptAt is not { } last)
            return true;

        if (IsLowFrequencyKind(sourceKind))
            return utcNow >= NextSlotAfter(last);

        return utcNow - last >= rssInterval;
    }

    private static bool IsLowFrequencyKind(string? kind)
        => !string.IsNullOrEmpty(kind)
           && LowFrequencyKinds.Any(k => string.Equals(kind, k, StringComparison.OrdinalIgnoreCase));
}
