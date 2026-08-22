namespace Vigie24.Tests.Unit.Services;

using Vigie24.Domain;

/// <summary>Tests unitaires pour la logique d'évaluation du health-check.</summary>
public class HealthEvaluatorTests
{
    private static readonly TimeSpan OkThreshold = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DegradedThreshold = TimeSpan.FromHours(2);

    [Fact]
    public void Evaluate_ProviderOk_RecentIngestion_ShouldReturnOk()
    {
        var level = HealthEvaluator.Evaluate(
            DateTime.UtcNow - TimeSpan.FromMinutes(5), true, OkThreshold, DegradedThreshold);

        Assert.Equal(HealthLevel.Ok, level);
    }

    [Fact]
    public void Evaluate_ProviderOk_OldIngestion_ShouldReturnDegraded()
    {
        var level = HealthEvaluator.Evaluate(
            DateTime.UtcNow - TimeSpan.FromMinutes(45), true, OkThreshold, DegradedThreshold);

        Assert.Equal(HealthLevel.Degraded, level);
    }

    [Fact]
    public void Evaluate_ProviderOk_VeryOldIngestion_ShouldReturnOffline()
    {
        var level = HealthEvaluator.Evaluate(
            DateTime.UtcNow - TimeSpan.FromHours(3), true, OkThreshold, DegradedThreshold);

        Assert.Equal(HealthLevel.Offline, level);
    }

    [Fact]
    public void Evaluate_ProviderUnreachable_RecentIngestion_ShouldReturnOffline()
    {
        var level = HealthEvaluator.Evaluate(
            DateTime.UtcNow - TimeSpan.FromMinutes(1), false, OkThreshold, DegradedThreshold);

        Assert.Equal(HealthLevel.Offline, level);
    }

    [Fact]
    public void Evaluate_NoIngestionAtAll_ShouldReturnOffline()
    {
        var level = HealthEvaluator.Evaluate(null, true, OkThreshold, DegradedThreshold);

        Assert.Equal(HealthLevel.Offline, level);
    }

    [Fact]
    public void FormatAge_UnderOneMinute_ShouldReturnInstant()
    {
        Assert.Equal("à l'instant", HealthEvaluator.FormatAge(TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void FormatAge_Minutes_ShouldBeReadable()
    {
        Assert.Equal("il y a 12 min", HealthEvaluator.FormatAge(TimeSpan.FromMinutes(12)));
    }

    [Fact]
    public void FormatAge_Hours_ShouldBeReadable()
    {
        Assert.Equal("il y a 3 h 05", HealthEvaluator.FormatAge(TimeSpan.FromHours(3) + TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void FormatAge_Days_ShouldBeReadable()
    {
        Assert.Equal("il y a 2 j", HealthEvaluator.FormatAge(TimeSpan.FromDays(2)));
    }
}
