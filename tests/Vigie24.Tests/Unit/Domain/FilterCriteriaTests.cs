namespace Vigie24.Tests.Unit.Domain;

using Vigie24.Domain.Models;

/// <summary>Tests des codecs JSON / query string du record FilterCriteria.</summary>
public class FilterCriteriaTests
{
    [Fact]
    public void ToQueryString_And_TryParse_Are_RoundTrippable()
    {
        var c = new FilterCriteria
        {
            SearchText = "IA & robotique",
            CountryCode = "CH",
            SourceKind = "press",
            SelectedSources = new[] { "Le Monde", "20 minutes" },
            MinScore = 60,
            MinRating = 3,
            RangeFrom = new DateTime(2026, 8, 1),
            RangeTo = new DateTime(2026, 8, 24),
            SortField = "score",
            PageSize = 100
        };

        Assert.True(FilterCriteria.TryParse(c.ToQueryString(), out var parsed));
        Assert.Equal(c, parsed);
    }

    [Fact]
    public void ToJson_And_FromJson_Are_RoundTrippable()
    {
        var c = new FilterCriteria
        {
            SearchText = "IA",
            CountryCode = "FR",
            SourceKind = "youtube",
            SelectedSources = new[] { "Numerama" },
            MinScore = 60,
            MinRating = 2,
            RangeFrom = new DateTime(2026, 8, 1),
            SortField = "clicks",
            PageSize = 20
        };

        var json = c.ToJson();
        var parsed = FilterCriteria.FromJson(json);

        Assert.NotNull(parsed);
        Assert.Equal(c, parsed);
    }

    [Fact]
    public void ToQueryString_EscapesSpecialCharacters()
    {
        var c = new FilterCriteria { SearchText = "a&b=c d%+é" };

        var qs = c.ToQueryString();

        Assert.DoesNotContain(' ', qs);
        Assert.DoesNotContain('&', qs[3..]); // seul le séparateur de paramètre initial
        Assert.True(FilterCriteria.TryParse(qs, out var parsed));
        Assert.Equal(c.SearchText, parsed.SearchText);
    }

    [Fact]
    public void TryParse_RejectsMalformedValues()
    {
        Assert.False(FilterCriteria.TryParse("?minScore=abc", out _));
        Assert.False(FilterCriteria.TryParse("?minScore=150", out _));
        Assert.False(FilterCriteria.TryParse("?minRating=9", out _));
        Assert.False(FilterCriteria.TryParse("?sort=zzz", out _));
        Assert.False(FilterCriteria.TryParse("?pageSize=0", out _));
        Assert.False(FilterCriteria.TryParse("?q=%zz", out _));
        Assert.False(FilterCriteria.TryParse("?maxAnxiety=abc", out _));
        Assert.False(FilterCriteria.TryParse("?maxAnxiety=11", out _));
        Assert.False(FilterCriteria.TryParse("?maxAnxiety=-1", out _));
    }

    [Fact]
    public void MaxAnxiety_RoundTrips_And_OmitsDefault()
    {
        // Default 10 = no filter → omitted from the query string
        Assert.Equal("", new FilterCriteria().ToQueryString());
        Assert.True(new FilterCriteria().IsEmpty);

        // Below 10 → serialized and parsed back
        var c = new FilterCriteria { MaxAnxiety = 3 };
        Assert.Equal("?maxAnxiety=3", c.ToQueryString());
        Assert.True(FilterCriteria.TryParse(c.ToQueryString(), out var parsed));
        Assert.Equal(3, parsed.MaxAnxiety);
        Assert.Equal(c, parsed);

        // Boundary values are accepted
        Assert.True(FilterCriteria.TryParse("?maxAnxiety=0", out var zero));
        Assert.Equal(0, zero.MaxAnxiety);
        Assert.True(FilterCriteria.TryParse("?maxAnxiety=10", out var ten));
        Assert.Equal(10, ten.MaxAnxiety);
    }

    [Fact]
    public void TryParse_EmptyOrNull_ReturnsEmptyCriteria()
    {
        Assert.True(FilterCriteria.TryParse(null, out var nullCriteria));
        Assert.True(nullCriteria.IsEmpty);

        Assert.True(FilterCriteria.TryParse("", out var emptyCriteria));
        Assert.True(emptyCriteria.IsEmpty);
    }

    [Fact]
    public void ToQueryString_OmitsDefaultValues()
    {
        var c = new FilterCriteria { CountryCode = "CH" };

        Assert.Equal("?country=CH", c.ToQueryString());
        Assert.Equal("", new FilterCriteria().ToQueryString());
    }

    [Fact]
    public void Equality_ComparesSourcesByContent()
    {
        var a = new FilterCriteria { SelectedSources = new[] { "Le Monde", "20 minutes" } };
        var b = new FilterCriteria { SelectedSources = new List<string> { "Le Monde", "20 minutes" } };
        var c = new FilterCriteria { SelectedSources = new[] { "20 minutes", "Le Monde" } };

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
