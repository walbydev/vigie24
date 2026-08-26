namespace Vigie24.Domain.Models;

/// <summary>
/// Trace of the AI analysis that produced an article's categorization and score.
/// Stored as JSON (jsonb) in articles."AnalysisJson" — keeps the raw model output
/// (detected categories/themes/keywords, justification, reasoning) that the derived
/// columns (CategoryId, tags, EditorialScore…) no longer carry. The provider/model
/// are recorded because the LLM in use can change over time (A/B tests, provider switch).
/// </summary>
public class ArticleAnalysis
{
    /// <summary>AI provider that performed the analysis (Infomaniak, DeepSeek, Mock…)</summary>
    public string? Provider { get; set; }

    /// <summary>Exact LLM model used for the analysis (e.g. "google/gemma-4-31B-it")</summary>
    public string? Model { get; set; }

    /// <summary>UTC timestamp of the analysis</summary>
    public DateTime? AnalyzedAtUtc { get; set; }

    /// <summary>Model justification (2-3 sentences): why this category and this editorial score</summary>
    public string? Justification { get; set; }

    /// <summary>Raw reasoning text emitted by reasoning models (Qwen3…), when available</summary>
    public string? Reasoning { get; set; }

    /// <summary>[parent, subcategory] pair exactly as returned by the model</summary>
    public List<string> DetectedCategories { get; set; } = new();

    /// <summary>Themes exactly as returned by the model (also persisted as tags)</summary>
    public List<string> DetectedThemes { get; set; } = new();

    /// <summary>Keywords exactly as returned by the model (not persisted anywhere else)</summary>
    public List<string> Keywords { get; set; } = new();
}
