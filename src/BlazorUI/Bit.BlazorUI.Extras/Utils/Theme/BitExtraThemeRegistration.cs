using System.Runtime.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// Declares this package's presets to <see cref="BitThemePresetRegistry"/> - the surfaces a host page
/// paints the browser chrome with before any stylesheet has loaded - so that
/// <see cref="BitThemeSurfaces"/> carries the Fluent 2, Material and Cupertino entries alongside the
/// core Fluent ones, from the one table an app already reads.
/// </summary>
/// <remarks>
/// <para>
/// The runtime runs <see cref="Register"/> before the first access to anything in this assembly, so
/// an app that uses this package at all - <c>AddBitBlazorUIExtrasServices</c>, a component,
/// <see cref="BitThemeSwitcher"/>, <c>BitThemePresets.MaterialDark</c>,
/// <c>BitThemeName.MaterialDark</c> - has the presets registered by the time it can ask about them.
/// An app that references the package only for its stylesheet and writes the <c>bit-theme</c> token
/// as a bare string never loads the assembly; the first-paint color then falls back to the scheme's
/// light / dark surface, and the app is otherwise unaffected, the preset being CSS. Calling
/// <see cref="Register"/> from <c>Program.cs</c> covers that case explicitly - it is public and
/// idempotent for exactly that.
/// </para>
/// <para>
/// <b>It never overrides the app.</b> The presets go in with
/// <see cref="BitThemePresetRegistry.TryRegister(IEnumerable{BitThemePreset})"/>, which fills gaps
/// only: a name the app has registered itself (a packaged preset it re-skinned in its own CSS) keeps
/// the app's colors, and a name the app has taken out with
/// <see cref="BitThemePresetRegistry.Remove(string?)"/> stays out - whether the app spoke before this
/// assembly loaded or after. That matters precisely because WHEN this runs is not the app's to see.
/// </para>
/// <para>
/// The colors are pinned to this package's palettes by a contract test that reads the stylesheets
/// themselves, so a re-generated palette cannot leave a stale color here.
/// </para>
/// </remarks>
public static class BitExtraThemeRegistration
{
    /// <summary>
    /// Registers this package's presets. Runs automatically when the assembly loads; safe to call
    /// again, and safe to call from several threads.
    /// </summary>
    // CA2255 says a library should not use a module initializer, because the consumer cannot see
    // when it runs. That is the point here: what runs is one idempotent dictionary fill with no
    // ordering requirement of its own, and running it at load is what lets naming a preset be all an
    // app has to do. The method is public and callable for the case the attribute does not cover -
    // an app that links a bundle but never touches this assembly - which is the alternative CA2255
    // would push us to, offered rather than imposed.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Register()
    {
        // The base alias of each family is its own entry, carrying the light palette's surfaces:
        // colors.<family>-light selects both names (:root[bit-theme="material"],
        // [bit-theme="material-light"]), so a page set to the bare family name and left out of this
        // table would paint its first frame in the Fluent fallback and then repaint.
        BitThemePresetRegistry.TryRegister(
        [
            new() { Name = BitExtraThemePresets.Fluent2, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F5F5F5" },
            new() { Name = BitExtraThemePresets.Fluent2Light, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F5F5F5" },
            new() { Name = BitExtraThemePresets.Fluent2Dark, BackgroundPrimary = "#131313", BackgroundSecondary = "#1F1F1F" },
            new() { Name = BitExtraThemePresets.Material, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F1F6FA" },
            new() { Name = BitExtraThemePresets.MaterialLight, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F1F6FA" },
            new() { Name = BitExtraThemePresets.MaterialDark, BackgroundPrimary = "#0C131B", BackgroundSecondary = "#182029" },
            new() { Name = BitExtraThemePresets.Cupertino, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F5F5F5" },
            new() { Name = BitExtraThemePresets.CupertinoLight, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F5F5F5" },
            new() { Name = BitExtraThemePresets.CupertinoDark, BackgroundPrimary = "#131313", BackgroundSecondary = "#202020" },
        ]);
    }
}
