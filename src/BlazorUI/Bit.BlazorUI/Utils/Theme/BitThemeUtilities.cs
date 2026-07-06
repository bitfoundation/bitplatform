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
    /// <para>
    /// The passed <paramref name="bitTheme"/> is not mutated: a hand-constructed sparse theme (e.g.
    /// <c>new BitTheme { Color = null }</c>) is normalized onto an internal copy before mapping, so
    /// any deliberate <see langword="null"/> branches on the original instance are preserved.
    /// </para>
    /// </remarks>
    public static IReadOnlyDictionary<string, string> ToCssVariables(BitTheme? bitTheme)
    {
        return BitThemeMapper.MapToCssVariables(bitTheme ?? new BitTheme());
    }

    /// <summary>Merges two themes: <paramref name="overrides"/> wins; missing values fall back to <paramref name="baseline"/>.</summary>
    /// <remarks>
    /// Returns a fresh <see cref="BitTheme"/>; the inputs are neither used as the result nor mutated.
    /// A hand-constructed sparse input (with <see langword="null"/> branch objects) is normalized
    /// onto internal copies before merging, so deliberate null branches on the passed instances are
    /// preserved.
    /// </remarks>
    public static BitTheme Merge(BitTheme? overrides, BitTheme? baseline)
    {
        return BitThemeMapper.Merge(overrides ?? new BitTheme(), baseline ?? new BitTheme());
    }
}
