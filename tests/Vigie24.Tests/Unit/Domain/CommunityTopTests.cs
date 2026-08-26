using Vigie24.Domain;
using Xunit;

namespace Vigie24.Tests.Unit.Domain;

/// <summary>Contract tests for the community top (votes 0-5).</summary>
public class CommunityTopTests
{
    private static CommunityTopEntry Entry(Guid id, decimal avg, int votes)
        => new(id, $"Article {id}", avg, votes, 0, 70m);

    [Fact]
    public void Compute_ShouldExcludeArticlesWithFewVotes()
    {
        var entries = new[]
        {
            Entry(Guid.NewGuid(), 5m, 1),   // exclu : 1 seul vote
            Entry(Guid.NewGuid(), 4.8m, 3),
            Entry(Guid.NewGuid(), 4.9m, 2)
        };

        var result = CommunityTop.Compute(entries);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.True(e.VoteCount >= CommunityTop.MinVotes));
    }

    [Fact]
    public void Compute_ShouldOrderByAverageThenVoteCount()
    {
        var entries = new[]
        {
            Entry(Guid.NewGuid(), 4.5m, 10),
            Entry(Guid.NewGuid(), 4.8m, 2),
            Entry(Guid.NewGuid(), 4.5m, 6)
        };

        var result = CommunityTop.Compute(entries);

        Assert.Equal(4.8m, result[0].VoteAverage);
        Assert.Equal(10, result[1].VoteCount); // 4,5 avec 10 votes avant 4,5 avec 6
        Assert.Equal(6, result[2].VoteCount);
    }

    [Fact]
    public void Compute_ShouldCapAtTen()
    {
        var entries = Enumerable.Range(0, 15)
            .Select(i => Entry(Guid.NewGuid(), 4m + i * 0.05m, 3))
            .ToList();

        var result = CommunityTop.Compute(entries);

        Assert.Equal(CommunityTop.TopCount, result.Count);
    }

    [Fact]
    public void Compute_Empty_ShouldReturnEmpty()
    {
        Assert.Empty(CommunityTop.Compute(Array.Empty<CommunityTopEntry>()));
    }
}
