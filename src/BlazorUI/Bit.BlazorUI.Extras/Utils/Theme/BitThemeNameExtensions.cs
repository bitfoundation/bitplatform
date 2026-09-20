namespace Bit.BlazorUI;

/// <summary>
/// <see cref="BitThemeName"/> factories for the design-system presets that ship with this package,
/// added to <see cref="BitThemeName"/> itself: <c>BitThemeName.MaterialDark</c> slots into
/// <see cref="BitThemeManager.SetThemeAsync(BitThemeName)"/> exactly as
/// <see cref="BitThemeName.FluentDark"/> does, from the same type.
/// </summary>
/// <remarks>
/// This container is never named in code - the members are extension members on
/// <see cref="BitThemeName"/>, visible wherever <c>Bit.BlazorUI</c> is in scope. Each value is built
/// through <see cref="BitThemeName.Custom(string)"/>, so it carries the same normalization and
/// validation as any other theme name, and reading one registers this package's presets (see
/// <see cref="BitThemePresetsExtensions"/>). The stylesheet each one requires is documented there.
/// </remarks>
public static class BitThemeNameExtensions
{
    extension(BitThemeName)
    {
        /// <summary>Fluent 2 base preset (<c>"fluent2"</c>; requires <c>bit.blazorui.fluent2.css</c>).</summary>
        public static BitThemeName Fluent2 => BitExtraThemeNames.Fluent2;

        /// <summary>Fluent 2 light preset (<c>"fluent2-light"</c>).</summary>
        public static BitThemeName Fluent2Light => BitExtraThemeNames.Fluent2Light;

        /// <summary>Fluent 2 dark preset (<c>"fluent2-dark"</c>).</summary>
        public static BitThemeName Fluent2Dark => BitExtraThemeNames.Fluent2Dark;

        /// <summary>Material base preset (<c>"material"</c>; requires <c>bit.blazorui.material.css</c>).</summary>
        public static BitThemeName Material => BitExtraThemeNames.Material;

        /// <summary>Material light preset (<c>"material-light"</c>).</summary>
        public static BitThemeName MaterialLight => BitExtraThemeNames.MaterialLight;

        /// <summary>Material dark preset (<c>"material-dark"</c>).</summary>
        public static BitThemeName MaterialDark => BitExtraThemeNames.MaterialDark;

        /// <summary>Cupertino base preset (<c>"cupertino"</c>; requires <c>bit.blazorui.cupertino.css</c>).</summary>
        public static BitThemeName Cupertino => BitExtraThemeNames.Cupertino;

        /// <summary>Cupertino light preset (<c>"cupertino-light"</c>).</summary>
        public static BitThemeName CupertinoLight => BitExtraThemeNames.CupertinoLight;

        /// <summary>Cupertino dark preset (<c>"cupertino-dark"</c>).</summary>
        public static BitThemeName CupertinoDark => BitExtraThemeNames.CupertinoDark;
    }
}

/// <summary>
/// The wrapped names behind the extension members above, built once. An extension member has no
/// backing storage of its own, so the singletons live here.
/// </summary>
internal static class BitExtraThemeNames
{
    internal static BitThemeName Fluent2 { get; } = BitThemeName.Custom(BitExtraThemeTokens.Fluent2);
    internal static BitThemeName Fluent2Light { get; } = BitThemeName.Custom(BitExtraThemeTokens.Fluent2Light);
    internal static BitThemeName Fluent2Dark { get; } = BitThemeName.Custom(BitExtraThemeTokens.Fluent2Dark);
    internal static BitThemeName Material { get; } = BitThemeName.Custom(BitExtraThemeTokens.Material);
    internal static BitThemeName MaterialLight { get; } = BitThemeName.Custom(BitExtraThemeTokens.MaterialLight);
    internal static BitThemeName MaterialDark { get; } = BitThemeName.Custom(BitExtraThemeTokens.MaterialDark);
    internal static BitThemeName Cupertino { get; } = BitThemeName.Custom(BitExtraThemeTokens.Cupertino);
    internal static BitThemeName CupertinoLight { get; } = BitThemeName.Custom(BitExtraThemeTokens.CupertinoLight);
    internal static BitThemeName CupertinoDark { get; } = BitThemeName.Custom(BitExtraThemeTokens.CupertinoDark);
}
