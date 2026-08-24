namespace Vigie24.Services.Interfaces;

/// <summary>
/// AI service configuration
/// </summary>
public class AiServiceSettings
{
    /// <summary>AI provider to use: "infomaniak" or "deepseek"</summary>
    public string Provider { get; set; } = "infomaniak";
    
    /// <summary>AI API URL</summary>
    public string ApiUrl { get; set; } = "https://api.infomaniak.com";
    
    /// <summary>API key</summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>Model to use (e.g. "gpt-4o-mini", "deepseek-v3", "llama-3.1-70b")</summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>Timeout in seconds for API calls</summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>Temperature for generation (0 = precise, 1 = creative)</summary>
    public decimal Temperature { get; set; } = 0.3m;
    
    /// <summary>Maximum number of tokens in the response</summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>Input price in CHF per million tokens (for daily cost estimation)</summary>
    public decimal InputPricePerMillionTokens { get; set; } = 0.20m;

    /// <summary>Output price in CHF per million tokens (for daily cost estimation)</summary>
    public decimal OutputPricePerMillionTokens { get; set; } = 0.40m;

    /// <summary>Embedding model for semantic matching (debates)</summary>
    public string EmbeddingModel { get; set; } = "bge_multilingual_gemma2";
}
