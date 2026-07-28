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

    /// <summary>
    /// Returns a CSS <c>color-mix()</c> expression that renders <paramref name="cssColor"/> at
    /// <paramref name="opacity"/> - the way to composite translucency from theme tokens without
    /// per-alpha token variants or channel variables.
    /// </summary>
    /// <param name="cssColor">
    /// Any CSS color expression: a token reference (<c>var(--bit-clr-pri)</c>), a hex literal,
    /// <c>currentcolor</c>, a system color, or even another <c>color-mix()</c>. Surrounding
    /// whitespace is trimmed.
    /// </param>
    /// <param name="opacity">The opacity to keep, from 0 (fully transparent) to 1 (unchanged).</param>
    /// <remarks>
    /// <para>
    /// Mixing with <c>transparent</c> in sRGB scales only the alpha channel, so
    /// <c>WithAlpha("#1276C6", 0.2)</c> renders identically to <c>rgba(18, 118, 198, 0.2)</c> -
    /// but unlike a precomputed rgba value it also works when the input is a <c>var()</c>
    /// reference whose concrete color is only known in the browser. The returned expression is a
    /// valid theme token value: it passes the mapper's injection screening, so it can be assigned
    /// to any <see cref="BitTheme"/> color slot (e.g. a brand-tinted overlay or shadow color).
    /// <c>color-mix()</c> is Baseline across evergreen browsers since mid-2023.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="cssColor"/> is null, blank, or contains characters that could escape a CSS
    /// declaration (the same screening <see cref="ToCssVariables"/> applies on emission).
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="opacity"/> is NaN or outside [0, 1].</exception>
    public static string WithAlpha(string cssColor, double opacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cssColor);

        if (double.IsNaN(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity), opacity, "Opacity must be between 0 and 1.");
        }

        var trimmed = cssColor.Trim();

        // Reject early with a clear error instead of letting the composed expression be silently
        // dropped later by the mapper's emission screening.
        if (BitThemeMapper.IsUnsafeCssTokenValue(trimmed))
        {
            throw new ArgumentException(
                $"'{cssColor}' contains characters that are not valid inside a CSS color expression.",
                nameof(cssColor));
        }

        // Invariant formatting: "0.####" never emits a decimal comma and trims trailing zeros
        // (0.125 → "12.5%", 0.2 → "20%").
        return FormattableString.Invariant($"color-mix(in srgb, {trimmed} {opacity * 100:0.####}%, transparent)");
    }
}
