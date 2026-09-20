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
/// an app that uses this package at all - a component, <see cref="BitThemeSwitcher"/>,
/// <c>BitThemePresets.MaterialDark</c>, <c>BitThemeName.MaterialDark</c> - has the presets registered
/// by the time it can ask about them. An app that references the package only for its stylesheet and
/// writes the <c>bit-theme</c> token as a bare string never loads the assembly; the first-paint color
/// then falls back to the scheme's light / dark surface, and the app is otherwise unaffected, the
/// preset being CSS. Calling <see cref="Register"/> from <c>Program.cs</c> covers that case
/// explicitly - it is public and idempotent for exactly that.
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
    [ModuleInitializer]
    public static void Register()
    {
        BitThemePresetRegistry.Register(
        [
            new() { Name = BitExtraThemeTokens.Fluent2Light, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F5F5F5" },
            new() { Name = BitExtraThemeTokens.Fluent2Dark, BackgroundPrimary = "#131313", BackgroundSecondary = "#1F1F1F" },
            new() { Name = BitExtraThemeTokens.MaterialLight, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F1F6FA" },
            new() { Name = BitExtraThemeTokens.MaterialDark, BackgroundPrimary = "#0C131B", BackgroundSecondary = "#182029" },
            new() { Name = BitExtraThemeTokens.CupertinoLight, BackgroundPrimary = "#FFFFFF", BackgroundSecondary = "#F5F5F5" },
            new() { Name = BitExtraThemeTokens.CupertinoDark, BackgroundPrimary = "#131313", BackgroundSecondary = "#202020" },
        ]);
    }
}
