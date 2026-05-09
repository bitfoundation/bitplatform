namespace Bit.BlazorUI;

/// <summary>
/// Public helpers around <see cref="BitTheme"/> and the internal CSS-variable mapper.
/// </summary>
public static class BitThemeUtilities
{
    /// <summary>Maps a theme to CSS custom property names and values for use with <see cref="BitThemeManager.ApplyBitThemeAsync"/> or inline styles.</summary>
    public static IReadOnlyDictionary<string, string> ToCssVariables(BitTheme? bitTheme)
    {
        return BitThemeMapper.MapToCssVariables(bitTheme ?? new BitTheme());
    }

    /// <summary>Merges two themes: <paramref name="overrides"/> wins; missing values fall back to <paramref name="baseline"/>.</summary>
    public static BitTheme Merge(BitTheme? overrides, BitTheme? baseline)
    {
        return BitThemeMapper.Merge(overrides ?? new BitTheme(), baseline ?? new BitTheme());
    }
}
