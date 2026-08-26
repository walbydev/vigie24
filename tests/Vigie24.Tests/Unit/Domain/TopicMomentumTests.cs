using Vigie24.Domain;
using Xunit;

namespace Vigie24.Tests.Unit.Domain;

/// <summary>Contract tests for topic momentum (« radar des sujets chauds »).</summary>
public class TopicMomentumTests
{
    [Fact]
    public void Compute_RisingTopic_ShouldScoreAboveThreshold()
    {
        // 8 articles sur 7 j alors que la baseline 28 j est de 16 (4/semaine) → ratio 2
        var result = TopicMomentumCalculator.Compute(new[] { ("IA", 8, 16) }).Single();

        Assert.Equal(2.0, result.Score, 6);
        Assert.True(result.Score >= TopicMomentumCalculator.RisingThreshold);
    }

    [Fact]
    public void Compute_FallingTopic_ShouldScoreBelowThreshold()
    {
        // 2 articles sur 7 j vs 16 sur 28 j (4/semaine) → ratio 0.5
        var result = TopicMomentumCalculator.Compute(new[] { ("Sécurité", 2, 16) }).Single();

        Assert.Equal(0.5, result.Score, 6);
        Assert.True(result.Score <= TopicMomentumCalculator.FallingThreshold);
    }

    [Fact]
    public void Compute_StableTopic_ShouldScoreAroundOne()
    {
        // 4 articles sur 7 j vs 16 sur 28 j → ratio 1
        var result = TopicMomentumCalculator.Compute(new[] { ("Hardware", 4, 16) }).Single();

        Assert.Equal(1.0, result.Score, 6);
    }

    [Fact]
    public void Compute_NewTopic_ShouldNotDivideByZero()
    {
        // Aucun historique 28 j → baseline plancher 0,5 → ratio = 7j/0,5
        var result = TopicMomentumCalculator.Compute(new[] { ("Nouveau", 3, 0) }).Single();

        Assert.Equal(6.0, result.Score, 6);
    }

    [Fact]
    public void Compute_ShouldSortDescending()
    {
        var result = TopicMomentumCalculator.Compute(new[]
        {
            ("B", 2, 16),
            ("A", 8, 16),
            ("C", 4, 16)
        });

        Assert.Equal(new[] { "A", "C", "B" }, result.Select(r => r.Slug));
    }

    [Fact]
    public void Compute_Empty_ShouldReturnEmpty()
    {
        Assert.Empty(TopicMomentumCalculator.Compute(Array.Empty<(string, int, int)>()));
    }
}
