namespace Bit.BlazorUI;

/// <summary>
/// The single-drop first-paint setup for the theme: place it at the start of a server-rendered host
/// page's <c>&lt;head&gt;</c> and it emits both halves of getting the appearance right before anything
/// is painted - the inline script that re-resolves <c>bit-theme</c> from <c>localStorage</c> / the
/// preference cookie (<see cref="BitThemeSsr"/>), and the <c>&lt;meta name="theme-color"&gt;</c> tag
/// that paints the browser chrome, with the color of the theme this response actually renders.
/// </summary>
/// <remarks>
/// <para>
/// "The start of <c>&lt;head&gt;</c>" means before any stylesheet - painting must not begin until the
/// script has run - but AFTER <c>&lt;meta charset&gt;</c>: the encoding declaration is only honored
/// inside the document's first 1024 bytes, and what this emits is more than that on its own.
/// </para>
/// <para>
/// Pair it with <see cref="BitThemeSsr.BuildRootThemeAttributeMap(string?, BitThemeSsrOptions)"/> on
/// the <c>&lt;html&gt;</c> element, handing both the same persisted preference: that call writes the
/// attributes this script reads, including <see cref="BitThemeAttributeNames.ThemeColorMeta"/>, which
/// is what keeps the tag current from the first frame the client can compute a color. The pair is
/// the whole of the host page's theme setup.
/// </para>
/// <para>
/// The theme-color half is the only part that needs colors as literals - the browser paints its
/// chrome long before a stylesheet has loaded - and they come from <see cref="ThemeColors"/>,
/// defaulting to <see cref="BitThemeSurfaces.BackgroundPrimary"/>, which carries every preset
/// registered with <see cref="BitThemePresetRegistry"/> - the packaged ones and the app's own. An
/// app whose pages sit on the secondary surface passes <c>BitThemeSurfaces.BackgroundSecondary</c>
/// instead, and an app with its own palette passes its own map. Set <see cref="EmitThemeColor"/> to
/// <see langword="false"/> to keep a hand-written tag (or a media-qualified pair) instead and have
/// this emit only the script.
/// </para>
/// <para>
/// <b>The default map is every preset the process has registered</b>, which for an app that
/// references Bit.BlazorUI.Extras includes Fluent 2, Material and Cupertino whether or not it links
/// their stylesheets. That is the right table for an app that offers them, and costs one small
/// lookup object in the correction script otherwise. An app that wants it exact says so once at
/// startup, and the registry keeps its word whatever loads afterwards:
/// <see cref="BitThemePresetRegistry.Remove(string?)"/> for a packaged preset it does not offer,
/// <see cref="BitThemePresetRegistry.Register(BitThemePreset)"/> for one whose surface it has
/// overridden in its own CSS - or it passes <see cref="ThemeColors"/> a map of its own.
/// </para>
/// <para>
/// Renders no element of its own, so it does not inherit <see cref="BitComponentBase"/>.
/// </para>
/// </remarks>
public partial class BitThemeHead : ComponentBase
{
    /// <summary>
    /// The visitor's stored theme preference - the
    /// <see cref="BitThemeCookie.PreferenceCookieName"/> cookie of this request, the same value handed
    /// to <see cref="BitThemeSsr.BuildRootThemeAttributeMap(string?, BitThemeSsrOptions)"/>. Blank
    /// (nothing stored) or <c>system</c> means the visitor follows the OS, which the server cannot
    /// read: the tag is then emitted with a guess and corrected before first paint by a script of its
    /// own. A tampered value is rejected and treated as nothing stored, exactly as it is there.
    /// </summary>
    [Parameter] public string? PersistedPreference { get; set; }

    /// <summary>
    /// The theme to fall back on when nothing is stored, mirroring the <c>defaultTheme</c> of
    /// <see cref="BitThemeSsr.BuildRootThemeAttributeMap(string?, BitThemeSsrOptions)"/> - pass the
    /// same value to both, or the tag is painted for a theme the document does not render.
    /// </summary>
    [Parameter] public string? DefaultTheme { get; set; }

    /// <summary>
    /// The two presets the OS-following resolution picks between, mirroring
    /// <see cref="BitThemeAttributeNames.ThemeLight"/> / <see cref="BitThemeAttributeNames.ThemeDark"/>
    /// on <c>&lt;html&gt;</c>. They are what the tag is painted from while the visitor follows the OS,
    /// so an app that renames the pair (a Fluent 2 site: <c>fluent2-light</c> / <c>fluent2-dark</c>)
    /// passes the same names here. Default to the core <c>light</c> / <c>dark</c> presets.
    /// </summary>
    [Parameter] public string? LightTheme { get; set; }

    /// <inheritdoc cref="LightTheme"/>
    [Parameter] public string? DarkTheme { get; set; }

