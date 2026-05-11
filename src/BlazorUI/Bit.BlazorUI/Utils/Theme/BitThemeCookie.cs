namespace Bit.BlazorUI;

/// <summary>
/// Cookie name convention for persisting a theme preference on the server when using SSR or hybrid hosting.
/// Read this value in middleware or layout code and emit <see cref="BitThemeSsr"/> / <c>bit-theme</c> consistently with the client.
/// </summary>
public static class BitThemeCookie
{
    /// <summary>Suggested cookie name for the abstract theme key (e.g. <c>system</c>, <c>dark</c>, <c>fluent-light</c>).</summary>
    public const string PreferenceCookieName = "bit-theme-preference";
}
