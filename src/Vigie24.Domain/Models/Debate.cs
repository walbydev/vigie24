namespace Vigie24.Domain.Models;

/// <summary>
/// Débat : regroupement d'articles traitant du même sujet avec des positions
/// opposées (pour / contre / nuancé) — « mise en parallèle d'articles
/// contradictoires pour illustrer un débat/propos » (spécification originale).
/// </summary>
public class Debate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Sujet du débat, normalisé (minuscules, sans accents, tirets)</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Sujet affichable (première formulation rencontrée)</summary>
    public string DisplayTopic { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<DebateArticle> Articles { get; set; } = new List<DebateArticle>();
}

/// <summary>Lien entre un débat et un article, avec la position de l'article</summary>
public class DebateArticle
{
    public Guid DebateId { get; set; }
    public Guid ArticleId { get; set; }

    /// <summary>Position de l'article : pour / contre / nuance / neutre</summary>
    public string Stance { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Debate Debate { get; set; } = null!;
    public virtual Article Article { get; set; } = null!;
}
