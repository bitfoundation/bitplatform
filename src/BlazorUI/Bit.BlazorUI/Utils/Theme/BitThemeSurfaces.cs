using System.Collections;

namespace Bit.BlazorUI;

/// <summary>
/// The two page surfaces each registered preset paints, as hex, for the one job that needs a color
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
/// Both maps are live views over <see cref="BitThemePresetRegistry"/>, so they carry every preset
/// that has registered, whoever shipped it: the core Fluent family, the Fluent 2 / Material /
/// Cupertino presets Bit.BlazorUI.Extras registers when it loads, and an app's own. There is one
/// table for all of them, and a package that adds a design system does not have to publish a second.
/// </para>
/// <para>
/// The packaged values are pinned to the packaged palettes by a contract test that reads the
/// stylesheets themselves, so a re-generated palette cannot leave a stale color here.
/// </para>
/// </remarks>
public static class BitThemeSurfaces
{
    /// <summary>
    /// <c>--bit-clr-bg-pri</c> per preset: the page background, and the usual choice for the browser
    /// chrome. Keyed by the preset's <c>bit-theme</c> name (<see cref="BitThemePresets"/>), ordinal.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BackgroundPrimary { get; } = new BitThemeSurfaceMap(preset => preset.BackgroundPrimary);

    /// <summary>
    /// <c>--bit-clr-bg-sec</c> per preset: the surface a page sits its cards and panels ON, and the
    /// right choice for an app whose own pages are drawn on it. Keyed as
    /// <see cref="BackgroundPrimary"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BackgroundSecondary { get; } = new BitThemeSurfaceMap(preset => preset.BackgroundSecondary);
}

/// <summary>
/// One surface of every registered preset, read through <see cref="BitThemePresetRegistry"/> on each
/// access rather than copied out of it - a preset registered after an app has handed one of these to
/// <see cref="BitThemeHead"/> still reaches the rendered tag.
/// </summary>
internal sealed class BitThemeSurfaceMap(Func<BitThemePreset, string?> surface) : IReadOnlyDictionary<string, string>
{
    public string this[string key] => TryGetValue(key, out var color) ? color : throw new KeyNotFoundException($"No surface color is registered for the theme preset '{key}'.");

    public IEnumerable<string> Keys => BitThemePresetRegistry.SurfacesOf(surface).Select(entry => entry.Key);

    public IEnumerable<string> Values => BitThemePresetRegistry.SurfacesOf(surface).Select(entry => entry.Value);

    // Counted off the registry rather than through Enumerable.Count(this), which is free to answer
    // from this very property once it recognizes the IReadOnlyCollection it is handed.
    public int Count => BitThemePresetRegistry.SurfacesOf(surface).Count();

    public bool ContainsKey(string key) => BitThemePresetRegistry.SurfaceOf(key, surface) is not null;

    public bool TryGetValue(string key, out string value)
    {
        value = BitThemePresetRegistry.SurfaceOf(key, surface)!;

        return value is not null;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => BitThemePresetRegistry.SurfacesOf(surface).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
