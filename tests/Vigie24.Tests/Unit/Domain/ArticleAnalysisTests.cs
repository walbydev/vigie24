using System.Text.Json;
using Vigie24.Domain.Models;
using Xunit;

namespace Vigie24.Tests.Unit.Domain;

/// <summary>
/// Contract tests for the AI analysis trace (articles."AnalysisJson", jsonb column).
/// The column stores camelCase JSON; the [NotMapped] Analysis property must
/// round-trip without throwing — even on corrupted data.
/// </summary>
public class ArticleAnalysisTests
{
    [Fact]
    public void SetAnalysis_ShouldSerializeCamelCaseJson()
    {
        var article = new Article();

        article.Analysis = new ArticleAnalysis
        {
            Provider = "Infomaniak",
            Model = "google/gemma-4-31B-it",
            AnalyzedAtUtc = new DateTime(2026, 8, 26, 9, 0, 0, DateTimeKind.Utc),
            Justification = "Raison de la catégorie.",
            Reasoning = "Raisonnement du modèle.",
            DetectedCategories = new List<string> { "IA", "Modèles LLM" },
            DetectedThemes = new List<string> { "LLM" },
            Keywords = new List<string> { "llm" }
        };

        Assert.NotNull(article.AnalysisJson);
        // camelCase property names + raw (unescaped) French text in the column
        using var doc = JsonDocument.Parse(article.AnalysisJson);
        var root = doc.RootElement;
        Assert.Equal("Infomaniak", root.GetProperty("provider").GetString());
        Assert.Equal("google/gemma-4-31B-it", root.GetProperty("model").GetString());
        Assert.Equal("Raison de la catégorie.", root.GetProperty("justification").GetString());
        Assert.Equal("Raisonnement du modèle.", root.GetProperty("reasoning").GetString());
        Assert.Equal(
            new[] { "IA", "Modèles LLM" },
            root.GetProperty("detectedCategories").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(
            new[] { "LLM" },
            root.GetProperty("detectedThemes").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(
            new[] { "llm" },
            root.GetProperty("keywords").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void GetAnalysis_ShouldRoundTripAllFields()
    {
        var article = new Article
        {
            Analysis = new ArticleAnalysis
            {
                Provider = "DeepSeek",
                Model = "deepseek-v4-flash",
                AnalyzedAtUtc = new DateTime(2026, 8, 26, 10, 30, 0, DateTimeKind.Utc),
                Justification = "Justification.",
                Reasoning = null,
                DetectedCategories = new List<string> { "Développement", "Backend" },
                DetectedThemes = new List<string> { ".NET", "API" },
                Keywords = new List<string> { "aspnet", "efcore" },
                AlternativeCategories = new List<string[]> { new[] { "Développement", "Frontend" } }
            }
        };

        var analysis = article.Analysis;

        Assert.NotNull(analysis);
        Assert.Equal("DeepSeek", analysis!.Provider);
        Assert.Equal("deepseek-v4-flash", analysis.Model);
        Assert.Equal(new DateTime(2026, 8, 26, 10, 30, 0, DateTimeKind.Utc), analysis.AnalyzedAtUtc);
        Assert.Equal("Justification.", analysis.Justification);
        Assert.Null(analysis.Reasoning);
        Assert.Equal(new[] { "Développement", "Backend" }, analysis.DetectedCategories);
        Assert.Equal(new[] { ".NET", "API" }, analysis.DetectedThemes);
        Assert.Equal(new[] { "aspnet", "efcore" }, analysis.Keywords);
        Assert.Single(analysis.AlternativeCategories);
        Assert.Equal(new[] { "Développement", "Frontend" }, analysis.AlternativeCategories[0]);
    }

    [Fact]
    public void SetAnalysisNull_ShouldClearJson()
    {
        var article = new Article
        {
            Analysis = new ArticleAnalysis { Provider = "Infomaniak" }
        };
        Assert.NotNull(article.AnalysisJson);

        article.Analysis = null;

        Assert.Null(article.AnalysisJson);
        Assert.Null(article.Analysis);
    }

    [Fact]
    public void GetAnalysis_CorruptedJson_ShouldReturnNullWithoutThrowing()
    {
        var article = new Article { AnalysisJson = "{ this is not valid json" };

        Assert.Null(article.Analysis);
    }

    [Fact]
    public void GetAnalysis_ValidJson_ShouldDeserializeWithoutJsonException()
    {
        // Extra unknown fields must be ignored (forward compatibility)
        var article = new Article
        {
            AnalysisJson = """{"provider":"Infomaniak","futureField":42}"""
        };

        var analysis = article.Analysis;

        Assert.NotNull(analysis);
        Assert.Equal("Infomaniak", analysis!.Provider);
    }
}
