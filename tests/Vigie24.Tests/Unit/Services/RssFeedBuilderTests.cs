namespace Vigie24.Tests.Unit.Services;

using System.Xml.Linq;
using Vigie24.Domain;

/// <summary>Tests unitaires du générateur de flux RSS 2.0.</summary>
public class RssFeedBuilderTests
{
    [Fact]
    public void Build_WithItems_ProducesWellFormedRss()
    {
        var items = new[]
        {
            new RssItem("Premier article", "https://vigie24.info/1", "Résumé 1",
                        new DateTime(2026, 8, 22, 10, 30, 0, DateTimeKind.Utc), "https://vigie24.info/1"),
        };

        var xml = RssFeedBuilder.Build("Vigie24", "https://vigie24.info", "Veille", items);
        var doc = XDocument.Parse(xml);

        Assert.Equal("rss", doc.Root!.Name.LocalName);
        Assert.Equal("2.0", doc.Root.Attribute("version")!.Value);
        Assert.Equal("Vigie24", doc.Root.Element("channel")!.Element("title")!.Value);
        Assert.Equal("Premier article", doc.Root.Element("channel")!.Element("item")!.Element("title")!.Value);
    }

    [Fact]
    public void Build_EscapesSpecialCharactersInTitleAndDescription()
    {
        var items = new[]
        {
            new RssItem("A & B <C> \"D\" 'E'", "https://x.test/a&b", "Résumé <b>gras</b> & plus", DateTime.UtcNow, "g1"),
        };

        var xml = RssFeedBuilder.Build("T & T", "https://x.test", "Desc & co", items);
        var doc = XDocument.Parse(xml);

        // Après parsing, les valeurs originales sont restituées (échappement correct).
        Assert.Equal("A & B <C> \"D\" 'E'", doc.Root!.Element("channel")!.Element("item")!.Element("title")!.Value);
        Assert.Equal("Résumé <b>gras</b> & plus", doc.Root.Element("channel")!.Element("item")!.Element("description")!.Value);
    }

    [Fact]
    public void Build_EmptyItems_ProducesValidChannel()
    {
        var xml = RssFeedBuilder.Build("Vigie24", "https://vigie24.info", "Veille", Array.Empty<RssItem>());
        var doc = XDocument.Parse(xml);

        Assert.Empty(doc.Root!.Element("channel")!.Elements("item"));
        Assert.Equal("fr", doc.Root.Element("channel")!.Element("language")!.Value);
    }

    [Fact]
    public void Build_PubDate_IsRfc1123WithGmt()
    {
        var items = new[]
        {
            new RssItem("Titre", "https://x.test/1", "Desc",
                        new DateTime(2026, 8, 22, 10, 30, 0, DateTimeKind.Utc), "g1"),
        };

        var xml = RssFeedBuilder.Build("Vigie24", "https://vigie24.info", "Veille", items);
        var doc = XDocument.Parse(xml);

        Assert.Equal("Sat, 22 Aug 2026 10:30:00 GMT",
                     doc.Root!.Element("channel")!.Element("item")!.Element("pubDate")!.Value);
    }

    [Fact]
    public void Build_Declaration_DeclaresUtf8()
    {
        var xml = RssFeedBuilder.Build("Vigie24", "https://vigie24.info", "Veille", Array.Empty<RssItem>());

        // La déclaration doit être utf-8, cohérente avec le charset servi par HTTP.
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);
    }
}
