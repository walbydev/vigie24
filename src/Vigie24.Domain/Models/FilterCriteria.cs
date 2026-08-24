using System.Globalization;
using System.Text.Json;

namespace Vigie24.Domain.Models;

/// <summary>
/// Article filtering criteria (search, country, source kind, sources, score,
/// rating, date range, sort, pagination). Single source of truth shared between
/// the UI (ArticleBrowser), local persistence (JSON) and RSS feeds (query string).
/// No EF Core dependency: JSON / query string codecs are hand-written.
/// </summary>
public sealed record FilterCriteria
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    private static readonly HashSet<string> KnownSortFields =
        new(StringComparer.Ordinal) { "date", "dateAsc", "score", "rating", "clicks" };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string? SearchText { get; init; }
    public string? CountryCode { get; init; }
    public string? SourceKind { get; init; }
    public IReadOnlyList<string> SelectedSources { get; init; } = [];
    public int MinScore { get; init; }
    public int MinRating { get; init; }
    public DateTime? RangeFrom { get; init; }
    public DateTime? RangeTo { get; init; }
    public string SortField { get; init; } = "date";
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>True if no criteria are active (equivalent to the full default).</summary>
    public bool IsEmpty =>
        string.IsNullOrEmpty(SearchText)
        && string.IsNullOrEmpty(CountryCode)
        && string.IsNullOrEmpty(SourceKind)
        && SelectedSources.Count == 0
        && MinScore == 0
        && MinRating == 0
        && RangeFrom is null
        && RangeTo is null
        && string.Equals(SortField, "date", StringComparison.Ordinal)
        && PageSize == DefaultPageSize;

    /// <summary>
    /// Serializes the criteria to a query string (prefixed with '?'), omitting default values.
    /// Short, readable names: q, country, kind, sources (comma-separated),
    /// minScore, minRating, from, to, sort, pageSize.
    /// </summary>
    public string ToQueryString()
    {
        var parts = new List<string>(10);

        if (!string.IsNullOrEmpty(SearchText))
            parts.Add($"q={Uri.EscapeDataString(SearchText)}");
        if (!string.IsNullOrEmpty(CountryCode))
            parts.Add($"country={Uri.EscapeDataString(CountryCode)}");
        if (!string.IsNullOrEmpty(SourceKind))
            parts.Add($"kind={Uri.EscapeDataString(SourceKind)}");
        if (SelectedSources.Count > 0)
            parts.Add($"sources={Uri.EscapeDataString(string.Join(",", SelectedSources))}");
        if (MinScore > 0)
            parts.Add($"minScore={MinScore}");
        if (MinRating > 0)
            parts.Add($"minRating={MinRating}");
        if (RangeFrom is { } from)
            parts.Add($"from={from:yyyy-MM-dd}");
        if (RangeTo is { } to)
            parts.Add($"to={to:yyyy-MM-dd}");
        if (!string.Equals(SortField, "date", StringComparison.Ordinal))
            parts.Add($"sort={Uri.EscapeDataString(SortField)}");
        if (PageSize != DefaultPageSize)
            parts.Add($"pageSize={PageSize}");

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    /// <summary>
    /// Parses a query string (with or without '?'). Returns false (without throwing) if a
    /// value is malformed or out of range: the caller can then answer 400.
    /// </summary>
    public static bool TryParse(string? query, out FilterCriteria criteria)
    {
        criteria = new FilterCriteria();
        var q = query?.TrimStart('?');
        if (string.IsNullOrEmpty(q))
            return true;

        foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            string key, value;
            try
            {
                // .NET is lenient with malformed % sequences (leaves them as-is):
                // we validate explicitly so an invalid query can get a 400.
                var rawKey = eq < 0 ? pair : pair[..eq];
                var rawValue = eq < 0 ? "" : pair[(eq + 1)..];
                if (!HasValidEscapes(rawKey) || !HasValidEscapes(rawValue))
                    return false;
                key = Uri.UnescapeDataString(rawKey);
                value = Uri.UnescapeDataString(rawValue);
            }
            catch (UriFormatException)
            {
                return false;
            }

            switch (key.ToLowerInvariant())
            {
                case "q":
                    criteria = criteria with { SearchText = value };
                    break;
                case "country":
                    criteria = criteria with { CountryCode = value };
                    break;
                case "kind":
                    criteria = criteria with { SourceKind = value };
                    break;
                case "sources":
                    criteria = criteria with
                    {
                        SelectedSources = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    };
                    break;
                case "minscore":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minScore)
                        || minScore is < 0 or > 100)
                        return false;
                    criteria = criteria with { MinScore = minScore };
                    break;
                case "minrating":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minRating)
                        || minRating is < 0 or > 5)
                        return false;
                    criteria = criteria with { MinRating = minRating };
                    break;
                case "from":
                    if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
                        return false;
                    criteria = criteria with { RangeFrom = from };
                    break;
                case "to":
                    if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
                        return false;
                    criteria = criteria with { RangeTo = to };
                    break;
                case "sort":
                    if (!KnownSortFields.Contains(value))
                        return false;
                    criteria = criteria with { SortField = value };
                    break;
                case "pagesize":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageSize)
                        || pageSize is < 1 or > MaxPageSize)
                        return false;
                    criteria = criteria with { PageSize = pageSize };
                    break;
                default:
                    // Unknown parameter: ignored (future-proof tolerance)
                    break;
            }
        }

        return true;
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>Checks that every '%' is followed by two hexadecimal digits (valid escape).</summary>
    private static bool HasValidEscapes(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] != '%') continue;
            if (i + 2 >= s.Length || !Uri.IsHexDigit(s[i + 1]) || !Uri.IsHexDigit(s[i + 2]))
                return false;
            i += 2;
        }
        return true;
    }

    public static FilterCriteria? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<FilterCriteria>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Structural equality: collections are compared by content and order.</summary>
    public bool Equals(FilterCriteria? other)
    {
        if (other is null) return false;
        return string.Equals(SearchText, other.SearchText, StringComparison.Ordinal)
            && string.Equals(CountryCode, other.CountryCode, StringComparison.Ordinal)
            && string.Equals(SourceKind, other.SourceKind, StringComparison.Ordinal)
            && SelectedSources.SequenceEqual(other.SelectedSources, StringComparer.Ordinal)
            && MinScore == other.MinScore
            && MinRating == other.MinRating
            && RangeFrom == other.RangeFrom
            && RangeTo == other.RangeTo
            && string.Equals(SortField, other.SortField, StringComparison.Ordinal)
            && PageSize == other.PageSize;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SearchText, StringComparer.Ordinal);
        hash.Add(CountryCode, StringComparer.Ordinal);
        hash.Add(SourceKind, StringComparer.Ordinal);
        foreach (var s in SelectedSources)
            hash.Add(s, StringComparer.Ordinal);
        hash.Add(MinScore);
        hash.Add(MinRating);
        hash.Add(RangeFrom);
        hash.Add(RangeTo);
        hash.Add(SortField, StringComparer.Ordinal);
        hash.Add(PageSize);
        return hash.ToHashCode();
    }
}
