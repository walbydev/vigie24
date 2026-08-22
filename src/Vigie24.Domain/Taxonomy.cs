using System.Globalization;
using System.Text;

namespace Vigie24.Domain;

/// <summary>
/// Taxonomie de catégories Vigie24 : catégories parentes + sous-catégories.
/// Partagée entre le Worker (seed + mapping IA) et le Web (affichage des noms).
/// </summary>
public static class VigieTaxonomy
{
    public sealed record CategoryDef(string Name, string Slug, string? ParentSlug);

    public static readonly CategoryDef[] Categories =
    {
        // IA
        new("IA", "ia", null),
        new("Modèles LLM", "ia-modeles-llm", "ia"),
        new("Agents IA", "ia-agents", "ia"),
        new("Vision & Génératif", "ia-vision-generatif", "ia"),
        new("Outils & Applications", "ia-outils-applications", "ia"),
        // Développement
        new("Développement", "developpement", null),
        new("Web", "dev-web", "developpement"),
        new("Backend", "dev-backend", "developpement"),
        new("Mobile", "dev-mobile", "developpement"),
        new("DevOps & Cloud", "dev-devops-cloud", "developpement"),
        new("Open Source", "dev-open-source", "developpement"),
        // Cybersécurité
        new("Cybersécurité", "cybersecurite", null),
        new("Vulnérabilités", "cyber-vulnerabilites", "cybersecurite"),
        new("Menaces & Acteurs", "cyber-menaces-acteurs", "cybersecurite"),
        new("Réglementation", "cyber-reglementation", "cybersecurite"),
        // Science
        new("Science", "science", null),
        new("Recherche", "science-recherche", "science"),
        new("Espace", "science-espace", "science"),
        new("Santé & Biotech", "science-sante-biotech", "science"),
        // Hardware
        new("Hardware", "hardware", null),
        new("Composants", "hardware-composants", "hardware"),
        new("Électronique", "hardware-electronique", "hardware"),
        new("Mobilité", "hardware-mobilite", "hardware")
    };

    /// <summary>Slugify : minuscules, sans accents, tirets (ex: "Vision & Génératif" → "vision-generatif")</summary>
    public static string Slugify(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue; // supprime les accents
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (sb.Length > 0 && sb[^1] != '-')
                sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }

    /// <summary>Nom d'affichage d'une catégorie (depuis la taxonomie, sinon slug humanisé)</summary>
    public static string DisplayName(string slug)
    {
        var def = Categories.FirstOrDefault(c => c.Slug == slug);
        if (def != null) return def.Name;

        var words = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));
    }

    /// <summary>
    /// Résout le slug officiel depuis un nom libre renvoyé par l'IA
    /// (ex: "Electronique" → "hardware-electronique", "DevOps & Cloud" → "dev-devops-cloud").
    /// Retourne null si aucun nom de la taxonomie ne correspond.
    /// </summary>
    public static string? ResolveSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var normalized = Slugify(name);
        var def = Categories.FirstOrDefault(c => Slugify(c.Name) == normalized);
        return def?.Slug;
    }

    /// <summary>Liste "(Parent (sous-cat, ...), ...)" injectée dans le prompt IA</summary>
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
