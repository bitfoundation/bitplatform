namespace Bit.BlazorUI;

/// <summary>
/// <see cref="BitThemeSurfaces"/> for every packaged preset: the Fluent 2, Material and Cupertino
/// surfaces this package's stylesheets declare, plus the core Fluent ones, so an app that references
/// Bit.BlazorUI.Extras has a single table to hand <see cref="BitThemeHead"/> - whichever design
/// system its visitors end up on.
/// </summary>
/// <remarks>
/// Same narrow purpose as the core table: the first-paint <c>&lt;meta name="theme-color"&gt;</c>
/// value of a server-rendered host page, which has to exist before any stylesheet has loaded. The
/// values are pinned to the packaged palettes by a contract test that reads the stylesheets.
/// </remarks>
public static class BitExtraThemeSurfaces
{
    /// <summary><c>--bit-clr-bg-pri</c> (the page background) per preset, ordinal by <c>bit-theme</c> name.</summary>
    public static IReadOnlyDictionary<string, string> BackgroundPrimary { get; } = Extend(BitThemeSurfaces.BackgroundPrimary, new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [BitExtraThemePresets.Fluent2Light] = "#FFFFFF",
        [BitExtraThemePresets.Fluent2Dark] = "#131313",
        [BitExtraThemePresets.MaterialLight] = "#FFFFFF",
        [BitExtraThemePresets.MaterialDark] = "#0C131B",
        [BitExtraThemePresets.CupertinoLight] = "#FFFFFF",
        [BitExtraThemePresets.CupertinoDark] = "#131313",
    });

    /// <summary><c>--bit-clr-bg-sec</c> (the surface cards and panels sit on) per preset, ordinal by <c>bit-theme</c> name.</summary>
    public static IReadOnlyDictionary<string, string> BackgroundSecondary { get; } = Extend(BitThemeSurfaces.BackgroundSecondary, new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [BitExtraThemePresets.Fluent2Light] = "#F5F5F5",
        [BitExtraThemePresets.Fluent2Dark] = "#1F1F1F",
        [BitExtraThemePresets.MaterialLight] = "#F1F6FA",
        [BitExtraThemePresets.MaterialDark] = "#182029",
        [BitExtraThemePresets.CupertinoLight] = "#F5F5F5",
        [BitExtraThemePresets.CupertinoDark] = "#202020",
    });

    private static IReadOnlyDictionary<string, string> Extend(IReadOnlyDictionary<string, string> core, Dictionary<string, string> extras)
    {
        // The core entries first, so this package's own presets are the ones that would win a name
        // collision - which cannot happen today (the two name sets are disjoint) and stays harmless
        // if a future core preset were re-skinned here.
        var merged = new Dictionary<string, string>(core.Count + extras.Count, StringComparer.Ordinal);
        foreach (var (key, value) in core)
        {
            merged[key] = value;
        }
        foreach (var (key, value) in extras)
        {
            merged[key] = value;
        }

        return merged;
    }
}
