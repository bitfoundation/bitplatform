namespace Bit.BlazorUI;

/// <summary>
/// <see cref="BitThemeName"/> factories for the design-system presets that ship with this package,
/// added to <see cref="BitThemeName"/> itself: <c>BitThemeName.MaterialDark</c> slots into
/// <see cref="BitThemeManager.SetThemeAsync(BitThemeName)"/> exactly as
/// <see cref="BitThemeName.FluentDark"/> does, from the same type.
/// </summary>
/// <remarks>
/// This container is never named in code - the members are extension members on
/// <see cref="BitThemeName"/>, visible wherever <c>Bit.BlazorUI</c> is in scope. Each returns the
/// matching <see cref="BitExtraThemeName"/> value, built through
/// <see cref="BitThemeName.Custom(string)"/>, so it carries the same normalization and validation as
/// any other theme name, and reading one registers this package's presets (see
/// <see cref="BitThemePresetsExtensions"/>). The stylesheet each one requires is documented there,
/// as is the one case that has to name <see cref="BitExtraThemeName"/> itself: a consumer compiling
/// at a language version older than C# 14 cannot read an extension member.
/// </remarks>
public static class BitThemeNameExtensions
{
    extension(BitThemeName)
    {
        /// <summary>Fluent 2 base preset (<c>"fluent2"</c>; requires <c>bit.blazorui.fluent2.css</c>).</summary>
        public static BitThemeName Fluent2 => BitExtraThemeName.Fluent2;

        /// <summary>Fluent 2 light preset (<c>"fluent2-light"</c>).</summary>
        public static BitThemeName Fluent2Light => BitExtraThemeName.Fluent2Light;

        /// <summary>Fluent 2 dark preset (<c>"fluent2-dark"</c>).</summary>
        public static BitThemeName Fluent2Dark => BitExtraThemeName.Fluent2Dark;

        /// <summary>Material base preset (<c>"material"</c>; requires <c>bit.blazorui.material.css</c>).</summary>
        public static BitThemeName Material => BitExtraThemeName.Material;

        /// <summary>Material light preset (<c>"material-light"</c>).</summary>
        public static BitThemeName MaterialLight => BitExtraThemeName.MaterialLight;

        /// <summary>Material dark preset (<c>"material-dark"</c>).</summary>
        public static BitThemeName MaterialDark => BitExtraThemeName.MaterialDark;

        /// <summary>Cupertino base preset (<c>"cupertino"</c>; requires <c>bit.blazorui.cupertino.css</c>).</summary>
        public static BitThemeName Cupertino => BitExtraThemeName.Cupertino;

        /// <summary>Cupertino light preset (<c>"cupertino-light"</c>).</summary>
        public static BitThemeName CupertinoLight => BitExtraThemeName.CupertinoLight;

        /// <summary>Cupertino dark preset (<c>"cupertino-dark"</c>).</summary>
        public static BitThemeName CupertinoDark => BitExtraThemeName.CupertinoDark;
    }
}
