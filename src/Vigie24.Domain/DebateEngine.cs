using System.Text;
using Vigie24.Domain.Models;

namespace Vigie24.Domain;

/// <summary>
/// Regroupement des articles en « débats » : articles traitant du même sujet
/// (topic normalisé) avec des positions opposées.
/// Règles :
///  - Un débat existe si ≥ 2 articles partagent le même topic normalisé
///    ET qu'au moins une position « pour » ET une position « contre » sont présentes
///    (les « nuance » et « neutre » rejoignent le débat sans le déclencher).
///  - Les topics sont normalisés (minuscules, sans accents, mots-clés canoniques)
///    pour rapprocher les formulations proches (« Régulation IA en Europe » ≈
///    « régulation de l'IA européenne »).
/// </summary>
public static class DebateEngine
{
    /// <summary>Position qui ne déclenche pas un débat (seule)</summary>
    public const string StanceFor = "pour";
    public const string StanceAgainst = "contre";
    public const string StanceNuanced = "nuance";
    public const string StanceNeutral = "neutre";

    /// <summary>
    /// Regroupe les articles en débats. Entrée : articles avec Topic + Stance non vides.
    /// Sortie : dictionnaire topicNormalisé → (DisplayTopic, liste (articleId, stance)).
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

        // Ne conserver que les débats réels : une position pour + une position contre
        return groups
            .Where(kv => HasOpposingStances(kv.Value.Entries))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>Vrai si le groupe contient au moins un « pour » et un « contre »</summary>
    public static bool HasOpposingStances(IEnumerable<DebateEntry> entries)
    {
        var stances = entries.Select(e => e.Stance).ToHashSet();
        return stances.Contains(StanceFor) && stances.Contains(StanceAgainst);
    }

    /// <summary>
    /// Normalise un topic pour le regroupement : minuscules, sans accents,
    /// sans ponctuation, mots-clés les plus porteurs uniquement (≤ 4 mots).
    /// Ex: "Régulation de l'IA en Europe" → "regulation ia europe"
    /// Table d'accents explicite : robuste même sans ICU (mode globalization-invariant).
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
            .Where(w => w.Length > 1)            // ignore "de", "la", "un"… (2 lettres, couverts par StopWords)
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

    /// <summary>Normalise une position : accepte pour/contre/nuance/neutre (et variantes)</summary>
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
