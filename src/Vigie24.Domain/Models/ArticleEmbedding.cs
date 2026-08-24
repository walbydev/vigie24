namespace Vigie24.Domain.Models;

/// <summary>
/// Semantic vector of an article (multilingual bge-multilingual-gemma2 embedding,
/// 3584 dimensions) — used to match articles covering the same subject
/// with different topic phrasings.
/// Stored in a dedicated table to keep site queries light.
/// </summary>
public class ArticleEmbedding
{
    public Guid ArticleId { get; set; }

    /// <summary>Semantic vector (real[3584])</summary>
    public float[] Vector { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Article Article { get; set; } = null!;
}
