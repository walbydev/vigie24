using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vigie24.Domain;
using Vigie24.Services.Interfaces;

namespace Vigie24.Services.Implementations;

/// <summary>
/// AI service for the DeepSeek API
/// Excellent value for money, specialised in code and technical content
/// </summary>
public class DeepSeekAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly AiServiceSettings _settings;
    private readonly ILogger<DeepSeekAiService> _logger;

    public string ProviderName => "DeepSeek";

    public string ModelName => _settings.Model;

    public DeepSeekAiService(
        HttpClient httpClient,
        AiServiceSettings settings,
        ILogger<DeepSeekAiService> logger)
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

    public async Task<AiProcessingResult> ProcessArticleAsync(
        string title,
        string content,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing article via DeepSeek: {Title}", title);

        var prompt = $@"
Tu es un assistant expert en veille technologique francophone. Ta tâche est d'analyser un article et de produire un résumé concis en français.

ARTICLE À ANALYSER:
Titre: {title}
Source: {sourceUrl}
Contenu: {Truncate(content, 8000)}

RÈGLE COMMERCIALE (à appliquer avant toute autre considération) :
Un contenu à visée commerciale ne peut pas dépasser 45 de score éditorial, même s'il est long, bien écrit et documenté. Sont concernés : les guides d'achat, les comparatifs d'outils ou de services, les « avis » et « tests » d'un fournisseur ou d'une plateforme, les pages de présentation d'une offre, et tout texte dont l'objectif principal est de provoquer un clic affilié ou une inscription.
Indices à relever : liens d'affiliation, mentions de partenaire ou de code promo, appels à s'inscrire ou à ouvrir un compte, frais et tarifs comparés, structure « définition / avantages / inconvénients / conclusion », listes de courtiers, d'outils ou de plateformes.
Ne sont PAS concernés : un article d'actualité qui mentionne un produit, un tutoriel technique sans promotion, une analyse de marché sans lien commercial.
Si le score est plafonné pour cette raison, indique-le explicitement dans ""justification"".

Fournis ta réponse au format JSON suivant (sans markdown, juste le JSON brut):
{{
  ""summary"": ""Résumé concis en français (2-3 phrases)"",
  ""editorial_score"": 0-100 (note de pertinence éditoriale. Plafond 45 pour tout contenu à visée commerciale — voir RÈGLE COMMERCIALE),
  ""anxiety_score"": 0-10 (potentiel anxiogène du sujet pour un lecteur de veille tech : 0 = neutre/rassurant, 10 = alarmiste/effrayant. Exemples : faille zero-day massive = 8-10 ; crise géopolitique = 6-8 ; lancement produit = 1-2 ; tutoriel = 0-1),
  ""categories"": [""catégorie parente"", ""sous-catégorie""],
  ""alternative_categories"": [[""parent"", ""sous-catégorie""]] — 1-2 paires alternatives si hésitation entre plusieurs catégories, sinon [],
  ""themes"": [""liste"", ""de"", ""themes""],
  ""keywords"": [""liste"", ""de"", ""mots-clés""],
  ""justification"": ""Explication factuelle en 2-3 phrases : pourquoi cette catégorie et pourquoi ce score éditorial (arguments tirés de l'article)"",
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
        
        var response = await _httpClient.PostAsync("chat/completions", httpContent, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("DeepSeek API error: {StatusCode} - {Reason}", 
                response.StatusCode, response.ReasonPhrase);
            throw new HttpRequestException($"DeepSeek API error: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<DeepSeekApiResponse>(responseContent);
        
        if (apiResponse?.Choices?.FirstOrDefault()?.Message?.Content is not string contentJson)
        {
            throw new InvalidOperationException("Invalid response format from DeepSeek API");
        }

        contentJson = CleanJson(contentJson);
        
        var result = JsonSerializer.Deserialize<AiProcessingResult>(contentJson) 
            ?? new AiProcessingResult();
        
        result.EditorialScore = Math.Min(result.EditorialScore, 100);

        // Token usage reported by the API (usage) — for per-article cost tracking
        if (apiResponse?.Usage is { } usage)
        {
            result.PromptTokens = usage.PromptTokens;
            result.CompletionTokens = usage.CompletionTokens;
        }
        
        _logger.LogInformation("Article processed successfully via DeepSeek. Score: {Score}, Themes: {Themes}",
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
        _logger.LogInformation("Checking for duplicates via DeepSeek: {Title1} vs {Title2}", 
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
        
        var response = await _httpClient.PostAsync("chat/completions", httpContent, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Duplicate check failed: {StatusCode}", response.StatusCode);
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<DeepSeekApiResponse>(responseContent);
        
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
            _logger.LogError(ex, "Health check failed for DeepSeek AI");
            return false;
        }
    }

    /// <summary>The DeepSeek fallback does not provide embeddings — no semantic matching.</summary>
    public Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult<float[]?>(null);

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

    private class DeepSeekApiResponse
    {
        public List<Choice>? Choices { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    /// <summary>Token usage reported by the OpenAI-compatible API</summary>
    private class Usage
    {
        [System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
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
