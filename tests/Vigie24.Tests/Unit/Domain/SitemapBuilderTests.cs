using System.Xml.Linq;
using Vigie24.Domain;

namespace Vigie24.Tests.Unit.Domain;

/// <summary>
/// Tests du SitemapBuilder : couverture des slugs (taxonomie + pages statiques),
/// exclusion of non-indexable URLs, priorities and lastmod.
/// </summary>
public class SitemapBuilderTests
{
    private const string BaseUrl = "https://vigie24.info";

    private static string BuildWithAllTaxonomy(DateTime? lastMod = null) =>
        SitemapBuilder.Build(
            BaseUrl,
            VigieTaxonomy.Categories.Select(c => c.Slug),
            new[] { "ia-agents", "devops" },
            lastMod);

    [Fact]
    public void Build_ShouldIncludeAllStaticPages()
    {
        var xml = BuildWithAllTaxonomy();
        var doc = XDocument.Parse(xml);

        var locs = doc.Descendants().Where(e => e.Name.LocalName == "loc").Select(e => e.Value).ToList();

        foreach (var (path, _) in SitemapBuilder.StaticUrls)
            Assert.Contains(BaseUrl + path, locs);
    }

    [Fact]
    public void Build_ShouldCoverEveryCategoryOfVigieTaxonomy()
    {
        var xml = BuildWithAllTaxonomy();
        var doc = XDocument.Parse(xml);

        var locs = doc.Descendants().Where(e => e.Name.LocalName == "loc").Select(e => e.Value).ToList();

        foreach (var def in VigieTaxonomy.Categories)
            Assert.Contains($"{BaseUrl}/categories/{def.Slug}", locs);
    }

    [Fact]
    public void Build_ShouldIncludeThemeUrls()
    {
        var xml = BuildWithAllTaxonomy();
        var doc = XDocument.Parse(xml);

        var locs = doc.Descendants().Where(e => e.Name.LocalName == "loc").Select(e => e.Value).ToList();

        Assert.Contains($"{BaseUrl}/themes/ia-agents", locs);
        Assert.Contains($"{BaseUrl}/themes/devops", locs);
    }

    [Fact]
    public void Build_ShouldExcludeArticleRedirectsAndFeed()
    {
        var xml = BuildWithAllTaxonomy();

        Assert.DoesNotContain("/articles/", xml);
        Assert.DoesNotContain("/feed", xml);
    }

    [Fact]
    public void Build_ShouldSetHomepagePriorityTo1_0AndCategoriesTo0_8()
    {
        var xml = BuildWithAllTaxonomy();
        var doc = XDocument.Parse(xml);

        var url = doc.Descendants().First(e => e.Name.LocalName == "url"
            && e.Descendants().Any(l => l.Name.LocalName == "loc" && l.Value == $"{BaseUrl}/"));
        var priority = url.Descendants().First(e => e.Name.LocalName == "priority").Value;
        Assert.Equal("1.0", priority);

        var categoryUrl = doc.Descendants().First(e => e.Name.LocalName == "url"
            && e.Descendants().Any(l => l.Name.LocalName == "loc" && l.Value == $"{BaseUrl}/categories/ia"));
        Assert.Equal("0.8", categoryUrl.Descendants().First(e => e.Name.LocalName == "priority").Value);
    }

    [Fact]
    public void Build_ShouldUseInvariantDecimalSeparator()
    {
        // La culture fr-FR utiliserait « 0,8 » — le sitemap doit rester « 0.8 »
        var xml = BuildWithAllTaxonomy();
        Assert.Contains("<priority>0.8</priority>", xml);
    }

    [Fact]
    public void Build_ShouldIncludeLastModOnlyWhenProvided()
    {
        var withLastMod = BuildWithAllTaxonomy(new DateTime(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc));
        Assert.Contains("<lastmod>2026-08-23</lastmod>", withLastMod);

        var withoutLastMod = BuildWithAllTaxonomy();
        Assert.DoesNotContain("lastmod", withoutLastMod);
    }
}
