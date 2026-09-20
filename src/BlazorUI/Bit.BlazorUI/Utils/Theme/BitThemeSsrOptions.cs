namespace Bit.BlazorUI;

/// <summary>
/// The document-level theme setup a host page asks for, handed to
/// <see cref="BitThemeSsr.BuildRootThemeAttributeMap(string?, BitThemeSsrOptions)"/> (or
/// <see cref="BitThemeSsr.BuildRootThemeAttributes(string?, BitThemeSsrOptions)"/>) so every
/// <c>bit-theme*</c> attribute of the <c>&lt;html&gt;</c> element comes out of one call instead of
/// being spelled by hand beside it. Each property below is one attribute of
/// <see cref="BitThemeAttributeNames"/>; leaving one at its default omits that attribute, which is
/// how the feature is turned off.
/// </summary>
public class BitThemeSsrOptions
{
    /// <summary>
    /// The theme to paint when nothing is stored (<see cref="BitThemeAttributeNames.ThemeDefault"/>).
    /// A blank value, or <c>system</c>, leaves the document following the OS.
    /// </summary>
    public string? DefaultTheme { get; set; }

    /// <summary>
    /// Keep the visitor's choice in <c>localStorage</c>
    /// (<see cref="BitThemeAttributeNames.ThemePersist"/>).
    /// </summary>
    public bool Persist { get; set; }

    /// <summary>
    /// Mirror the choice into the <see cref="BitThemeCookie.PreferenceCookieName"/> cookie
    /// (<see cref="BitThemeAttributeNames.ThemePersistCookie"/>) - which is what lets the next
    /// server-rendered response paint that theme itself, since <c>localStorage</c> cannot be read
    /// from the server. A server-rendered app that persists at all wants this one too.
    /// </summary>
    public bool PersistCookie { get; set; }

    /// <summary>
    /// Cross-fade theme swaps through the View Transitions API
    /// (<see cref="BitThemeAttributeNames.ThemeViewTransition"/>).
    /// </summary>
    public bool ViewTransition { get; set; }

    /// <summary>
    /// Keep every <c>&lt;meta name="theme-color"&gt;</c> tag equal to the live page
    /// (<see cref="BitThemeAttributeNames.ThemeColorMeta"/>): <see langword="true"/> for the page
    /// background, or the name of the custom property to read instead (<c>--bit-clr-bg-sec</c> for an
    /// app whose pages sit on the secondary surface). <see langword="null"/> leaves it off.
    /// </summary>
    /// <remarks>
    /// This is the runtime half of the browser chrome; <see cref="BitThemeHead"/> emits the tag it
    /// keeps current, and reads its first-paint color from a
    /// <see cref="BitThemeSurfaces">surface table</see> - so an app that points this at another
    /// property hands that component the matching map.
    /// </remarks>
    public object? ThemeColorMeta { get; set; }

    /// <summary>
    /// The names the OS-following resolution picks between
    /// (<see cref="BitThemeAttributeNames.ThemeLight"/> /
    /// <see cref="BitThemeAttributeNames.ThemeDark"/>), for an app whose light and dark presets are
    /// not the core <c>light</c> / <c>dark</c> pair - a Fluent 2 site passes <c>fluent2-light</c> and
    /// <c>fluent2-dark</c>. Pass the same names to <see cref="BitThemeHead"/>.
    /// </summary>
    public string? LightTheme { get; set; }

    /// <inheritdoc cref="LightTheme"/>
    public string? DarkTheme { get; set; }
}
