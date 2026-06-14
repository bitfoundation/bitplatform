namespace Bit.BlazorUI;

/// <summary>
/// Cookie name convention for persisting a theme preference on the server when using SSR or hybrid hosting.
/// Read this value in middleware or layout code and pass it to <see cref="BitThemeSsr.BuildRootThemeAttributes(string?, string?)"/>
/// to emit the matching <c>bit-theme</c> attributes on the root <c>&lt;html&gt;</c> element, consistently with the client.
/// </summary>
/// <remarks>
/// The app can either write this cookie itself (client- or server-side) when the user picks a
/// theme, or opt the client script into writing it automatically by adding the
/// <see cref="BitThemeAttributeNames.ThemePersistCookie"/> (<c>bit-theme-persist-cookie</c>)
/// attribute to the root <c>&lt;html&gt;</c> element — in which case the client mirrors every theme
/// change into this cookie so the server stays in sync.
/// <see cref="BitThemeSsr.BuildRootThemeAttributes(string?, string?)"/> is the read/resolve half:
/// it turns the stored value into validated, injection-safe root attributes.
/// </remarks>
public static class BitThemeCookie
{
    /// <summary>Suggested cookie name for the abstract theme key (e.g. <c>system</c>, <c>dark</c>, <c>fluent-light</c>).</summary>
    public const string PreferenceCookieName = "bit-theme-preference";
}
