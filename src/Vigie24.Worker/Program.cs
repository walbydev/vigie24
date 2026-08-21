using Microsoft.EntityFrameworkCore;
using Vigie24.Infrastructure.Data;
using Vigie24.Services;
using Vigie24.Services.Interfaces;
using Vigie24.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
var aiSettings = builder.Configuration.GetSection("AiService").Get<AiServiceSettings>() 
    ?? new AiServiceSettings { Provider = "infomaniak" };

// Database
var connectionString = builder.Configuration.GetConnectionString("Database");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Fallback local dev connection string (matches project README / .env recommendations)
    connectionString = "Host=localhost;Database=vigie24;Username=postgres;Password=postgres";
}

builder.Services.AddDbContext<Vigie24DbContext>(options =>
    options.UseNpgsql(connectionString));

// HTTP Client
builder.Services.AddHttpClient();

// AI Service - Factory pattern pour basculer entre Infomaniak et DeepSeek
var serviceProvider = builder.Services.BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var aiService = AiServiceFactory.Create(aiSettings, loggerFactory);
builder.Services.AddSingleton<IAiService>(aiService);

// Worker Service
builder.Services.AddHostedService<Vigie24Worker>();

// Logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var host = builder.Build();

// Auto-migration au demarrage (si configure)
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup", true))
{
    using var scope = host.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<Vigie24DbContext>();
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Base de donnees migree avec succes");

    // Seed de la taxonomie de categories (parents + sous-categories)
    await Vigie24Worker.EnsureTaxonomyAsync(dbContext);
    Console.WriteLine("Taxonomie de categories seedee");
}

host.Run();
