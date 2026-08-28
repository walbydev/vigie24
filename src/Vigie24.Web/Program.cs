using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;
using Vigie24.Infrastructure.Queries;
using Vigie24.Services;
using Vigie24.Services.Implementations;
using Vigie24.Services.Interfaces;
using Vigie24.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

// Reader engagement (votes 0-5, clicks) — direct DB access in the circuit
builder.Services.AddScoped<IEngagementService, EngagementService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<ZenModeService>(); // mode zen global (localStorage)
builder.Services.Configure<HealthCheckOptions>(builder.Configuration.GetSection("HealthCheck"));

// Database
builder.Services.AddDbContext<Vigie24DbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Dedicated context for components that query in the background (ArticleBrowser):
// in Blazor Server, the circuit's scoped DbContext is shared between the parent and its
// children — their OnInitializedAsync run in parallel. The factory avoids
// concurrency on a single context.
builder.Services.AddDbContextFactory<Vigie24DbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// HTTP Client
builder.Services.AddHttpClient();

// AI Service - Factory pattern to switch between Infomaniak and DeepSeek
// Register as factory to avoid initialization issues at startup
builder.Services.AddSingleton<AiServiceSettings>(sp => {
    var settings = builder.Configuration.GetSection("AiService").Get<AiServiceSettings>() 
        ?? new AiServiceSettings { Provider = "infomaniak" };
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
    logger.LogInformation("AI Service configured: Provider={Provider}, ApiUrl={ApiUrl}", 
        settings.Provider, settings.ApiUrl);
    return settings;
});

builder.Services.AddSingleton<IAiService>(sp => {
    var settings = sp.GetRequiredService<AiServiceSettings>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<Program>();
    
    try {
        logger.LogInformation("Creating AI service with provider: {Provider}", settings.Provider);
        var service = AiServiceFactory.Create(settings, loggerFactory);
        logger.LogInformation("AI service created successfully: {ServiceType}", service.GetType().Name);
        return service;
    } catch (Exception ex) {
        // Fallback to a mock service if creation fails
        logger.LogError(ex, "Failed to create AI service, using Mock fallback");
        return new MockAiService(loggerFactory.CreateLogger<MockAiService>());
    }
});

var app = builder.Build();

// Behind the reverse proxy (Caddy in production): preserve the HTTPS scheme
// (X-Forwarded-Proto) so HTTPS redirection and links are correct.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Caddy runs on the Docker bridge network (172.16.0.0/12) — we only accept
    // proxy headers from that network.
    KnownIPNetworks = { new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12) }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // HSTS : durée et sous-domaines configurés dans la section « Hsts » d'appsettings.json
    // (1 an + includeSubDomains ; le domaine est servi intégralement en HTTPS par Caddy).
    // Le header est émis sur les réponses HTTPS (schéma rétabli par UseForwardedHeaders).
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// ===== Engagement: votes (0-5) and click tracking to the source =====

// GET /api/articles/{id}/vote — vote state for the UI: my vote (cookie) + aggregates
app.MapGet("/api/articles/{id:guid}/vote", async (Guid id, Vigie24DbContext db, HttpContext http) =>
{
    var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id);
    if (article is null) return Results.NotFound();

    var voterKey = http.Request.Cookies["voter_id"];
    int? myVote = null;
    if (!string.IsNullOrEmpty(voterKey))
    {
        var mine = await db.ArticleVotes.FirstOrDefaultAsync(v => v.ArticleId == id && v.VoterKey == voterKey);
        myVote = mine?.VoteValue;
    }

    return Results.Ok(new
    {
        myVote,
        voteAverage = Math.Round(article.VoteAverage, 2),
        voteCount = article.VoteCount,
        clickCount = article.ClickCount,
        rankingScore = Math.Round(article.RankingScore, 2)
    });
});

