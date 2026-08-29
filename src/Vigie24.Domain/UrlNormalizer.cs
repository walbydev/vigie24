namespace Vigie24.Domain;

/// <summary>
/// URL normalization helpers.
/// </summary>
public static class UrlNormalizer
{
    /// <summary>
    /// Percent-encodes non-ASCII characters (RFC 3987 → RFC 3986) so a URL can be
    /// used in an HTTP header (Location), stored cleanly, or embedded in feeds.
    /// Reddit RSS feeds ship post slugs with raw accents (…/progrès_à_la_salle/…);
    /// Kestrel rejects such characters in headers, which broke the /track redirect
    /// (HTTP 500). ASCII-only URLs are returned untouched.
    /// </summary>
    public static string? ToAscii(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        if (url.All(c => c < 128)) return url;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsoluteUri : url;
    }
}
