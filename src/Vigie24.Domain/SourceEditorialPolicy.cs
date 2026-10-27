namespace Vigie24.Domain;

/// <summary>
/// Editorial policy attached to a source.
///
/// Sources flagged <see cref="Commercial"/> (buying guides, affiliate reviews,
/// provider landing pages) are never ingested: such content is long, well
/// structured and passes the editorial score threshold, yet carries no
/// monitoring value — it exists to generate a click or a signup.
///
/// <c>IsActive</c> is deliberately NOT reused for this: the S3 recovery probe
/// (ProbeDisabledSourcesAsync) re-activates auto-disabled sources after 24 h,
/// and would therefore resurrect an exclusion decided by editorial policy.
/// Pure logic — no infrastructure, unit-testable.
/// </summary>
public static class SourceEditorialPolicy
{
    /// <summary>Default: the source is ingested normally.</summary>
    public const string Standard = "Standard";

    /// <summary>Commercial / affiliate source — excluded from ingestion.</summary>
    public const string Commercial = "Commercial";

    /// <summary>Whether the policy forbids ingesting this source's feed.</summary>
    public static bool BlocksIngestion(string? policy)
        => string.Equals(policy, Commercial, StringComparison.OrdinalIgnoreCase);

    /// <summary>Canonical value for a nullable/legacy column value.</summary>
    public static string Normalize(string? policy)
        => string.IsNullOrWhiteSpace(policy) ? Standard : policy.Trim();
}
