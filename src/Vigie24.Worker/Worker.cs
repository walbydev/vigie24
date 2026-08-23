using System.Net.Http.Json;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Services.Interfaces;
using Vigie24.Services.Implementations;
using Vigie24.Infrastructure.Data;

namespace Vigie24.Worker;

public class Vigie24Worker : BackgroundService
{
    private readonly ILogger<Vigie24Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAiService _aiService;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// User-Agent HTTP utilisé pour les flux et le scraping (FeedReader:UserAgent).
    /// Certains sites (Reddit notamment) bloquent les User-Agent robots explicites ;
    /// la valeur par défaut est donc un User-Agent navigateur réaliste.
    /// </summary>
    private string UserAgent => _configuration.GetValue<string>("FeedReader:UserAgent")
        ?? "Vigie24-RSS-Monitor/1.0 (Walby Developments; philippe@walby.dev)";
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

        // Mode mock : generer des articles fictifs au lieu de fetch de vrais feeds
        if (_aiService is MockAiService)
        {
            await GenerateMockArticlesAsync(dbContext, cancellationToken);
            return;
        }

        // Récupérer les sources actives (trackées : le worker met à jour LastIngestedAt
        // après chaque traitement — AsNoTracking empêcherait la persistance de ce champ)
        var activeSources = await dbContext.Sources
            .Where(s => s.IsActive)
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
                // Les flux YouTube (feeds/videos.xml) et Reddit (.rss) sont de l'Atom/RSS
                // standard : on les ingère comme n'importe quel flux (les cas dédiés
                // "Youtube"/"Reddit" sont tolérés et traités comme du RSS).
                "Api" => await FetchApiFeedAsync(source, cancellationToken),
                _ => await FetchRssFeedAsync(source.FeedUrl!, cancellationToken)
            };
        // Délai entre items (FeedReader:ItemDelayMilliseconds) pour respecter les rate-limits IA
        var itemDelayMs = _configuration.GetValue<int>("FeedReader:ItemDelayMilliseconds", 0);

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

            if (itemDelayMs > 0)
            {
                await Task.Delay(itemDelayMs, cancellationToken);
            }
        }

        // Mettre à jour le timestamp d'ingestion (sauvegardé immédiatement pour
        // que la page Sources reflète la fraîcheur de chaque source en temps réel)
        source.LastIngestedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<ArticleFeedItem>> FetchRssFeedAsync(string feedUrl, CancellationToken cancellationToken)
    {
        try
        {
            // Check if this domain should be excluded (e.g., YouTube in dev)
            var excludeDomains = _configuration.GetSection("FeedReader:ExcludeDomains").Value;
            if (!string.IsNullOrEmpty(excludeDomains))
            {
                var excluded = excludeDomains.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var domain in excluded)
                {
                    if (feedUrl.Contains(domain, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Skipping excluded domain: {Domain} from {FeedUrl}", domain, feedUrl);
                        return new List<ArticleFeedItem>();
                    }
                }
            }
            
            // Use SyndicationFeed to parse RSS/Atom XML feeds
            // Add User-Agent to avoid being blocked by some sites
            // (configurable via FeedReader:UserAgent — certains sites comme Reddit
            //  bloquent les User-Agent robots explicites).
            var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/rss+xml, application/xml, text/xml, */*");

            // Retry léger sur rate-limit (429) et erreurs 5xx : certains flux
            // (Reddit, YouTube) limitent fortement les accès non authentifiés.
            HttpResponseMessage response = null!;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode != 429 && (int)response.StatusCode < 500)
                    break;

                response.Dispose();
                if (attempt < 3)
                {
                    _logger.LogWarning("Feed {FeedUrl} limité ({Status}), nouvelle tentative {Attempt}/3 dans {Delay}s",
                        feedUrl, (int)response.StatusCode, attempt, 10 * attempt);
                    await Task.Delay(TimeSpan.FromSeconds(10 * attempt), cancellationToken);
                }
            }
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

            // Limite d'articles par feed et par cycle : les flux pointent vers les
            // articles les plus récents — 15 suffisent pour une veille. Borne le volume
            // IA (chaque nouvel article coûte plusieurs appels) lors des premières ingestions.
            var maxItemsPerFeed = _configuration.GetValue<int>("FeedReader:MaxItemsPerFeed", 15);

            var items = new List<ArticleFeedItem>();
            foreach (var item in feed.Items.Take(maxItemsPerFeed))
            {
                items.Add(new ArticleFeedItem
                {
                    Title = item.Title?.Text ?? "Untitled"!,
                    Content = item.Summary?.Text ?? (item.Content != null ? item.Content.ToString() : ""), 
                    Url = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                    PublishedAt = item.PublishDate.UtcDateTime,
                    Author = item.Authors.FirstOrDefault()?.Name ?? "",
                    ImageUrl = ExtractFeedImage(item)
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
        // Les flux YouTube (feeds/videos.xml) sont de l'Atom standard : gérés via FetchRssFeedAsync.
        return await FetchRssFeedAsync(source.FeedUrl!, cancellationToken);
    }

    private async Task<List<ArticleFeedItem>> FetchRedditFeedAsync(Source source, CancellationToken cancellationToken)
    {
        // Les flux Reddit (.rss) sont de l'Atom standard : gérés via FetchRssFeedAsync.
        return await FetchRssFeedAsync(source.FeedUrl!, cancellationToken);
    }

    /// <summary>
    /// Mode mock : genere des articles fictifs (Bogus) sans fetch de vrais feeds.
    /// Cree une source + categorie factices si absentes, puis 8-15 articles.
    /// </summary>
    private async Task GenerateMockArticlesAsync(Vigie24DbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock mode: generating fictional articles with Bogus...");

        // Source factice (creee si absente)
        var mockSource = await dbContext.Sources
            .FirstOrDefaultAsync(s => s.Name == "Mock Source", cancellationToken);
        if (mockSource == null)
        {
            mockSource = new Source
            {
                Name = "Mock Source",
                CountryCode = "CH",
                LanguageCode = "fr",
                SourceKind = "News",
                IngestionMethod = "Rss",
                WebsiteUrl = "https://mock.vigie24.local",
                FeedUrl = "https://mock.vigie24.local/feed.xml",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Sources.Add(mockSource);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Mock source created");
        }

        // Categories standard (creees si absentes - requises par GetOrCreateCategoryAsync)
        var defaultCategories = new[]
        {
            ("ia", 1), ("developpement", 2), ("science", 3),
            ("cybersecurite", 4), ("hardware", 5)
        };
        foreach (var (slug, order) in defaultCategories)
        {
            var cat = await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
            if (cat == null)
            {
                dbContext.Categories.Add(new Category
                {
                    Slug = slug,
                    DisplayOrder = order,
                    IsActive = true
                });
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var faker = new Bogus.Faker("fr");
        var count = faker.Random.Int(8, 15);
        var generated = 0;

        for (int i = 0; i < count; i++)
        {
            var url = $"https://mock.vigie24.local/articles/{Guid.NewGuid():N}";
            var exists = await dbContext.Articles
                .AnyAsync(a => a.CanonicalUrl == url, cancellationToken);
            if (exists) continue;

            var title = faker.Lorem.Sentence(faker.Random.Int(4, 12)).TrimEnd('.');
            var item = new ArticleFeedItem
            {
                Title = title,
                Content = faker.Lorem.Paragraphs(faker.Random.Int(2, 5)),
                Url = url,
                PublishedAt = DateTime.UtcNow.AddHours(-faker.Random.Int(1, 72)),
                Author = faker.Name.FullName(),
                ImageUrl = faker.Image.PicsumUrl()
            };

            await ProcessFeedItemAsync(item, mockSource, dbContext, cancellationToken);
            generated++;
        }

        _logger.LogInformation("Mock generation done: {Generated} new articles", generated);
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
        // Plafonné via FeedReader:MaxDuplicateCheckArticles pour limiter les appels IA (rate-limit)
        var maxDuplicateCheckArticles = _configuration.GetValue<int>("FeedReader:MaxDuplicateCheckArticles", 10);
        var recentArticles = await dbContext.Articles
            .Where(a => a.PublishedAt > DateTime.UtcNow.AddDays(-7))
            .Take(maxDuplicateCheckArticles)
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

        // Illustration : image du flux RSS (media:content/enclosure), sinon scraping de la page article
        var imageUrl = item.ImageUrl;
        if (string.IsNullOrWhiteSpace(imageUrl) &&
            _configuration.GetValue<bool>("FeedReader:ScrapeArticleImages", true) &&
            !string.IsNullOrWhiteSpace(item.Url))
        {
            imageUrl = await ScrapeArticleImageAsync(item.Url, cancellationToken);
        }

        // Créer l'article
        var article = new Article
        {
            CanonicalUrl = item.Url,
            Title = item.Title,
            Summary = aiResult.Summary,
            EditorialScore = aiResult.EditorialScore,
            LanguageCode = "fr",
            PublishedAt = item.PublishedAt,
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl,
            ImageAltText = string.IsNullOrWhiteSpace(imageUrl) ? null : item.Title,
            CategoryId = await GetOrCreateCategoryAsync(dbContext, aiResult, cancellationToken),
            IsPublished = aiResult.EditorialScore >= 60 // Seuil de publication
        };

        // Calculer le score algorithmique (formules partagées dans Vigie24.Domain.ArticleScoring)
        article.AlgorithmicScore = ArticleScoring.CalculateAlgorithmicScore(article, recentArticles.Count);
        article.RankingScore = ArticleScoring.CalculateRanking(article.EditorialScore, article.AlgorithmicScore);

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
        // NB: la source vient d'une requête AsNoTracking() — passer par la navigation
        // ferait tenter à EF Core un INSERT dupliqué (violation sources_pkey).
        // On n'utilise donc que la clé étrangère.
        article.Sources.Add(new ArticleSource
        {
            SourceId = source.Id,
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

        try
        {
            // Normalize ALL tracked DateTime values immediately before saving
            // This catches any DateTime added by EF Core during Add() or navigation property traversal
            NormalizeTrackedDateTimes(dbContext);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            // Log DateTime kinds to help diagnose Npgsql ArgumentException
            _logger.LogError(dbEx, "DbUpdateException saving article. Dumping DateTime kinds for diagnosis...");
            _logger.LogError("Article.PublishedAt: {Kind}", article.PublishedAt.Kind);
            _logger.LogError("Article.FirstSeenAt: {Kind}", article.FirstSeenAt.Kind);
            _logger.LogError("Article.CreatedAt: {Kind}", article.CreatedAt.Kind);
            _logger.LogError("Article.UpdatedAt: {Kind}", article.UpdatedAt.Kind);

            foreach (var src in article.Sources)
            {
                _logger.LogError("Source {SourceId} CreatedAt: {Kind}", src.SourceId, src.CreatedAt.Kind);
                _logger.LogError("Source {SourceId} SourcePublishedAt: {Value}", src.SourceId, src.SourcePublishedAt?.ToString() ?? "null");
            }

            foreach (var a in article.Authors)
            {
                _logger.LogError("Author {AuthorId} CreatedAt: {Kind}", a.AuthorId, a.CreatedAt.Kind);
            }

            foreach (var t in article.Tags)
            {
                _logger.LogError("Tag {TagId} CreatedAt: {Kind}", t.TagId, t.CreatedAt.Kind);
            }

            // Skip this article on DB errors to avoid blocking the worker loop
            _logger.LogWarning("Skipping article due to DB error: {Title}", article.Title);
            return;
        }

        _logger.LogInformation("Article saved: {Title} (Score: {Score})", 
            article.Title, article.RankingScore);
    }

    /// <summary>
    /// Mapping hiérarchique : l'IA renvoie [parent, sous-catégorie] (ex: ["IA", "Modèles LLM"]).
    /// Retourne l'Id de la sous-catégorie, sinon du parent.
    /// Fallback : ancien mapping par thème principal.
    /// </summary>
    private async Task<Guid> GetOrCreateCategoryAsync(
        Vigie24DbContext dbContext,
        AiProcessingResult aiResult,
        CancellationToken cancellationToken)
    {
        var cats = aiResult.DetectedCategories
            .Select(c => c?.Trim() ?? "")
            .Where(c => c.Length > 0)
            .ToList();

        if (cats.Count > 0)
        {
            // Résolution via la taxonomie officielle d'abord (ex: "Electronique" → "hardware-electronique"),
            // sinon slug libre (dégradé pour les catégories inconnues).
            var parentSlug = VigieTaxonomy.ResolveSlug(cats[0]) ?? VigieTaxonomy.Slugify(cats[0]);
            var parent = await GetOrCreateCategoryBySlugAsync(
                dbContext, parentSlug, null, cancellationToken);
            if (parent == null) return Guid.Empty;

            if (cats.Count > 1)
            {
                var subSlug = VigieTaxonomy.ResolveSlug(cats[1]) ?? VigieTaxonomy.Slugify(cats[1]);
                var sub = await GetOrCreateCategoryBySlugAsync(
                    dbContext, subSlug, parent.Id, cancellationToken);
                return sub?.Id ?? parent.Id;
            }

            return parent.Id;
        }

        // Fallback : mapping par thème principal (ancien comportement)
        var primaryTheme = aiResult.DetectedThemes.FirstOrDefault()?.ToLower() ?? "";
        var slug = VigieTaxonomy.ResolveSlug(primaryTheme) ?? primaryTheme switch
        {
            var t when t.Contains("ai") || t.Contains("llm") || t.Contains("machine learning") => "ia",
            var t when t.Contains("code") || t.Contains("dev") || t.Contains("programming") => "developpement",
            var t when t.Contains("science") || t.Contains("research") => "science",
            var t when t.Contains("security") || t.Contains("cyber") => "cybersecurite",
            var t when t.Contains("hardware") || t.Contains("chip") || t.Contains("gpu") => "hardware",
            _ => "ia"
        };
        var cat = await GetOrCreateCategoryBySlugAsync(dbContext, slug, null, cancellationToken);
        return cat?.Id ?? Guid.Empty;
    }

    /// <summary>Get-or-create une catégorie par slug (avec rattachement au parent si sous-catégorie)</summary>
    private async Task<Category?> GetOrCreateCategoryBySlugAsync(
        Vigie24DbContext dbContext,
        string slug,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        slug = slug.ToLowerInvariant();
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

        if (category == null)
        {
            category = new Category
            {
                Slug = slug,
                ParentCategoryId = parentId,
                IsActive = true
            };
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (parentId != null && category.ParentCategoryId != parentId)
        {
            category.ParentCategoryId = parentId;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return category;
    }

    private async Task<Domain.Models.Tag> GetOrCreateTagAsync(
        Vigie24DbContext dbContext,
        string theme,
        CancellationToken cancellationToken)
    {
        // Slugify complet : minuscules, sans accents, tirets (ex: "Jeux Vidéo" → "jeux-video")
        var slug = VigieTaxonomy.Slugify(theme);
        
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

        // Normalize ArticleSource entities (junction table)
        foreach (var articleSrc in article.Sources)
        {
            if (articleSrc.SourcePublishedAt.HasValue)
            {
                var v = articleSrc.SourcePublishedAt.Value;
                articleSrc.SourcePublishedAt = DateTime.SpecifyKind(v, DateTimeKind.Utc);
            }
            articleSrc.CreatedAt = ToUtc(articleSrc.CreatedAt);
        }

        // Normalize ArticleAuthor entities (junction table)
        foreach (var articleAuth in article.Authors)
        {
            articleAuth.CreatedAt = ToUtc(articleAuth.CreatedAt);
        }

        // Normalize ArticleTag entities (junction table)
        foreach (var articleTag in article.Tags)
        {
            articleTag.CreatedAt = ToUtc(articleTag.CreatedAt);
        }
    }

    private void NormalizeTrackedDateTimes(Vigie24DbContext dbContext)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.ClrType == typeof(DateTime))
                {
                    if (prop.CurrentValue is DateTime dt)
                    {
                        prop.CurrentValue = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }
                }
                else if (prop.Metadata.ClrType == typeof(DateTime?))
                {
                    if (prop.CurrentValue is DateTime ndt)
                    {
                        prop.CurrentValue = ndt.Kind == DateTimeKind.Utc ? ndt : DateTime.SpecifyKind(ndt, DateTimeKind.Utc);
                    }
                }
            }
        }
    }

    private decimal CalculateAlgorithmicScore(Article article, int recentArticlesCount)
        => ArticleScoring.CalculateAlgorithmicScore(article, recentArticlesCount);

    private decimal CalculateRecencyScore(DateTime publishedAt)
        => ArticleScoring.CalculateRecencyScore(publishedAt);

    private decimal CalculateContentScore(string summary)
        => ArticleScoring.CalculateContentScore(summary);

    /// <summary>
    /// Extraction d'une illustration depuis le flux RSS :
    /// media:content / media:thumbnail (y compris dans media:group), puis <enclosure> image.
    /// </summary>
    private static string? ExtractFeedImage(SyndicationItem item)
    {
        const string mediaNs = "http://search.yahoo.com/mrss/";
        var media = XNamespace.Get(mediaNs);

        foreach (var ext in item.ElementExtensions)
        {
            if (ext.OuterName is not ("content" or "thumbnail" or "image" or "group"))
                continue;

            try
            {
                using var reader = ext.GetReader();
                var el = XElement.Load(reader);

                if (el.Name == media + "content" || el.Name == media + "thumbnail" || el.Name == media + "image")
                {
                    var url = (string?)el.Attribute("url");
                    if (!string.IsNullOrEmpty(url)) return url;
                }
                else if (el.Name == media + "group")
                {
                    var child = el.Elements(media + "content").FirstOrDefault()
                                ?? el.Elements(media + "thumbnail").FirstOrDefault();
                    var url = (string?)child?.Attribute("url");
                    if (!string.IsNullOrEmpty(url)) return url;
                }
            }
            catch
            {
                // extension illisible : on passe à la suivante
            }
        }

        var enclosure = item.Links.FirstOrDefault(l =>
            l.RelationshipType == "enclosure" &&
            l.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true);
        return enclosure?.Uri.ToString();
    }

    private static readonly Regex OgImageRegex = new(
        @"<meta[^>]+(?:property|name)=[""'](?:og:image|twitter:image)[^>]+content=[""'](?<url>[^""']+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ArticleImageRegex = new(
        @"<(?:img|link)[^>]+(?:src|href)=[""'](?<url>[^""']+\.(?:jpe?g|png|webp|gif)(?:\?[^""']*)?)[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Scrape l'URL d'illustration d'une page article (og:image / twitter:image, sinon première image).
    /// Seule l'URL est récupérée — l'image elle-même n'est pas téléchargée.
    /// </summary>
    private async Task<string?> ScrapeArticleImageAsync(string articleUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Scraping article image from: {Url}", articleUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(12));

            using var request = new HttpRequestMessage(HttpMethod.Get, articleUrl);
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!response.IsSuccessStatusCode) return null;

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)) return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 8192);
            var sb = new StringBuilder();
            var buffer = new char[65536];
            while (sb.Length < 524288)
            {
                var n = await reader.ReadAsync(buffer, 0, buffer.Length);
                if (n <= 0) break;
                sb.Append(buffer, 0, n);
            }

            var imageUrl = ExtractImageUrlFromHtml(sb.ToString(), articleUrl);
            if (imageUrl != null)
                _logger.LogInformation("Article image found: {ImageUrl}", imageUrl);
            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Image scraping failed for {Url}", articleUrl);
            return null;
        }
    }

    private static string? ExtractImageUrlFromHtml(string html, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        var match = OgImageRegex.Match(html);
        var url = match.Success ? match.Groups["url"].Value : null;

        if (string.IsNullOrWhiteSpace(url))
        {
            match = ArticleImageRegex.Match(html);
            url = match.Success ? match.Groups["url"].Value : null;
        }

        if (string.IsNullOrWhiteSpace(url)) return null;

        url = System.Net.WebUtility.HtmlDecode(url).Trim();
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;

        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;
            if (Uri.TryCreate(baseUri, url, out var resolved)) return resolved.ToString();
        }

        return url;
    }

    /// <summary>Seed idempotent de la taxonomie de catégories (parents + sous-catégories)</summary>
    public static async Task EnsureTaxonomyAsync(
        Vigie24DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var order = 1;
        foreach (var parent in VigieTaxonomy.Categories.Where(c => c.ParentSlug is null))
        {
            var parentCat = await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Slug == parent.Slug, cancellationToken);
            if (parentCat == null)
            {
                parentCat = new Category { Slug = parent.Slug, DisplayOrder = order, IsActive = true };
                dbContext.Categories.Add(parentCat);
            }

            foreach (var sub in VigieTaxonomy.Categories.Where(c => c.ParentSlug == parent.Slug))
            {
                var subCat = await dbContext.Categories
                    .FirstOrDefaultAsync(c => c.Slug == sub.Slug, cancellationToken);
                if (subCat == null)
                {
                    dbContext.Categories.Add(new Category
                    {
                        Slug = sub.Slug,
                        ParentCategoryId = parentCat.Id,
                        IsActive = true
                    });
                }
                else if (subCat.ParentCategoryId != parentCat.Id)
                {
                    subCat.ParentCategoryId = parentCat.Id;
                }
            }

            order++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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
    public string? ImageUrl { get; set; }
}
