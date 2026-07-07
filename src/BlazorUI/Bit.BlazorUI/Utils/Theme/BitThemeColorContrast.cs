using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// WCAG 2.x contrast helpers for pairs of solid hex colors (sRGB).
/// </summary>
public static partial class BitThemeColorContrast
{
    // Accepts #RGB or #RRGGBB (case-insensitive). The underlying BitInternalColor parser silently
    // falls back to white on invalid input, which would produce a misleading ratio - so we gate
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

        if (TryNormalizeHex(foregroundHex, out var fgHex) is false)
        {
            throw new ArgumentException(
                $"'{foregroundHex}' is not a valid hex color. Expected '#RGB' or '#RRGGBB'.",
                nameof(foregroundHex));
        }

        if (TryNormalizeHex(backgroundHex, out var bgHex) is false)
        {
            throw new ArgumentException(
                $"'{backgroundHex}' is not a valid hex color. Expected '#RGB' or '#RRGGBB'.",
                nameof(backgroundHex));
        }

        var fg = new BitInternalColor(fgHex);
        var bg = new BitInternalColor(bgHex);
        var l1 = RelativeLuminance(fg.R, fg.G, fg.B);
        var l2 = RelativeLuminance(bg.R, bg.G, bg.B);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Trims, validates (<c>#RGB</c> or <c>#RRGGBB</c>), and expands the input to its canonical
    /// 6-digit <c>#RRGGBB</c> form. Returns <see langword="false"/> for null, blank, or invalid
    /// input. Shared with <see cref="BitThemeColorDerivation"/> so both helpers agree on what a
    /// valid color is (and both expand shorthand the same way before handing it to the parser).
    /// </summary>
    internal static bool TryNormalizeHex(string? value, out string sixDigitHex)
    {
        sixDigitHex = string.Empty;

        if (string.IsNullOrWhiteSpace(value)) return false;

        var trimmed = value.Trim();
        if (HexColorRegex().IsMatch(trimmed) is false) return false;

        // BitInternalColor parses hex as 24-bit '#RRGGBB', so '#FFF' would otherwise be read as
        // '#000FFF'. Expand to 6 digits first.
        sixDigitHex = ExpandShorthandHex(trimmed);
        return true;
    }

    /// <summary>Returns true when <paramref name="contrastRatio"/> meets WCAG AA for normal text (≥ 4.5:1).</summary>
    public static bool MeetsWcagAaNormalText(double contrastRatio) => contrastRatio >= 4.5;

    /// <summary>
    /// Returns true when <paramref name="contrastRatio"/> meets WCAG AA for large text (≥ 3.0:1).
    /// </summary>
    /// <remarks>
    /// "Large" per WCAG 2.x means at least 18pt (≈24px), or 14pt (≈18.66px) when bold. Apply this
    /// helper to body copy at your own risk - for paragraph text use <see cref="MeetsWcagAaNormalText"/>.
    /// </remarks>
    public static bool MeetsWcagAaLargeText(double contrastRatio) => contrastRatio >= 3.0;

    /// <summary>
    /// Expands a 3-digit shorthand hex (e.g. <c>#FFF</c>) to its 6-digit form (<c>#FFFFFF</c>).
    /// Inputs already in 6-digit form are returned unchanged.
    /// </summary>
    private static string ExpandShorthandHex(string hex)
    {
        // hex is already validated as '#RGB' or '#RRGGBB' at this point.
        if (hex.Length != 4) return hex;

        var r = hex[1];
        var g = hex[2];
        var b = hex[3];
        return string.Create(7, (r, g, b), static (span, c) =>
        {
            span[0] = '#';
            span[1] = c.r;
            span[2] = c.r;
            span[3] = c.g;
            span[4] = c.g;
            span[5] = c.b;
            span[6] = c.b;
        });
    }

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
