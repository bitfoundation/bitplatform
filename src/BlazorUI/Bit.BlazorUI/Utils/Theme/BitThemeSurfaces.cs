namespace Bit.BlazorUI;

/// <summary>
/// The two page surfaces each packaged preset paints, as hex, for the one job that needs a color
/// before any stylesheet exists: the <c>&lt;meta name="theme-color"&gt;</c> tag of a server-rendered
/// host page, which the browser reads to paint its chrome (an installed PWA's status bar, the
/// address bar on mobile) at the very first paint. <see cref="BitThemeHead"/> is what normally reads
/// this; an app only touches it directly to emit the tag itself.
/// </summary>
/// <remarks>
/// <para>
/// This is the ONLY place the library restates a color the stylesheets already declare, and it is
/// restated because nothing else can be: the value is needed while the page is still being parsed,
/// before the CSS that carries it has loaded. Everywhere else - including
/// <see cref="BitThemeAttributeNames.ThemeColorMeta"/>, which takes over from the first frame the app
/// can compute anything - the color is read off the live page instead, so an app's own token
/// overrides and a picked accent are reflected without this table knowing about them.
/// </para>
/// <para>
/// Only the Fluent family the core stylesheet implements is here. The Fluent 2, Material and
/// Cupertino presets ship with Bit.BlazorUI.Extras, and so do their surfaces, on
/// <c>BitExtraThemeSurfaces</c> - which carries these entries too, so an app that references Extras
/// has one table for every packaged preset.
/// </para>
/// <para>
/// The values are pinned to the packaged palettes by a contract test that reads the stylesheets
/// themselves, so a re-generated palette cannot leave a stale color here.
/// </para>
/// </remarks>
public static class BitThemeSurfaces
{
    /// <summary>
    /// <c>--bit-clr-bg-pri</c> per preset: the page background, and the usual choice for the browser
    /// chrome. Keyed by the preset's <c>bit-theme</c> name (<see cref="BitThemePresets"/>), ordinal.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BackgroundPrimary { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // light / dark are the same two palettes under their family-less names: colors.fluent-light
        // and colors.fluent-dark each select both (:root[bit-theme="light"], [bit-theme="fluent-light"]).
        [BitThemePresets.Light] = "#FFFFFF",
        [BitThemePresets.Dark] = "#0F1318",
        [BitThemePresets.FluentLight] = "#FFFFFF",
        [BitThemePresets.FluentDark] = "#0F1318",
    };

    /// <summary>
    /// <c>--bit-clr-bg-sec</c> per preset: the surface a page sits its cards and panels ON, and the
    /// right choice for an app whose own pages are drawn on it. Keyed as
    /// <see cref="BackgroundPrimary"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BackgroundSecondary { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [BitThemePresets.Light] = "#F5F5F5",
        [BitThemePresets.Dark] = "#1B2025",
        [BitThemePresets.FluentLight] = "#F5F5F5",
        [BitThemePresets.FluentDark] = "#1B2025",
    };
}
