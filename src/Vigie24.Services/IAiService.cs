namespace Vigie24.Services.Interfaces;

/// <summary>
/// Résultat du traitement d'un article par l'IA
/// </summary>
public class AiProcessingResult
{
    /// <summary>Résumé de l'article en français</summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>Score éditorial (0-100)</summary>
    public decimal EditorialScore { get; set; }
    
    /// <summary>Liste des thèmes/catégories détectés</summary>
    public List<string> DetectedThemes { get; set; } = new();
    
    /// <summary>Indique si cet article est un doublon d'un article existant</summary>
    public bool IsDuplicate { get; set; }
    
    /// <summary>ID de l'article doublon (si applicable)</summary>
    public Guid? DuplicateOf { get; set; }
    
    /// <summary>Liste des mots-clés extraits</summary>
    public List<string> Keywords { get; set; } = new();
}

/// <summary>
/// Interface pour les services d'IA externes
/// Permet de basculer facilement entre Infomaniak, DeepSeek, ou autres fournisseurs
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Nom du fournisseur IA (pour logging et monitoring)
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Traite un article : génère un résumé, attribue un score, détecte les thèmes
    /// </summary>
    /// <param name="title">Titre de l'article</param>
    /// <param name="content">Contenu de l'article (HTML ou texte brut)</param>
    /// <param name="sourceUrl">URL de la source originale</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Résultat du traitement IA</returns>
    Task<AiProcessingResult> ProcessArticleAsync(
        string title,
        string content,
        string sourceUrl,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Détecte si deux articles traitent du même sujet
    /// </summary>
    /// <param name="article1Title">Titre du premier article</param>
    /// <param name="article1Content">Contenu du premier article</param>
    /// <param name="article2Title">Titre du deuxième article</param>
    /// <param name="article2Content">Contenu du deuxième article</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>True si les articles sont des doublons, false sinon</returns>
    Task<bool> IsDuplicateAsync(
        string article1Title,
        string article1Content,
        string article2Title,
        string article2Content,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Vérifie si le service est disponible (health check)
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
