// Built-in glyph-advance metrics for the PDF "Standard 14" fonts, used when a
// non-embedded simple font omits its /Widths array. These are the factual
// Adobe Core-14 AFM advance widths (1/1000 em) for character codes 32..126.

namespace Bit.BlazorUI;

/// <summary>
/// Provides advance widths for the standard PDF fonts (Helvetica, Times,
/// Courier and their bold variants) when a font dictionary has no explicit
/// <c>/Widths</c>. Widths are indexed by character code starting at 0x20.
/// </summary>
internal static class BitPdfStandardFonts
{
    private const int First = 32;
    private const double DefaultWidth = 500;

    private static readonly int[] Helvetica =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584,
    ];

    private static readonly int[] HelveticaBold =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584,
    ];

    private static readonly int[] Times =
    [
        250, 333, 408, 500, 500, 833, 778, 180, 333, 333, 500, 564, 250, 333, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 278, 278, 564, 564, 564, 444,
        921, 722, 667, 667, 722, 611, 556, 722, 722, 333, 389, 722, 611, 889, 722, 722,
        556, 722, 667, 556, 611, 722, 722, 944, 722, 722, 611, 333, 278, 333, 469, 500,
        333, 444, 500, 444, 500, 444, 333, 500, 500, 278, 278, 500, 278, 778, 500, 500,
        500, 500, 333, 389, 278, 500, 500, 722, 500, 500, 444, 480, 200, 480, 541,
    ];

    private static readonly int[] TimesBold =
    [
        250, 333, 555, 500, 500, 1000, 833, 278, 333, 333, 500, 570, 250, 333, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 333, 333, 570, 570, 570, 500,
        930, 722, 667, 722, 722, 667, 611, 778, 778, 389, 500, 778, 667, 944, 722, 778,
        611, 778, 722, 556, 667, 722, 722, 1000, 722, 722, 667, 333, 278, 333, 581, 500,
        333, 500, 556, 444, 556, 444, 333, 500, 556, 278, 333, 556, 278, 833, 556, 500,
        556, 556, 444, 389, 333, 556, 500, 722, 500, 500, 444, 394, 220, 394, 520,
    ];

    /// <summary>
    /// Returns a per-code width lookup for the named base font, or <c>null</c>
    /// when the font is not one of the standard families.
    /// </summary>
    public static Func<int, double>? Resolve(string baseFont)
    {
        if (string.IsNullOrEmpty(baseFont))
        {
            return null;
        }

        // Strip a subset prefix such as "ABCDEF+".
        int plus = baseFont.IndexOf('+');
        string name = (plus >= 0 ? baseFont[(plus + 1)..] : baseFont).ToLowerInvariant();
        bool bold = name.Contains("bold");

        if (name.Contains("courier") || name.Contains("mono"))
        {
            return static _ => 600; // Courier is monospaced.
        }
        if (name.Contains("times") || name.Contains("serif") || name.Contains("roman") || name.Contains("georgia"))
        {
            int[] table = bold ? TimesBold : Times;
            return code => Lookup(table, code);
        }
        if (name.Contains("helvetica") || name.Contains("arial") || name.Contains("sans"))
        {
            int[] table = bold ? HelveticaBold : Helvetica;
            return code => Lookup(table, code);
        }
        return null;
    }

    private static double Lookup(int[] table, int code)
    {
        int index = code - First;
        return index >= 0 && index < table.Length ? table[index] : DefaultWidth;
    }
}
