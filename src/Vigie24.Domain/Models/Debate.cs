namespace Vigie24.Domain.Models;

/// <summary>
/// Debate: a grouping of articles covering the same subject with opposing
/// stances (for / against / nuanced) — "side-by-side of contradictory articles
/// to illustrate a debate/point" (original specification).
/// </summary>
public class Debate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Debate topic, normalized (lowercase, no diacritics, hyphens)</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Displayable topic (first formulation encountered)</summary>
    public string DisplayTopic { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<DebateArticle> Articles { get; set; } = new List<DebateArticle>();
}

/// <summary>Link between a debate and an article, with the article's stance</summary>
public class DebateArticle
{
    public Guid DebateId { get; set; }
    public Guid ArticleId { get; set; }

    /// <summary>Article stance: pour / contre / nuance / neutre</summary>
    public string Stance { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Debate Debate { get; set; } = null!;
    public virtual Article Article { get; set; } = null!;
}
