using Microsoft.Extensions.Logging;
using Vigie24.Services.Interfaces;

namespace Vigie24.Services.Implementations;

/// <summary>
/// Mock AI service used for local development when no API key is configured.
/// Returns deterministic, lightweight responses so the worker can process feeds without external calls.
/// </summary>
public class MockAiService : IAiService
{
    private readonly ILogger<MockAiService> _logger;

    public MockAiService(ILogger<MockAiService> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "Mock";

    public Task<AiProcessingResult> ProcessArticleAsync(string title, string content, string sourceUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock processing article: {Title}", title);

        var summary = string.IsNullOrWhiteSpace(content)
            ? "(Pas de contenu disponible)"
            : (content.Length > 200 ? content.Substring(0, 200) + "..." : content);

        var result = new AiProcessingResult
        {
            Summary = summary,
            EditorialScore = 50m,
            DetectedThemes = new List<string> { "général" },
            IsDuplicate = false
        };

        return Task.FromResult(result);
    }

    public Task<bool> IsDuplicateAsync(string article1Title, string article1Content, string article2Title, string article2Content, CancellationToken cancellationToken = default)
    {
        // Simple heuristic mock: never mark as duplicate in dev
        _logger.LogDebug("Mock duplicate check: {A} vs {B}", article1Title, article2Title);
        return Task.FromResult(false);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
