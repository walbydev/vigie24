using Bogus;
using Microsoft.Extensions.Logging;
using Vigie24.Services.Interfaces;

namespace Vigie24.Services.Implementations;

public class MockAiService : IAiService
{
    private readonly ILogger<MockAiService> _logger;
    private readonly Random _random;
    
    private static readonly string[] TechThemes = 
    {
        "Intelligence Artificielle", "Developpement Web", "Cybersecurite", 
        "Cloud Computing", "DevOps", "Machine Learning", "Blockchain",
        "IoT", "Data Science", "Open Source", "Hardware", "Mobile"
    };
    
    private static readonly string[] ActionVerbs = 
    {
        "Nouvelle avancee dans", "Decouverte majeure en", "Revolution dans",
        "Analyse approfondie de", "Guide complet sur", "Les secrets de",
        "Pourquoi", "Comment", "Lavenir de", "Le futur de",
        "Comprendre", "Maitriser", "Optimiser", "Securiser"
    };
    
    private static readonly string[] Adjectives = 
    {
        "revolutionnaire", "innovant", "surprenant", "decisif", "crucial",
        "essentiel", "inattendu", "prometteur", "controverse", "reveleur"
    };

    private static readonly (string Parent, string Sub)[] MockCategories =
    {
        ("IA", "Modèles LLM"), ("IA", "Agents IA"), ("IA", "Outils & Applications"),
        ("Développement", "Web"), ("Développement", "Backend"), ("Développement", "DevOps & Cloud"),
        ("Cybersécurité", "Vulnérabilités"), ("Cybersécurité", "Menaces & Acteurs"),
        ("Science", "Recherche"), ("Science", "Espace"),
        ("Hardware", "Composants"), ("Hardware", "Électronique")
    };

    public MockAiService(ILogger<MockAiService> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public string ProviderName => "Mock";

    public string ModelName => "Bogus (simulation locale)";

    public Task<AiProcessingResult> ProcessArticleAsync(string title, string content, string sourceUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock processing article: {Title}", title);

        var summary = GenerateMockSummary(title, content);
        var editorialScore = Math.Round((decimal)(_random.Next(40, 96)), 1);
        
        var themeCount = _random.Next(2, 5);
        var detectedThemes = new List<string>();
        for (int i = 0; i < themeCount; i++)
        {
            var theme = TechThemes[_random.Next(0, TechThemes.Length)];
            if (!detectedThemes.Contains(theme))
                detectedThemes.Add(theme);
        }

        var category = MockCategories[_random.Next(0, MockCategories.Length)];

        var result = new AiProcessingResult
        {
            Summary = summary,
            EditorialScore = editorialScore,
            DetectedThemes = detectedThemes,
            DetectedCategories = new List<string> { category.Parent, category.Sub },
            IsDuplicate = false,
            // Realistic simulation of token consumption (the Mock makes no API calls)
            PromptTokens = 1200 + _random.Next(0, 600),
            CompletionTokens = 150 + _random.Next(0, 120),
            // The Mock sometimes simulates a debate topic with a stance
            Topic = _random.Next(4) == 0 ? "régulation IA Europe" : null,
            Stance = _random.Next(4) == 0 ? new[] { "pour", "contre", "nuance" }[_random.Next(3)] : null,
            // Deterministic justification so the « Réflexion & analyse » block renders in mock mode
            Justification = $"Cet article a été classé dans {category.Parent} / {category.Sub} : le sujet correspond aux critères éditoriaux de la veille, avec un score de pertinence de {editorialScore:0.0}/100."
        };

        _logger.LogDebug("Mock result: Score={Score}, Themes={Themes}", 
            editorialScore, string.Join(", ", detectedThemes));

        return Task.FromResult(result);
    }

    public Task<bool> IsDuplicateAsync(string article1Title, string article1Content, 
        string article2Title, string article2Content, CancellationToken cancellationToken = default)
    {
        var isDuplicate = _random.Next(0, 100) < 5;
        _logger.LogDebug("Mock duplicate check: {A} vs {B} => {Duplicate}", 
            article1Title, article2Title, isDuplicate);
        return Task.FromResult(isDuplicate);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <summary>Simulated embedding: deterministic vector (3584 dims) derived from the text hash,
    /// so that two identical texts produce the same vector in mock mode.</summary>
    public Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var seed = text.GetHashCode();
        var rng = new Random(seed);
        var vector = new float[3584];
        for (int i = 0; i < vector.Length; i++)
            vector[i] = (float)(rng.NextDouble() * 2 - 1);
        return Task.FromResult<float[]?>(vector);
    }
    
    private string GenerateMockSummary(string title, string originalContent)
    {
        if (!string.IsNullOrWhiteSpace(originalContent) && originalContent.Length > 100)
        {
            var truncated = originalContent.Length > 300 
                ? originalContent.Substring(0, 300) + "..." 
                : originalContent;
            return truncated;
        }
        
        var faker = new Faker("fr");
        var intro = faker.Lorem.Sentence(_random.Next(8, 15));
        var body = faker.Lorem.Paragraph(_random.Next(3, 6));
        
        var conclusions = new[]
        {
            "Cette evolution pourrait transformer le secteur.",
            "Les experts restent prudents mais optimistes.",
            "Une avancee qui merite une attention particuliere.",
            "Les implications sont potentiellement majeures.",
            "Cette technologie est appelee a se generaliser rapidement.",
            "Limpact sur lindustrie est deja perceptible.",
            "Une tendance qui devrait saccentuer dans les mois a venir."
        };
        
        var conclusion = conclusions[_random.Next(0, conclusions.Length)];
        
        return $"{intro} {body} {conclusion}";
    }
}
