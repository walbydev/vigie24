using System.Xml.Linq;

namespace Vigie24.Domain;

/// <summary>
/// Builds the sitemap.xml (sitemaps.org protocol) for Vigie24.
/// URLs are built from baseUrl; XML escaping is handled by XDocument.
/// Includes NEITHER /articles/{id} (301 redirect to the source) NOR /feed (feed, not a page).
/// </summary>
public static class SitemapBuilder
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

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
    /// Builds the complete sitemap document (XML declaration included, UTF-8).
    /// </summary>
    /// <param name="baseUrl">Site origin, without trailing slash (ex: https://vigie24.info).</param>
    /// <param name="categorySlugs">Slugs of active categories (priority 0.8).</param>
    /// <param name="themeSlugs">Slugs of themes linked to published articles (priority 0.5).</param>
    /// <param name="lastMod">Last modification date (optional, ISO yyyy-MM-dd format).</param>
    public static string Build(
        string baseUrl,
        IEnumerable<string> categorySlugs,
        IEnumerable<string> themeSlugs,
        DateTime? lastMod = null)
    {
        var urls = new List<XElement>();

        foreach (var (path, priority) in StaticUrls)
            urls.Add(UrlElement($"{baseUrl}{path}", priority, lastMod));

        foreach (var slug in categorySlugs)
            urls.Add(UrlElement($"{baseUrl}/categories/{slug}", 0.8, lastMod));

        foreach (var slug in themeSlugs)
            urls.Add(UrlElement($"{baseUrl}/themes/{slug}", 0.5, lastMod));

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
