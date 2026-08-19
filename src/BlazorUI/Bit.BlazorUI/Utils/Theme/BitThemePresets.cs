namespace Bit.BlazorUI;

/// <summary>
/// Well-known values for the <c>bit-theme</c> HTML attribute and for <see cref="BitThemeManager.SetThemeAsync"/>.
/// Fluent presets load colors from the packaged Fluent stylesheets; <see cref="BitTheme"/> overrides apply on top via inline CSS variables.
/// The Material and Cupertino presets additionally require linking the corresponding stylesheet
/// (<c>bit.blazorui.material.css</c> / <c>bit.blazorui.cupertino.css</c>) after <c>bit.blazorui.css</c>.
/// </summary>
public static class BitThemePresets
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string Fluent = "fluent";
    public const string FluentLight = "fluent-light";
    public const string FluentDark = "fluent-dark";
    public const string Material = "material";
    public const string MaterialLight = "material-light";
    public const string MaterialDark = "material-dark";
    public const string Cupertino = "cupertino";
    public const string CupertinoLight = "cupertino-light";
    public const string CupertinoDark = "cupertino-dark";
    public const string System = "system";
}
