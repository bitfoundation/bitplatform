namespace Bit.BlazorUI;

/// <summary>
/// Public helpers around <see cref="BitTheme"/> and the internal CSS-variable mapper.
/// </summary>
public static class BitThemeUtilities
{
    /// <summary>
    /// Maps a theme to CSS custom property names and values for use with <see cref="BitThemeManager.ApplyBitThemeAsync"/> or inline styles.
    /// </summary>
    /// <remarks>
    /// The result is recomputed on every call. <see cref="BitTheme"/> is mutable, so caching by
    /// instance would return stale values when callers mutate a theme between calls. The mapper
    /// builds a ~280-entry dictionary of strings; for the typical call path through
    /// <see cref="BitThemeManager.ApplyBitThemeAsync"/> this cost is dwarfed by the JS-interop
    /// boundary. If you have a hot, allocation-sensitive path, hold the result yourself and pass
    /// the same reference until you intentionally rebuild it.
    /// <para>
    /// Token values that contain characters capable of escaping a single CSS declaration (for
    /// example <c>;</c>, <c>{</c>, <c>}</c>, <c>&lt;</c>, <c>&gt;</c>, or comment markers) are
    /// dropped rather than emitted. Theme values can originate from untrusted sources (see
    /// <see cref="BitThemeSerialization"/>), and the output is concatenated into an inline
    /// <c>style</c> attribute by <see cref="BitThemeProvider"/>; dropping injection-prone values
    /// prevents a malicious token from adding extra CSS. A dropped token falls back to the
    /// stylesheet default.
    /// </para>
    /// </remarks>
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
