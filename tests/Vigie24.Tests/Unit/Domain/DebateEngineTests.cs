namespace Vigie24.Tests.Unit.Domain;

using Vigie24.Domain;
using Vigie24.Domain.Models;

/// <summary>
/// Tests for grouping articles into debates (DebateEngine).
/// </summary>
public class DebateEngineTests
{
    private static Article Article(Guid id, string? topic, string? stance)
        => new() { Id = id, Topic = topic, Stance = stance };

    [Fact]
    public void Build_WithForAndAgainst_SameTopic_CreatesDebate()
    {
        var articles = new[]
        {
            Article(Guid.NewGuid(), "régulation IA Europe", "pour"),
            Article(Guid.NewGuid(), "Régulation de l'IA en Europe", "contre")
        };

        var groups = DebateEngine.Build(articles);

        Assert.Single(groups);
        Assert.Equal(2, groups.Values.Single().Entries.Count);
    }

    [Fact]
    public void Build_WithSameStance_NoDebate()
    {
        var articles = new[]
        {
            Article(Guid.NewGuid(), "régulation IA Europe", "pour"),
            Article(Guid.NewGuid(), "régulation IA Europe", "pour")
        };

        var groups = DebateEngine.Build(articles);

        Assert.Empty(groups);
    }

    [Fact]
    public void Build_WithDifferentTopics_NoDebate()
    {
        var articles = new[]
        {
            Article(Guid.NewGuid(), "régulation IA Europe", "pour"),
            Article(Guid.NewGuid(), "télétravail obligatoire", "contre")
        };

        var groups = DebateEngine.Build(articles);

        Assert.Empty(groups);
    }

    [Fact]
    public void Build_WithNuancedAndNeutral_JoinsDebateWithoutTriggeringIt()
    {
        var articles = new[]
        {
            Article(Guid.NewGuid(), "régulation IA Europe", "pour"),
            Article(Guid.NewGuid(), "régulation IA Europe", "contre"),
            Article(Guid.NewGuid(), "régulation IA Europe", "nuance"),
            Article(Guid.NewGuid(), "régulation IA Europe", "neutre")
        };

        var groups = DebateEngine.Build(articles);

        var debate = Assert.Single(groups).Value;
        Assert.Equal(4, debate.Entries.Count);
    }

    [Fact]
    public void Build_WithOnlyNuanced_NoDebate()
    {
        var articles = new[]
        {
            Article(Guid.NewGuid(), "régulation IA Europe", "nuance"),
            Article(Guid.NewGuid(), "régulation IA Europe", "nuance")
        };

        Assert.Empty(DebateEngine.Build(articles));
    }

    [Fact]
    public void NormalizeTopic_RemovesAccentsAndStopWords()
    {
        Assert.Equal("regulation ia europe", DebateEngine.NormalizeTopic("Régulation de l'IA en Europe"));
        Assert.Equal("regulation ia europeenne", DebateEngine.NormalizeTopic("régulation IA européenne"));
    }

    [Fact]
    public void NormalizeStance_AcceptsVariants()
    {
        Assert.Equal("pour", DebateEngine.NormalizeStance("Pour"));
        Assert.Equal("contre", DebateEngine.NormalizeStance("opposition"));
        Assert.Equal("nuance", DebateEngine.NormalizeStance("nuancé"));
        Assert.Equal("nuance", DebateEngine.NormalizeStance("Mitigé"));
        Assert.Equal("neutre", DebateEngine.NormalizeStance("neutre"));
        Assert.Equal("", DebateEngine.NormalizeStance(""));
        Assert.Equal("", DebateEngine.NormalizeStance("rien"));
    }
}
