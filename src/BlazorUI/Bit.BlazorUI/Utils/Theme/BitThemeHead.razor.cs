namespace Bit.BlazorUI;

/// <summary>
/// The single-drop first-paint setup for the theme: place it at the very start of a server-rendered
/// host page's <c>&lt;head&gt;</c> (before any stylesheet) and it emits both halves of getting the
/// appearance right before anything is painted - the inline script that re-resolves
/// <c>bit-theme</c> from <c>localStorage</c> / the preference cookie (<see cref="BitThemeSsr"/>), and
/// the <c>&lt;meta name="theme-color"&gt;</c> tag that paints the browser chrome, with the color of
/// the theme this response actually renders.
/// </summary>
/// <remarks>
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
/// defaulting to the packaged Fluent surfaces. An app on the Fluent 2 / Material / Cupertino presets
/// passes <c>BitExtraThemeSurfaces.BackgroundPrimary</c> (or <c>.BackgroundSecondary</c>), and an app
/// with its own palette passes its own map. Set <see cref="EmitThemeColor"/> to <see langword="false"/>
/// to keep a hand-written tag (or a media-qualified pair) instead and have this emit only the script.
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
    /// <see cref="BitThemeSurfaces.BackgroundPrimary"/> - the page background of the packaged Fluent
    /// presets. <c>BitExtraThemeSurfaces</c> has the same two maps covering every packaged preset, and
    /// an app with its own palette (or one whose pages sit on the secondary surface) passes its own.
    /// A name the map does not carry falls back to the light / dark entry by the same "ends with dark"
    /// rule the packaged stylesheets classify names with.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, string>? ThemeColors { get; set; }

    /// <summary>
    /// Whether to emit the <c>&lt;meta name="theme-color"&gt;</c> tag (and, for an OS-following
    /// visitor, the script that corrects it before first paint). Default <see langword="true"/>. Turn
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
    /// The theme this response renders, or <see langword="null"/> when the visitor follows the OS -
    /// the case the correction script covers. Resolved exactly as the root attributes are, so the tag
    /// and the document can never disagree about which theme was painted.
    /// </summary>
    private string? ResolvedTheme =>
        BitThemeSsr.BuildRootThemeAttributeMap(PersistedPreference, new BitThemeSsrOptions { DefaultTheme = DefaultTheme })
                   .TryGetValue(BitThemeAttributeNames.Theme, out var theme)
        ? theme as string
        : null;

    private IReadOnlyDictionary<string, string> Colors => ThemeColors ?? BitThemeSurfaces.BackgroundPrimary;

    private string ResolveLightThemeColor() => ColorOf(LightTheme ?? BitThemePresets.Light, isDark: false);

    private string ResolveDarkThemeColor() => ColorOf(DarkTheme ?? BitThemePresets.Dark, isDark: true);

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

        var fallback = isDark ? DarkTheme ?? BitThemePresets.Dark : LightTheme ?? BitThemePresets.Light;
        if (fallback != theme && Colors.TryGetValue(fallback, out color)) return color;

        // Nothing in the map for either name: the packaged surfaces of the scheme, which is what the
        // tag would have carried before an app handed in a map of its own.
        return isDark
            ? BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Dark]
            : BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Light];
    }

    /// <summary>
    /// "Ends with dark" (ordinal) - the same classification the packaged stylesheets, the accent's
    /// generated first-paint CSS and the runtime client all apply, so every layer puts a given name
    /// on the same side of light / dark.
    /// </summary>
    private static bool IsDarkName(string theme) => theme.EndsWith("dark", StringComparison.Ordinal);

    /// <summary>
    /// Reads back the attribute the inline script resolved and rewrites the tag to match, while the
    /// browser is still parsing head. Only the two colors reach the document, both from the map.
    /// </summary>
    private string BuildThemeColorCorrectionScript()
    {
        var body =
            "(function(){var t=document.documentElement.getAttribute('" + BitThemeAttributeNames.Theme + "')||'';" +
            "var m=document.querySelector('meta[name=theme-color]');" +
            "if(m){m.content=/dark$/.test(t)?'" + JsString(ResolveDarkThemeColor()) + "':'" + JsString(ResolveLightThemeColor()) + "';}})();";

        return string.IsNullOrWhiteSpace(Nonce)
            ? $"<script>{body}</script>"
            : $"<script nonce=\"{BitThemeSsr.HtmlEncodeAttribute(Nonce)}\">{body}</script>";
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
