using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vigie24.Domain;
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

    public string ModelName => _settings.Model;

    public InfomaniakAiService(
        HttpClient httpClient,
        AiServiceSettings settings,
        ILogger<InfomaniakAiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        
        // IMPORTANT: BaseAddress doit se terminer par "/" pour que les requêtes relatives
        // ("chat/completions") se combinent en préservant le chemin complet de l'URL.
        _httpClient.BaseAddress = new Uri(settings.ApiUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    }

    /// <summary>
    /// POST /chat/completions avec retry sur rate-limit (429) :
    /// 3 tentatives max, backoff exponentiel (5s puis 10s).
    /// </summary>
    private async Task<HttpResponseMessage> PostChatCompletionsAsync(
        string jsonRequest,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("chat/completions", httpContent, cancellationToken);

            if ((int)response.StatusCode == 429 && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(5 * attempt);
                _logger.LogWarning("Infomaniak API rate limited (429) — retry {Attempt}/{Max} dans {Delay}s",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
                response.Dispose();
                continue;
            }

            return response;
        }

        throw new InvalidOperationException("Unreachable code path");
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
  ""categories"": [""catégorie parente"", ""sous-catégorie""],
  ""themes"": [""liste"", ""de"", ""themes""],
  ""keywords"": [""liste"", ""de"", ""mots-clés""],
  ""is_duplicate"": false
}}

Catégories disponibles (choisis UNE paire [parent, sous-catégorie] dans cette liste, ou la plus proche si l'article ne correspond pas exactement) : {VigieTaxonomy.PromptList()}

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
        
        var response = await PostChatCompletionsAsync(jsonRequest, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Infomaniak API error: {StatusCode} - {Reason}", 
                response.StatusCode, response.ReasonPhrase);
            throw new HttpRequestException($"Infomaniak API error: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<InfomaniakApiResponse>(responseContent);

        var contentJson = ExtractJsonFromMessage(apiResponse?.Choices?.FirstOrDefault()?.Message);
        if (contentJson is null)
        {
            var dbgMsg = apiResponse?.Choices?.FirstOrDefault()?.Message;
            _logger.LogError("Invalid response format. RawContent=[{RawContent}] RawReasoning=[{RawReasoning}] FullResponse=[{Full}]",
                Truncate(dbgMsg?.Content ?? "(null)", 500),
                Truncate(dbgMsg?.Reasoning ?? "(null)", 500),
                Truncate(responseContent, 2500));
            throw new InvalidOperationException("Invalid response format from Infomaniak API");
        }
        
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
        
        var response = await PostChatCompletionsAsync(jsonRequest, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Duplicate check failed: {StatusCode}", response.StatusCode);
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<InfomaniakApiResponse>(responseContent);
        
        var contentJson = ExtractJsonFromMessage(apiResponse?.Choices?.FirstOrDefault()?.Message);
        if (contentJson is not null)
        {
            var duplicateCheck = JsonSerializer.Deserialize<DuplicateCheckResult>(contentJson);
            return duplicateCheck?.IsDuplicate ?? false;
        }

        return false;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            // GET /models : valide la clé API et l'endpoint SANS consommer de tokens
            // (contrairement à un chat/completions, même avec max_tokens=1).
            var response = await _httpClient.GetAsync("models", cts.Token);
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

    /// <summary>
    /// Extrait le JSON de la réponse du modèle.
    /// Les modèles de raisonnement (Qwen3.x) mettent le raisonnement dans
    /// <c>reasoning</c> et le JSON final dans <c>content</c> — mais si la
    /// génération est tronquée (max_tokens atteint), content peut être vide :
    /// on retombe alors sur le JSON présent à la fin du raisonnement.
    /// </summary>
    private static string? ExtractJsonFromMessage(Message? message)
    {
        var text = message?.Content;
        if (string.IsNullOrWhiteSpace(text))
            text = message?.Reasoning;

        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Prendre le premier '{' jusqu'au dernier '}' (le JSON est souvent précédé/suivi de texte)
        var firstBrace = text.IndexOf('{');
        var lastBrace = text.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
            text = text[firstBrace..(lastBrace + 1)];

        return CleanJson(text);
    }

    private class InfomaniakApiResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>Raisonnement du modèle (modèles de raisonnement type Qwen3)</summary>
        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; set; }
    }

    private class DuplicateCheckResult
    {
        [JsonPropertyName("is_duplicate")]
        public bool IsDuplicate { get; set; }

        public decimal Confidence { get; set; }
    }
}
