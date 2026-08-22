using Vigie24.Domain;

namespace Vigie24.Tests.Unit.Domain;

/// <summary>
/// Tests unitaires du score d'engagement (votes 0-5 + clics) et de son intégration
/// dans le score de classement (Vigie24.Domain.ArticleScoring).
/// </summary>
public class ArticleScoringEngagementTests
{
    [Fact]
    public void NoEngagement_ShouldReturnZero()
    {
        Assert.Equal(0m, ArticleScoring.CalculateEngagementScore(0, 0m, 0));
    }

    [Fact]
    public void VotesOnly_PerfectAverage_ShouldReturnSixty()
    {
        // 5 votes à 5/5 → composante vote 100 × 0,6 = 60
        Assert.Equal(60m, ArticleScoring.CalculateEngagementScore(5, 5m, 0));
    }

    [Fact]
    public void VotesOnly_MidAverage_ShouldScaleLinearly()
    {
        // Moyenne 2,5/5 → 50 × 0,6 = 30
        var score = ArticleScoring.CalculateEngagementScore(4, 2.5m, 0);
        Assert.Equal(30m, score);
    }

    [Fact]
    public void LowVotes_ShouldYieldLowerEngagementThanHighVotes()
    {
        var bad = ArticleScoring.CalculateEngagementScore(3, 1m, 0);
        var good = ArticleScoring.CalculateEngagementScore(3, 5m, 0);
        Assert.True(bad < good);
        Assert.Equal(12m, bad);   // 1/5 → 20 × 0,6
        Assert.Equal(60m, good);  // 5/5 → 100 × 0,6
    }

    [Fact]
    public void ClicksOnly_ShouldScaleAndCapAtForty()
    {
        Assert.Equal(4m, ArticleScoring.CalculateEngagementScore(0, 0m, 1));
        Assert.Equal(40m, ArticleScoring.CalculateEngagementScore(0, 0m, 10));
        Assert.Equal(40m, ArticleScoring.CalculateEngagementScore(0, 0m, 500));
    }

    [Fact]
    public void MixedEngagement_ShouldCombineVotesAndClicks()
    {
        // Votes : 5/5 → 100 (×0,6 = 60) ; Clics : 5 → 50 (×0,4 = 20) → 80
        Assert.Equal(80m, ArticleScoring.CalculateEngagementScore(2, 5m, 5));
    }

    [Fact]
    public void Ranking_WithEngagement_ShouldBeHigherThanWithout()
    {
        var without = ArticleScoring.CalculateRanking(80m, 60m);
        var with = ArticleScoring.CalculateRanking(80m, 60m, 50m);
        Assert.True(with > without);
        Assert.Equal(5m, with - without); // 50 × 0,10
    }

    [Fact]
    public void Ranking_WithoutEngagement_ShouldEqualTwoArgOverload()
    {
        Assert.Equal(
            ArticleScoring.CalculateRanking(80m, 60m),
            ArticleScoring.CalculateRanking(80m, 60m, 0m));
    }

    [Fact]
    public void Weights_ShouldSumToOne()
    {
        Assert.Equal(1.00m,
            ArticleScoring.EditorialWeight + ArticleScoring.AlgorithmicWeight + ArticleScoring.EngagementWeight);
    }
}
