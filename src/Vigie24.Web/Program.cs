using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;
using Vigie24.Services;
using Vigie24.Services.Implementations;
using Vigie24.Services.Interfaces;
using Vigie24.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

// Engagement des lecteurs (votes 0-5, clics) — accès DB direct dans le circuit
builder.Services.AddScoped<IEngagementService, EngagementService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.Configure<HealthCheckOptions>(builder.Configuration.GetSection("HealthCheck"));

// Database
builder.Services.AddDbContext<Vigie24DbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Contexte dédié pour les composants qui requêtent en arrière-plan (ArticleBrowser) :
// en Blazor Server, le DbContext scoped du circuit est partagé entre le parent et ses
// enfants — leurs OnInitializedAsync s'exécutent en parallèle. La factory évite la
// concurrence sur un même contexte.
builder.Services.AddDbContextFactory<Vigie24DbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// HTTP Client
builder.Services.AddHttpClient();

// AI Service - Factory pattern pour basculer entre Infomaniak et DeepSeek
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

// Derrière le reverse proxy (Caddy en production) : préserver le schéma HTTPS
// (X-Forwarded-Proto) pour que la redirection HTTPS et les liens soient corrects.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Caddy tourne dans le réseau Docker bridge (172.16.0.0/12) — on n'accepte
    // les en-têtes de proxy que depuis ce réseau.
    KnownIPNetworks = { new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12) }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// ===== Engagement : votes (0-5) et suivi des clics vers la source =====

// GET /api/articles/{id}/vote — état du vote pour l'UI : mon vote (cookie) + agrégats
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

// POST /api/articles/{id}/vote — vote anonyme (cookie voter_id), échelle 0-5.
// Recalcule VoteCount/VoteAverage et le score de classement (engagement inclus).
app.MapPost("/api/articles/{id:guid}/vote", async (Guid id, VoteRequest request, Vigie24DbContext db, HttpContext http) =>
{
    if (request.Value < 0 || request.Value > 5)
        return Results.BadRequest(new { error = "Le vote doit être entre 0 et 5." });

    var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id);
    if (article is null) return Results.NotFound();

    // Identifiant de votant anonyme persistant (cookie)
    var voterKey = http.Request.Cookies["voter_id"];
    if (string.IsNullOrEmpty(voterKey))
    {
        voterKey = Guid.NewGuid().ToString("N");
        http.Response.Cookies.Append("voter_id", voterKey, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
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

    // Agrégats + score de classement avec engagement
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

// GET /api/articles/{id}/track — incrémente le compteur de clics, recalcule le score,
// puis redirige (302) vers l'article original sur le site de la source.
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

// ===== Flux RSS 2.0 (abonnement) =====
// /feed                     → les 50 derniers articles publiés, toutes catégories
// /categories/{slug}/feed   → les 50 derniers articles publiés de la catégorie (et ses sous-catégories)
// Le canal "link" utilise le schéma détecté (https derrière Caddy grâce à UseForwardedHeaders).
const int RssFeedSize = 50;

app.MapGet("/feed", async (Vigie24DbContext db, HttpContext http) =>
{
    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var articles = await db.Articles
        .Where(a => a.IsPublished)
        .OrderByDescending(a => a.PublishedAt)
        .Take(RssFeedSize)
        .Select(a => new RssItem(a.Title, a.CanonicalUrl, a.Summary, a.PublishedAt, a.CanonicalUrl))
        .ToListAsync();

    var xml = RssFeedBuilder.Build(
        "Vigie24 — Surveillance RSS et analyse IA",
        baseUrl,
        "Veille technologique francophone (France, Suisse, Belgique, Québec, Luxembourg) — articles sélectionnés et analysés par IA.",
        articles);

    return Results.Content(xml, "application/rss+xml; charset=utf-8");
});

app.MapGet("/categories/{slug}/feed", async (string slug, Vigie24DbContext db, HttpContext http) =>
{
    var category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
    if (category is null)
        return Results.NotFound();

    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var articles = await db.Articles
        .Where(a => a.IsPublished
                    && a.Category != null
                    && (a.Category.Slug == slug
                        || (a.Category.ParentCategory != null && a.Category.ParentCategory.Slug == slug)))
        .OrderByDescending(a => a.PublishedAt)
        .Take(RssFeedSize)
        .Select(a => new RssItem(a.Title, a.CanonicalUrl, a.Summary, a.PublishedAt, a.CanonicalUrl))
        .ToListAsync();

    var xml = RssFeedBuilder.Build(
        $"Vigie24 — {VigieTaxonomy.DisplayName(slug)}",
        baseUrl,
        $"Vigie24 — articles de la catégorie {VigieTaxonomy.DisplayName(slug)}.",
        articles);

    return Results.Content(xml, "application/rss+xml; charset=utf-8");
});

// /sources.json → liste complète des sources au format JSON (pour développeurs)
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

// /sources.xml → liste complète des sources au format XML (pour développeurs)
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

app.Run();

/// <summary>Corps de requête du vote : { value: 0..5 }</summary>
public record VoteRequest(int Value);
