using Microsoft.Extensions.Logging;
using Vigie24.Services.Implementations;
using Vigie24.Services.Interfaces;

namespace Vigie24.Services;

/// <summary>
/// Factory pour créer le service IA approprié selon la configuration
/// Permet de basculer facilement entre Infomaniak et DeepSeek
/// </summary>
public static class AiServiceFactory
{
    /// <summary>
    /// Crée le service IA configuré
    /// </summary>
    public static IAiService Create(AiServiceSettings settings, ILoggerFactory loggerFactory)
    {
        // If no API key is configured, use a local mock service for development to avoid external calls
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            // Use existing MockAiService implementation that accepts settings + loggerFactory
            return new MockAiService(settings, loggerFactory);
        }

        var provider = settings.Provider.ToLowerInvariant();

        return provider switch
        {
            "infomaniak" => new InfomaniakAiService(
                new HttpClient(), 
                settings, 
                loggerFactory.CreateLogger<InfomaniakAiService>()),
            
            "deepseek" => new DeepSeekAiService(
                new HttpClient(), 
                settings, 
                loggerFactory.CreateLogger<DeepSeekAiService>()),
            
            _ => throw new InvalidOperationException(
                $"Unsupported AI provider: {settings.Provider}. Use 'infomaniak' or 'deepseek'.")
        };
    }
}
