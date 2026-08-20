using Microsoft.EntityFrameworkCore;
using Vigie24.Infrastructure.Data;
using Vigie24.Services;
using Vigie24.Services.Implementations;
using Vigie24.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Database
builder.Services.AddDbContext<Vigie24DbContext>(options =>
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

app.Run();
