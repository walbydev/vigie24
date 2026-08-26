using Microsoft.Extensions.Logging.Abstractions;
using Vigie24.Services.Implementations;
using Xunit;

namespace Vigie24.Tests.Unit.Services;

/// <summary>
/// Mock AI service contract: it must fill the justification field so the
/// « Réflexion & analyse » block renders in Development/Mock environments.
/// </summary>
public class MockAiServiceTests
{
    [Fact]
    public void ProcessArticle_ShouldReturnNonEmptyJustification()
    {
        var service = new MockAiService(NullLogger<MockAiService>.Instance);

        var result = service.ProcessArticleAsync("Titre test", "Contenu test", "https://example.com").GetAwaiter().GetResult();

        Assert.False(string.IsNullOrWhiteSpace(result.Justification));
        Assert.Contains("classé", result.Justification);
    }

    [Fact]
    public void ProcessArticle_ShouldReportMockProviderAndModel()
    {
        var service = new MockAiService(NullLogger<MockAiService>.Instance);

        var result = service.ProcessArticleAsync("Titre test", "Contenu test", "https://example.com").GetAwaiter().GetResult();

        Assert.Equal("Mock", service.ProviderName);
        Assert.False(string.IsNullOrWhiteSpace(service.ModelName));
        Assert.NotEmpty(result.DetectedCategories);
        Assert.NotEmpty(result.DetectedThemes);
    }
}