// POST /api/articles/{id}/vote — anonymous vote (voter_id cookie), scale 0-5.
// Recomputes VoteCount/VoteAverage and the ranking score (engagement included).
app.MapPost("/api/articles/{id:guid}/vote", async (Guid id, VoteRequest request, Vigie24DbContext db, HttpContext http) =>
{
    if (request.Value < 0 || request.Value > 5)
        return Results.BadRequest(new { error = "Le vote doit être entre 0 et 5." });

    var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id);
    if (article is null) return Results.NotFound();

    // Persistent anonymous voter identifier (cookie)
    var voterKey = http.Request.Cookies["voter_id"];
    if (string.IsNullOrEmpty(voterKey))
    {
        voterKey = Guid.NewGuid().ToString("N");
        http.Response.Cookies.Append("voter_id", voterKey, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps, // jamais envoyé en clair (HTTPS derrière Caddy)
            MaxAge = TimeSpan.FromDays(365)
        });
    }

    var existing = await db.ArticleVotes
        .FirstOrDefaultAsync(v => v.ArticleId == id && v.VoterKey == voterKey);
    if (existing is null)
    {
        db.ArticleVotes.Add(new ArticleVote { ArticleId = id, VoterKey = voterKey, VoteValue = (short)request.Value });
    }
    else
    {
        existing.VoteValue = (short)request.Value;
    }
    await db.SaveChangesAsync();

    // Aggregates + ranking score including engagement
    var votes = await db.ArticleVotes.Where(v => v.ArticleId == id).ToListAsync();
    article.VoteCount = votes.Count;
    article.VoteAverage = votes.Count > 0 ? votes.Average(v => (decimal)v.VoteValue) : 0;
    article.RankingScore = ArticleScoring.CalculateRanking(
        article.EditorialScore, article.AlgorithmicScore,
        ArticleScoring.CalculateEngagementScore(article.VoteCount, article.VoteAverage, article.ClickCount));
    article.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        voteAverage = Math.Round(article.VoteAverage, 2),
        voteCount = article.VoteCount,
        rankingScore = Math.Round(article.RankingScore, 2),
        myVote = request.Value
    });
});

// GET /api/articles/{id}/track — increments the click counter, recomputes the score,
// then redirects (302) to the original article on the source site.
app.MapGet("/api/articles/{id:guid}/track", async (Guid id, Vigie24DbContext db) =>
{
    var article = await db.Articles
        .Include(a => a.Sources)
        .FirstOrDefaultAsync(a => a.Id == id);
    if (article is null) return Results.NotFound();

    article.ClickCount++;
    article.RankingScore = ArticleScoring.CalculateRanking(
        article.EditorialScore, article.AlgorithmicScore,
        ArticleScoring.CalculateEngagementScore(article.VoteCount, article.VoteAverage, article.ClickCount));
    article.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var primary = article.Sources.FirstOrDefault(s => s.IsPrimary) ?? article.Sources.FirstOrDefault();
    var target = primary?.SourceArticleUrl ?? article.CanonicalUrl;
    return string.IsNullOrWhiteSpace(target) ? Results.NotFound() : Results.Redirect(target);
});

// ===== RSS 2.0 feeds (subscription) =====
// /feed                     → the 50 most recently published articles, all categories
// /categories/{slug}/feed   → the 50 most recently published articles of the category (and its subcategories)
// Both accept the filter query params (same criteria as the ArticleBrowser UI):
// q, country, kind, sources, minScore, minRating, from, to. A malformed query → 400.
// The channel "link" uses the detected scheme (https behind Caddy thanks to UseForwardedHeaders).
// Feed construction (filters + ordering + limit) is shared via RssFeedService.

app.MapGet("/feed", async (Vigie24DbContext db, HttpContext http) =>
{
    if (!FilterCriteria.TryParse(http.Request.QueryString.Value, out var criteria))
        return Results.BadRequest(new { error = "Query string de filtres invalide." });

    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var articles = await RssFeedService.GetItemsAsync(db, criteria);

    var xml = RssFeedBuilder.Build(
        "Vigie24 — Surveillance RSS et analyse IA",
        baseUrl,
        "Veille technologique francophone (France, Suisse, Belgique, Québec, Luxembourg), ouverte sur l'actualité internationale — articles sélectionnés et analysés par IA.",
        articles);

    http.Response.Headers.CacheControl = "public, max-age=300";
    return Results.Content(xml, "application/rss+xml; charset=utf-8");
});

// /feed/zen → flux « zen » : uniquement les articles peu anxiogènes (anxiété ≤ 6),
// même filtres que /feed (q, country, minScore…).
app.MapGet("/feed/zen", async (Vigie24DbContext db, HttpContext http) =>
{
    if (!FilterCriteria.TryParse(http.Request.QueryString.Value, out var criteria))
        return Results.BadRequest(new { error = "Query string de filtres invalide." });

    criteria = criteria with { MaxAnxiety = 6 };

    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var articles = await RssFeedService.GetItemsAsync(db, criteria);

    var xml = RssFeedBuilder.Build(
        "Vigie24 — Flux zen (anxiété ≤ 6)",
        baseUrl,
        "Veille technologique francophone — version « zen » : articles peu anxiogènes, sélectionnés et analysés par IA.",
        articles);

    http.Response.Headers.CacheControl = "public, max-age=300";
    return Results.Content(xml, "application/rss+xml; charset=utf-8");
});

