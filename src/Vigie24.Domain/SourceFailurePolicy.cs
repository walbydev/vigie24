namespace Vigie24.Domain;

/// <summary>
/// Nature of a feed-fetch failure. Only permanent failures may auto-disable
/// a source; transient ones (rate limits, 5xx, timeouts) just back off.
/// </summary>
public enum FetchFailureKind
{
    /// <summary>Rate limit, 5xx, 403, timeout, transport error — retry later, never auto-disable.</summary>
    Transient,

    /// <summary>404/410, malformed XML, dead DNS — the feed is really gone.</summary>
    Permanent
}

/// <summary>
/// Pure decision logic for per-source fetch-failure handling:
/// consecutive-failure counter, auto-disable threshold, retry backoff ladder,
/// permanent-vs-transient classification and auto-recovery probing.
/// No infrastructure — unit-testable.
/// </summary>
public static class SourceFailurePolicy
{
    /// <summary>Default number of consecutive PERMANENT failures before a source is auto-disabled (FeedReader:MaxFetchFailures).</summary>
    public const int DefaultMaxFailures = 8;

    /// <summary>Default delay before a disabled source is re-tested (FeedReader:RecoveryProbeAfterHours).</summary>
    public const int DefaultRecoveryProbeAfterHours = 24;

    /// <summary>Counter value after one more consecutive failure.</summary>
    public static int NextFailureCount(int current) => current + 1;

    /// <summary>True when the source must be disabled (maxFailures &lt;= 0 disables the rule).</summary>
    public static bool ShouldDisable(int failureCount, int maxFailures)
        => maxFailures > 0 && failureCount >= maxFailures;

    /// <summary>French reason recorded on the source when auto-disabled (product language).</summary>
    public static string DisableReason(int failureCount)
        => $"Désactivée automatiquement après {failureCount} échecs consécutifs (erreur définitive)";

    /// <summary>
    /// Retry backoff in minutes after N consecutive failures — exponential ladder
    /// capped at 6 h: 5, 10, 20, 40, 80, 160, 320, 360, 360, …
    /// </summary>
    public static int BackoffMinutes(int failureCount)
    {
        if (failureCount <= 0) return 0;
        var minutes = 5 * (1 << Math.Min(failureCount - 1, 7)); // 5,10,20,40,80,160,320,640
        return Math.Min(minutes, 360);
    }

    /// <summary>
    /// Classifies a fetch exception. Permanent = the feed is really gone
    /// (404/410, XML parse error, DNS host not found). Everything else
    /// (429/5xx/403, timeout, transport) is transient and must NOT disable.
    /// </summary>
    public static FetchFailureKind Classify(Exception ex)
    {
        if (ex is HttpRequestException hre && hre.StatusCode is { } status)
        {
            return status is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Gone
                ? FetchFailureKind.Permanent
                : FetchFailureKind.Transient;
        }

        if (ex is System.Xml.XmlException) return FetchFailureKind.Permanent;

        if (ex is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.HostNotFound })
            return FetchFailureKind.Permanent;

        // TaskCanceledException (timeout), HttpRequestException without status
        // (transport-level), anything else — conservative: transient.
        return FetchFailureKind.Transient;
    }
}
