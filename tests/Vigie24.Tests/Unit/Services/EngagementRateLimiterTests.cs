using Vigie24.Services;
using Xunit;

namespace Vigie24.Tests.Unit.Services;

/// <summary>
/// Tests de l'anti-abus d'engagement (fenêtre fixe par clé/IP).
/// </summary>
public class EngagementRateLimiterTests
{
    [Fact]
    public void TryConsume_AllowsUpToPermitLimit()
    {
        var limiter = new EngagementRateLimiter(permitLimit: 3, window: TimeSpan.FromHours(1));

        Assert.True(limiter.TryConsume("1.2.3.4"));
        Assert.True(limiter.TryConsume("1.2.3.4"));
        Assert.True(limiter.TryConsume("1.2.3.4"));
        Assert.False(limiter.TryConsume("1.2.3.4")); // limite atteinte
    }

    [Fact]
    public void TryConsume_KeysAreIndependent()
    {
        var limiter = new EngagementRateLimiter(permitLimit: 2, window: TimeSpan.FromHours(1));

        Assert.True(limiter.TryConsume("ip-a"));
        Assert.True(limiter.TryConsume("ip-a"));
        Assert.False(limiter.TryConsume("ip-a")); // ip-a épuisée

        Assert.True(limiter.TryConsume("ip-b"));   // ip-b intacte
    }

    [Fact]
    public void TryConsume_WindowExpiryResetsBudget()
    {
        var limiter = new EngagementRateLimiter(permitLimit: 1, window: TimeSpan.FromMilliseconds(50));

        Assert.True(limiter.TryConsume("ip"));
        Assert.False(limiter.TryConsume("ip"));

        Thread.Sleep(80); // fenêtre expirée
        Assert.True(limiter.TryConsume("ip"));
    }

    [Fact]
    public void Cleanup_RemovesExpiredEntries()
    {
        var limiter = new EngagementRateLimiter(permitLimit: 1, window: TimeSpan.FromMilliseconds(20));

        limiter.TryConsume("ip1");
        limiter.TryConsume("ip2");
        Thread.Sleep(60);

        limiter.Cleanup();
        // Les entrées sont purgées : les deux clés peuvent reconsommer sans limite résiduelle.
        Assert.True(limiter.TryConsume("ip1"));
        Assert.True(limiter.TryConsume("ip2"));
    }
}
