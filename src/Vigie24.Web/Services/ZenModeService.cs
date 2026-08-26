using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Vigie24.Web.Services;

/// <summary>
/// Global « mode zen » : when enabled, the content surfaces (home, articles page,
/// briefing) hide articles whose anxiety score exceeds <see cref="ZenMaxAnxiety"/>.
/// Opt-in, persisted in browser storage. The RSS feeds stay explicit
/// (/feed vs /feed/zen) because feed readers have no JS state.
/// </summary>
public class ZenModeService
{
    /// <summary>Anxiety ceiling applied by the global zen mode (0-10 scale).</summary>
    public const int ZenMaxAnxiety = 6;

    private const string StorageKey = "vigie24.zen";

    private readonly ProtectedLocalStorage _storage;

    public bool IsEnabled { get; private set; }

    public ZenModeService(ProtectedLocalStorage storage) => _storage = storage;

    /// <summary>Reads the persisted state (browser storage — only after first render).</summary>
    public async Task InitializeAsync()
    {
        try
        {
            var result = await _storage.GetAsync<bool>(StorageKey);
            IsEnabled = result.Success && result.Value;
        }
        catch
        {
            IsEnabled = false; // prerender / stockage indisponible → zen off par défaut
        }
    }

    public async Task SetAsync(bool enabled)
    {
        IsEnabled = enabled;
        try
        {
            await _storage.SetAsync(StorageKey, enabled);
        }
        catch
        {
            // Stockage indisponible : l'état reste en mémoire pour la session.
        }
    }
}
