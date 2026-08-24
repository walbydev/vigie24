using System.Xml.Linq;

namespace Vigie24.Domain;

/// <summary>
/// Builds the sitemap.xml (sitemaps.org protocol) for Vigie24.
/// URLs are built from baseUrl; XML escaping is handled by XDocument.
/// Includes the reading pages /articles/{id} (AI summary), but NEITHER /feed (feed, not a page)
/// NOR the /track redirect endpoint.
/// </summary>
public static class SitemapBuilder
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>Reading page entry for the sitemap (/articles/{id} — AI summary page).</summary>
    public sealed record SitemapArticle(Guid Id, DateTime PublishedAt);

    /// <summary>Indexable static pages: homepage priority 1.0, the rest 0.5.</summary>
    public static readonly (string Path, double Priority)[] StaticUrls =
    {
        ("/", 1.0),
        ("/categories", 0.5),
        ("/themes", 0.5),
        ("/sources", 0.5),
        ("/debats", 0.5),
        ("/projet", 0.5),
        ("/charte-graphique", 0.5),
        ("/couts", 0.5),
        ("/license", 0.5)
    };

    /// <summary>
    /// Construit le document sitemap complet (déclaration XML incluse, UTF-8).
    /// </summary>
    /// <param name="baseUrl">Origine du site, sans slash final (ex: https://vigie24.info).</param>
    /// <param name="categorySlugs">Slugs des catégories actives (priorité 0.8).</param>
    /// <param name="themeSlugs">Slugs des thèmes liés à des articles publiés (priorité 0.5).</param>
    /// <param name="lastMod">Date de dernière modification globale (facultatif, format ISO yyyy-MM-dd).</param>
    /// <param name="articles">Fiches de lecture /articles/{id} (priorité 0.5, lastmod = date de publication).</param>
    public static string Build(
        string baseUrl,
        IEnumerable<string> categorySlugs,
        IEnumerable<string> themeSlugs,
        DateTime? lastMod = null,
        IEnumerable<SitemapArticle>? articles = null)
    {
        var urls = new List<XElement>();

        foreach (var (path, priority) in StaticUrls)
            urls.Add(UrlElement($"{baseUrl}{path}", priority, lastMod));

        foreach (var slug in categorySlugs)
            urls.Add(UrlElement($"{baseUrl}/categories/{slug}", 0.8, lastMod));

        foreach (var slug in themeSlugs)
            urls.Add(UrlElement($"{baseUrl}/themes/{slug}", 0.5, lastMod));

        foreach (var article in articles ?? [])
            urls.Add(UrlElement($"{baseUrl}/articles/{article.Id}", 0.5, article.PublishedAt));

        var doc = new XDocument(new XElement(SitemapNs + "urlset", urls));
        return doc.ToString();
    }

    private static XElement UrlElement(string location, double priority, DateTime? lastMod)
    {
        var el = new XElement(SitemapNs + "url",
            new XElement(SitemapNs + "loc", location),
            new XElement(SitemapNs + "changefreq", "daily"),
            new XElement(SitemapNs + "priority",
                priority.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)));

        if (lastMod is { } lm)
            el.Add(new XElement(SitemapNs + "lastmod", lm.ToString("yyyy-MM-dd")));

        return el;
    }
}
