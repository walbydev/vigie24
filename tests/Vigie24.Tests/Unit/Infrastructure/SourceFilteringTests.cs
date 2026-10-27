using Microsoft.EntityFrameworkCore;
using Vigie24.Domain;
using Vigie24.Domain.Models;
using Vigie24.Infrastructure.Data;
using Vigie24.Infrastructure.Queries;
using Xunit;

namespace Vigie24.Tests.Unit.Infrastructure;

/// <summary>
/// Editorial-policy guard: a source flagged <c>Commercial</c> (buying guides,
/// affiliate reviews) must never be ingested nor re-activated by the recovery
/// probe, while staying untouched otherwise (no reuse of <c>IsActive</c>).
/// </summary>
public class SourceFilteringTests
{
    private static async Task<Vigie24DbContext> SeedAsync(params Source[] sources)
    {
        var options = new DbContextOptionsBuilder<Vigie24DbContext>()
            .UseInMemoryDatabase($"SourceFiltering_{Guid.NewGuid()}")
            .Options;
        var db = new Vigie24DbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Sources.AddRange(sources);
        await db.SaveChangesAsync();
        return db;
    }

    private static Source NewSource(string name, string policy, bool isActive = true) => new()
    {
        Name = name,
        SourceKind = "Press",
        IngestionMethod = "Rss",
        WebsiteUrl = $"https://{name.ToLowerInvariant()}.example",
        IsActive = isActive,
        EditorialPolicy = policy
    };

    [Fact]
    public async Task Ingestible_ExcludesCommercialSources()
    {
        await using var db = await SeedAsync(
            NewSource("Standard1", SourceEditorialPolicy.Standard),
            NewSource("Commercial1", SourceEditorialPolicy.Commercial),
            NewSource("Inactive1", SourceEditorialPolicy.Standard, isActive: false));

        var names = await db.Sources.Ingestible().Select(s => s.Name).ToListAsync();

        Assert.Equal(["Standard1"], names);
    }

    [Fact]
    public async Task Ingestible_KeepsLegacyEmptyPolicy()
    {
        // Legacy rows written before the column existed get "Standard" from the
        // column default; a null/empty value must not be read as an exclusion.
        await using var db = await SeedAsync(NewSource("Legacy", policy: ""));

        var names = await db.Sources.Ingestible().Select(s => s.Name).ToListAsync();

        Assert.Equal(["Legacy"], names);
    }

    [Fact]
    public async Task Probeable_NeverResurrectsCommercialSource()
    {
        var old = DateTime.UtcNow.AddHours(-48);
        var autoDisabled = NewSource("AutoDisabled", SourceEditorialPolicy.Standard, isActive: false);
        autoDisabled.DisabledReason = "Flux introuvable (404)";
        autoDisabled.DisabledAt = old;
        autoDisabled.UpdatedAt = old;

        var commercial = NewSource("Commercial", SourceEditorialPolicy.Commercial, isActive: false);
        commercial.DisabledReason = "Flux introuvable (404)";
        commercial.DisabledAt = old;
        commercial.UpdatedAt = old;

        await using var db = await SeedAsync(autoDisabled, commercial);

        var names = await db.Sources.Probeable(DateTime.UtcNow.AddHours(-24))
            .Select(s => s.Name).ToListAsync();

        Assert.Equal(["AutoDisabled"], names);
    }

    [Theory]
    [InlineData(SourceEditorialPolicy.Commercial, true)]
    [InlineData("commercial", true)]
    [InlineData(SourceEditorialPolicy.Standard, false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void BlocksIngestion_FollowsPolicy(string? policy, bool expected)
        => Assert.Equal(expected, SourceEditorialPolicy.BlocksIngestion(policy));

    [Theory]
    [InlineData(null, SourceEditorialPolicy.Standard)]
    [InlineData("  ", SourceEditorialPolicy.Standard)]
    [InlineData(" Commercial ", SourceEditorialPolicy.Commercial)]
    public void Normalize_FallsBackToStandard(string? policy, string expected)
        => Assert.Equal(expected, SourceEditorialPolicy.Normalize(policy));
}
