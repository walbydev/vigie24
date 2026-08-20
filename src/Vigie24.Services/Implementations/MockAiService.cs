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

    public MockAiService(ILogger<MockAiService> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public string ProviderName => "Mock";

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

        var result = new AiProcessingResult
        {
            Summary = summary,
            EditorialScore = editorialScore,
            DetectedThemes = detectedThemes,
            IsDuplicate = false
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
