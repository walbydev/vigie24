using System.Text.Json;
using Vigie24.Services.Interfaces;
using Xunit;

namespace Vigie24.Tests.Unit.Services;

/// <summary>
/// Contract tests for the AI result JSON (snake_case). They use the same
/// deserialization the providers perform (System.Text.Json, default options,
/// case-sensitive — which is why [JsonPropertyName] attributes exist).
/// </summary>
public class AiProcessingResultSerializationTests
{
    [Fact]
    public void Deserialize_ShouldMapAllSnakeCaseFields()
    {
        const string json = """
        {
          "summary": "Résumé de l'article en français.",
          "editorial_score": 78.5,
          "themes": ["LLM", "Open Source"],
          "categories": ["IA", "Modèles LLM"],
          "is_duplicate": false,
          "duplicate_of": null,
          "keywords": ["llm", "open source"],
          "justification": "Cet article relève de l'IA : il traite des modèles de langage, sujet majeur de la veille.",
          "topic": "Réglementation de l'IA en Europe",
          "stance": "nuance",
          "anxiety_score": 4
        }
        """;

        var result = JsonSerializer.Deserialize<AiProcessingResult>(json)!;

        Assert.Equal("Résumé de l'article en français.", result.Summary);
        Assert.Equal(78.5m, result.EditorialScore);
        Assert.Equal(new[] { "LLM", "Open Source" }, result.DetectedThemes);
        Assert.Equal(new[] { "IA", "Modèles LLM" }, result.DetectedCategories);
        Assert.False(result.IsDuplicate);
        Assert.Null(result.DuplicateOf);
        Assert.Equal(new[] { "llm", "open source" }, result.Keywords);
        Assert.Equal("Cet article relève de l'IA : il traite des modèles de langage, sujet majeur de la veille.", result.Justification);
        Assert.Equal("Réglementation de l'IA en Europe", result.Topic);
        Assert.Equal("nuance", result.Stance);
        Assert.Equal(4, result.AnxietyScore);
    }

    [Fact]
    public void Deserialize_MissingFields_ShouldUseDefaults()
    {
        const string json = """{ "summary": "Seul résumé fourni" }""";

        var result = JsonSerializer.Deserialize<AiProcessingResult>(json)!;

        Assert.Equal("Seul résumé fourni", result.Summary);
        Assert.Equal(0m, result.EditorialScore);
        Assert.Empty(result.DetectedThemes);
        Assert.Empty(result.DetectedCategories);
        Assert.False(result.IsDuplicate);
        Assert.Empty(result.Keywords);
        Assert.Null(result.Justification);
        Assert.Null(result.Topic);
        Assert.Null(result.Stance);
        Assert.Equal(0, result.AnxietyScore);
    }

    [Fact]
    public void Deserialize_OutOfRangeAnxiety_ShouldStillParse()
    {
        // The model sometimes returns values outside the 0-10 contract.
        // Parsing must succeed; the range guard lives in the Worker
        // (out-of-range values are stored as null).
        const string json = """{ "anxiety_score": 12 }""";

        var result = JsonSerializer.Deserialize<AiProcessingResult>(json)!;

        Assert.Equal(12, result.AnxietyScore);
    }

    [Fact]
    public void Deserialize_DuplicateResult_ShouldMapDuplicateOf()
    {
        const string json = """
        { "is_duplicate": true, "duplicate_of": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
        """;

        var result = JsonSerializer.Deserialize<AiProcessingResult>(json)!;

        Assert.True(result.IsDuplicate);
        Assert.Equal(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), result.DuplicateOf);
    }

    [Fact]
    public void Serialize_ShouldEmitSnakeCaseFieldNames()
    {
        var result = new AiProcessingResult
        {
            Summary = "Résumé",
            EditorialScore = 66m,
            DetectedThemes = new List<string> { "IA" },
            AnxietyScore = 3,
            Justification = "Justification courte.",
        };

        var json = JsonSerializer.Serialize(result);

        Assert.Contains("\"summary\":", json);
        Assert.Contains("\"editorial_score\":", json);
        Assert.Contains("\"anxiety_score\":", json);
        Assert.Contains("\"is_duplicate\":", json);
        Assert.Contains("\"justification\":", json);
    }
}
