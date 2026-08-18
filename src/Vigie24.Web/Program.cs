using Microsoft.EntityFrameworkCore;
using Vigie24.Infrastructure.Data;
using Vigie24.Services;
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
var aiSettings = builder.Configuration.GetSection("AiService").Get<AiServiceSettings>() 
    ?? new AiServiceSettings { Provider = "infomaniak" };
var aiService = AiServiceFactory.Create(aiSettings, builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>());
builder.Services.AddSingleton<IAiService>(aiService);

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
