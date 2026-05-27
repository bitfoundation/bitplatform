using System.Runtime.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// Public helpers around <see cref="BitTheme"/> and the internal CSS-variable mapper.
/// </summary>
public static class BitThemeUtilities
{
    // Reuse the same dictionary for the same BitTheme instance. The mapper produces ~280 entries
    // per call; without caching, every <see cref="BitThemeManager.ApplyBitThemeAsync"/> call
    // serializes a fresh copy across the JS boundary even when the theme reference hasn't changed.
    // ConditionalWeakTable holds weak references so the cache cannot keep a theme alive.
    private static readonly ConditionalWeakTable<BitTheme, IReadOnlyDictionary<string, string>> _cache = new();

    /// <summary>Maps a theme to CSS custom property names and values for use with <see cref="BitThemeManager.ApplyBitThemeAsync"/> or inline styles.</summary>
    public static IReadOnlyDictionary<string, string> ToCssVariables(BitTheme? bitTheme)
    {
        // The default-instance fallback is cheap (the empty dictionary is tiny), but plumbing it
        // through the cache would tie the cache to a transient new BitTheme() that's discarded
        // immediately. Just compute and return for the null/default path.
        if (bitTheme is null)
        {
            return BitThemeMapper.MapToCssVariables(new BitTheme());
        }

        return _cache.GetValue(bitTheme, t => BitThemeMapper.MapToCssVariables(t));
    }

    /// <summary>Merges two themes: <paramref name="overrides"/> wins; missing values fall back to <paramref name="baseline"/>.</summary>
    public static BitTheme Merge(BitTheme? overrides, BitTheme? baseline)
    {
        return BitThemeMapper.Merge(overrides ?? new BitTheme(), baseline ?? new BitTheme());
    }
}
