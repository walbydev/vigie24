namespace Vigie24.Tests.Unit.Domain;

using System.Net;
using System.Net.Sockets;
using Vigie24.Domain;

/// <summary>
/// Unit tests for SourceFailurePolicy (per-source fetch-failure counter,
/// auto-disable threshold, retry backoff ladder and failure classification).
/// </summary>
public class SourceFailurePolicyTests
{
    [Fact]
    public void DefaultMaxFailures_IsEight()
        => Assert.Equal(8, SourceFailurePolicy.DefaultMaxFailures);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 4)]
    [InlineData(9, 10)]
    public void NextFailureCount_Increments(int current, int expected)
        => Assert.Equal(expected, SourceFailurePolicy.NextFailureCount(current));

    [Theory]
    [InlineData(7, 8, false)]   // below threshold → keep active
    [InlineData(8, 8, true)]    // at threshold → disable
    [InlineData(11, 8, true)]   // beyond threshold → disable
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

    [Theory]
    [InlineData(0, 0)]    // no failure → no backoff
    [InlineData(1, 5)]    // 5 min
    [InlineData(2, 10)]   // 10 min
    [InlineData(3, 20)]   // 20 min
    [InlineData(4, 40)]   // 40 min
    [InlineData(5, 80)]   // 80 min
    [InlineData(6, 160)]  // 160 min
    [InlineData(7, 320)]  // 320 min
    [InlineData(8, 360)]  // capped at 6 h
    [InlineData(20, 360)] // capped at 6 h
    public void BackoffMinutes_ExponentialLadderCappedAtSixHours(int failures, int expected)
        => Assert.Equal(expected, SourceFailurePolicy.BackoffMinutes(failures));

    [Theory]
    [InlineData(404)]  // feed really gone
    [InlineData(410)]  // gone
    public void Classify_HttpStatus404Or410_IsPermanent(int statusCode)
    {
        var ex = new HttpRequestException("Not found", null, (HttpStatusCode)statusCode);

        Assert.Equal(FetchFailureKind.Permanent, SourceFailurePolicy.Classify(ex));
    }

    [Theory]
    [InlineData(429)]  // rate limited — transient
    [InlineData(403)]  // anti-bot — treated as transient (never auto-disable on 403)
    [InlineData(500)]  // server error — transient
    [InlineData(503)]  // server error — transient
    public void Classify_HttpStatusTransient_IsTransient(int statusCode)
    {
        var ex = new HttpRequestException("Error", null, (HttpStatusCode)statusCode);

        Assert.Equal(FetchFailureKind.Transient, SourceFailurePolicy.Classify(ex));
    }

    [Fact]
    public void Classify_XmlException_IsPermanent()
        => Assert.Equal(FetchFailureKind.Permanent, SourceFailurePolicy.Classify(new System.Xml.XmlException("malformed")));

    [Fact]
    public void Classify_DnsHostNotFound_IsPermanent()
        => Assert.Equal(FetchFailureKind.Permanent, SourceFailurePolicy.Classify(new SocketException((int)SocketError.HostNotFound)));

    [Fact]
    public void Classify_Timeout_IsTransient()
        => Assert.Equal(FetchFailureKind.Transient, SourceFailurePolicy.Classify(new TaskCanceledException("timeout")));

    [Fact]
    public void Classify_TransportError_IsTransient()
        => Assert.Equal(FetchFailureKind.Transient, SourceFailurePolicy.Classify(new HttpRequestException("connection reset")));

    [Fact]
    public void Classify_UnknownException_IsTransient()
        => Assert.Equal(FetchFailureKind.Transient, SourceFailurePolicy.Classify(new InvalidOperationException("?")));
}
