namespace Bit.BlazorUI;

/// <summary>
/// <see cref="BitThemeName"/> factories for the design-system presets that ship with this package.
/// <see cref="BitThemeName"/> itself carries the same nine names as extension members, and that is
/// the form to reach for - <c>BitThemeName.MaterialDark</c> next to
/// <see cref="BitThemeName.FluentDark"/>. This type is what those members are built from, and it
/// stays public for a consumer that cannot read them: extension members need C# 14 on the READING
/// side too, which a <c>net8.0</c> or <c>net9.0</c> app - the two other frameworks this package
/// targets - does not compile at by default. See <see cref="BitExtraThemePresets"/> for the whole of
/// that story and for the stylesheet each preset requires.
/// </summary>
/// <remarks>
/// Each value is built through <see cref="BitThemeName.Custom(string)"/>, so it carries the same
/// normalization and validation as any other theme name. Unlike the <c>const</c>s on
/// <see cref="BitExtraThemePresets"/>, reading one of these is a real read of this assembly, so it
/// also registers this package's first-paint surfaces (see <see cref="BitExtraThemeRegistration"/>).
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
