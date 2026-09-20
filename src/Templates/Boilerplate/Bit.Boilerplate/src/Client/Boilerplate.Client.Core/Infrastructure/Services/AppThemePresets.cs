namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// The design system this app was created with, in the one place every head reads it from.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Light"/> / <see cref="Dark"/> are the bit BlazorUI presets the host pages pin as
/// <c>bit-theme-light</c> / <c>bit-theme-dark</c>, and <see cref="Background"/> is the page background the
/// native chrome is painted with - the Windows caption, the Android status bar, and every WebView before it
/// has painted anything. Those are not part of the document, so no CSS variable can reach them and the color
/// has to exist in C# before the app starts.
/// </para>
/// <para>
/// Customize freely - point these at another preset, or return your own <c>#RRGGBB</c> literals. What
/// <see cref="Background"/> returns has to stay equal to the <c>--bit-clr-bg-pri</c> the WebView then paints
/// with, or the native chrome and the page meet at a visible seam on launch and on every theme switch.
/// </para>
/// </remarks>
public static class AppThemePresets
{
    public const string Light = BitExtraThemePresets.Fluent2Light;

    public const string Dark = BitExtraThemePresets.Fluent2Dark;

    /// <summary><c>--bit-clr-bg-pri</c> of <see cref="Light"/>, as the packaged stylesheet defines it.</summary>
    public static string LightBackground => BitExtraThemeSurfaces.BackgroundPrimary[Light];

    /// <summary><c>--bit-clr-bg-pri</c> of <see cref="Dark"/>, as the packaged stylesheet defines it.</summary>
    public static string DarkBackground => BitExtraThemeSurfaces.BackgroundPrimary[Dark];

    /// <summary>The page background of the theme the app is currently on.</summary>
    public static string Background(bool isDark) => isDark ? DarkBackground : LightBackground;
}
