namespace Vigie24.Domain.Models;

/// <summary>
/// Vecteur sémantique d'un article (embedding multilingue bge-multilingual-gemma2,
/// 3584 dimensions) — utilisé pour rapprocher des articles traitant du même sujet
/// avec des formulations de topic différentes.
/// Stocké dans une table dédiée pour ne pas alourdir les requêtes du site.
/// </summary>
public class ArticleEmbedding
{
    public Guid ArticleId { get; set; }

    /// <summary>Vecteur sémantique (real[3584])</summary>
    public float[] Vector { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Article Article { get; set; } = null!;
}
