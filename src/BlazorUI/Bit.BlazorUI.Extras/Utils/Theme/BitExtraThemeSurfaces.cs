namespace Bit.BlazorUI;

/// <summary>
/// <see cref="BitThemeSurfaces"/>, reached through this package - the same two live tables, and the
/// guarantee that this package's Fluent 2, Material and Cupertino presets are in them by the time
/// they are read.
/// </summary>
/// <remarks>
/// <para>
/// The tables themselves belong to the core package and carry every preset registered with
/// <see cref="BitThemePresetRegistry"/> - the core Fluent family, this package's presets, and an
/// app's own. What this type adds is WHEN: reading it is a read of this assembly, which is what runs
/// <see cref="BitExtraThemeRegistration"/> and puts this package's surfaces in. A host page that
/// names <see cref="BitThemeSurfaces"/> directly, in a process where nothing has touched this package
/// yet, is served the core entries alone and paints a packaged dark preset with the core dark surface
/// instead of its own - and then paints it correctly on a later request, once something has loaded
/// the assembly. Naming this type, or calling <see cref="BitExtraThemeRegistration.Register"/> at
/// startup (<c>AddBitBlazorUIExtrasServices</c> already does), is what makes the two requests agree.
/// </para>
/// <para>
/// The narrow purpose is the core table's: the first-paint <c>&lt;meta name="theme-color"&gt;</c>
/// value of a server-rendered host page, which has to exist before any stylesheet has loaded. The
/// values are pinned to the packaged palettes by a contract test that reads the stylesheets.
/// </para>
/// </remarks>
public static class BitExtraThemeSurfaces
{
    /// <summary><c>--bit-clr-bg-pri</c> (the page background) per preset, ordinal by <c>bit-theme</c> name.</summary>
    public static IReadOnlyDictionary<string, string> BackgroundPrimary => BitThemeSurfaces.BackgroundPrimary;

    /// <summary><c>--bit-clr-bg-sec</c> (the surface cards and panels sit on) per preset, ordinal by <c>bit-theme</c> name.</summary>
    public static IReadOnlyDictionary<string, string> BackgroundSecondary => BitThemeSurfaces.BackgroundSecondary;
}
