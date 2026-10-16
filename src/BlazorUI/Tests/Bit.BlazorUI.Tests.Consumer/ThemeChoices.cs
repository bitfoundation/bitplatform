using Bit.BlazorUI;

namespace Bit.BlazorUI.Tests.Consumer;

/// <summary>
/// The theming calls an app makes, written the way C# 12 has to write them: the plain BitExtra* types stand in for the
/// extension members of BitThemePresets and BitThemeName, and a preset name is a constant a case label can use.
/// </summary>
public static class ThemeChoices
{
    public static string Describe(string? theme)
    {
        switch (theme)
        {
            case BitThemePresets.Light:
            case BitThemePresets.FluentLight:
            case BitExtraThemePresets.Fluent2Light:
            case BitExtraThemePresets.MaterialLight:
            case BitExtraThemePresets.CupertinoLight:
                return "light";
            case BitThemePresets.Dark:
            case BitExtraThemePresets.Fluent2Dark:
            case BitExtraThemePresets.MaterialDark:
            case BitExtraThemePresets.CupertinoDark:
                return "dark";
            default:
                return "system";
        }
    }

    public static IReadOnlyList<BitThemeName> Presets { get; } =
    [
        BitExtraThemeName.Fluent2,
        BitExtraThemeName.Material,
        BitExtraThemeName.Cupertino,
    ];

    public static ValueTask<string?> UseMaterialAsync(BitThemeManager themeManager) => themeManager.SetThemeAsync(BitExtraThemeName.Material);

    public static ValueTask<string?> UseDarkAsync(BitThemeManager themeManager) => themeManager.SetThemeAsync(BitThemePresets.Dark);
}
