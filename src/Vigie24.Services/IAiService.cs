using System.Text.Json.Serialization;

namespace Vigie24.Services.Interfaces;

/// <summary>
/// Résultat du traitement d'un article par l'IA
/// Les [JsonPropertyName] matchent le format JSON renvoyé par les modèles
/// (snake_case), System.Text.Json étant sensible à la casse par défaut.
/// </summary>
public class AiProcessingResult
{
    /// <summary>Résumé de l'article en français</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>Score éditorial (0-100)</summary>
    [JsonPropertyName("editorial_score")]
    public decimal EditorialScore { get; set; }
    
    /// <summary>Liste des thèmes/catégories détectés</summary>
    [JsonPropertyName("themes")]
    public List<string> DetectedThemes { get; set; } = new();
    
    /// <summary>
    /// Hiérarchie de catégories détectée : [catégorie parente, sous-catégorie].
    /// Ex: ["IA", "Modèles LLM"]
    /// </summary>
    [JsonPropertyName("categories")]
    public List<string> DetectedCategories { get; set; } = new();
    
    /// <summary>Indique si cet article est un doublon d'un article existant</summary>
    [JsonPropertyName("is_duplicate")]
    public bool IsDuplicate { get; set; }
    
    /// <summary>ID de l'article doublon (si applicable)</summary>
    public Guid? DuplicateOf { get; set; }
    
    /// <summary>Liste des mots-clés extraits</summary>
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    /// <summary>Tokens d'entrée consommés par l'API (usage.prompt_tokens) — rempli par le service IA</summary>
    public int PromptTokens { get; set; }

    /// <summary>Tokens de sortie consommés par l'API (usage.completion_tokens) — rempli par le service IA</summary>
    public int CompletionTokens { get; set; }

    /// <summary>Tokens totaux consommés par l'API (usage.total_tokens)</summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
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
    /// Modèle LLM utilisé (nom + version, ex: "Qwen/Qwen3.5-122B-A10B-FP8")
    /// </summary>
    string ModelName { get; }
    
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