    /// <summary>
    /// Theme name to browser-chrome color, as CSS colors. Defaults to
    /// <see cref="BitThemeSurfaces.BackgroundPrimary"/> - the page background of every registered
    /// preset, packaged or the app's own (the remarks on this type say how to narrow it). An app
    /// whose pages sit on the secondary surface passes
    /// <see cref="BitThemeSurfaces.BackgroundSecondary"/>, and an app with its own palette passes its own.
    /// Keys are matched exactly (ordinal) against the normalized <c>bit-theme</c> token, which is
    /// the only form the attribute ever carries.
    /// A name the map does not carry falls back to the light / dark entry - and, failing that, to any
    /// entry of the map on the same side of the scheme - by the same "ends with dark" rule the
    /// packaged stylesheets classify names with.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, string>? ThemeColors { get; set; }

    /// <summary>
    /// Whether to emit the <c>&lt;meta name="theme-color"&gt;</c> tag (and the script that corrects it
    /// before first paint, once the client has re-resolved the theme). Default <see langword="true"/>. Turn
    /// it off to keep a tag the host page writes itself - a media-qualified light / dark pair, say -
    /// which <see cref="BitThemeAttributeNames.ThemeColorMeta"/> still keeps current afterwards.
    /// </summary>
    [Parameter] public bool EmitThemeColor { get; set; } = true;

    /// <summary>
    /// Optional CSP nonce, stamped on every script this component emits so a
    /// <c>script-src 'nonce-…'</c> policy does not block the first-paint setup.
    /// </summary>
    [Parameter] public string? Nonce { get; set; }



    /// <summary>
    /// The theme this response renders, or <see langword="null"/> when the visitor follows the OS.
    /// Resolved exactly as the root attributes are, so the tag and the server-rendered document
    /// cannot disagree - the client can still re-resolve to a third theme from a store the server
    /// never saw, which is what the correction script covers.
    /// </summary>
    private string? ResolvedTheme =>
        BitThemeSsr.BuildRootThemeAttributeMap(PersistedPreference, new BitThemeSsrOptions { DefaultTheme = DefaultTheme })
                   .TryGetValue(BitThemeAttributeNames.Theme, out var theme)
        ? theme as string
        : null;

    private IReadOnlyDictionary<string, string> Colors => ThemeColors ?? BitThemeSurfaces.BackgroundPrimary;

    // Normalized, as every other layer sees them: the attribute the client reads back carries the
    // normalized token, the maps are keyed by it, and IsDarkName tests it ordinally - so a pair
    // written as "Fluent2-Dark" has to become that token before it is looked up or classified.
    private string LightThemeName => BitThemeName.NormalizeToken(LightTheme, out _) ?? BitThemePresets.Light;

    private string DarkThemeName => BitThemeName.NormalizeToken(DarkTheme, out _) ?? BitThemePresets.Dark;

    private string ResolveLightThemeColor() => ColorOf(LightThemeName, isDark: false);

    private string ResolveDarkThemeColor() => ColorOf(DarkThemeName, isDark: true);

    /// <summary>
    /// The color of the theme being rendered. While the visitor follows the OS there is no such
    /// theme, and the dark one is the guess - the same side a document with no stored preference
    /// takes for its own first paint, and corrected a moment later by the script below either way.
    /// </summary>
    private string ResolveThemeColor()
    {
        var theme = ResolvedTheme;
        return theme is null ? ResolveDarkThemeColor() : ColorOf(theme, IsDarkName(theme));
    }

    /// <summary>
    /// The map's entry for a name, or - for a theme it does not carry (an app's own preset, with no
    /// color handed in for it) - the light or dark entry, so an unknown name still lands on the right
    /// side of the scheme instead of painting the chrome against the page.
    /// </summary>
    private string ColorOf(string theme, bool isDark)
    {
        if (Colors.TryGetValue(theme, out var color)) return color;

        var fallback = isDark ? DarkThemeName : LightThemeName;
        if (fallback != theme && Colors.TryGetValue(fallback, out color)) return color;

        // Still nothing: any entry of the CALLER's map on the right side of the scheme, before the
        // packaged surfaces - an app whose pages sit on the secondary surface (or on a palette of its
        // own) must not have the chrome painted from a table it deliberately replaced. The smallest
        // key, so the pick is the same on every render rather than whatever the dictionary happens to
        // enumerate first - found in one pass, with nothing sorted on the way.
        string? sameSchemeKey = null, sameScheme = null;
        foreach (var entry in Colors)
        {
            if (IsDarkName(entry.Key) != isDark) continue;
            if (sameSchemeKey is not null && string.CompareOrdinal(entry.Key, sameSchemeKey) >= 0) continue;

            (sameSchemeKey, sameScheme) = (entry.Key, entry.Value);
        }
        if (sameScheme is not null) return sameScheme;

        // An empty map, or one with no name on this side at all: the core stylesheet's own surfaces
        // for the scheme. Constants rather than a read of BitThemeSurfaces, which is a view over a
        // registry an app is free to take "light" and "dark" out of - and this is the one line that
        // has to answer on every request regardless.
        return isDark
            ? BitThemePresetRegistry.CoreDarkBackgroundPrimary
            : BitThemePresetRegistry.CoreLightBackgroundPrimary;
    }

