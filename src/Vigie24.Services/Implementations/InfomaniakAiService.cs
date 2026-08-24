using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vigie24.Domain;
using Vigie24.Services.Interfaces;

namespace Vigie24.Services.Implementations;

/// <summary>
/// AI service for the Infomaniak API
/// Excellent value for money, specialised in code and technical content
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
        
        // IMPORTANT: BaseAddress must end with "/" so that relative requests
        // ("chat/completions") combine while preserving the full URL path.
        _httpClient.BaseAddress = new Uri(settings.ApiUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    }

    /// <summary>
    /// POST /chat/completions with retry on rate-limit (429):
    /// 3 attempts max, exponential backoff (5s then 10s).
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
  ""anxiety_score"": 0-10 (potentiel anxiogène du sujet pour un lecteur de veille tech : 0 = neutre/rassurant, 10 = alarmiste/effrayant. Exemples : faille zero-day massive = 8-10 ; crise géopolitique = 6-8 ; lancement produit = 1-2 ; tutoriel = 0-1),
  ""categories"": [""catégorie parente"", ""sous-catégorie""],
  ""themes"": [""liste"", ""de"", ""themes""],
  ""keywords"": [""liste"", ""de"", ""mots-clés""],
  ""topic"": ""sujet précis du débat en 2-5 mots neutres sans opinion (ex: régulation IA Europe) — OU vide si l'article ne participe à aucun débat"",
  ""stance"": ""position de l'article sur ce sujet : pour / contre / nuance / neutre — OU vide si pas de débat"",
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

        // Token usage reported by the API (usage) — for per-article cost tracking
        if (apiResponse?.Usage is { } usage)
        {
            result.PromptTokens = usage.PromptTokens;
            result.CompletionTokens = usage.CompletionTokens;
        }
        
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
            // GET /models: validates the API key and endpoint WITHOUT consuming tokens
            // (unlike a chat/completions call, even with max_tokens=1).
            var response = await _httpClient.GetAsync("models", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Infomaniak AI");
            return false;
        }
    }

    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = _settings.EmbeddingModel,
                input = new[] { Truncate(text, 8000) }
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("embeddings", httpContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Embedding request failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseContent);
            if (doc.RootElement.TryGetProperty("data", out var data)
                && data.GetArrayLength() > 0
                && data[0].TryGetProperty("embedding", out var embedding))
            {
                return embedding.EnumerateArray().Select(e => e.GetSingle()).ToArray();
            }

            _logger.LogWarning("Embedding response without data");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding request failed");
            return null;
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
    /// Extracts the JSON from the model response.
    /// Reasoning models (Qwen3.x) put the reasoning in
    /// <c>reasoning</c> and the final JSON in <c>content</c> — but if the
    /// generation is truncated (max_tokens reached), content can be empty:
    /// we then fall back to the JSON present at the end of the reasoning.
    /// </summary>
    private static string? ExtractJsonFromMessage(Message? message)
    {
        var text = message?.Content;
        if (string.IsNullOrWhiteSpace(text))
            text = message?.Reasoning;

        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Take from the first '{' to the last '}' (the JSON is often surrounded by text)
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

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    /// <summary>Token usage reported by the OpenAI-compatible API</summary>
    private class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
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

        /// <summary>Model reasoning (reasoning models like Qwen3)</summary>
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