app.MapGet("/categories/{slug}/feed", async (string slug, Vigie24DbContext db, HttpContext http) =>
{
    var category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
    if (category is null)
        return Results.NotFound();

    if (!FilterCriteria.TryParse(http.Request.QueryString.Value, out var criteria))
        return Results.BadRequest(new { error = "Query string de filtres invalide." });

    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var articles = await RssFeedService.GetItemsAsync(db, criteria, slug);

    var xml = RssFeedBuilder.Build(
        $"Vigie24 — {VigieTaxonomy.DisplayName(slug)}",
        baseUrl,
        $"Vigie24 — articles de la catégorie {VigieTaxonomy.DisplayName(slug)}.",
        articles);

    http.Response.Headers.CacheControl = "public, max-age=300";
    return Results.Content(xml, "application/rss+xml; charset=utf-8");
});

// /sources.json → complete list of sources as JSON (for developers)
app.MapGet("/sources.json", async (Vigie24DbContext db) =>
{
    var sources = await db.Sources.AsNoTracking()
        .OrderBy(s => s.SourceKind).ThenBy(s => s.Name)
        .Select(s => new
        {
            s.Id,
            s.Name,
            Kind = s.SourceKind,
            Ingestion = s.IngestionMethod,
            s.CountryCode,
            s.LanguageCode,
            s.WebsiteUrl,
            s.FeedUrl,
            IsActive = s.IsActive,
            s.LastIngestedAt
        })
        .ToListAsync();

    return Results.Json(sources, new JsonSerializerOptions(JsonSerializerDefaults.Web));
});

// /sources.xml → complete list of sources as XML (for developers)
app.MapGet("/sources.xml", async (Vigie24DbContext db) =>
{
    var sources = await db.Sources.AsNoTracking()
        .OrderBy(s => s.SourceKind).ThenBy(s => s.Name)
        .ToListAsync();

    var doc = new XDocument(new XElement("sources",
        sources.Select(s => new XElement("source",
            new XAttribute("id", s.Id),
            new XAttribute("name", s.Name),
            new XAttribute("kind", s.SourceKind),
            new XAttribute("ingestion", s.IngestionMethod),
            new XAttribute("country", s.CountryCode ?? ""),
            new XAttribute("language", s.LanguageCode),
            new XAttribute("active", s.IsActive ? "true" : "false"),
            new XElement("website", s.WebsiteUrl),
            new XElement("feed", s.FeedUrl ?? ""),
            new XElement("lastIngestedAt", s.LastIngestedAt?.ToString("O") ?? "")
        ))));

    return Results.Content(doc.ToString(), "application/xml; charset=utf-8");
});

// /sitemap.xml → XML sitemap (SEO): static pages + active categories + themes linked to
// published articles + reading pages /articles/{id} (AI summary). Excludes /feed and /track.
app.MapGet("/sitemap.xml", async (Vigie24DbContext db) =>
{
    const string baseUrl = "https://vigie24.info";

    var categorySlugs = await db.Categories.AsNoTracking()
        .Where(c => c.IsActive)
        .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Slug)
        .Select(c => c.Slug)
        .ToListAsync();

    // Themes = tags attached to at least one published article (same logic as the /themes page)
    var themeSlugs = await db.Articles.AsNoTracking()
        .Where(a => a.IsPublished)
        .SelectMany(a => a.Tags.Select(t => t.Tag!.Slug))
        .Distinct()
        .OrderBy(s => s)
        .ToListAsync();

    // lastmod = date of the most recently published article (single query, shared by all URLs)
    var lastMod = await db.Articles.AsNoTracking()
        .Where(a => a.IsPublished)
        .MaxAsync(a => (DateTime?)a.PublishedAt);

    // Reading pages (mode lecture) — bounded to the 500 most recent articles to keep the
    // sitemap reasonable; each <lastmod> is the article publication date.
    var articlePages = await db.Articles.AsNoTracking()
        .Where(a => a.IsPublished)
        .OrderByDescending(a => a.PublishedAt)
        .Take(500)
        .Select(a => new SitemapBuilder.SitemapArticle(a.Id, a.PublishedAt))
        .ToListAsync();

    var xml = SitemapBuilder.Build(baseUrl, categorySlugs, themeSlugs, lastMod, articlePages);
    return Results.Content(xml, "application/xml; charset=utf-8");
});

app.Run();

/// <summary>Vote request body: { value: 0..5 }</summary>
public record VoteRequest(int Value);

/// <summary>Entry point exposed for WebApplicationFactory (HTTP integration tests).</summary>
public partial class Program;
