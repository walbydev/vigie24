namespace Vigie24.Tests.Unit.Domain;

using Vigie24.Domain;

public class CountryInfoFlagUrlTests
{
    [Theory]
    [InlineData("FR", "/img/flags/fr.svg")]
    [InlineData("ch", "/img/flags/ch.svg")]
    [InlineData("BE", "/img/flags/be.svg")]
    public void FlagUrl_ReturnsLocalSvgPath(string code, string expected)
        => Assert.Equal(expected, CountryInfo.FlagUrl(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("FRA")]
    public void FlagUrl_UnknownCode_ReturnsNull(string? code)
        => Assert.Null(CountryInfo.FlagUrl(code));

    [Fact]
    public void Flag_StillReturnsEmojiForTextContexts()
        => Assert.Equal("🇫🇷", CountryInfo.Flag("FR"));
}
