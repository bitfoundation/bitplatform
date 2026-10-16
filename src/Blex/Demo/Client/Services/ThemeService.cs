using Microsoft.JSInterop;

namespace Bit.Blex.Demo.Client.Services;

/// <summary>
/// Light/dark preference for the documentation site's own chrome. Deliberately <em>not</em> a
/// Blex store: the site keeps its presentation concerns out of the demos so that everything a
/// visitor sees in the action feed is theirs, not the site's.
/// </summary>
public sealed class ThemeService(IJSRuntime js)
{
    private const string StorageKey = "blex-docs-theme";

    /// <summary>Raised when the theme changes.</summary>
    public event Action? Changed;

    /// <summary><c>"dark"</c> or <c>"light"</c>.</summary>
    public string Theme { get; private set; } = "dark";

    /// <summary>Reads the stored preference (falling back to the OS setting) and applies it.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            var stored = await js.InvokeAsync<string?>("blexDocs.getTheme", StorageKey);
            if (!string.IsNullOrEmpty(stored))
                Theme = stored;
            await ApplyAsync();
        }
        catch (JSException)
        {
            // Theme is cosmetic; a missing helper must not break the site.
        }
    }

    /// <summary>Flips between light and dark and persists the choice.</summary>
    public async Task ToggleAsync()
    {
        Theme = Theme == "dark" ? "light" : "dark";
        await ApplyAsync();
        Changed?.Invoke();
    }

    private async Task ApplyAsync()
    {
        try
        {
            await js.InvokeVoidAsync("blexDocs.setTheme", StorageKey, Theme);
        }
        catch (JSException)
        {
        }
    }
}
