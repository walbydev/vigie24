namespace Vigie24.Tests.Unit.Domain;

using Vigie24.Domain;

/// <summary>
/// Unit tests for SourceFailurePolicy (per-source fetch-failure counter
/// and auto-disable threshold).
/// </summary>
public class SourceFailurePolicyTests
{
    [Fact]
    public void DefaultMaxFailures_IsFour()
        => Assert.Equal(4, SourceFailurePolicy.DefaultMaxFailures);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 4)]
    [InlineData(9, 10)]
    public void NextFailureCount_Increments(int current, int expected)
        => Assert.Equal(expected, SourceFailurePolicy.NextFailureCount(current));

    [Theory]
    [InlineData(3, 4, false)]   // below threshold → keep active
    [InlineData(4, 4, true)]    // at threshold → disable
    [InlineData(7, 4, true)]    // beyond threshold → disable
    [InlineData(1, 1, true)]    // single-failure policy
    [InlineData(99, 0, false)]  // threshold disabled → never auto-disable
    public void ShouldDisable_RespectsThreshold(int failures, int max, bool expected)
        => Assert.Equal(expected, SourceFailurePolicy.ShouldDisable(failures, max));

    [Fact]
    public void DisableReason_MentionsFailureCount()
    {
        var reason = SourceFailurePolicy.DisableReason(4);

        Assert.Contains("4", reason);
        Assert.Contains("échecs", reason);
    }
}
