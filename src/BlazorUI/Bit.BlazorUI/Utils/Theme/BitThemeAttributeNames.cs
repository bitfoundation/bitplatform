namespace Bit.BlazorUI;

/// <summary>
/// Single source of truth for the HTML attributes and storage keys used by the theme system.
/// Keep these aligned with <c>bit-theme.ts</c> and <see cref="BitThemeSsr"/>; consumers should
/// reference these constants rather than spelling the names inline so renames stay coordinated.
/// </summary>
public static class BitThemeAttributeNames
{
    /// <summary>Active theme name attribute on <c>&lt;html&gt;</c>; matches <c>:root[bit-theme=…]</c> selectors in the packaged Fluent CSS.</summary>
    public const string Theme = "bit-theme";

    /// <summary>Optional default theme name attribute; consulted when <see cref="Theme"/> is absent at first paint.</summary>
    public const string ThemeDefault = "bit-theme-default";

    /// <summary>Marker attribute that opts the document into following <c>prefers-color-scheme</c>.</summary>
    public const string ThemeSystem = "bit-theme-system";

    /// <summary>Marker attribute that opts the document into persisting the user's theme choice via <see cref="ThemeStorageKey"/>.</summary>
    public const string ThemePersist = "bit-theme-persist";

    /// <summary>
    /// Marker attribute that opts the client into mirroring the persisted preference into the
    /// <see cref="BitThemeCookie.PreferenceCookieName"/> cookie, keeping server-side SSR
    /// (<see cref="BitThemeSsr.BuildRootThemeAttributes(string?, string?)"/>) in sync with
    /// client-side theme changes.
    /// </summary>
    public const string ThemePersistCookie = "bit-theme-persist-cookie";

    /// <summary>Resolved name to use when <see cref="ThemeSystem"/> is active and <c>prefers-color-scheme</c> is dark.</summary>
    public const string ThemeDark = "bit-theme-dark";

    /// <summary>Resolved name to use when <see cref="ThemeSystem"/> is active and <c>prefers-color-scheme</c> is light.</summary>
    public const string ThemeLight = "bit-theme-light";

    /// <summary>
    /// Opt-in list of the themes the page ships stylesheets for, space-separated
    /// (<c>bit-theme-available="fluent2-light fluent2-dark high-contrast"</c>). A stored pair naming a
    /// theme outside it falls back to <see cref="ThemeLight"/> / <see cref="ThemeDark"/>, and a stored
    /// preference outside it to the active pair's theme of its scheme (a name ending in <c>dark</c> to the
    /// dark one, any other to the light one) - so a visitor whose saved theme came from a preset the app no
    /// longer offers keeps their light or dark choice instead of a theme nothing styles. The declared pair
    /// and <c>system</c> count as offered whether listed or not, and a call that sets a theme or a pair outside
    /// it (<see cref="BitThemeManager.SetThemeAsync(string)"/>, <see cref="BitThemeManager.SetThemePairAsync(string, string)"/>)
    /// is ignored rather than painted unstyled, while one another tab stores (a tab of an older build) is fitted
    /// onto the pair the way a stored one is. Names are matched lower-cased, as the server normalizes them.
    /// Absent, every stored name is restored as it is.
    /// </summary>
    public const string ThemeAvailable = "bit-theme-available";

    /// <summary>
    /// Marker attribute that opts theme swaps into the View Transitions API: the page cross-fades
    /// to the new palette instead of hard-swapping. Checked live on every change, so it can be
    /// added/removed at runtime. Ignored (plain swap) when the browser lacks
    /// <c>document.startViewTransition</c> or the user prefers reduced motion.
    /// </summary>
    public const string ThemeViewTransition = "bit-theme-view-transition";

    /// <summary>
    /// Marker attribute that opts the document into keeping every <c>&lt;meta name="theme-color"&gt;</c>
    /// tag equal to a palette color of the live page, so the browser chrome CSS cannot reach - an
    /// installed PWA's status bar, the address bar on mobile - follows the theme, the design system
    /// and any accent overlay. Its value names the custom property to read
    /// (<c>bit-theme-color-meta="--bit-clr-bg-sec"</c>); an empty value means
    /// <see cref="ThemeColorVariable"/>. The tag is created when the document has none.
    /// </summary>
    public const string ThemeColorMeta = "bit-theme-color-meta";

    /// <summary>
    /// The custom property <see cref="ThemeColorMeta"/> reads when it carries no value: the page
    /// background, which is what a status bar sits above in nearly every app.
    /// </summary>
    public const string ThemeColorVariable = "--bit-clr-bg-pri";

    /// <summary>
    /// Marker attribute on the element of an inline theme overlay - a <see cref="BitThemeProvider"/>
    /// wrapper, or <see cref="BitThemeManager.ApplyBitThemeAsync"/>'s target - naming, space-separated,
    /// the groups of family aliases whose inputs the overlay re-values (<c>radius</c>, <c>radius-control</c>,
    /// <c>shadow</c>, <c>tooltip</c>, <c>foreground</c> and the eight roles <c>pri</c> ... <c>err</c>). Every
    /// theme stylesheet re-declares its own aliases for each named group on that element, so they follow
    /// the overlay's values as that theme builds them (<c>Styles/theme-overlay.scss</c>). Written by the
    /// library; a preset of an app's own selects it (<c>[bit-theme-overlay~="shadow"]</c>) to re-declare a
    /// family alias it decides differently from the core stylesheet.
    /// </summary>
    public const string ThemeOverlay = "bit-theme-overlay";

    /// <summary><c>localStorage</c> key used by the client script when persistence is enabled.</summary>
    public const string ThemeStorageKey = "bit-current-theme";

    /// <summary>
    /// <c>localStorage</c> key the client script keeps, beside <see cref="ThemeStorageKey"/> and when
    /// persistence is enabled, what the stored preference resolves against: the light / dark pair as
    /// <c>"&lt;light&gt; &lt;dark&gt;"</c> once the visitor has switched it at runtime
    /// (<see cref="BitThemeManager.SetThemePairAsync(string, string)"/>), which outranks
    /// <see cref="ThemeLight"/> / <see cref="ThemeDark"/> on the next visit; otherwise <c>light</c> /
    /// <c>dark</c> while the preference is that theme of the page's own pair, so it follows a later
    /// change of that pair. Absent for any other preference, which is restored as it is - unless
    /// <see cref="ThemeAvailable"/> leaves it out.
    /// </summary>
    public const string ThemePairStorageKey = "bit-theme-pair";
}
