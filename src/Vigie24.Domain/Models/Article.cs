using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Vigie24.Domain.Models;

/// <summary>
/// Canonical editorial entity — one event = one article
/// </summary>
public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string CanonicalUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ImageAltText { get; set; }
    public string LanguageCode { get; set; } = "fr";
    public DateTime PublishedAt { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Editorial score assigned by the AI (0-100)</summary>
    public decimal EditorialScore { get; set; }
    
    /// <summary>Algorithmic score based on various technical factors</summary>
    public decimal AlgorithmicScore { get; set; }
    
    /// <summary>Combined score displayed to users</summary>
    public decimal RankingScore { get; set; }
    
    public int VoteCount { get; set; }
    public decimal VoteAverage { get; set; }
    /// <summary>Number of clicks to the original source (engagement)</summary>
    public int ClickCount { get; set; }
    public int CrossSourceCount { get; set; }
    public bool IsPublished { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Input tokens consumed by the AI to analyze this article</summary>
    public int? PromptTokens { get; set; }

    /// <summary>Output tokens consumed by the AI to analyze this article</summary>
    public int? CompletionTokens { get; set; }

    /// <summary>Raw AI analysis trace (JSON, jsonb column) — see <see cref="Analysis"/></summary>
    public string? AnalysisJson { get; set; }

    /// <summary>
    /// Typed view of the AI analysis trace (provider, model, justification, reasoning,
    /// detected categories/themes/keywords). Serialized into <see cref="AnalysisJson"/>.
    /// Not mapped: the column already carries the JSON.
    /// </summary>
    [NotMapped]
    public ArticleAnalysis? Analysis
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AnalysisJson))
                return null;
            try
            {
                return JsonSerializer.Deserialize<ArticleAnalysis>(AnalysisJson, AnalysisJsonOptions);
            }
            catch (JsonException)
            {
                // Defensive: a corrupted trace must never break the article page.
                return null;
            }
        }
        set => AnalysisJson = value is null ? null : JsonSerializer.Serialize(value, AnalysisJsonOptions);
    }

    /// <summary>JSON options used for the analysis trace (camelCase, readable in the DB)</summary>
    /// <remarks>UnsafeRelaxedJsonEscaping keeps French accents raw in the jsonb column.
    /// Safe here: the column is never embedded in HTML directly — the Razor renderer
    /// (ArticleDetail.razor) escapes the text at display time.</remarks>
    private static readonly JsonSerializerOptions AnalysisJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Specific debate topic (normalized) — empty if the article is not part of any debate</summary>
    public string? Topic { get; set; }

    /// <summary>Article stance: pour / contre / nuance / neutre / empty</summary>
    public string? Stance { get; set; }

    /// <summary>Anxiety score 0-10 assigned by the AI (null = not evaluated)</summary>
    public int? AnxietyScore { get; set; }
    
    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual ICollection<ArticleSource> Sources { get; set; } = new List<ArticleSource>();
    public virtual ICollection<ArticleAuthor> Authors { get; set; } = new List<ArticleAuthor>();
    public virtual ICollection<ArticleTag> Tags { get; set; } = new List<ArticleTag>();
    public virtual ICollection<DebateArticle> DebateArticles { get; set; } = new List<DebateArticle>();
}

/// <summary>
/// Hierarchical category (AI, Dev, Science, Cyber, Hardware)
/// </summary>
public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
}

/// <summary>
/// Link between an article and a source (an article can come from multiple sources)
/// </summary>
public class ArticleSource
{
    public Guid ArticleId { get; set; }
    public Guid SourceId { get; set; }
    public string SourceArticleUrl { get; set; } = string.Empty;
    public string? SourceTitle { get; set; }
    public DateTime? SourcePublishedAt { get; set; }
    public string? SourceAuthorText { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Article Article { get; set; } = null!;
    public virtual Source Source { get; set; } = null!;
}

/// <summary>
/// Information source (media outlet, blog, YouTube channel, Reddit, etc.)
/// </summary>
public class Source
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string LanguageCode { get; set; } = "fr";
    public string SourceKind { get; set; }
    public string IngestionMethod { get; set; }
    public string WebsiteUrl { get; set; } = string.Empty;
    public string? FeedUrl { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Editorial policy of the source (see <see cref="SourceEditorialPolicy"/>):
    /// <c>Standard</c> = ingested, <c>Commercial</c> = excluded (buying guides,
    /// affiliate reviews). Independent of <see cref="IsActive"/> on purpose.
    /// </summary>
    public string EditorialPolicy { get; set; } = SourceEditorialPolicy.Standard;

    /// <summary>Consecutive feed-fetch failures (reset on success, used for auto-disable).</summary>
    public int FetchFailureCount { get; set; }

    /// <summary>Human-readable reason when the source was auto-disabled (null when active).</summary>
    public string? DisabledReason { get; set; }

    /// <summary>UTC time the source was auto-disabled (null when active or never disabled).</summary>
    public DateTime? DisabledAt { get; set; }

    /// <summary>UTC time of the last fetch attempt (success or failure) — drives the retry backoff.</summary>
    public DateTime? LastFetchAttemptAt { get; set; }

    public DateTime? LastIngestedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
}

/// <summary>
/// Source type
/// </summary>
public enum SourceKind
{
    Press,
    BlogLab,
    Youtube,
    Reddit,
    Other
}

/// <summary>
/// Ingestion method
/// </summary>
public enum IngestionMethod
{
    Rss,
    Api,
    Scraping,
    Youtube,
    Reddit
}

/// <summary>
/// Link between an article and an author
/// </summary>
public class ArticleAuthor
{
    public Guid ArticleId { get; set; }
    public Guid AuthorId { get; set; }
    public short AuthorOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Article Article { get; set; } = null!;
    public virtual Author Author { get; set; } = null!;
}

/// <summary>
/// Article author
/// </summary>
public class Author
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? ProfileUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<ArticleAuthor> Articles { get; set; } = new List<ArticleAuthor>();
}

/// <summary>
/// Link between an article and a tag
/// </summary>
public class ArticleTag
{
    public Guid ArticleId { get; set; }
    public Guid TagId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Article Article { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}

/// <summary>
/// Tag/Keyword
/// </summary>
public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<ArticleTag> Articles { get; set; } = new List<ArticleTag>();
}

/// <summary>
/// User (for authentication and votes)
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "User"; // Admin, Editor, User
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<ArticleVote> Votes { get; set; } = new List<ArticleVote>();
}

/// <summary>
/// A visitor's vote on an article — 0 to 5 scale (0-1: not interesting, 5: very interesting).
/// Anonymous vote via VoterKey (session cookie); UserId reserved for future authentication.
/// </summary>
public class ArticleVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArticleId { get; set; }
    public Guid? UserId { get; set; }
    public string? VoterKey { get; set; }
    public short VoteValue { get; set; } // 0 to 5
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Article Article { get; set; } = null!;
    public virtual User? User { get; set; }
}
