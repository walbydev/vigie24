using System.Net.Http.Json;
using System.ServiceModel.Syndication;
using Microsoft.EntityFrameworkCore;
using Vigie24.Domain.Models;
using Vigie24.Services.Interfaces;
using Vigie24.Infrastructure.Data;

namespace Vigie24.Worker;

public class Vigie24Worker : BackgroundService
{
    private readonly ILogger<Vigie24Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAiService _aiService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public Vigie24Worker(
        ILogger<Vigie24Worker> logger,
        IServiceProvider serviceProvider,
        IAiService aiService,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _aiService = aiService;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Vigie24 Worker starting...");
        
        var pollingInterval = TimeSpan.FromMinutes(
            _configuration.GetValue<int>("FeedReader:PollingIntervalMinutes", 15));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Worker iteration starting at {Time}", DateTime.UtcNow);
                
                await ProcessFeedsAsync(stoppingToken);
                
                _logger.LogInformation("Worker iteration completed. Next run in {Interval}", pollingInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during worker iteration");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessFeedsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Vigie24DbContext>();

        // Récupérer les sources actives
        var activeSources = await dbContext.Sources
            .Where(s => s.IsActive).AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processing {Count} active sources", activeSources.Count);

        foreach (var source in activeSources)
        {
            try
            {
                await ProcessSourceAsync(source, dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing source {SourceName}", source.Name);
            }
        }

        // Mettre à jour les timestamps d'ingestion
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessSourceAsync(Source source, Vigie24DbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing source: {SourceName} ({Kind})", 
            source.Name, source.SourceKind);

        List<ArticleFeedItem> feedItems = new();

        feedItems = source.IngestionMethod switch
            {
                "Rss" => await FetchRssFeedAsync(source.FeedUrl!, cancellationToken),
                "Api" => await FetchApiFeedAsync(source, cancellationToken),
                "Youtube" => await FetchYoutubeFeedAsync(source, cancellationToken),
                "Reddit" => await FetchRedditFeedAsync(source, cancellationToken),
                _ => await FetchRssFeedAsync(source.FeedUrl!, cancellationToken)
            };
        foreach (var item in feedItems)
        {
            try
            {
                await ProcessFeedItemAsync(item, source, dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item: {ItemTitle}", item.Title);
            }
        }

        // Mettre à jour le timestamp d'ingestion
        source.LastIngestedAt = DateTime.UtcNow;
    }

    private async Task<List<ArticleFeedItem>> FetchRssFeedAsync(string feedUrl, CancellationToken cancellationToken)
    {
        try
        {
            // Use SyndicationFeed to parse RSS/Atom XML feeds
            // Add User-Agent to avoid being blocked by some sites
            var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
            request.Headers.Add("User-Agent", "Vigie24-RSS-Monitor/1.0 (Philippe Walby; philippe@walby.dev)");
            request.Headers.Add("Accept", "application/rss+xml, application/xml, text/xml, */*");
            
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var settings = new System.Xml.XmlReaderSettings
            {
                DtdProcessing = System.Xml.DtdProcessing.Parse,
                IgnoreWhitespace = true,
                IgnoreComments = true
            };
            using var reader = System.Xml.XmlReader.Create(stream, settings);
            var feed = SyndicationFeed.Load(reader);

            var items = new List<ArticleFeedItem>();
            foreach (var item in feed.Items)
            {
                items.Add(new ArticleFeedItem
                {
                    Title = item.Title?.Text ?? "Untitled",
                    Content = item.Summary?.Text ?? (item.Content != null ? item.Content.ToString() : ""), 
                    Url = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                    PublishedAt = item.PublishDate.UtcDateTime,
                    Author = item.Authors.FirstOrDefault()?.Name ?? ""
                });
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch RSS feed: {FeedUrl}", feedUrl);
            return new List<ArticleFeedItem>();
        }
    }

    private async Task<List<ArticleFeedItem>> FetchApiFeedAsync(Source source, CancellationToken cancellationToken)
    {
        // Implémentation spécifique selon l'API (Reddit, etc.)
        // Pour l'instant, on retourne une liste vide
        _logger.LogInformation("API feed fetching not yet implemented for {SourceName}", source.Name);
        return new List<ArticleFeedItem>();
    }

    private async Task<List<ArticleFeedItem>> FetchYoutubeFeedAsync(Source source, CancellationToken cancellationToken)
    {
        // Implémentation YouTube via API
        _logger.LogInformation("YouTube feed fetching not yet implemented for {SourceName}", source.Name);
        return new List<ArticleFeedItem>();
    }

    private async Task<List<ArticleFeedItem>> FetchRedditFeedAsync(Source source, CancellationToken cancellationToken)
    {
        // Implémentation Reddit via API
        _logger.LogInformation("Reddit feed fetching not yet implemented for {SourceName}", source.Name);
        return new List<ArticleFeedItem>();
    }

    private async Task ProcessFeedItemAsync(
        ArticleFeedItem item, 
        Source source, 
        Vigie24DbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Vérifier si l'article existe déjà
        var existingArticle = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.CanonicalUrl == item.Url, cancellationToken);

        if (existingArticle != null)
        {
            _logger.LogDebug("Article already exists: {Title}", item.Title);
            return;
        }

        // Vérifier les doublons avec les articles récents
        var recentArticles = await dbContext.Articles
            .Where(a => a.PublishedAt > DateTime.UtcNow.AddDays(-7))
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var recent in recentArticles)
        {
            var isDuplicate = await _aiService.IsDuplicateAsync(
                item.Title,
                item.Content,
                recent.Title,
                recent.Summary,
                cancellationToken);

            if (isDuplicate)
            {
                _logger.LogInformation("Duplicate detected: {Title} (similar to {ExistingTitle})", 
                    item.Title, recent.Title);
                return;
            }
        }

        // Traiter l'article avec l'IA
        _logger.LogInformation("Processing article with AI: {Title}", item.Title);
        
        var aiResult = await _aiService.ProcessArticleAsync(
            item.Title,
            item.Content,
            item.Url,
            cancellationToken);

        // Créer l'article
        var article = new Article
        {
            CanonicalUrl = item.Url,
            Title = item.Title,
            Summary = aiResult.Summary,
            EditorialScore = aiResult.EditorialScore,
            LanguageCode = "fr",
            PublishedAt = item.PublishedAt,
            CategoryId = await GetOrCreateCategoryAsync(dbContext, aiResult, cancellationToken),
            IsPublished = aiResult.EditorialScore >= 60 // Seuil de publication
        };

        // Calculer le score algorithmique
        article.AlgorithmicScore = CalculateAlgorithmicScore(article, recentArticles.Count);
        article.RankingScore = article.EditorialScore * 0.6m + article.AlgorithmicScore * 0.4m;

        // Ajouter les tags
        foreach (var theme in aiResult.DetectedThemes)
        {
            var tag = await GetOrCreateTagAsync(dbContext, theme, cancellationToken);
            article.Tags.Add(new Domain.Models.ArticleTag
            {
                Tag = tag,
                Article = article
            });
        }

        // Ajouter la source
        article.Sources.Add(new ArticleSource
        {
            Source = source,
            SourceArticleUrl = item.Url,
            IsPrimary = true
        });

        // Ajouter l'auteur si présent
        if (!string.IsNullOrWhiteSpace(item.Author))
        {
            var author = await GetOrCreateAuthorAsync(dbContext, item.Author, cancellationToken);
            article.Authors.Add(new ArticleAuthor
            {
                Author = author,
                Article = article
            });
        }

        dbContext.Articles.Add(article);

        // Ensure all DateTime.Kind are UTC to avoid Npgsql errors when saving timestamptz
        NormalizeArticleDates(article);

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Article saved: {Title} (Score: {Score})", 
            article.Title, article.RankingScore);
    }

    private async Task<Guid> GetOrCreateCategoryAsync(
        Vigie24DbContext dbContext,
        AiProcessingResult aiResult,
        CancellationToken cancellationToken)
    {
        // Logique de mapping des thèmes IA vers catégories
        var primaryTheme = aiResult.DetectedThemes.FirstOrDefault()?.ToLower() ?? "";
        
        Guid? categoryId = primaryTheme switch
        {
            var t when t.Contains("ai") || t.Contains("llm") || t.Contains("machine learning") => 
                await GetCategoryIdBySlug(dbContext, "ia", cancellationToken),
            var t when t.Contains("code") || t.Contains("dev") || t.Contains("programming") => 
                await GetCategoryIdBySlug(dbContext, "developpement", cancellationToken),
            var t when t.Contains("science") || t.Contains("research") => 
                await GetCategoryIdBySlug(dbContext, "science", cancellationToken),
            var t when t.Contains("security") || t.Contains("cyber") => 
                await GetCategoryIdBySlug(dbContext, "cybersecurite", cancellationToken),
            var t when t.Contains("hardware") || t.Contains("chip") || t.Contains("gpu") => 
                await GetCategoryIdBySlug(dbContext, "hardware", cancellationToken),
            _ => await GetCategoryIdBySlug(dbContext, "ia", cancellationToken)
        };

        return categoryId ?? await GetCategoryIdBySlug(dbContext, "ia", cancellationToken) ?? Guid.Empty;
    }

    private async Task<Guid?> GetCategoryIdBySlug(
        Vigie24DbContext dbContext,
        string slug,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
        
        return category?.Id;
    }

    private async Task<Domain.Models.Tag> GetOrCreateTagAsync(
        Vigie24DbContext dbContext,
        string theme,
        CancellationToken cancellationToken)
    {
        var slug = theme.ToLower().Replace(" ", "-").Replace(".", "");
        
        var tag = await dbContext.Tags
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

        if (tag == null)
        {
            tag = new Domain.Models.Tag { Slug = slug };
            dbContext.Tags.Add(tag);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return tag;
    }

    private async Task<Author> GetOrCreateAuthorAsync(
        Vigie24DbContext dbContext,
        string authorName,
        CancellationToken cancellationToken)
    {
        var author = await dbContext.Authors
            .FirstOrDefaultAsync(a => a.Name == authorName, cancellationToken);

        if (author == null)
        {
            author = new Author { Name = authorName };
            dbContext.Authors.Add(author);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return author;
    }

    private void NormalizeArticleDates(Article article)
    {
        // Helper to ensure DateTime.Kind is UTC for all date properties we set
        DateTime ToUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        article.PublishedAt = ToUtc(article.PublishedAt);
        article.FirstSeenAt = ToUtc(article.FirstSeenAt);
        article.CreatedAt = ToUtc(article.CreatedAt);
        article.UpdatedAt = ToUtc(article.UpdatedAt);

        foreach (var src in article.Sources)
        {
            if (src.SourcePublishedAt.HasValue)
            {
                var v = src.SourcePublishedAt.Value;
                src.SourcePublishedAt = DateTime.SpecifyKind(v, DateTimeKind.Utc);
            }
            src.CreatedAt = ToUtc(src.CreatedAt);
        }

        foreach (var a in article.Authors)
        {
            a.CreatedAt = ToUtc(a.CreatedAt);
        }

        foreach (var t in article.Tags)
        {
            t.CreatedAt = ToUtc(t.CreatedAt);
        }
    }

    private decimal CalculateAlgorithmicScore(Article article, int recentArticlesCount)
    {
        // Score algorithmique basé sur :
        // - Récence (plus récent = meilleur score)
        // - Popularité potentielle (basé sur le contenu)
        // - Diversité des sources
        
        var recencyScore = CalculateRecencyScore(article.PublishedAt);
        var contentScore = CalculateContentScore(article.Summary);
        
        return (recencyScore + contentScore) / 2;
    }

    private decimal CalculateRecencyScore(DateTime publishedAt)
    {
        var hoursOld = (decimal)(DateTime.UtcNow - publishedAt).TotalHours;
        if (hoursOld <= 1) return 100;
        if (hoursOld <= 6) return 80;
        if (hoursOld <= 24) return 60;
        if (hoursOld <= 48) return 40;
        return Math.Max(0, 40 - (hoursOld - 48) / 24 * 10);
    }

    private decimal CalculateContentScore(string summary)
    {
        // Score basé sur la longueur et la densité d'information
        var wordCount = summary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        if (wordCount >= 50) return 90;
        if (wordCount >= 30) return 70;
        if (wordCount >= 15) return 50;
        return 30;
    }
}

// Classes pour la désérialisation RSS
public class Feed
{
    public List<FeedItem> Items { get; set; } = new();
}

public class FeedItem
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? Link { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class ArticleFeedItem
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string Author { get; set; } = string.Empty;
}
