namespace Bit.BlazorUI;

/// <summary>
/// Well-known values for the <c>bit-theme</c> HTML attribute and for <see cref="BitThemeManager.SetThemeAsync"/>.
/// Fluent presets load colors from the packaged Fluent stylesheets; <see cref="BitTheme"/> overrides apply on top via inline CSS variables.
/// </summary>
/// <remarks>
/// The constants below are the presets the core stylesheet itself implements. A package that ships
/// more - Bit.BlazorUI.Extras and its Fluent 2, Material and Cupertino bundles - adds ITS names to
/// this same class as extension members, so <c>BitThemePresets.MaterialDark</c> reads the same as
/// <see cref="FluentDark"/> and an app never reaches for a second, package-specific table. The
/// difference is one of form, not of standing: a name contributed from another assembly is a static
/// property rather than a <c>const</c>, so it cannot be a <c>case</c> label or an attribute argument.
/// See <see cref="BitThemePresetRegistry"/> for the other half - what a package declares ABOUT its
/// presets - and do the same for an app's own preset.
/// </remarks>
public static class BitThemePresets
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string Fluent = "fluent";
    public const string FluentLight = "fluent-light";
    public const string FluentDark = "fluent-dark";
    public const string System = "system";
}
