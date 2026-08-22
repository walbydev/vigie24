namespace Vigie24.Domain.Models;

/// <summary>
/// Entité éditoriale canonique — un événement = un article
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
    
    /// <summary>Score éditorial attribué par l'IA (0-100)</summary>
    public decimal EditorialScore { get; set; }
    
    /// <summary>Score algorithmique basé sur divers facteurs techniques</summary>
    public decimal AlgorithmicScore { get; set; }
    
    /// <summary>Score combiné affiché aux utilisateurs</summary>
    public decimal RankingScore { get; set; }
    
    public int VoteCount { get; set; }
    public decimal VoteAverage { get; set; }
    /// <summary>Nombre de clics vers la source originale (engagement)</summary>
    public int ClickCount { get; set; }
    public int CrossSourceCount { get; set; }
    public bool IsPublished { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual ICollection<ArticleSource> Sources { get; set; } = new List<ArticleSource>();
    public virtual ICollection<ArticleAuthor> Authors { get; set; } = new List<ArticleAuthor>();
    public virtual ICollection<ArticleTag> Tags { get; set; } = new List<ArticleTag>();
}

/// <summary>
/// Catégorie hiérarchique (IA, Dev, Science, Cyber, Hardware)
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
/// Lien entre un article et une source (un article peut venir de plusieurs sources)
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
/// Source d'information (média, blog, chaîne YouTube, Reddit, etc.)
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
    public DateTime? LastIngestedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
}

/// <summary>
/// Type de source
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
/// Méthode d'ingestion
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
/// Lien entre un article et un auteur
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
/// Auteur d'article
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
/// Lien entre un article et un tag
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
/// Tag/Mot-clé
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
/// Utilisateur (pour authentification et votes)
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
/// Vote d'un visiteur sur un article — échelle 0 à 5 (0-1 : pas intéressant, 5 : très intéressant).
/// Vote anonyme via VoterKey (cookie de session) ; UserId réservé à une future authentification.
/// </summary>
public class ArticleVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArticleId { get; set; }
    public Guid? UserId { get; set; }
    public string? VoterKey { get; set; }
    public short VoteValue { get; set; } // 0 à 5
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Article Article { get; set; } = null!;
    public virtual User? User { get; set; }
}
