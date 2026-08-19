using Microsoft.Extensions.Logging;
using Vigie24.Services.Interfaces;

namespace Vigie24.Services;

/// <summary>
/// Service IA factice pour éviter les erreurs de démarrage
/// </summary>
public class MockAiService : IAiService
{
    private readonly ILogger<MockAiService> _logger;
    private readonly AiServiceSettings _settings;

    public MockAiService(AiServiceSettings settings, ILoggerFactory loggerFactory)
    {
        _settings = settings;
        _logger = loggerFactory.CreateLogger<MockAiService>();
    }

    public string ProviderName => "Mock";

    public Task<AiProcessingResult> ProcessArticleAsync(string title, string content, string sourceUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MockAiService used - no actual AI analysis performed");
        return Task.FromResult(new AiProcessingResult
        {
            Summary = "[Analyse IA désactivée - Mock mode]",
            EditorialScore = 0,
            DetectedThemes = new List<string>(),
            IsDuplicate = false,
            Keywords = new List<string>()
        });
    }

    public Task<bool> IsDuplicateAsync(string article1Title, string article1Content, string article2Title, string article2Content, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
