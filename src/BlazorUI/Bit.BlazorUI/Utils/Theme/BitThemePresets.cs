namespace Bit.BlazorUI;

/// <summary>
/// Well-known values for the <c>bit-theme</c> HTML attribute and for <see cref="BitThemeManager.SetThemeAsync"/>.
/// Fluent presets load colors from the packaged Fluent stylesheets; <see cref="BitTheme"/> overrides apply on top via inline CSS variables.
/// The Material and Cupertino design systems ship with the <c>Bit.BlazorUI.Extras</c> package - their names live on
/// <c>BitExtraThemePresets</c> there, next to the stylesheet bundles that implement them.
/// </summary>
public static class BitThemePresets
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string Fluent = "fluent";
    public const string FluentLight = "fluent-light";
    public const string FluentDark = "fluent-dark";
    public const string System = "system";
}
