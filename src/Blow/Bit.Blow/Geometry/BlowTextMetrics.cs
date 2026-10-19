namespace Bit.Blow;

/// <summary>
/// A cheap estimate of how wide a run of text will be drawn.
/// </summary>
/// <remarks>
/// SVG cannot size a rectangle to the text inside it, and asking the browser for the real
/// measurement would mean a JS round-trip per label on every render - far too expensive for
/// something drawn once per edge. So the backing plate behind an edge label is sized from an
/// estimate. Getting it wrong is only cosmetic (a plate slightly too wide or too narrow), and
/// <c>BlowEdge.LabelBgPaddingX</c> is there to tune the result for an unusual font.
/// </remarks>
public static class BlowTextMetrics
{
    /// <summary>Font size the built-in edge label stylesheet draws labels at.</summary>
    public const double DefaultLabelFontSize = 11;

    /// <summary>
    /// Estimated rendered width of <paramref name="text"/> in pixels. Characters are weighted
    /// by rough advance width - an 'i' is not an 'm', and a CJK glyph is a full em - which is
    /// far closer than treating every character alike.
    /// </summary>
    public static double EstimateWidth(string? text, double fontSize = DefaultLabelFontSize)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        // By code point rather than by UTF-16 unit, so an emoji (two surrogates) counts once.
        var ems = 0.0;
        foreach (var rune in text.EnumerateRunes()) ems += AdvanceEms(rune);
        return ems * fontSize;
    }

    /// <summary>Rough advance width of one character, as a fraction of the font size.</summary>
    private static double AdvanceEms(System.Text.Rune rune)
    {
        // Combining accents sit on the previous character and take no room of their own.
        var category = System.Text.Rune.GetUnicodeCategory(rune);
        if (category is System.Globalization.UnicodeCategory.NonSpacingMark
            or System.Globalization.UnicodeCategory.EnclosingMark
            or System.Globalization.UnicodeCategory.Format)
        {
            return 0;
        }

        if (IsWide(rune.Value)) return 1.0;

        if (!rune.IsAscii) return 0.6;

        return (char)rune.Value switch
        {
            ' ' => 0.28,
            'i' or 'j' or 'l' or 'I' or '|' or '!' or '.' or ',' or ':' or ';' or '\'' or '`' => 0.3,
            'f' or 'r' or 't' or '(' or ')' or '[' or ']' or '{' or '}' or '-' => 0.36,
            'm' or 'w' or 'M' or 'W' or '@' => 0.9,
            >= 'A' and <= 'Z' => 0.68,
            >= '0' and <= '9' => 0.56,
            _ => 0.55,
        };
    }

    /// <summary>
    /// The scripts and symbols drawn a full em wide: Hangul, CJK and their punctuation, the
    /// full-width forms, and emoji. Everything else above ASCII - accented Latin, Greek,
    /// Cyrillic, dashes, curly quotes - is ordinary proportional text.
    /// </summary>
    private static bool IsWide(int cp) =>
        cp is >= 0x1100 and <= 0x115F       // Hangul Jamo
            or >= 0x2E80 and <= 0x303E      // CJK radicals, symbols and punctuation
            or >= 0x3041 and <= 0x33FF      // Kana, Bopomofo, CJK compatibility
            or >= 0x3400 and <= 0x4DBF      // CJK extension A
            or >= 0x4E00 and <= 0x9FFF      // CJK unified ideographs
            or >= 0xA000 and <= 0xA4CF      // Yi
            or >= 0xAC00 and <= 0xD7A3      // Hangul syllables
            or >= 0xF900 and <= 0xFAFF      // CJK compatibility ideographs
            or >= 0xFE30 and <= 0xFE4F      // CJK compatibility forms
            or >= 0xFF00 and <= 0xFF60      // Full-width forms
            or >= 0xFFE0 and <= 0xFFE6
            or >= 0x1F300 and <= 0x1FAFF    // Emoji and pictographs
            or >= 0x20000 and <= 0x3FFFD;   // CJK extensions B onwards
}
