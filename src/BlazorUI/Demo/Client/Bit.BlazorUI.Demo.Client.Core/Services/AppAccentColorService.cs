using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

/// <summary>
/// Owns the accent color the home page's "Make it yours" swatches pick: the applied theme overlay,
/// the localStorage copy that survives a refresh, and the re-derivation a dark/light switch needs.
/// </summary>
/// <remarks>
/// This lives above the home page because the accent applies to the whole app. A component that only
/// exists on "/" cannot restore the accent after a refresh that lands on another page, cannot keep it
/// in step with a theme toggle made from the header while away from home, and loses track of what is
/// applied every time the visitor navigates back - which is exactly what the swatches read to mark
/// the active one.
/// </remarks>
public partial class AppAccentColorService : IDisposable
{
    // Prefixed so it cannot collide with the library's own bit-current-theme key.
    private const string StorageKey = "bit-blazorui-demo-accent";

    /// <summary>
    /// The swatches the home page offers, and the only values <see cref="ApplyAsync"/> honors: a
    /// persisted value is attacker-editable (it is just a localStorage entry), so it is matched
    /// against this list rather than handed to <see cref="BitThemeFactory"/> as-is.
    /// </summary>
    public static readonly (string Name, string Hex)[] Presets =
    [
        ("Blue", BitAccentColorPresets.Blue),
        ("Purple", BitAccentColorPresets.Purple),
        ("Green", BitAccentColorPresets.Green),
        ("Orange", BitAccentColorPresets.Orange),
        ("Teal", BitAccentColorPresets.Teal),
        ("Rose", BitAccentColorPresets.Rose),
    ];

    [AutoInject] private IJSRuntime _js = default!;
    [AutoInject] private BitThemeManager _themeManager = default!;
    [AutoInject] private BitThemeNotifications _themeNotifications = default!;
    [AutoInject] private ILogger<AppAccentColorService> _logger = default!;

    private bool _initialized;

    /// <summary>
    /// The accent currently applied. <see cref="BitAccentColorPresets.Blue"/> is the packaged
    /// palette's own primary, i.e. "no override".
    /// </summary>
    public string ActiveAccent { get; private set; } = BitAccentColorPresets.Blue;

    /// <summary>Raised after <see cref="ActiveAccent"/> changes, so the swatches can re-render.</summary>
    public event EventHandler? AccentChanged;

    /// <summary>
    /// Restores the persisted accent, applies it, and starts tracking dark/light switches. Reading
    /// localStorage needs interactivity, so callers have to wait for the first render; calling it
    /// during prerendering is a no-op that leaves the service ready to initialize on the retry.
    /// Safe to call repeatedly - only the first interactive call does the work.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        if (_js.IsRuntimeInvalid()) return; // prerendering / disconnected circuit: retry on the next call.

        _initialized = true;

        // Subscribed here rather than in the constructor because attaching the handler kicks off the
        // JS notifier registration, which can only succeed once the client is live.
        _themeNotifications.ThemeChanged += OnThemeChanged;

        var stored = await _js.Invoke<string?>("localStorage.getItem", StorageKey);
        if (stored is null || stored == ActiveAccent) return;
        if (Presets.Any(p => p.Hex == stored) is false) return;

        ActiveAccent = stored;

        await ApplyToCurrentThemeAsync(stored);

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Applies <paramref name="hex"/> as the accent and persists it. Values outside
    /// <see cref="Presets"/> are ignored.
    /// </summary>
    public async Task ApplyAsync(string hex)
    {
        if (Presets.Any(p => p.Hex == hex) is false) return;

        ActiveAccent = hex;

        await _js.InvokeVoid("localStorage.setItem", StorageKey, hex);

        await ApplyToCurrentThemeAsync(hex);

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ApplyToCurrentThemeAsync(string hex)
    {
        // Blue is the packaged palette's own primary, so seeding it reproduces that palette exactly -
        // clearing the overrides reaches the same place without pushing 224 custom properties across
        // the interop boundary to do it.
        if (hex is BitAccentColorPresets.Blue)
        {
            await _themeManager.ClearBitThemeOverridesAsync();
            return;
        }

        await ApplyForThemeAsync(hex, await _themeManager.GetCurrentThemeAsync());
    }

    private async Task ApplyForThemeAsync(string hex, string? themeName)
    {
        // The whole-theme factory rather than the accent-only one: the point of the swatches is to
        // show what a single brand color does to an entire product, so the surfaces, text, strokes
        // and status colors all move with it instead of an accent changing under a fixed gray page.
        var isDark = themeName?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;
        var theme = isDark ? BitThemeFactory.CreateDarkThemeFromSeed(hex) : BitThemeFactory.CreateLightThemeFromSeed(hex);
        await _themeManager.ApplyBitThemeAsync(theme);
    }

    // Re-derives the accent overlay for the new scheme, otherwise a light-scheme primary would linger
    // on the dark palette (and vice versa).
    private void OnThemeChanged(object? sender, BitThemeChangedEventArgs e)
    {
        if (ActiveAccent is BitAccentColorPresets.Blue) return;

        _ = ReapplyForThemeAsync(e.NewTheme);
    }

    // The event accessor cannot await, so this is fire-and-forget and has to swallow its own
    // failures: the notification is raised from the JS interop callback, where an unobserved fault
    // would surface far from anything that could report it. BitThemeNotifications only guards the
    // synchronous part of a handler.
    private async Task ReapplyForThemeAsync(string? themeName)
    {
        try
        {
            await ApplyForThemeAsync(ActiveAccent, themeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Re-applying the {Accent} accent for the new theme failed.", ActiveAccent);
        }
    }

    public void Dispose()
    {
        _themeNotifications.ThemeChanged -= OnThemeChanged;

        GC.SuppressFinalize(this);
    }
}
