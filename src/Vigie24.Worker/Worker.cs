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
    /// HTTP User-Agent used for feeds and scraping (FeedReader:UserAgent).
    /// Some sites (Reddit in particular) block explicit robot User-Agents;
    /// the default is therefore a realistic browser User-Agent.
    /// </summary>
    private string UserAgent => _configuration.GetValue<string>("FeedReader:UserAgent")
        ?? "Vigie24-RSS-Monitor/1.0 (Walby Developments; https://vigie24.info)";
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

        // Mock mode: generate fictional articles instead of fetching real feeds
        if (_aiService is MockAiService)
        {
            await GenerateMockArticlesAsync(dbContext, cancellationToken);
            return;
        }

        // Retrieve active sources (tracked: the worker updates LastIngestedAt
        // after each run — AsNoTracking would prevent this field from being persisted)
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

        // Update the ingestion timestamps
        await dbContext.SaveChangesAsync(cancellationToken);

        // Rebuild "debates": pairing up contradictory articles
        await RebuildDebatesAsync(dbContext, cancellationToken);
    }

    /// <summary>
    /// Rebuilds debates from articles of the last 14 days carrying a
    /// topic + stance. Groups by normalized topic, then semantically merges
    /// isolated topics by embedding similarity (configurable cosine threshold).
    /// </summary>
    private async Task RebuildDebatesAsync(Vigie24DbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var since = DateTime.UtcNow.AddDays(-14);
            var candidates = await dbContext.Articles.AsNoTracking()
                .Where(a => a.Topic != null && a.Stance != null && a.PublishedAt >= since)
                .Select(a => new { a.Id, a.Topic, a.Stance })
                .ToListAsync(cancellationToken);

            var groups = DebateEngine.Build(candidates.Select(a => new Article
            {
                Id = a.Id,
                Topic = a.Topic,
                Stance = a.Stance
            }));

            // Semantic merge: isolated topics (without an opponent) are matched with
            // nearby debates by cosine similarity of their embedding.
            groups = await MergeIsolatedGroupsAsync(groups, dbContext, cancellationToken);

            var existing = await dbContext.Debates
                .Include(d => d.Articles)
                .ToListAsync(cancellationToken);
            var existingByTopic = existing.ToDictionary(d => d.Topic);
            var wantedTopics = groups.Keys.ToHashSet();

            foreach (var (topic, group) in groups)
            {
                if (!existingByTopic.TryGetValue(topic, out var debate))
                {
                    debate = new Debate { Topic = topic, DisplayTopic = group.DisplayTopic };
                    dbContext.Debates.Add(debate);
                }
                else
                {
                    debate.DisplayTopic = group.DisplayTopic;
                    debate.UpdatedAt = DateTime.UtcNow;
                }

                var wantedIds = group.Entries.Select(e => e.ArticleId).ToHashSet();

                // Remove links that have become obsolete
                foreach (var link in debate.Articles.Where(l => !wantedIds.Contains(l.ArticleId)).ToList())
                    dbContext.DebateArticles.Remove(link);

                // Add the debate's new articles
                foreach (var entry in group.Entries.Where(e => !debate.Articles.Any(l => l.ArticleId == e.ArticleId)))
                {
                    debate.Articles.Add(new DebateArticle
                    {
                        ArticleId = entry.ArticleId,
                        Stance = entry.Stance
                    });
                }
            }

            // Remove debates that no longer have opposing articles
            foreach (var stale in existing.Where(d => !wantedTopics.Contains(d.Topic)).ToList())
                dbContext.Debates.Remove(stale);

            await dbContext.SaveChangesAsync(cancellationToken);

            if (groups.Count > 0)
            {
                _logger.LogInformation("Debates rebuilt: {Count} active debates", groups.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debate rebuild failed");
        }
    }

    /// <summary>
    /// Matches isolated debate groups (a single article, so not yet a debate)
    /// with an existing debate whose embedding is semantically close (cosine ≥ threshold),
    /// which can create the missing opposition.
    /// </summary>
    private static async Task<Dictionary<string, DebateEngine.DebateGroup>> MergeIsolatedGroupsAsync(
        Dictionary<string, DebateEngine.DebateGroup> groups,
        Vigie24DbContext dbContext,
        CancellationToken cancellationToken)
    {
        const float similarityThreshold = 0.85f;
        var isolated = groups.Values.Where(g => g.Entries.Count < 2).ToList();
        if (isolated.Count == 0)
            return groups;

        var isolatedIds = isolated.SelectMany(g => g.Entries.Select(e => e.ArticleId)).ToHashSet();
        var embeddings = await dbContext.ArticleEmbeddings.AsNoTracking()
            .Where(e => isolatedIds.Contains(e.ArticleId))
            .ToDictionaryAsync(e => e.ArticleId, e => e.Vector, cancellationToken);

        var result = groups.ToDictionary(kv => kv.Key, kv => kv.Value);

        foreach (var single in isolated)
        {
            var vector = single.Entries
                .Select(e => embeddings.GetValueOrDefault(e.ArticleId))
                .FirstOrDefault(v => v is { Length: > 0 });
            if (vector is null)
                continue;

            var best = result.Values
                .Where(g => g != single && DebateEngine.HasOpposingStances(single.Entries.Concat(g.Entries)))
                .Select(g => (Group: g, Sim: g.Entries.Max(e =>
                    embeddings.GetValueOrDefault(e.ArticleId) is { } v && v.Length == vector.Length
                        ? CosineSimilarity(vector, v)
                        : 0f)))
                .Where(x => x.Sim >= similarityThreshold)
                .OrderByDescending(x => x.Sim)
                .FirstOrDefault();

            if (best.Group is not null)
            {
                foreach (var entry in single.Entries)
                    best.Group.Entries.Add(entry);
                result.Remove(single.Topic);
            }
        }

        return result;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        if (normA == 0f || normB == 0f)
            return 0f;
        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    private async Task ProcessSourceAsync(Source source, Vigie24DbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing source: {SourceName} ({Kind})", 
            source.Name, source.SourceKind);

        List<ArticleFeedItem> feedItems;
        try
        {
            feedItems = source.IngestionMethod switch
                {
                    // YouTube feeds (feeds/videos.xml) and Reddit (.rss) are standard Atom/RSS:
                    // they are ingested like any other feed (the dedicated
                    // "Youtube"/"Reddit" cases are tolerated and treated as RSS).
                    "Api" => await FetchApiFeedAsync(source, cancellationToken),
                    _ => await FetchRssFeedAsync(source.FeedUrl!, cancellationToken)
                };
        }
        catch (FeedFetchException ffe)
        {
            // Fetch failed: bump the per-source consecutive-failure counter and
            // auto-disable the source once the threshold (FeedReader:MaxFetchFailures)
            // is reached — announced in the worker logs (failure report).
            await HandleFetchFailureAsync(source, dbContext, ffe.FeedUrl, cancellationToken);
            return;
        }

        // A successful fetch clears the consecutive-failure counter.
        if (source.FetchFailureCount > 0)
        {
            source.FetchFailureCount = 0;
            _logger.LogInformation("Feed fetch recovered for source {SourceName}, failure counter reset", source.Name);
        }

        // Delay between items (FeedReader:ItemDelayMilliseconds) to respect AI rate limits
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

        // Update the ingestion timestamp (saved immediately so that
        // the Sources page reflects each source's freshness in real time)
        source.LastIngestedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Records a fetch failure for a source: bumps the consecutive-failure counter,
    /// auto-disables the source once FeedReader:MaxFetchFailures is reached and
    /// announces the event in the worker logs (failure report).
    /// </summary>
    private async Task HandleFetchFailureAsync(Source source, Vigie24DbContext dbContext, string feedUrl, CancellationToken cancellationToken)
    {
        source.FetchFailureCount = SourceFailurePolicy.NextFailureCount(source.FetchFailureCount);
        var maxFailures = _configuration.GetValue<int>("FeedReader:MaxFetchFailures", SourceFailurePolicy.DefaultMaxFailures);

        if (SourceFailurePolicy.ShouldDisable(source.FetchFailureCount, maxFailures))
        {
            source.IsActive = false;
            source.DisabledReason = SourceFailurePolicy.DisableReason(source.FetchFailureCount);
            _logger.LogError("Source {SourceName} AUTO-DISABLED after {Count}/{Max} consecutive fetch failures ({FeedUrl})",
                source.Name, source.FetchFailureCount, maxFailures, feedUrl);
        }
        else
        {
            _logger.LogWarning("Fetch failed for source {SourceName} ({Count}/{Max}): {FeedUrl}",
                source.Name, source.FetchFailureCount, maxFailures, feedUrl);
        }

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
            // (configurable via FeedReader:UserAgent — some sites like Reddit
            //  block explicit robot User-Agents).
            // NB: the HttpRequestMessage is created on EVERY attempt — a message
            // that has already been sent cannot be reused ("request message was already sent").

            // Light retry on rate-limit (429) and 5xx errors: some feeds
            // (Reddit, YouTube) heavily throttle unauthenticated access.
            HttpResponseMessage response = null!;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
                request.Headers.Add("User-Agent", UserAgent);
                request.Headers.Add("Accept", "application/rss+xml, application/xml, text/xml, */*");
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode != 429 && (int)response.StatusCode < 500)
                    break;

                response.Dispose();
                if (attempt < 3)
                {
                    _logger.LogWarning("Feed {FeedUrl} rate-limited ({Status}), retry {Attempt}/3 in {Delay}s",
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

            // Article limit per feed and per cycle: feeds point to the most
            // recent articles — 15 are enough for monitoring. Caps the AI
            // volume (each new article costs several calls) during first ingestions.
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
            // Signal the failure to ProcessSourceAsync (per-source consecutive-failure
            // counter + auto-disable) instead of silently returning an empty list.
            throw new FeedFetchException(feedUrl, ex);
        }
    }

    private async Task<List<ArticleFeedItem>> FetchApiFeedAsync(Source source, CancellationToken cancellationToken)
    {
        // API-specific implementation (Reddit, etc.)
        // For now, return an empty list
        _logger.LogInformation("API feed fetching not yet implemented for {SourceName}", source.Name);
        return new List<ArticleFeedItem>();
    }

    private async Task<List<ArticleFeedItem>> FetchYoutubeFeedAsync(Source source, CancellationToken cancellationToken)
    {
        // YouTube feeds (feeds/videos.xml) are standard Atom: handled via FetchRssFeedAsync.
        return await FetchRssFeedAsync(source.FeedUrl!, cancellationToken);
    }

    private async Task<List<ArticleFeedItem>> FetchRedditFeedAsync(Source source, CancellationToken cancellationToken)
    {
        // Reddit feeds (.rss) are standard Atom: handled via FetchRssFeedAsync.
        return await FetchRssFeedAsync(source.FeedUrl!, cancellationToken);
    }

    /// <summary>
    /// Mock mode: generates fictional articles (Bogus) without fetching real feeds.
    /// Creates a fake source + category if absent, then 8-15 articles.
    /// </summary>
    private async Task GenerateMockArticlesAsync(Vigie24DbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock mode: generating fictional articles with Bogus...");

        // Fake source (created if absent)
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

        // Standard categories (created if absent - required by GetOrCreateCategoryAsync)
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
        // Check if the article already exists
        var existingArticle = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.CanonicalUrl == item.Url, cancellationToken);

        if (existingArticle != null)
        {
            _logger.LogDebug("Article already exists: {Title}", item.Title);
            return;
        }

        // Check for duplicates against recent articles
        // Capped via FeedReader:MaxDuplicateCheckArticles to limit AI calls (rate-limit)
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

        // Process the article with AI
        _logger.LogInformation("Processing article with AI: {Title}", item.Title);
        
        var aiResult = await _aiService.ProcessArticleAsync(
            item.Title,
            item.Content,
            item.Url,
            cancellationToken);

        // Illustration: image from the RSS feed (media:content/enclosure), otherwise scrape the article page
        var imageUrl = item.ImageUrl;
        if (string.IsNullOrWhiteSpace(imageUrl) &&
            _configuration.GetValue<bool>("FeedReader:ScrapeArticleImages", true) &&
            !string.IsNullOrWhiteSpace(item.Url))
        {
            imageUrl = await ScrapeArticleImageAsync(item.Url, cancellationToken);
        }

        // Create the article
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
            IsPublished = aiResult.EditorialScore >= ArticleScoring.PublicationThreshold, // Publication threshold
            PromptTokens = aiResult.PromptTokens > 0 ? aiResult.PromptTokens : null,
            CompletionTokens = aiResult.CompletionTokens > 0 ? aiResult.CompletionTokens : null,
            Topic = string.IsNullOrWhiteSpace(aiResult.Topic) ? null : aiResult.Topic.Trim(),
            Stance = string.IsNullOrWhiteSpace(aiResult.Stance) ? null : aiResult.Stance.Trim(),
            // Guard: out-of-range values from the model are stored as null rather than invalid data
            AnxietyScore = aiResult.AnxietyScore is >= 0 and <= 10 ? aiResult.AnxietyScore : null
        };

        // Decision trace for the « Réflexion & analyse » block: keep the exact
        // provider/model used and the raw model output (justification, reasoning,
        // detected categories/themes/keywords) — the derived columns (CategoryId,
        // tags…) only keep the final choices.
        article.Analysis = new ArticleAnalysis
        {
            Provider = _aiService.ProviderName,
            Model = _aiService.ModelName,
            AnalyzedAtUtc = DateTime.UtcNow,
            Justification = string.IsNullOrWhiteSpace(aiResult.Justification) ? null : aiResult.Justification.Trim(),
            Reasoning = string.IsNullOrWhiteSpace(aiResult.Reasoning) ? null : aiResult.Reasoning.Trim(),
            DetectedCategories = aiResult.DetectedCategories,
            DetectedThemes = aiResult.DetectedThemes,
            Keywords = aiResult.Keywords,
            AlternativeCategories = aiResult.AlternativeCategories
        };

        // Add the article to the context IMMEDIATELY (before the embedding and the
        // GetOrCreate* calls that perform intermediate SaveChanges) — otherwise the embedding
        // would be inserted before the article → foreign key violation.
        dbContext.Articles.Add(article);

        // Semantic vector (embeddings) for debate matching — non-blocking
        try
        {
            var embedding = await _aiService.GetEmbeddingAsync($"{item.Title} — {aiResult.Summary}", cancellationToken);
            if (embedding is { Length: > 0 })
            {
                dbContext.ArticleEmbeddings.Add(new ArticleEmbedding
                {
                    ArticleId = article.Id,
                    Vector = embedding
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding calculation failed for article {Title}", item.Title);
        }

        // Calculate the algorithmic score (formulas shared in Vigie24.Domain.ArticleScoring)
        article.AlgorithmicScore = ArticleScoring.CalculateAlgorithmicScore(article, recentArticles.Count);
        article.RankingScore = ArticleScoring.CalculateRanking(article.EditorialScore, article.AlgorithmicScore);

        // Add the tags
        foreach (var theme in aiResult.DetectedThemes)
        {
            var tag = await GetOrCreateTagAsync(dbContext, theme, cancellationToken);
            article.Tags.Add(new Domain.Models.ArticleTag
            {
                Tag = tag,
                Article = article
            });
        }

        // Add the source
        // NB: the source comes from an AsNoTracking() query — using the navigation
        // property would make EF Core attempt a duplicate INSERT (sources_pkey violation).
        // Only the foreign key is therefore used.
        // SourcePublishedAt = the feed item's date: needed for the « premier sur le
        // sujet » / scoop features (first source to cover a topic).
        article.Sources.Add(new ArticleSource
        {
            SourceId = source.Id,
            SourceArticleUrl = item.Url,
            SourcePublishedAt = item.PublishedAt,
            IsPrimary = true
        });

        // Add the author if present
        if (!string.IsNullOrWhiteSpace(item.Author))
        {
            var author = await GetOrCreateAuthorAsync(dbContext, item.Author, cancellationToken);
            article.Authors.Add(new ArticleAuthor
            {
                Author = author,
                Article = article
            });
        }

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
    /// Hierarchical mapping: the AI returns [parent, subcategory] (e.g. ["AI", "LLM Models"]).
    /// Returns the Id of the subcategory, otherwise that of the parent.
    /// Fallback: legacy mapping by primary theme.
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
            // Resolve via the official taxonomy first (e.g. "Electronique" → "hardware-electronique"),
            // otherwise a free-form slug (fallback for unknown categories).
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

        // Fallback: mapping by primary theme (legacy behavior)
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

    /// <summary>Get-or-create a category by slug (attached to the parent if it is a subcategory)</summary>
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
        // Full slugify: lowercase, no accents, hyphens (e.g. "Video Games" → "video-games")
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
    /// Extracts an illustration from the RSS feed:
    /// media:content / media:thumbnail (including inside media:group), then <enclosure> image.
    /// </summary>
    internal static string? ExtractFeedImage(SyndicationItem item)
    {
        const string mediaNs = "http://search.yahoo.com/mrss/";
        var media = XNamespace.Get(mediaNs);

        foreach (var ext in item.ElementExtensions)
        {
            if (ext.OuterName is not ("content" or "thumbnail" or "image" or "group"))
                continue;

            try
            {
                // XNode.ReadFrom (not XElement.Load): the wrapper reader from GetReader()
                // does not end at EOF (stops on extensionWrapper) → Load used to throw an
                // exception swallowed by the catch → no image extracted from feeds.
                using var reader = ext.GetReader();
                reader.MoveToContent();
                var el = XNode.ReadFrom(reader) as XElement;
                if (el is null) continue;

                if (el.Name == media + "content" || el.Name == media + "thumbnail" || el.Name == media + "image")
                {
                    // media:content is an image only if its type is image/* (e.g. YouTube
                    // exposes the Flash player URL with type=application/x-shockwave-flash).
                    if (el.Name != media + "content")
                    {
                        var url = (string?)el.Attribute("url");
                        if (!string.IsNullOrEmpty(url)) return url;
                    }
                    else
                    {
                        var type = (string?)el.Attribute("type") ?? "";
                        var url = (string?)el.Attribute("url");
                        if (type.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(url))
                            return url;
                    }
                }
                else if (el.Name == media + "group")
                {
                    // Prefer the thumbnail (media:thumbnail); media:content only if image/*
                    var thumb = el.Elements(media + "thumbnail").FirstOrDefault()
                                ?? el.Elements(media + "image").FirstOrDefault();
                    if (thumb != null)
                    {
                        var url = (string?)thumb.Attribute("url");
                        if (!string.IsNullOrEmpty(url)) return url;
                    }

                    var content = el.Elements(media + "content").FirstOrDefault(c =>
                        ((string?)c.Attribute("type") ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase));
                    var contentUrl = (string?)content?.Attribute("url");
                    if (!string.IsNullOrEmpty(contentUrl)) return contentUrl;
                }
            }
            catch
            {
                // unreadable extension: move on to the next one
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
    /// Scrapes the illustration URL of an article page (og:image / twitter:image, otherwise first image).
    /// Only the URL is retrieved — the image itself is not downloaded.
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

    /// <summary>Idempotent seed of the category taxonomy (parents + subcategories)</summary>
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

    /// <summary>Raised when a feed fetch fails (network, HTTP status or parse).</summary>
    private sealed class FeedFetchException(string feedUrl, Exception inner)
        : Exception($"Failed to fetch feed: {feedUrl}", inner)
    {
        public string FeedUrl { get; } = feedUrl;
    }
}

// Classes for RSS deserialization
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
