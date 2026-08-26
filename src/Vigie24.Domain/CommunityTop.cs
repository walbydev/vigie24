namespace Vigie24.Domain;

/// <summary>One entry of the community top (votes 0-5 over a period).</summary>
public record CommunityTopEntry(
    Guid ArticleId,
    string Title,
    decimal VoteAverage,
    int VoteCount,
    int ClickCount,
    decimal EditorialScore);

/// <summary>
/// Community top (« top de la communauté »): ranks the most voted articles
/// of the period. A minimum vote count avoids a single 5★ vote topping the list.
/// </summary>
public static class CommunityTop
{
    /// <summary>Minimum number of votes for an article to be eligible.</summary>
    public const int MinVotes = 2;

    public const int TopCount = 10;

    public static List<CommunityTopEntry> Compute(IEnumerable<CommunityTopEntry> entries)
        => entries
            .Where(e => e.VoteCount >= MinVotes)
            .OrderByDescending(e => e.VoteAverage)
            .ThenByDescending(e => e.VoteCount)
            .ThenByDescending(e => e.EditorialScore)
            .Take(TopCount)
            .ToList();
}
