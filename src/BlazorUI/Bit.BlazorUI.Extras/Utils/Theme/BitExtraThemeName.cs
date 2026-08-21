namespace Bit.BlazorUI;

/// <summary>
/// <see cref="BitThemeName"/> factories for the design-system presets that ship with this package, so the
/// Fluent 2, Material and Cupertino names slot into <see cref="BitThemeManager.SetThemeAsync(BitThemeName)"/>
/// the same way the core <see cref="BitThemeName.Fluent"/> family does.
/// </summary>
/// <remarks>
/// A separate type rather than more members on <see cref="BitThemeName"/>: that struct lives in the core
/// package, which knows nothing about these presets - their stylesheets, and therefore their names, belong
/// to this one. Each value is built through <see cref="BitThemeName.Custom(string)"/>, so it carries the
/// same normalization and validation as any other theme name. See <see cref="BitExtraThemePresets"/> for
/// the stylesheet each one requires.
/// </remarks>
public static class BitExtraThemeName
{
    /// <summary>Fluent 2 base preset (<c>"fluent2"</c>; requires <c>bit.blazorui.fluent2.css</c>).</summary>
    public static BitThemeName Fluent2 { get; } = BitThemeName.Custom(BitExtraThemePresets.Fluent2);

    /// <summary>Fluent 2 light preset (<c>"fluent2-light"</c>).</summary>
    public static BitThemeName Fluent2Light { get; } = BitThemeName.Custom(BitExtraThemePresets.Fluent2Light);

    /// <summary>Fluent 2 dark preset (<c>"fluent2-dark"</c>).</summary>
    public static BitThemeName Fluent2Dark { get; } = BitThemeName.Custom(BitExtraThemePresets.Fluent2Dark);

    /// <summary>Material base preset (<c>"material"</c>; requires <c>bit.blazorui.material.css</c>).</summary>
    public static BitThemeName Material { get; } = BitThemeName.Custom(BitExtraThemePresets.Material);

    /// <summary>Material light preset (<c>"material-light"</c>).</summary>
    public static BitThemeName MaterialLight { get; } = BitThemeName.Custom(BitExtraThemePresets.MaterialLight);

    /// <summary>Material dark preset (<c>"material-dark"</c>).</summary>
    public static BitThemeName MaterialDark { get; } = BitThemeName.Custom(BitExtraThemePresets.MaterialDark);

    /// <summary>Cupertino base preset (<c>"cupertino"</c>; requires <c>bit.blazorui.cupertino.css</c>).</summary>
    public static BitThemeName Cupertino { get; } = BitThemeName.Custom(BitExtraThemePresets.Cupertino);

    /// <summary>Cupertino light preset (<c>"cupertino-light"</c>).</summary>
    public static BitThemeName CupertinoLight { get; } = BitThemeName.Custom(BitExtraThemePresets.CupertinoLight);

    /// <summary>Cupertino dark preset (<c>"cupertino-dark"</c>).</summary>
    public static BitThemeName CupertinoDark { get; } = BitThemeName.Custom(BitExtraThemePresets.CupertinoDark);
}
