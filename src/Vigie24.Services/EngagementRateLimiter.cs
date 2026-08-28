using System.Collections.Concurrent;

namespace Vigie24.Services;

/// <summary>
/// Levée quand l'anti-abus d'engagement (votes/clics) refuse une action.
/// </summary>
public sealed class RateLimitExceededException(string message) : InvalidOperationException(message);

/// <summary>
/// Anti-abus simple pour les votes (0-5) et les clics : fenêtre fixe par clé
/// (adresse IP du visiteur). Limite le nombre d'actions par fenêtre — par défaut
/// 30 actions/heure/IP, largement au-dessus d'un usage humain, suffisant pour
/// stopper un bot qui fausserait les scores de classement.
///
/// Thread-safe (ConcurrentDictionary + verrou par entrée). Les entrées périmées
/// sont purgées quand le dictionnaire dépasse <see cref="MaxEntries"/>.
/// </summary>
public sealed class EngagementRateLimiter
{
    /// <summary>Purgé au-delà de cette taille pour éviter une croissance sans borne.</summary>
    private const int MaxEntries = 10_000;

    private sealed class WindowCounter
    {
        public DateTime Start;
        public int Count;
    }

    private readonly ConcurrentDictionary<string, WindowCounter> _counters = new();
    private readonly int _permitLimit;
    private readonly TimeSpan _window;

    public EngagementRateLimiter(int permitLimit, TimeSpan window)
    {
        if (permitLimit <= 0) throw new ArgumentOutOfRangeException(nameof(permitLimit));
        _permitLimit = permitLimit;
        _window = window;
    }

    /// <summary>
    /// Consomme une unité du budget de la clé si elle n'est pas dépassée.
    /// Retourne <c>true</c> si l'action est autorisée, <c>false</c> sinon.
    /// </summary>
    public bool TryConsume(string key)
    {
        var now = DateTime.UtcNow;
        var counter = _counters.GetOrAdd(key, _ => new WindowCounter { Start = now });
        lock (counter)
        {
            if (now - counter.Start >= _window)
            {
                counter.Start = now;
                counter.Count = 0;
            }
            if (counter.Count >= _permitLimit)
                return false;
            counter.Count++;
        }

        if (_counters.Count > MaxEntries)
            Cleanup();
        return true;
    }

    /// <summary>Purge les entrées dont la fenêtre est entièrement périmée.</summary>
    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _counters)
        {
            if (now - kv.Value.Start > _window)
                _counters.TryRemove(kv.Key, out _);
        }
    }
}
