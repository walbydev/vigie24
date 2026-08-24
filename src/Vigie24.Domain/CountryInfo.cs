namespace Vigie24.Domain;

/// <summary>
/// Country names in French + flag (emoji) from an ISO 3166-1 alpha-2 code
/// (Source.CountryCode field). Unknown codes display 🌐 + the raw code.
/// </summary>
public static class CountryInfo
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FR"] = "France", ["CH"] = "Suisse", ["BE"] = "Belgique", ["LU"] = "Luxembourg",
        ["MC"] = "Monaco", ["CA"] = "Canada", ["US"] = "États-Unis", ["GB"] = "Royaume-Uni",
        ["DE"] = "Allemagne", ["AT"] = "Autriche", ["ES"] = "Espagne", ["IT"] = "Italie",
        ["PT"] = "Portugal", ["NL"] = "Pays-Bas", ["IE"] = "Irlande", ["SE"] = "Suède",
        ["NO"] = "Norvège", ["DK"] = "Danemark", ["FI"] = "Finlande", ["PL"] = "Pologne",
        ["CZ"] = "Tchéquie", ["RO"] = "Roumanie", ["RU"] = "Russie", ["UA"] = "Ukraine",
        ["TR"] = "Turquie", ["IL"] = "Israël", ["AE"] = "Émirats arabes unis", ["SA"] = "Arabie saoudite",
        ["QA"] = "Qatar", ["JP"] = "Japon", ["CN"] = "Chine", ["KR"] = "Corée du Sud",
        ["TW"] = "Taïwan", ["HK"] = "Hong Kong", ["SG"] = "Singapour", ["IN"] = "Inde",
        ["ID"] = "Indonésie", ["TH"] = "Thaïlande", ["VN"] = "Vietnam", ["AU"] = "Australie",
        ["NZ"] = "Nouvelle-Zélande", ["BR"] = "Brésil", ["MX"] = "Mexique", ["AR"] = "Argentine",
        ["MA"] = "Maroc", ["DZ"] = "Algérie", ["TN"] = "Tunisie", ["SN"] = "Sénégal",
        ["CI"] = "Côte d'Ivoire", ["CM"] = "Cameroun", ["ML"] = "Mali", ["BF"] = "Burkina Faso",
        ["MG"] = "Madagascar", ["CD"] = "RD Congo", ["EG"] = "Égypte", ["ZA"] = "Afrique du Sud",
        ["KE"] = "Kenya", ["NG"] = "Nigéria", ["GH"] = "Ghana"
    };

    /// <summary>Emoji flag from the ISO code (regional indicators), 🌐 if unknown</summary>
    public static string Flag(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
            return "🌐";
        var code = countryCode.ToUpperInvariant();
        var sb = new System.Text.StringBuilder(4);
        foreach (var c in code)
            sb.Append(char.ConvertFromUtf32(0x1F1E6 + (c - 'A')));
        return sb.ToString();
    }

    /// <summary>French country name, otherwise the raw code</summary>
    public static string Name(string? countryCode)
        => countryCode != null && Names.TryGetValue(countryCode, out var name) ? name : countryCode ?? "";

    /// <summary>"🇫🇷 France" — empty if no code</summary>
    public static string Display(string? countryCode)
        => string.IsNullOrWhiteSpace(countryCode) ? "" : $"{Flag(countryCode)} {Name(countryCode)}";
}
