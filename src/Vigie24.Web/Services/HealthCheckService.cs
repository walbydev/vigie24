using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vigie24.Domain;
using Vigie24.Infrastructure.Data;
using Vigie24.Services.Interfaces;

namespace Vigie24.Web.Services;

/// <summary>Options du health-check (section "HealthCheck" d'appsettings).</summary>
public sealed class HealthCheckOptions
{
    /// <summary>Âge max de la dernière ingestion pour rester « Opérationnel » (minutes).</summary>
    public int IngestionOkMinutes { get; set; } = 30;

    /// <summary>Âge max de la dernière ingestion avant « Hors ligne » (minutes).</summary>
    public int IngestionDegradedMinutes { get; set; } = 120;

    /// <summary>Intervalle minimum entre deux pings du fournisseur IA (minutes).</summary>
    public int ProviderPingMinutes { get; set; } = 10;
}

/// <summary>Résultat du health-check affiché sur la page d'accueil.</summary>
public sealed class HealthStatus
{
    public HealthLevel Level { get; set; }
    public DateTime? LastIngestionUtc { get; set; }
    public bool ProviderHealthy { get; set; }

    public string Label => Level switch
    {
        HealthLevel.Ok => "Opérationnel",
        HealthLevel.Degraded => "Dégradé",
        _ => "Hors ligne",
    };

    public string CssClass => Level switch
    {
        HealthLevel.Ok => "text-success",
        HealthLevel.Degraded => "text-warning",
        _ => "text-danger",
    };
}

public interface IHealthCheckService
{
    Task<HealthStatus> GetStatusAsync(CancellationToken ct = default);
}

/// <summary>
/// Health-check basé sur l'état réel du système :
/// fraîcheur de la dernière ingestion (base) + disponibilité du fournisseur IA.
/// Le ping du fournisseur est mis en cache (ProviderPingMinutes) pour ne pas
/// consommer de tokens ni de latence à chaque chargement de page.
/// </summary>
public sealed class HealthCheckService(
    Vigie24DbContext db,
    IAiService ai,
    IOptions<HealthCheckOptions> opts,
    ILogger<HealthCheckService> logger) : IHealthCheckService
{
    private readonly object _pingLock = new();
    private DateTime _lastPingUtc = DateTime.MinValue;
    private bool _pingOk;
    private string? _pingError;

    public async Task<HealthStatus> GetStatusAsync(CancellationToken ct = default)
    {
        // 1. Fraîcheur de l'ingestion : date du dernier article vu par le worker.
        var last = await db.Articles.MaxAsync(a => (DateTime?)a.FirstSeenAt, ct);

        // 2. Disponibilité du fournisseur IA (ping léger, mis en cache).
        var (pingOk, pingError) = await PingProviderCachedAsync(ct);

        var level = HealthEvaluator.Evaluate(
            last,
            pingOk,
            TimeSpan.FromMinutes(opts.Value.IngestionOkMinutes),
            TimeSpan.FromMinutes(opts.Value.IngestionDegradedMinutes));

        logger.LogInformation(
            "HealthCheck : niveau={Level}, dernière ingestion={Last}, fournisseur IA={Ok} ({Error})",
            level, last?.ToString("O"), pingOk, pingError);

        return new HealthStatus
        {
            Level = level,
            LastIngestionUtc = last,
            ProviderHealthy = pingOk,
        };
    }

    private async Task<(bool Ok, string? Error)> PingProviderCachedAsync(CancellationToken ct)
    {
        var ttl = TimeSpan.FromMinutes(opts.Value.ProviderPingMinutes);

        lock (_pingLock)
        {
            if (DateTime.UtcNow - _lastPingUtc < ttl)
                return (_pingOk, _pingError);
        }

        bool ok;
        string? error = null;
        try
        {
            ok = await ai.IsHealthyAsync(ct);
        }
        catch (Exception ex)
        {
            ok = false;
            error = ex.Message;
        }

        lock (_pingLock)
        {
            _lastPingUtc = DateTime.UtcNow;
            _pingOk = ok;
            _pingError = error;
        }

        return (ok, error);
    }
}
