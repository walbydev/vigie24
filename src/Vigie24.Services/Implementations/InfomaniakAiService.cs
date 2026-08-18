using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vigie24.Services.Interfaces;

namespace Vigie24.Services.Implementations;

/// <summary>
/// Service IA pour Infomaniak API
/// Excellent rapport qualité/prix, spécialisé dans le code et la technique
/// </summary>
public class InfomaniakAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly AiServiceSettings _settings;
    private readonly ILogger<InfomaniakAiService> _logger;

    public string ProviderName => "Infomaniak";

    public InfomaniakAiService(
        HttpClient httpClient,
        AiServiceSettings settings,
        ILogger<InfomaniakAiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(settings.ApiUrl);
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    }

    public async Task<AiProcessingResult> ProcessArticleAsync(
        string title,
        string content,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing article via Infomaniak: {Title}", title);

        var prompt = $@"
Tu es un assistant expert en veille technologique francophone. Ta tâche est d'analyser un article et de produire un résumé concis en français.

ARTICLE À ANALYSER:
Titre: {title}
Source: {sourceUrl}
Contenu: {Truncate(content, 8000)}

Fournis ta réponse au format JSON suivant (sans markdown, juste le JSON brut):
{{
  ""summary"": ""Résumé concis en français (2-3 phrases)"",
  ""editorial_score"": 0-100 (note de pertinence éditoriale),
  ""themes"": [""liste"", ""de"", ""themes""],
  ""keywords"": [""liste"", ""de"", ""mots-clés""],
  ""is_duplicate"": false
}}

IMPORTANT: Ne retourne QUE le JSON, sans texte supplémentaire.";

        var request = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = "Tu es un assistant expert en veille technologique francophone. Réponds uniquement en JSON valide." },
                new { role = "user", content = prompt }
            },
            temperature = (double)_settings.Temperature,
            max_tokens = _settings.MaxTokens
        };

        var jsonRequest = JsonSerializer.Serialize(request);
        var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/chat/completions", httpContent, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Infomaniak API error: {StatusCode} - {Reason}", 
                response.StatusCode, response.ReasonPhrase);
            throw new HttpRequestException($"Infomaniak API error: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<InfomaniakApiResponse>(responseContent);
        
        if (apiResponse?.Choices?.FirstOrDefault()?.Message?.Content is not string contentJson)
        {
            throw new InvalidOperationException("Invalid response format from Infomaniak API");
        }

        contentJson = CleanJson(contentJson);
        
        var result = JsonSerializer.Deserialize<AiProcessingResult>(contentJson) 
            ?? new AiProcessingResult();
        
        result.EditorialScore = Math.Min(result.EditorialScore, 100);
        
        _logger.LogInformation("Article processed successfully via Infomaniak. Score: {Score}, Themes: {Themes}",
            result.EditorialScore, string.Join(", ", result.DetectedThemes));
        
        return result;
    }

    public async Task<bool> IsDuplicateAsync(
        string article1Title,
        string article1Content,
        string article2Title,
        string article2Content,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking for duplicates via Infomaniak: {Title1} vs {Title2}", 
            article1Title, article2Title);

        var prompt = $@"
Détermine si les deux articles suivants traitent du même sujet.

ARTICLE 1:
Titre: {article1Title}
Contenu: {Truncate(article1Content, 4000)}

ARTICLE 2:
Titre: {article2Title}
Contenu: {Truncate(article2Content, 4000)}

Réponds UNIQUEMENT par JSON: {{ ""is_duplicate"": true/false, ""confidence"": 0-1 }}";

        var request = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            max_tokens = 50
        };

        var jsonRequest = JsonSerializer.Serialize(request);
        var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/chat/completions", httpContent, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Duplicate check failed: {StatusCode}", response.StatusCode);
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<InfomaniakApiResponse>(responseContent);
        
        if (apiResponse?.Choices?.FirstOrDefault()?.Message?.Content is string contentJson)
        {
            contentJson = CleanJson(contentJson);
            var duplicateCheck = JsonSerializer.Deserialize<DuplicateCheckResult>(contentJson);
            return duplicateCheck?.IsDuplicate ?? false;
        }

        return false;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = _settings.Model,
                messages = new[] { new { role = "user", content = "ok" } },
                max_tokens = 1
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/chat/completions", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Infomaniak AI");
            return false;
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        
        return value[..maxLength] + "...";
    }

    private static string CleanJson(string json)
    {
        json = json.Trim();
        if (json.StartsWith("```json"))
            json = json[7..];
        if (json.StartsWith("```"))
            json = json[3..];
        if (json.EndsWith("```"))
            json = json[..^3];
        
        return json.Trim();
    }

    private class InfomaniakApiResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }

    private class DuplicateCheckResult
    {
        public bool IsDuplicate { get; set; }
        public decimal Confidence { get; set; }
    }
}
