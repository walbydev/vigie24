using System.Globalization;
using System.Text;

namespace Vigie24.Domain;

/// <summary>
/// Vigie24 category taxonomy: parent categories + subcategories.
/// Shared between the Worker (seed + AI mapping) and the Web (displaying names).
/// </summary>
public static class VigieTaxonomy
{
    public sealed record CategoryDef(string Name, string Slug, string? ParentSlug);

    public static readonly CategoryDef[] Categories =
    {
        // AI
        new("IA", "ia", null),
        new("Modèles LLM", "ia-modeles-llm", "ia"),
        new("Agents IA", "ia-agents", "ia"),
        new("Vision & Génératif", "ia-vision-generatif", "ia"),
        new("Outils & Applications", "ia-outils-applications", "ia"),
        // Development
        new("Développement", "developpement", null),
        new("Web", "dev-web", "developpement"),
        new("Backend", "dev-backend", "developpement"),
        new("Mobile", "dev-mobile", "developpement"),
        new("DevOps & Cloud", "dev-devops-cloud", "developpement"),
        new("Open Source", "dev-open-source", "developpement"),
        new("Vulgarisation technique", "dev-vulgarisation-technique", "developpement"),
        // Cybersecurity
        new("Cybersécurité", "cybersecurite", null),
        new("Vulnérabilités", "cyber-vulnerabilites", "cybersecurite"),
        new("Menaces & Acteurs", "cyber-menaces-acteurs", "cybersecurite"),
        new("Réglementation", "cyber-reglementation", "cybersecurite"),
        // Science
        new("Science", "science", null),
        new("Recherche", "science-recherche", "science"),
        new("Espace", "science-espace", "science"),
        new("Santé & Biotech", "science-sante-biotech", "science"),
        new("Vulgarisation scientifique", "science-vulgarisation", "science"),
        // Hardware
        new("Hardware", "hardware", null),
        new("Composants", "hardware-composants", "hardware"),
        new("Électronique", "hardware-electronique", "hardware"),
        new("Mobilité", "hardware-mobilite", "hardware"),
        // Entertainment
        new("Divertissements", "divertissements", null),
        new("Gaming", "divertissements-gaming", "divertissements")
    };

    /// <summary>Slugify: lowercase, no diacritics, hyphens (ex: "Vision & Generative" → "vision-generative")</summary>
    public static string Slugify(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue; // strips diacritics
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (sb.Length > 0 && sb[^1] != '-')
                sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }

    /// <summary>Display name of a category (from the taxonomy, otherwise humanized slug)</summary>
    public static string DisplayName(string slug)
    {
        var def = Categories.FirstOrDefault(c => c.Slug == slug);
        if (def != null) return def.Name;

        var words = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));
    }

    /// <summary>
    /// Resolves the official slug from a free-form name returned by the AI
    /// (ex: "electronique" → "hardware-electronique", "DevOps & Cloud" → "dev-devops-cloud").
    /// Returns null if no taxonomy name matches.
    /// </summary>
    public static string? ResolveSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var normalized = Slugify(name);
        var def = Categories.FirstOrDefault(c => Slugify(c.Name) == normalized);
        return def?.Slug;
    }

    /// <summary>List "(Parent (sub-cat, ...), ...)" injected into the AI prompt</summary>
    public static string PromptList()
    {
        var parents = Categories.Where(c => c.ParentSlug is null).ToList();
        var parts = parents.Select(p =>
        {
            var subs = Categories.Where(c => c.ParentSlug == p.Slug).Select(s => s.Name).ToList();
            return subs.Count > 0 ? $"{p.Name} ({string.Join(", ", subs)})" : p.Name;
        });
        return string.Join(", ", parts);
    }
}
