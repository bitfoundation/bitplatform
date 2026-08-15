using System.Collections.Concurrent;

namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// Owns the app's accent (main theme) color: the applied theme overlay, the storage + cookie copies
/// that survive a refresh, and the re-derivation a dark/light switch needs.
/// </summary>
/// <remarks>
/// The accent applies to the whole app, so it lives in a service rather than in the component that
/// renders the swatches: a component could not restore the accent after a refresh that lands on
/// another page, nor keep it in step with a theme toggle made while it is not rendered.
/// </remarks>
public partial class AppAccentColorService : IDisposable
{
    private const string StorageKey = "AccentColor";

    /// <summary>
    /// The cookie mirroring <see cref="StorageKey"/>. localStorage is unreachable while the server
    /// prerenders, so the accent is written to a cookie as well and the server paints the accented
    /// palette into the first response - see <see cref="BuildPrerenderCss"/>.
    /// </summary>
    public const string CookieName = "app-accent-color";

    // ~400 days, the upper bound modern browsers clamp persistent cookies to.
    private const int CookieMaxAgeSeconds = 34560000;

    /// <summary>
    /// Id of the <c>&lt;style&gt;</c> element carrying <see cref="BuildPrerenderCss"/> in the
    /// server-rendered document (see Server.Web's App.razor). The element is never removed; the
    /// inline overlay <see cref="ApplyAsync"/> writes always outranks it.
    /// </summary>
    public const string PrerenderStyleElementId = "app-prerender-accent";

    /// <summary>
    /// The accents the switcher offers, and the only values <see cref="ApplyAsync"/> honors: a
    /// persisted value is attacker-editable (it is just a localStorage entry / cookie), so it is
    /// matched against this list rather than handed to <see cref="BitThemeFactory"/> as-is.
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

    /// <summary>Rendered <see cref="BuildPrerenderCss"/> output, keyed by accent hex.</summary>
    private static readonly ConcurrentDictionary<string, string> prerenderCss = new(StringComparer.Ordinal);

    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private BitThemeNotifications bitThemeNotifications = default!;
    [AutoInject] private ILogger<AppAccentColorService> logger = default!;

    private bool initialized;

    /// <summary>
    /// Serializes the persist + theme-lookup + apply sequence of an accent transition, so
    /// overlapping restore / pick / scheme-switch work cannot land out of order and leave a stale
    /// palette as the final applied state.
    /// </summary>
    private readonly SemaphoreSlim transitionGate = new(1, 1);

    /// <summary>
    /// Monotonic stamp of the latest requested transition. A transition that finds a newer stamp
    /// once it holds <see cref="transitionGate"/> abandons itself: the newer request is already
    /// queued behind it and will apply the up-to-date accent and scheme.
    /// </summary>
    private int transitionVersion;

    /// <summary>
    /// True once an inline overlay has been applied in this session. From then on every dark/light
    /// switch (and a switch back to <see cref="BitAccentColorPresets.Blue"/>) must re-apply a full
    /// seeded theme, because the packaged stylesheet alone no longer describes what is on screen.
    /// </summary>
    private bool overlayApplied;

    /// <summary>
    /// The accent currently applied. <see cref="BitAccentColorPresets.Blue"/> is the packaged
    /// palette's own primary, i.e. "no override".
    /// </summary>
    public string ActiveAccent { get; private set; } = BitAccentColorPresets.Blue;

    /// <summary>
    /// Restores the persisted accent and applies it. Reading the stores needs an interactive
    /// session, so this is called from AppClientCoordinator once prerendering is over. Safe to call
    /// repeatedly - only the first call does the work. Never throws: losing the accent is not a
    /// reason to take app startup down.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (initialized) return;
        initialized = true;

