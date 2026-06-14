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

    /// <summary><c>localStorage</c> key used by the client script when persistence is enabled.</summary>
    public const string ThemeStorageKey = "bit-current-theme";
}
