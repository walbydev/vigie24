using System.Text.Json.Serialization;

namespace Vigie24.Services.Interfaces;

/// <summary>
/// Result of AI processing of an article
/// The [JsonPropertyName] attributes match the JSON format returned by the models
/// (snake_case), since System.Text.Json is case-sensitive by default.
/// </summary>
public class AiProcessingResult
{
    /// <summary>Article summary in French</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>Editorial score (0-100)</summary>
    [JsonPropertyName("editorial_score")]
    public decimal EditorialScore { get; set; }
    
    /// <summary>List of detected themes/categories</summary>
    [JsonPropertyName("themes")]
    public List<string> DetectedThemes { get; set; } = new();
    
    /// <summary>
    /// Detected category hierarchy: [parent category, sub-category].
    /// Ex: ["IA", "LLM Models"]
    /// </summary>
    [JsonPropertyName("categories")]
    public List<string> DetectedCategories { get; set; } = new();
    
    /// <summary>Indicates whether this article is a duplicate of an existing article</summary>
    [JsonPropertyName("is_duplicate")]
    public bool IsDuplicate { get; set; }
    
    /// <summary>ID of the duplicated article (if applicable)</summary>
    [JsonPropertyName("duplicate_of")]
    public Guid? DuplicateOf { get; set; }
    
    /// <summary>List of extracted keywords</summary>
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    /// <summary>Input tokens consumed by the API (usage.prompt_tokens) — filled in by the AI service</summary>
    public int PromptTokens { get; set; }

    /// <summary>Output tokens consumed by the API (usage.completion_tokens) — filled in by the AI service</summary>
    public int CompletionTokens { get; set; }

    /// <summary>Total tokens consumed by the API (usage.total_tokens)</summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Precise debate topic (2-5 words, neutral, no opinion), e.g. "AI regulation in Europe".
    /// Empty if the article does not take part in any debate.
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// Article stance on the topic: "pour", "contre", "nuance", "neutre" (for/against/nuanced/neutral) or "" (not a debate).
    /// </summary>
    [JsonPropertyName("stance")]
    public string? Stance { get; set; }

    /// <summary>
    /// Anxiety score 0-10 (0 = not anxiety-inducing, 10 = very anxiety-inducing).
    /// Subjective, editorial: measures how much the topic is likely to worry a
    /// French tech-watch reader (threats, crises, alarmism...).
    /// </summary>
    [JsonPropertyName("anxiety_score")]
    public int AnxietyScore { get; set; }
}

/// <summary>
/// Interface for external AI services
/// Makes it easy to switch between Infomaniak, DeepSeek, or other providers
/// </summary>
public interface IAiService
{
    /// <summary>
    /// AI provider name (for logging and monitoring)
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// LLM model in use (name + version, e.g. "Qwen/Qwen3.5-122B-A10B-FP8")
    /// </summary>
    string ModelName { get; }
    
    /// <summary>
    /// Processes an article: generates a summary, assigns a score, detects themes
    /// </summary>
    /// <param name="title">Article title</param>
    /// <param name="content">Article content (HTML or plain text)</param>
    /// <param name="sourceUrl">URL of the original source</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI processing result</returns>
    Task<AiProcessingResult> ProcessArticleAsync(
        string title,
        string content,
        string sourceUrl,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Detects whether two articles cover the same topic
    /// </summary>
    /// <param name="article1Title">Title of the first article</param>
    /// <param name="article1Content">Content of the first article</param>
    /// <param name="article2Title">Title of the second article</param>
    /// <param name="article2Content">Content of the second article</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the articles are duplicates, false otherwise</returns>
    Task<bool> IsDuplicateAsync(
        string article1Title,
        string article1Content,
        string article2Title,
        string article2Content,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks whether the service is available (health check)
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the semantic vector of a text (multilingual embedding) —
    /// used to match articles covering the same debate topic.
    /// Returns null if the provider does not offer embeddings.
    /// </summary>
    Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
