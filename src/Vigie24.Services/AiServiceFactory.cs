using Microsoft.Extensions.Logging;
using Vigie24.Services.Implementations;
using Vigie24.Services.Interfaces;

namespace Vigie24.Services;

/// <summary>
/// Factory for creating the appropriate AI service based on the configuration
/// Makes it easy to switch between Infomaniak and DeepSeek
/// </summary>
public static class AiServiceFactory
{
    /// <summary>
    /// Creates the configured AI service
    /// </summary>
    public static IAiService Create(AiServiceSettings settings, ILoggerFactory loggerFactory)
    {
        // If no API key is configured, use a local mock service for development to avoid external calls
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            // Use the Bogus-based MockAiService implementation
            return new MockAiService(loggerFactory.CreateLogger<MockAiService>());
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
