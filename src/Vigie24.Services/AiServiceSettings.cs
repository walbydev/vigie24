namespace Vigie24.Services.Interfaces;

/// <summary>
/// Configuration du service IA
/// </summary>
public class AiServiceSettings
{
    /// <summary>Fournisseur IA à utiliser : "infomaniak" ou "deepseek"</summary>
    public string Provider { get; set; } = "infomaniak";
    
    /// <summary>URL de l'API IA</summary>
    public string ApiUrl { get; set; } = "https://api.infomaniak.com";
    
    /// <summary>Clé API</summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>Modèle à utiliser (ex: "gpt-4o-mini", "deepseek-v3", "llama-3.1-70b")</summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>Timeout en secondes pour les appels API</summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>Température pour la génération (0 = précis, 1 = créatif)</summary>
    public decimal Temperature { get; set; } = 0.3m;
    
    /// <summary>Nombre maximum de tokens dans la réponse</summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>Prix d'entrée en CHF par million de tokens (pour l'estimation du coût quotidien)</summary>
    public decimal InputPricePerMillionTokens { get; set; } = 0.20m;

    /// <summary>Prix de sortie en CHF par million de tokens (pour l'estimation du coût quotidien)</summary>
    public decimal OutputPricePerMillionTokens { get; set; } = 0.40m;

    /// <summary>Modèle d'embedding pour le rapprochement sémantique (débats)</summary>
    public string EmbeddingModel { get; set; } = "bge_multilingual_gemma2";
}
