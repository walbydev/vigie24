namespace Vigie24.Web.Services;

/// <summary>
/// Fallback illustration palette shared by ArticleCard and ArticleThumb:
/// a colored gradient per category prefix + the title initial.
/// </summary>
public static class ArticlePalette
{
    private static readonly (string Prefix, string C1, string C2)[] _palettes =
    {
        ("ia", "#4f46e5", "#9333ea"), ("developpement", "#0891b2", "#2563eb"),
        ("cyber", "#dc2626", "#7c3aed"), ("science", "#059669", "#0d9488"),
        ("hardware", "#d97706", "#dc2626"), ("", "#475569", "#64748b")
    };

    /// <summary>Returns the gradient colors for a category slug (fallback: neutral).</summary>
    public static (string C1, string C2) ForCategory(string? categorySlug)
    {
        var slug = categorySlug ?? "";
        var palette = _palettes.FirstOrDefault(p => slug.StartsWith(p.Prefix));
        if (palette.Prefix.Length == 0)
            palette = _palettes[^1];
        return (palette.C1, palette.C2);
    }

    /// <summary>First letter of the title, uppercased (placeholder text).</summary>
    public static string Initial(string title)
        => title.Length > 0 ? title[..1].ToUpperInvariant() : "?";
}