        try
        {
            // Subscribed here rather than in the constructor because attaching the handler kicks off
            // the JS notifier registration, which can only succeed once the client is live.
            bitThemeNotifications.ThemeChanged += OnThemeChanged;

            var version = ++transitionVersion;

            // Both stores are read (they are written together, so they only diverge when one of
            // them was unavailable or cleared alone): storage is authoritative, and the cookie both
            // backfills a cleared storage and reveals what the server prerendered.
            var fromStorage = NormalizeAccent(await TryReadStorageAsync());
            var fromCookie = AppPlatform.IsBlazorHybrid ? null : NormalizeAccent(await TryReadCookieAsync());

            var stored = fromStorage ?? fromCookie;

            if (stored is null) return; // Nothing persisted: the packaged palette is already correct.

            await transitionGate.WaitAsync();
            try
            {
                if (version != transitionVersion) return; // The user already picked an accent; this restore is stale.

                ActiveAccent = stored;

                // Rewrite both stores from whichever one answered, so a divergence (e.g. the cookie's
                // absolute ~400-day cap expiring, or storage cleared alone) does not keep the server
                // prerendering the packaged Blue while the client repaints the accent on every load.
                await PersistAsync(stored, rewriteStorage: fromStorage is null);

                // Blue is the packaged palette's own primary, so normally there is nothing to
                // override - unless the stores diverged and the server prerendered another accent
                // from the cookie, which the seeded Blue theme must then overwrite.
                if (stored == BitAccentColorPresets.Blue && (fromCookie is null || fromCookie == BitAccentColorPresets.Blue)) return;

                await ApplyForThemeAsync(stored, await bitThemeManager.GetCurrentThemeAsync());
            }
            finally
            {
                transitionGate.Release();
            }

            pubSubService.Publish(ClientAppMessages.ACCENT_COLOR_CHANGED, stored);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Restoring the persisted accent color failed.");
        }
    }

    /// <summary>
    /// Applies <paramref name="hex"/> as the accent and persists it. Values outside
    /// <see cref="Presets"/> are ignored.
    /// </summary>
    public async Task ApplyAsync(string hex)
    {
        if (Presets.Any(p => p.Hex == hex) is false) return;

        var version = ++transitionVersion;

        ActiveAccent = hex;

        await transitionGate.WaitAsync();
        try
        {
            if (version != transitionVersion) return; // A newer pick supersedes this one.

            await PersistAsync(hex, rewriteStorage: true);

            if (hex == BitAccentColorPresets.Blue && overlayApplied is false)
            {
                // No overlay and no prerendered accent rule can be in the document, so the packaged
                // palette is already showing - skip pushing ~200 custom properties for a no-op.
            }
            else
            {
                await ApplyForThemeAsync(hex, await bitThemeManager.GetCurrentThemeAsync());
            }
        }
        finally
        {
            transitionGate.Release();
        }

        pubSubService.Publish(ClientAppMessages.ACCENT_COLOR_CHANGED, hex);
    }

    /// <summary>
    /// The stylesheet the server emits so a prerendered page paints <paramref name="accentHex"/>
    /// immediately, instead of the packaged palette that the client would then have to repaint.
    /// Returns <see langword="null"/> when there is nothing to override - no stored accent, an
    /// unrecognized one, or Blue, which is the packaged palette's own primary.
    /// </summary>
    /// <remarks>
    /// Both schemes are emitted, each scoped to the <c>bit-theme</c> attribute the library's inline
    /// head script resolves before first paint, so the server does not have to know (and, while
    /// following the OS, cannot know) whether the visitor lands on dark or light.
    /// <para>
    /// The doubled <c>:root:root</c> is there for specificity: the packaged palette declares the
    /// same tokens at <c>:root[bit-theme=…]</c>, and outranking it lets this block sit anywhere in
    /// the document instead of having to come after every stylesheet link.
    /// </para>
    /// </remarks>
    public static string? BuildPrerenderCss(string? accentHex)
    {
        var hex = NormalizeAccent(accentHex);

        if (hex is null || hex == BitAccentColorPresets.Blue) return null;

        // Deriving a palette from a seed is real work (OKLCH conversions over ~200 tokens) and there
        // are only six of them, so the rendered CSS is built once per accent rather than per request.
        return prerenderCss.GetOrAdd(hex, static hex =>
        {
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
            if (string.Equals(hex, candidate, StringComparison.OrdinalIgnoreCase)) return hex;
        }

        return null;
    }

    /// <summary>
    /// Writes the accent to storage and (on web) mirrors it into the cookie the server prerender
    /// reads. Each write is tolerated to fail on its own: the pick still applies for this session
    /// and is restored on the next load from whichever store did take it.
    /// </summary>
    private async Task PersistAsync(string hex, bool rewriteStorage)
    {
        if (rewriteStorage)
        {
            try
            {
                await storageService.SetItem(StorageKey, hex, persistent: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Persisting the accent color to storage failed.");
            }
        }

        if (AppPlatform.IsBlazorHybrid) return; // No server prerender to inform.

        try
        {
            await cookie.Set(new()
            {
                Name = CookieName,
                Value = hex,
                MaxAge = CookieMaxAgeSeconds,
                Path = "/",
                SameSite = SameSite.Strict,
                Secure = AppEnvironment.IsDevelopment() is false
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Persisting the accent color cookie failed.");
        }
    }

    private async Task<string?> TryReadStorageAsync()
    {
        try
        {
            return await storageService.GetItem(StorageKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reading the persisted accent color from storage failed.");
            return null;
        }
    }

    private async Task<string?> TryReadCookieAsync()
    {
        try
        {
            return await cookie.GetValue(CookieName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reading the persisted accent color cookie failed.");
            return null;
        }
    }

    private async Task ApplyForThemeAsync(string hex, string? themeName)
    {
        // The whole-theme factory rather than the accent-only one, so the surfaces, text, strokes
        // and status colors all move with the brand color instead of an accent changing under a
        // fixed gray page. Applied even for Blue (which reproduces the packaged palette exactly)
        // because an earlier overlay or the server's prerendered accent rule may still be painting
        // another color - inline custom properties are the one thing that outranks both.
        var isDark = themeName?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;
        var theme = isDark ? BitThemeFactory.CreateDarkThemeFromSeed(hex) : BitThemeFactory.CreateLightThemeFromSeed(hex);

        await bitThemeManager.ApplyBitThemeAsync(theme);

        overlayApplied = true;
    }

    // Re-derives the accent overlay for the new scheme, otherwise a light-scheme primary would
    // linger on the dark palette (and vice versa).
    private void OnThemeChanged(object? sender, BitThemeChangedEventArgs e)
    {
        if (overlayApplied is false) return;

        _ = ReapplyForThemeAsync(e.NewTheme);
    }

    // The event accessor cannot await, so this is fire-and-forget and has to swallow its own
    // failures: the notification is raised from the JS interop callback, where an unobserved fault
    // would surface far from anything that could report it.
    private async Task ReapplyForThemeAsync(string? themeName)
    {
        try
        {
            var version = ++transitionVersion;

            await transitionGate.WaitAsync();
            try
            {
                if (version != transitionVersion) return; // The newer transition re-reads the current theme itself.

                await ApplyForThemeAsync(ActiveAccent, themeName);
            }
            finally
            {
                transitionGate.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Re-applying the {Accent} accent color for the new theme failed.", ActiveAccent);
        }
    }

    public void Dispose()
    {
        bitThemeNotifications.ThemeChanged -= OnThemeChanged;

        GC.SuppressFinalize(this);
    }
}
