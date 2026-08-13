using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// Owns the accent color the <see cref="BitAccentColorSwitcher"/> instances pick: the applied theme
/// overlay, the store copies that survive a refresh (per the configured
/// <see cref="BitAccentColorPersistence"/>; nothing is persisted by default - and additionally an
/// app-supplied <see cref="IBitAccentColorStore"/>, when one is registered), and the re-derivation
/// a dark/light switch needs.
/// </summary>
/// <remarks>
/// This lives above the switcher because the accent applies to the whole app: a scoped service is
/// what keeps every switcher instance showing and setting the same one color, restores the accent
/// after a refresh that lands on a page without a switcher, and keeps the overlay in step with a
/// theme toggle made anywhere in the app.
/// </remarks>
public class BitAccentColorService : IDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger? _logger;
    private readonly BitThemeManager _themeManager;
    private readonly IBitAccentColorStore? _customStore;
    private readonly BitThemeNotifications _themeNotifications;

    private bool _initialized;
    private BitAccentColorFirstPaintStrategy _firstPaintStrategy = BitAccentColorFirstPaintStrategy.None;
    private BitAccentColorPersistence _persistence = BitAccentColorPersistence.None;
    private IReadOnlyList<BitAccentColorItem> _accents = BitAccentColorSwitcher.DefaultAccents;

    /// <summary>
    /// Serializes the theme-lookup + apply + persist sequence of an accent transition, so
    /// overlapping restore / pick / scheme-switch work cannot land out of order and leave a stale
    /// palette as the final applied state.
    /// </summary>
    private readonly SemaphoreSlim _transitionGate = new(1, 1);

    /// <summary>
    /// Monotonic stamp of the latest requested transition. A transition that finds a newer stamp
    /// once it holds <see cref="_transitionGate"/> abandons itself: the newer request is already
    /// queued behind it and will apply the up-to-date accent and scheme.
    /// </summary>
    private int _transitionVersion;

    public BitAccentColorService(IJSRuntime js, BitThemeManager themeManager, BitThemeNotifications themeNotifications, ILoggerFactory? loggerFactory = null, IBitAccentColorStore? customStore = null)
    {
        _js = js;
        _customStore = customStore;
        _themeManager = themeManager;
        _themeNotifications = themeNotifications;
        _logger = loggerFactory?.CreateLogger<BitAccentColorService>();
    }

    /// <summary>
    /// The accent currently applied, as the canonical hex of the matching
    /// <see cref="BitAccentColorItem"/>. <see cref="BitAccentColorPresets.Blue"/> is the packaged
    /// palette's own primary, i.e. "no override".
    /// </summary>
    public string ActiveAccent { get; private set; } = BitAccentColorPresets.Blue;

    /// <summary>Raised after <see cref="ActiveAccent"/> changes, so the switchers can re-render.</summary>
    public event EventHandler? AccentChanged;

    /// <summary>
    /// Adopts the accent the server read from the <see cref="BitAccentColorNames.CookieName"/>
    /// cookie while prerendering, so the prerendered markup already marks the right swatch as active
    /// instead of blinking from the default to the visitor's color once the client comes up. Paints
    /// nothing: the matching palette reaches the same response through the selected strategy's
    /// first-paint machinery (see <see cref="BitAccentColorSsr"/>). A missing or unrecognized value
    /// is ignored.
    /// </summary>
    public void SeedFromPrerender(string? accent)
    {
        // The interactive pass has already read the authoritative store; letting a (possibly stale)
        // cascaded value overwrite it afterwards would undo a fresh pick.
        if (_initialized) return;

        var normalized = Normalize(accent);
        if (normalized is null || normalized == ActiveAccent) return;

        ActiveAccent = normalized;

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Restores the persisted accent, applies it, and starts tracking dark/light switches. Reading
    /// the stores needs interactivity, so callers have to wait for the first render; calling it
    /// during prerendering is a no-op that leaves the service ready to initialize on the retry.
    /// Safe to call repeatedly - only the first interactive call does the work, and its
    /// <paramref name="accents"/>/<paramref name="firstPaintStrategy"/>/<paramref name="persistence"/>
    /// become the restore configuration.
    /// </summary>
    /// <param name="accents">
    /// The accents a persisted value is validated against; <see langword="null"/> keeps
    /// <see cref="BitAccentColorSwitcher.DefaultAccents"/>. The stores are visitor-editable, so a
    /// value outside this list is treated as "nothing persisted" rather than handed to
    /// <see cref="BitThemeFactory"/> as-is.
    /// </param>
    /// <param name="firstPaintStrategy">The first-paint strategy whose persistence shape the restore maintains.</param>
    /// <param name="persistence">The stores the accent is persisted to, and restored from.</param>
    public async Task InitializeAsync(IReadOnlyList<BitAccentColorItem>? accents = null, BitAccentColorFirstPaintStrategy firstPaintStrategy = BitAccentColorFirstPaintStrategy.None, BitAccentColorPersistence persistence = BitAccentColorPersistence.None)
    {
        if (_initialized) return;

        if (accents is not null) _accents = accents;
        _firstPaintStrategy = firstPaintStrategy;
        _persistence = persistence;

        if (_js.IsRuntimeInvalid()) return; // prerendering / disconnected circuit: retry on the next call.

        _initialized = true;

        // Subscribed here rather than in the constructor because attaching the handler kicks off the
        // JS notifier registration, which can only succeed once the client is live.
        _themeNotifications.ThemeChanged += OnThemeChanged;

        var version = ++_transitionVersion;

        string? persisted = null;

        // An app-supplied store outranks the web stores: it is the copy the app itself owns (e.g.
        // native preferences in a Hybrid host), while localStorage / the cookie may lag behind it.
        if (_customStore is not null)
        {
            try
            {
                persisted = await _customStore.GetAccentAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Reading the persisted accent from the custom store failed.");
            }
        }

        if (persisted is null)
        {
            try
            {
                persisted = await _js.BitAccentColorGetPersisted(_persistence);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Reading the persisted accent failed.");
            }
        }

        var stored = Normalize(persisted);

        // Nothing restored and nothing seeded, so there is no overlay to apply and nothing to
        // persist. This is the path almost every visit takes, so it is worth keeping off the
        // interop calls below.
        if (stored is null && IsNeutral(ActiveAccent)) return;

        var changed = false;

        await _transitionGate.WaitAsync();
        try
        {
            if (version != _transitionVersion) return; // The user already picked an accent; this restore is stale.

            // The overlay is applied even when the prerender seed already put us on this accent: the
            // first paint came through a stylesheet rule, which the WebAssembly and Hybrid clients never
            // receive. Applying through BitAccentColorApply also rewrites both stores from whichever one
            // answered - a divergence left standing here would otherwise be permanent - and refreshes
            // (or drops) the StoredCss snapshot for the running library version.
            var accent = stored ?? ActiveAccent;

            // Only a store can report a change here - matching the seed, or falling back to it, is what
            // the switchers already rendered.
            changed = stored is not null && stored != ActiveAccent;

            ActiveAccent = accent;

            await ApplyCoreAsync(accent, _firstPaintStrategy, _persistence);
        }
        finally
        {
            _transitionGate.Release();
        }

        if (changed)
        {
            AccentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Applies <paramref name="accentColor"/> as the accent and persists it. Values outside the
    /// accents configured by the first <see cref="InitializeAsync"/> call (or
    /// <see cref="BitAccentColorSwitcher.DefaultAccents"/>) are re-validated as plain hex, so an app
    /// can programmatically apply an accent it never offers as a swatch; anything that is not a
    /// valid <c>#RGB</c>/<c>#RRGGBB</c> hex is ignored.
    /// </summary>
    /// <param name="accentColor">The accent color to apply.</param>
    /// <param name="firstPaintStrategy">
    /// The first-paint strategy whose persistence shape to write; <see langword="null"/> keeps the
    /// strategy of the first <see cref="InitializeAsync"/> call.
    /// </param>
    /// <param name="persistence">
    /// The stores to persist the accent to; <see langword="null"/> keeps the persistence of the
    /// first <see cref="InitializeAsync"/> call.
    /// </param>
    public async Task ApplyAsync(string accentColor, BitAccentColorFirstPaintStrategy? firstPaintStrategy = null, BitAccentColorPersistence? persistence = null)
    {
        // Normalize returns the canonical hex of a configured accent; for a programmatic
        // off-list value, fall back to syntax validation alone (BuildSnapshotCss / the factory
        // throws on anything that is not hex, so nothing unvalidated gets further than this).
        var accent = Normalize(accentColor) ?? (BitAccentColorSsr.NormalizeToken(accentColor) is null ? null : accentColor.Trim());
        if (accent is null) return;

        var version = ++_transitionVersion;

        ActiveAccent = accent;

        await _transitionGate.WaitAsync();
        try
        {
            if (version != _transitionVersion) return; // A newer pick supersedes this one.

            await ApplyCoreAsync(accent, firstPaintStrategy ?? _firstPaintStrategy, persistence ?? _persistence);
        }
        finally
        {
            _transitionGate.Release();
        }

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ApplyCoreAsync(string accent, BitAccentColorFirstPaintStrategy firstPaintStrategy, BitAccentColorPersistence persistence)
    {
        try
        {
            if (IsNeutral(accent))
            {
                // The packaged palette's own primary reproduces that palette exactly - clearing the
                // overrides, the attribute and the stores reaches the same place without pushing
                // ~280 custom properties across the interop boundary to do it, and leaves the next
                // load painting the default with no work at all.
                await _themeManager.ClearBitThemeOverridesAsync();
                await _js.BitAccentColorClear();
                await PersistToCustomStoreAsync(null);
                return;
            }

            await ApplyForThemeAsync(accent, await _themeManager.GetCurrentThemeAsync());

            // The None strategy keeps the pre-first-paint document untouched: the enabled stores are
            // still written (they are what restores the accent after hydration), but no bit-accent
            // attribute is set and no palette snapshot is kept - and any of either left behind by a
            // CSS strategy is dropped, as is any store the persistence flags disable, so switching
            // configurations self-heals.
            var setAttribute = firstPaintStrategy is not BitAccentColorFirstPaintStrategy.None;
            var snapshotCss = firstPaintStrategy is BitAccentColorFirstPaintStrategy.StoredCss ? BitAccentColorSsr.BuildSnapshotCss(accent) : null;
            await _js.BitAccentColorApply(BitAccentColorSsr.NormalizeToken(accent)!, snapshotCss, BitAccentColorSsr.SnapshotVersion, setAttribute, persistence);

            await PersistToCustomStoreAsync(accent);
        }
        catch (Exception ex)
        {
            // The pick still applies for this session wherever the calls got through, and is
            // restored on the next load from whichever store took it; a circuit that dropped
            // mid-apply must not surface here as an unhandled error.
            _logger?.LogWarning(ex, "Applying the {Accent} accent failed.", accent);
        }
    }

    /// <summary>
    /// Mirrors the accent into the app-supplied <see cref="IBitAccentColorStore"/>, when one is
    /// registered - written on every apply (independently of the persistence flags, which only
    /// govern the built-in web stores) and removed when the accent reverts to the packaged
    /// primary. Tolerated to fail on its own: the pick still applies for this session and is
    /// restored from whichever store did take it.
    /// </summary>
    private async Task PersistToCustomStoreAsync(string? accent)
    {
        if (_customStore is null) return;

        try
        {
            if (accent is null)
            {
                await _customStore.RemoveAccentAsync();
            }
            else
            {
                await _customStore.SetAccentAsync(accent);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persisting the accent to the custom store failed.");
        }
    }

    private async Task ApplyForThemeAsync(string accent, string? themeName)
    {
        // The whole-theme factory rather than the accent-only one: the point of the switcher is to
        // show what a single brand color does to an entire product, so the surfaces, text, strokes
        // and status colors all move with it instead of an accent changing under a fixed gray page.
        var isDark = themeName?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;
        var theme = isDark ? BitThemeFactory.CreateDarkThemeFromSeed(accent) : BitThemeFactory.CreateLightThemeFromSeed(accent);
        await _themeManager.ApplyBitThemeAsync(theme);
    }

    /// <summary>
    /// Matches a value from any of the (visitor-editable) stores against the configured accents,
    /// returning the canonical hex or <see langword="null"/>.
    /// </summary>
    private string? Normalize(string? value)
    {
        var token = BitAccentColorSsr.NormalizeToken(value);
        if (token is null) return null;

        foreach (var item in _accents)
        {
            // Token-level comparison so a hand-edited "8764B8" or "#8764b8" still resolves; the
            // item's own casing is returned, keeping ActiveAccent comparable by ordinal equality.
            if (BitAccentColorSsr.NormalizeToken(item.Color) == token) return item.Color;
        }

        return null;
    }

    private static bool IsNeutral(string accent)
    {
        return BitAccentColorSsr.NormalizeToken(accent) == BitAccentColorSsr.NormalizeToken(BitAccentColorPresets.Blue);
    }

    // Re-derives the accent overlay for the new scheme, otherwise a light-scheme primary would linger
    // on the dark palette (and vice versa).
    private void OnThemeChanged(object? sender, BitThemeChangedEventArgs e)
    {
        if (IsNeutral(ActiveAccent)) return;

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
            var version = ++_transitionVersion;

            await _transitionGate.WaitAsync();
            try
            {
                if (version != _transitionVersion) return; // The newer transition re-reads the current theme itself.

                await ApplyForThemeAsync(ActiveAccent, themeName);
            }
            finally
            {
                _transitionGate.Release();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Re-applying the {Accent} accent for the new theme failed.", ActiveAccent);
        }
    }

    public void Dispose()
    {
        _themeNotifications.ThemeChanged -= OnThemeChanged;

        GC.SuppressFinalize(this);
    }
}
