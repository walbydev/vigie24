using System.Text;
using System.Xml;

namespace Vigie24.Domain;

/// <summary>An RSS feed item (fields are XML-escaped at serialization).</summary>
public sealed record RssItem(string Title, string Link, string Description, DateTime PubDateUtc, string Guid);

/// <summary>
/// Builds a well-formed RSS 2.0 feed. XML escaping is handled by XmlWriter,
/// which guarantees no tag injection in titles/descriptions.
/// </summary>
public static class RssFeedBuilder
{
    /// <summary>
    /// Builds the complete RSS 2.0 document.
    /// </summary>
    /// <param name="channelTitle">Feed title (ex: « Vigie24 — Intelligence artificielle »).</param>
    /// <param name="channelLink">Site URL (ex: https://vigie24.info).</param>
    /// <param name="channelDescription">Feed description.</param>
    /// <param name="items">Feed items (in the desired order, usually newest to oldest).</param>
    public static string Build(
        string channelTitle,
        string channelLink,
        string channelDescription,
        IEnumerable<RssItem> items)
    {
        // MemoryStream (not StringWriter) so the XML declaration is UTF-8,
        // consistent with the charset served over HTTP (a strict parser would otherwise reject it).
        var ms = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = false,
        };

        using (var xml = XmlWriter.Create(ms, settings))
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

            // atom:link so feed readers can discover the feed even without auto-discovery.
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

        return new UTF8Encoding(false).GetString(ms.ToArray());
    }
}
