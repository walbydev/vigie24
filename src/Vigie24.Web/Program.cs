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

app.Run();

/// <summary>Corps de requête du vote : { value: 0..5 }</summary>
public record VoteRequest(int Value);
