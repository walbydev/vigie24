using System.Text;
using System.Xml;

namespace Vigie24.Domain;

/// <summary>Un article de flux RSS (les champs sont échappés XML à la sérialisation).</summary>
public sealed record RssItem(string Title, string Link, string Description, DateTime PubDateUtc, string Guid);

/// <summary>
/// Construit un flux RSS 2.0 bien formé. L'échappement XML est géré par XmlWriter,
/// ce qui garantit l'absence d'injection de balises dans les titres/descriptions.
/// </summary>
public static class RssFeedBuilder
{
    /// <summary>
    /// Construit le document RSS 2.0 complet.
    /// </summary>
    /// <param name="channelTitle">Titre du flux (ex: « Vigie24 — Intelligence artificielle »).</param>
    /// <param name="channelLink">URL du site (ex: https://vigie24.info).</param>
    /// <param name="channelDescription">Description du flux.</param>
    /// <param name="items">Articles du flux (dans l'ordre voulu, généralement du plus récent au plus ancien).</param>
    public static string Build(
        string channelTitle,
        string channelLink,
        string channelDescription,
        IEnumerable<RssItem> items)
    {
        var sw = new StringWriter();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = false,
        };

        using (var xml = XmlWriter.Create(sw, settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("rss");
            xml.WriteAttributeString("version", "2.0");
            xml.WriteAttributeString("xmlns", "atom", null, "http://www.w3.org/2005/Atom");

            xml.WriteStartElement("channel");
            xml.WriteElementString("title", channelTitle);
            xml.WriteElementString("link", channelLink);
            xml.WriteElementString("description", channelDescription);
            xml.WriteElementString("language", "fr");

            // Lien atom:link pour que les lecteurs détectent le flux même sans découverte.
            xml.WriteStartElement("atom", "link", null);
            xml.WriteAttributeString("href", channelLink);
            xml.WriteAttributeString("rel", "self");
            xml.WriteAttributeString("type", "application/rss+xml");
            xml.WriteEndElement();

            foreach (var item in items)
            {
                xml.WriteStartElement("item");
                xml.WriteElementString("title", item.Title);
                xml.WriteElementString("link", item.Link);
                xml.WriteElementString("description", item.Description);
                xml.WriteElementString("pubDate", item.PubDateUtc.ToString("R"));
                xml.WriteElementString("guid", item.Guid);
                xml.WriteEndElement();
            }

            xml.WriteEndElement(); // channel
            xml.WriteEndElement(); // rss
            xml.WriteEndDocument();
        }

        return sw.ToString();
    }
}