    /// <summary>
    /// "Ends with dark" (ordinal) - the same classification the packaged stylesheets, the accent's
    /// generated first-paint CSS and the runtime client all apply, so every layer puts a given name
    /// on the same side of light / dark.
    /// </summary>
    private static bool IsDarkName(string theme) => theme.EndsWith("dark", StringComparison.Ordinal);

    /// <summary>
    /// Reads back the attribute the inline script resolved and rewrites the tag to match, while the
    /// browser is still parsing head. Only colors from the map reach the document: the light / dark
    /// pair, plus a lookup table for the names whose color the pair does not already give - which is
    /// most of them, so the table is usually small and often empty.
    /// </summary>
    private string BuildThemeColorCorrectionScript()
    {
        var light = ResolveLightThemeColor();
        var dark = ResolveDarkThemeColor();

        var body =
            "(function(){var m=document.querySelector('meta[name=theme-color]');if(!m)return;" +
            "var t=document.documentElement.getAttribute('" + BitThemeAttributeNames.Theme + "')||'';" +
            BuildThemeColorLookup(light, dark) +
            "if(c&&m.content!==c){m.content=c;}})();";

        return string.IsNullOrWhiteSpace(Nonce)
            ? $"<script>{body}</script>"
            : $"<script nonce=\"{BitThemeSsr.HtmlEncodeAttribute(Nonce)}\">{body}</script>";
    }

    /// <summary>
    /// The statements that leave the corrected color in <c>c</c>. The scheme fallback alone would
    /// paint every light name with the configured light theme's surface, which is wrong for a map
    /// whose presets do not share one (Material's light surface is not Fluent 2's) - so the names the
    /// fallback would get wrong, and only those, are carried as an object literal in front of it.
    /// </summary>
    private string BuildThemeColorLookup(string light, string dark)
    {
        // The configured dark preset by name, ahead of the suffix rule: an app whose dark theme is
        // not named "...dark" (a "midnight", say) has that name in bit-theme, and the suffix test
        // alone would hand it the light surface. Normalized, because the attribute the script reads
        // back carries the normalized form. Only emitted when the suffix rule does not already cover
        // the name, so the default pair keeps the short test it had.
        var darkName = DarkThemeName;
        var isDark = IsDarkName(darkName)
            ? "/dark$/.test(t)"
            : "(t==='" + JsString(darkName) + "'||/dark$/.test(t))";

        var scheme = "c=" + isDark + "?'" + JsString(dark) + "':'" + JsString(light) + "';";

        // Only what the fallback gets wrong is kept, and only that is ordered - the registry's own
        // views already enumerate in key order, so for them (the default) there is nothing to sort.
        var wrong = Colors.Where(entry => entry.Value != (IsDarkName(entry.Key) ? dark : light));
        if (Colors is not BitThemeSurfaceMap) wrong = wrong.OrderBy(entry => entry.Key, StringComparer.Ordinal);

        var overrides = wrong.Select(entry => "'" + JsString(entry.Key) + "':'" + JsString(entry.Value) + "'")
                             .ToArray();

        // Nothing the fallback gets wrong: the whole table would be dead weight in front of every
        // first paint. This is the default map's case, where all four names share the two colors.
        if (overrides.Length == 0) return "var " + scheme;

        // The typeof guard rather than a plain falsy test: t is whatever the client resolved, so a
        // name like 'constructor' or 'toString' reaches Object.prototype and hands back a function
        // the tag would then be painted with.
        return "var c={" + string.Join(',', overrides) + "}[t];" +
               "if(typeof c!=='string'){" + scheme + "}";
    }

    /// <summary>
    /// The colors come from an app-supplied map, so they are escaped for the single-quoted JS string
    /// they are interpolated into rather than trusted to be hex.
    /// </summary>
    private static string JsString(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\': builder.Append("\\\\"); break;
                case '\'': builder.Append("\\'"); break;
                case '<': builder.Append("\\u003c"); break;
                case '>': builder.Append("\\u003e"); break;
                case '&': builder.Append("\\u0026"); break;
                case '\r': builder.Append("\\r"); break;
                case '\n': builder.Append("\\n"); break;
                default: builder.Append(ch); break;
            }
        }
        return builder.ToString();
    }
}
