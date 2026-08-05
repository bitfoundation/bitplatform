using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

/// <summary>
/// Owns the accent color the home page's "Make it yours" swatches pick: the applied theme overlay,
/// the localStorage + cookie copies that survive a refresh, and the re-derivation a dark/light
/// switch needs.
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
    /// The cookie mirroring <see cref="StorageKey"/>. localStorage is unreachable while the server
    /// prerenders, so the accent is written to a cookie as well and the server paints the accented
    /// palette into the first response - see <see cref="BuildPrerenderCss"/>. Named after the
    /// storage key so the two stores are recognizably one preference.
    /// </summary>
    public const string CookieName = StorageKey;

    // ~400 days, the upper bound modern browsers clamp persistent cookies to. Matches what the
    // library's own theme-preference cookie uses.
    private const int CookieMaxAgeSeconds = 34560000;

    /// <summary>
    /// Id of the <c>&lt;style&gt;</c> element carrying <see cref="BuildPrerenderCss"/>, so
    /// <see cref="InitializeAsync"/> can drop it once the client owns the overlay.
    /// </summary>
    public const string PrerenderStyleElementId = "app-prerender-accent";

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

    /// <summary>Rendered <see cref="BuildPrerenderCss"/> output, keyed by accent hex. See its remarks.</summary>
    private static readonly ConcurrentDictionary<string, string> _prerenderCss = new(StringComparer.Ordinal);

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
    /// Adopts the accent the server read from <see cref="CookieName"/>, so the prerendered markup
    /// already marks the right swatch as active instead of blinking from Blue to the visitor's color
    /// once the client comes up. Paints nothing: the matching palette is emitted into the same
    /// response by <see cref="BuildPrerenderCss"/>. A missing or unrecognized value is ignored.
    /// </summary>
    public void SeedFromPrerender(string? hex)
    {
        // The interactive pass has already read the authoritative store; letting a (possibly stale)
        // cascaded value overwrite it afterwards would undo a fresh pick.
        if (_initialized) return;

        var accent = NormalizeAccent(hex);
        if (accent is null || accent == ActiveAccent) return;

        ActiveAccent = accent;

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Restores the persisted accent, applies it, and starts tracking dark/light switches. Reading
    /// the stores needs interactivity, so callers have to wait for the first render; calling it
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

        // localStorage first, cookie second: they are written together, so they only diverge when
        // one of them is unavailable (localStorage throws in Safari private mode / blocked-storage
        // setups, and a visitor can clear cookies alone). Either one on its own restores the accent,
        // which is why each read is isolated - see TryReadAsync.
        var fromStorage = NormalizeAccent(await TryReadAsync("localStorage.getItem", StorageKey));
        var stored = fromStorage ?? NormalizeAccent(await TryReadAsync("getCookie", CookieName));

        // Nothing restored and nothing seeded, so there is no overlay to apply - and no prerendered
        // <style> to drop either, because BuildPrerenderCss emits none for the packaged Blue. This is
        // the path almost every visit takes, so it is worth keeping off the interop calls below.
        if (stored is null && ActiveAccent is BitAccentColorPresets.Blue) return;

        // Both stores are rewritten from whichever one answered, because ApplyAsync - the only other
        // writer - needs a fresh pick to run, so a divergence left standing here would be permanent:
        //  - the cookie on every visit, because its ~400-day cap is absolute, and because a visitor
        //    who picked an accent before this cookie existed has only the localStorage copy. Without
        //    this the server keeps prerendering the packaged Blue and the accent keeps flashing in.
        //  - localStorage only when it did not answer: it either threw, or was cleared while the
        //    cookie survived.
        // The theme half of the pair already self-heals the same way - BitTheme.init's set() rewrites
        // the preference cookie on boot.
        if (stored is not null)
        {
            await TryInvokeVoidAsync("setCookie", CookieName, stored, CookieMaxAgeSeconds);

            if (fromStorage is null)
            {
                await TryInvokeVoidAsync("localStorage.setItem", StorageKey, stored);
            }
        }

        // The overlay is applied even when the prerender seed already put us on this accent: the
        // server painted it through a stylesheet rule, which the WebAssembly and Hybrid clients never
        // receive - and which is about to be dropped below in any case. That last part is also why it
        // is applied when neither store answered: the seed then stands on its own, and dropping the
        // rule without replacing it would leave the page on the packaged palette while the swatches
        // still mark the seeded color as active.
        var accent = stored ?? ActiveAccent;

        // Only a store can report a change here - matching the seed, or falling back to it, is what
        // the swatches already rendered.
        var changed = stored is not null && stored != ActiveAccent;

        ActiveAccent = accent;

        await ApplyToCurrentThemeAsync(accent);

        if (changed)
        {
            AccentChanged?.Invoke(this, EventArgs.Empty);
        }

        // The server's first-paint copy has done its job, and the overlay just applied covers the
        // same ground. Leaving it would make a later switch back to Blue - which only clears the
        // overlay - look like it did nothing, because the rule would keep repainting the accent the
        // page happened to load with. Removed after the apply so nothing paints unaccented in between,
        // and through the tolerant path because a circuit that dropped mid-restore must not surface
        // here as an unhandled error - the overlay is already applied by this point.
        await TryInvokeVoidAsync("removeElementById", PrerenderStyleElementId);
    }

    /// <summary>
    /// Applies <paramref name="hex"/> as the accent and persists it. Values outside
    /// <see cref="Presets"/> are ignored.
    /// </summary>
    public async Task ApplyAsync(string hex)
    {
        if (Presets.Any(p => p.Hex == hex) is false) return;

        ActiveAccent = hex;

        await TryInvokeVoidAsync("localStorage.setItem", StorageKey, hex);

        // Mirrored into a cookie so the next navigation's prerender can paint this accent server-side
        // - localStorage is unreachable from there, which is what made the color flash on every load.
        await TryInvokeVoidAsync("setCookie", CookieName, hex, CookieMaxAgeSeconds);

        await ApplyToCurrentThemeAsync(hex);

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The stylesheet the server emits so a prerendered page paints <paramref name="accentHex"/>
    /// immediately, instead of the packaged palette that the client would then have to repaint.
    /// Returns <see langword="null"/> when there is nothing to override - no stored accent, an
    /// unrecognized one, or Blue, which is the packaged palette's own primary.
    /// </summary>
    /// <remarks>
    /// Both schemes are emitted, each scoped to the <c>bit-theme</c> attribute that the library's
    /// inline head script resolves before first paint. That keeps this independent of the theme
    /// preference: the server does not have to know (and, while following the OS, cannot know)
    /// whether the visitor lands on dark or light, and a later theme toggle is covered by the same
    /// two rules.
    /// <para>
    /// The doubled <c>:root:root</c> is there for specificity, not for matching: the packaged
    /// palette declares the same tokens at <c>:root[bit-theme=…]</c>, so an equal-specificity rule
    /// would only win by coming later in the document and this would have to be emitted after every
    /// stylesheet. Outranking it instead lets the block sit in <c>&lt;head&gt;</c>, where it is in
    /// the CSSOM from the first byte of body.
    /// </para>
    /// </remarks>
    public static string? BuildPrerenderCss(string? accentHex)
    {
        var hex = NormalizeAccent(accentHex);

        if (hex is null || hex == BitAccentColorPresets.Blue) return null;

        // Deriving a palette from a seed is real work (OKLCH conversions over ~280 tokens) and there
        // are only six of them, so the rendered CSS is built once per accent rather than per request.
        return _prerenderCss.GetOrAdd(hex, static hex =>
        {
            // The dark/light split matches the app's own [bit-theme$=dark] convention, so "fluent-dark"
            // and any other dark preset land on the dark palette too.
            return $":root:root[bit-theme$=dark]{{{Declarations(BitThemeFactory.CreateDarkThemeFromSeed(hex))}}}" +
                   $":root:root:not([bit-theme$=dark]){{{Declarations(BitThemeFactory.CreateLightThemeFromSeed(hex))}}}";

            static string Declarations(BitTheme theme)
            {
                return string.Concat(BitThemeUtilities.ToCssVariables(theme).Select(v => $"{v.Key}:{v.Value};"));
            }
        });
    }

    /// <summary>
    /// Matches a value from any of the (attacker-editable) stores against <see cref="Presets"/>,
    /// returning the canonical hex or <see langword="null"/>. Nothing outside the presets is ever
    /// handed to <see cref="BitThemeFactory"/> or written into the document.
    /// </summary>
    private static string? NormalizeAccent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var candidate = value.Trim();

        foreach (var (_, hex) in Presets)
        {
            // Case-insensitive so a hand-edited "#8764b8" still resolves; the preset's own casing is
            // returned, keeping ActiveAccent comparable by ordinal equality everywhere else.
            if (string.Equals(hex, candidate, StringComparison.OrdinalIgnoreCase)) return hex;
        }

        return null;
    }

    /// <summary>
    /// Reads one store, treating an unreachable one as "nothing persisted here". Neither store is
    /// guaranteed to answer - a browser set to block site data throws on the very first localStorage
    /// access, and Safari's private mode throws on write - and a store that refuses must not take the
    /// other one, nor the applied overlay, down with it. See the remarks on
    /// <see cref="TryInvokeVoidAsync"/> for why the catch is this wide.
    /// </summary>
    private async Task<string?> TryReadAsync(string function, string key)
    {
        try
        {
            return await _js.Invoke<string?>(function, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reading the persisted accent through {Function} failed.", function);
            return null;
        }
    }

    /// <summary>
    /// Invokes one store write (or the prerender-style cleanup), tolerating a call that cannot get
    /// through. The pick still applies for this session, and is restored on the next load from
    /// whichever store did take it; losing one of them is not a reason to leave the visitor on the
    /// old color. See <see cref="TryReadAsync"/>.
    /// </summary>
    /// <remarks>
    /// Catches <see cref="Exception"/> rather than <see cref="JSException"/> because that is not what
    /// actually fails here: a browser that blocks site data throws a <see cref="JSException"/>, but
    /// the far more common failure on the Server render mode is the circuit going away
    /// (<see cref="JSDisconnectedException"/>, which derives straight from <see cref="Exception"/>) or
    /// the interop call timing out (<see cref="TaskCanceledException"/>). Narrowing to
    /// <see cref="JSException"/> let exactly those unwind the whole restore.
    /// </remarks>
    private async Task TryInvokeVoidAsync(string function, params object?[] args)
    {
        try
        {
            await _js.InvokeVoid(function, args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invoking {Function} for the accent failed.", function);
        }
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
