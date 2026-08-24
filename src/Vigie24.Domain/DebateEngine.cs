using System.Text;
using Vigie24.Domain.Models;

namespace Vigie24.Domain;

/// <summary>
/// Groups articles into "debates": articles covering the same subject
/// (normalized topic) with opposing stances.
/// Rules:
///  - A debate exists if ≥ 2 articles share the same normalized topic
///    AND at least one "for" AND one "against" stance are present
///    ("nuanced" and "neutral" join the debate without triggering it).
///  - Topics are normalized (lowercase, no diacritics, canonical keywords)
///    to bring similar formulations together ("AI Regulation in Europe" ≈
///    "European AI regulation").
/// </summary>
public static class DebateEngine
{
    /// <summary>A stance that alone does not trigger a debate</summary>
    public const string StanceFor = "pour";
    public const string StanceAgainst = "contre";
    public const string StanceNuanced = "nuance";
    public const string StanceNeutral = "neutre";

    /// <summary>
    /// Groups articles into debates. Input: articles with non-empty Topic + Stance.
    /// Output: dictionary normalizedTopic → (DisplayTopic, list of (articleId, stance)).
    /// </summary>
    public static Dictionary<string, DebateGroup> Build(IEnumerable<Article> articles)
    {
        var groups = new Dictionary<string, DebateGroup>();

        foreach (var article in articles)
        {
            var topic = NormalizeTopic(article.Topic);
            if (string.IsNullOrEmpty(topic))
                continue;

            var stance = NormalizeStance(article.Stance);
            if (string.IsNullOrEmpty(stance))
                continue;

            if (!groups.TryGetValue(topic, out var group))
            {
                group = new DebateGroup(topic, article.Topic!.Trim());
                groups[topic] = group;
            }

            group.Entries.Add(new DebateEntry(article.Id, stance));
        }

        // Keep only real debates: at least one for + one against stance
        return groups
            .Where(kv => HasOpposingStances(kv.Value.Entries))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>True if the group contains at least one "for" and one "against"</summary>
    public static bool HasOpposingStances(IEnumerable<DebateEntry> entries)
    {
        var stances = entries.Select(e => e.Stance).ToHashSet();
        return stances.Contains(StanceFor) && stances.Contains(StanceAgainst);
    }

    /// <summary>
    /// Normalizes a topic for grouping: lowercase, no diacritics,
    /// no punctuation, most significant keywords only (≤ 4 words).
    /// Ex: "AI Regulation in Europe" → "ai regulation europe"
    /// Explicit accent table: robust even without ICU (globalization-invariant mode).
    /// </summary>
    public static string NormalizeTopic(string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return string.Empty;

        var sb = new StringBuilder(topic.Length);
        foreach (var c in topic.ToLowerInvariant())
        {
            if (AccentMap.TryGetValue(c, out var replacement))
            {
                sb.Append(replacement);
            }
            else if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(' ');
            }
        }

        var words = sb.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 1)            // skips "de", "la", "un", etc. (2-letter words, covered by StopWords)
            .Where(w => !StopWords.Contains(w))
            .Take(4)
            .ToList();

        return string.Join(' ', words);
    }

    private static readonly Dictionary<char, string> AccentMap = new()
    {
        ['à'] = "a", ['â'] = "a", ['ä'] = "a", ['á'] = "a", ['ã'] = "a", ['å'] = "a",
        ['ç'] = "c",
        ['é'] = "e", ['è'] = "e", ['ê'] = "e", ['ë'] = "e",
        ['î'] = "i", ['ï'] = "i", ['í'] = "i", ['ì'] = "i",
        ['ô'] = "o", ['ö'] = "o", ['ó'] = "o", ['ò'] = "o", ['õ'] = "o",
        ['ù'] = "u", ['û'] = "u", ['ü'] = "u", ['ú'] = "u",
        ['ÿ'] = "y", ['ñ'] = "n",
        ['œ'] = "oe", ['æ'] = "ae"
    };

    /// <summary>Normalizes a stance: accepts pour/contre/nuance/neutre (and variants)</summary>
    public static string NormalizeStance(string? stance)
    {
        if (string.IsNullOrWhiteSpace(stance))
            return string.Empty;

        var s = stance.Trim().ToLowerInvariant();
        return s switch
        {
            "pour" or "favorable" or "soutien" or "pro" or "pour " => StanceFor,
            "contre" or "defavorable" or "opposition" or "anti" => StanceAgainst,
            "nuance" or "nuançé" or "nuançe" or "mitigé" or "mitige" or "prudent" or "reserve" or "nuancé" => StanceNuanced,
            "neutre" or "neutral" or "objectif" or "information" => StanceNeutral,
            _ => s.StartsWith("pour") ? StanceFor
                : s.StartsWith("contre") ? StanceAgainst
                : s.StartsWith("nuanc") ? StanceNuanced
                : string.Empty
        };
    }

    private static readonly HashSet<string> StopWords = new()
    {
        "dans", "avec", "pour", "contre", "cette", "cet", "ces", "son", "ses", "sur",
        "tout", "tous", "plus", "moins", "entre", "vers", "chez", "ainsi", "aussi",
        "encore", "alors", "donc", "mais", "ou", "et", "de", "des", "du", "une",
        "dun", "la", "le", "les", "un", "au", "aux", "en", "est", "sont", "fait",
        "faire", "être", "etre", "the", "of", "to", "and", "in", "for", "new", "as"
    };

    public sealed record DebateEntry(Guid ArticleId, string Stance);

    public sealed class DebateGroup(string Topic, string DisplayTopic)
    {
        public string Topic { get; } = Topic;
        public string DisplayTopic { get; } = DisplayTopic;
        public List<DebateEntry> Entries { get; } = new();
    }
}
