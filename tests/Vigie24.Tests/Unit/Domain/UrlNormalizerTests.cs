namespace Vigie24.Tests.Unit.Domain;

using Vigie24.Domain;

/// <summary>
/// Unit tests for UrlNormalizer (non-ASCII URLs → header-safe ASCII).
/// </summary>
public class UrlNormalizerTests
{
    [Theory]
    [InlineData("https://www.reddit.com/r/AskFrance/comments/1w1fk2y/comment_suivre_ses_progrès_à_la_salle_de_sport/",
                "https://www.reddit.com/r/AskFrance/comments/1w1fk2y/comment_suivre_ses_progr%C3%A8s_%C3%A0_la_salle_de_sport/")]
    [InlineData("https://example.com/article?q=héllo", "https://example.com/article?q=h%C3%A9llo")]
    public void ToAscii_PercentEncodesNonAscii(string input, string expected)
        => Assert.Equal(expected, UrlNormalizer.ToAscii(input));

    [Theory]
    [InlineData("https://www.reddit.com/r/france/comments/1w0vaiz/elon_musks_xai_used_child_porn_to_train_grok/")]
    [InlineData("https://example.com/a%20b?x=1&y=2")]
    [InlineData("https://vigie24.info/articles")]
    public void ToAscii_LeavesAsciiUrlsUntouched(string input)
        => Assert.Equal(input, UrlNormalizer.ToAscii(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToAscii_EmptyOrNull_ReturnsAsIs(string? input)
        => Assert.Equal(input, UrlNormalizer.ToAscii(input));

    [Fact]
    public void ToAscii_DoesNotDoubleEncodeExistingEscapes()
    {
        const string url = "https://example.com/d%C3%A9j%C3%A0-encod%C3%A9";

        Assert.Equal(url, UrlNormalizer.ToAscii(url));
    }
}
