using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// WCAG 2.x contrast helpers for pairs of solid hex colors (sRGB).
/// </summary>
public static partial class BitThemeColorContrast
{
    // Accepts #RGB or #RRGGBB (case-insensitive). The underlying BitInternalColor parser silently
    // falls back to white on invalid input, which would produce a misleading ratio — so we gate
    // GetContrastRatio with this stricter check before constructing BitInternalColor.
    [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexColorRegex();

    /// <summary>Returns the contrast ratio in [1, 21].</summary>
    /// <exception cref="ArgumentException">
    /// When either input is null, empty, or whitespace, or is not a <c>#RGB</c>/<c>#RRGGBB</c> hex string.
    /// </exception>
    public static double GetContrastRatio(string foregroundHex, string backgroundHex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(foregroundHex);
        ArgumentException.ThrowIfNullOrWhiteSpace(backgroundHex);

        var fgTrimmed = foregroundHex.Trim();
        var bgTrimmed = backgroundHex.Trim();

        if (HexColorRegex().IsMatch(fgTrimmed) is false)
        {
            throw new ArgumentException(
                $"'{foregroundHex}' is not a valid hex color. Expected '#RGB' or '#RRGGBB'.",
                nameof(foregroundHex));
        }

        if (HexColorRegex().IsMatch(bgTrimmed) is false)
        {
            throw new ArgumentException(
                $"'{backgroundHex}' is not a valid hex color. Expected '#RGB' or '#RRGGBB'.",
                nameof(backgroundHex));
        }

        var fg = new BitInternalColor(fgTrimmed);
        var bg = new BitInternalColor(bgTrimmed);
        var l1 = RelativeLuminance(fg.R, fg.G, fg.B);
        var l2 = RelativeLuminance(bg.R, bg.G, bg.B);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>Returns true when <paramref name="contrastRatio"/> meets WCAG AA for normal text (≥ 4.5:1).</summary>
    public static bool MeetsWcagAaNormalText(double contrastRatio) => contrastRatio >= 4.5;

    /// <summary>
    /// Returns true when <paramref name="contrastRatio"/> meets WCAG AA for large text (≥ 3.0:1).
    /// </summary>
    /// <remarks>
    /// "Large" per WCAG 2.x means at least 18pt (≈24px), or 14pt (≈18.66px) when bold. Apply this
    /// helper to body copy at your own risk — for paragraph text use <see cref="MeetsWcagAaNormalText"/>.
    /// </remarks>
    public static bool MeetsWcagAaLargeText(double contrastRatio) => contrastRatio >= 3.0;

    private static double RelativeLuminance(byte r, byte g, byte b)
    {
        var rs = Linearize(r / 255.0);
        var gs = Linearize(g / 255.0);
        var bs = Linearize(b / 255.0);
        return 0.2126 * rs + 0.7152 * gs + 0.0722 * bs;
    }

    private static double Linearize(double channel)
    {
        return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
