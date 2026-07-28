namespace Bit.BlazorUI;

/// <summary>
/// The scheme a color role is derived for. Step directions differ between the two: on a light
/// scheme tints mix toward white and disabled colors are pale; on a dark scheme the accent sits
/// on a dark surface, so interactive states dim toward black and disabled colors are deep,
/// desaturated shades - matching the packaged Fluent light/dark palettes.
/// </summary>
public enum BitThemeColorScheme
{
    Light,
    Dark,
}
