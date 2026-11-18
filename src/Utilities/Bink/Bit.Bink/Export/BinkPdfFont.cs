using System.Numerics;
using System.Text;

namespace Bit.Bink;

/// <summary>The standard fonts' encoding and widths, for setting text in a PDF without embedding a font.</summary>
internal static class BinkPdfFont
{
    // Advance widths in thousandths of the font size, from the Adobe font metrics, for ' ' to '~'.
    private static readonly short[] Helvetica =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584,
    ];

    private static readonly short[] HelveticaBold =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584,
    ];

    private static readonly short[] Times =
    [
        250, 333, 408, 500, 500, 833, 778, 180, 333, 333, 500, 564, 250, 333, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 278, 278, 564, 564, 564, 444,
        921, 722, 667, 667, 722, 611, 556, 722, 722, 333, 389, 722, 611, 889, 722, 722,
        556, 722, 667, 556, 611, 722, 722, 944, 722, 722, 611, 333, 278, 333, 469, 500,
        333, 444, 500, 444, 500, 444, 333, 500, 500, 278, 278, 500, 278, 778, 500, 500,
        500, 500, 333, 389, 278, 500, 500, 722, 500, 500, 444, 480, 200, 480, 541,
    ];

    // The same metrics for WinAnsiEncoding's bytes 0x80 to 0xFF; zero where the encoding has no character.
    private static readonly short[] HelveticaHigh =
    [
        556, 0, 222, 556, 333, 1000, 556, 556, 333, 1000, 667, 333, 1000, 0, 611, 0,
        0, 222, 222, 333, 333, 350, 556, 1000, 333, 1000, 500, 333, 944, 0, 500, 667,
        278, 333, 556, 556, 556, 556, 260, 556, 333, 737, 370, 556, 584, 333, 737, 333,
        400, 584, 333, 333, 333, 556, 537, 278, 333, 333, 365, 556, 834, 834, 834, 611,
        667, 667, 667, 667, 667, 667, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278,
        722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611,
        556, 556, 556, 556, 556, 556, 889, 500, 556, 556, 556, 556, 278, 278, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 584, 611, 556, 556, 556, 556, 500, 556, 500,
    ];

    private static readonly short[] HelveticaBoldHigh =
    [
        556, 0, 278, 556, 500, 1000, 556, 556, 333, 1000, 667, 333, 1000, 0, 611, 0,
        0, 278, 278, 500, 500, 350, 556, 1000, 333, 1000, 556, 333, 944, 0, 500, 667,
        278, 333, 556, 556, 556, 556, 280, 556, 333, 737, 370, 556, 584, 333, 737, 333,
        400, 584, 333, 333, 333, 611, 556, 278, 333, 333, 365, 556, 834, 834, 834, 611,
        722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278,
        722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611,
        556, 556, 556, 556, 556, 556, 889, 556, 556, 556, 556, 556, 278, 278, 278, 278,
        611, 611, 611, 611, 611, 611, 611, 584, 611, 611, 611, 611, 611, 556, 611, 556,
    ];

    private static readonly short[] TimesHigh =
    [
        500, 0, 333, 500, 444, 1000, 500, 500, 333, 1000, 556, 333, 889, 0, 611, 0,
        0, 333, 333, 444, 444, 350, 500, 1000, 333, 980, 389, 333, 722, 0, 444, 722,
        250, 333, 500, 500, 500, 500, 200, 500, 333, 760, 276, 500, 564, 333, 760, 333,
        400, 564, 300, 300, 333, 500, 453, 250, 333, 300, 310, 500, 750, 750, 750, 444,
        722, 722, 722, 722, 722, 722, 889, 667, 611, 611, 611, 611, 333, 333, 333, 333,
        722, 722, 722, 722, 722, 722, 722, 564, 722, 722, 722, 722, 722, 722, 556, 500,
        444, 444, 444, 444, 444, 444, 667, 444, 444, 444, 444, 444, 278, 278, 278, 278,
        500, 500, 500, 500, 500, 500, 500, 564, 500, 500, 500, 500, 500, 500, 500, 500,
    ];

    // WinAnsiEncoding's bytes 0x80..0x9F, which are not Latin-1: the character each stands for, 0 where none.
    private static readonly char[] WinAnsiHigh =
    [
        '€', '\0', '‚', 'ƒ', '„', '…', '†', '‡', 'ˆ', '‰', 'Š', '‹', 'Œ', '\0', 'Ž', '\0',
        '\0', '‘', '’', '“', '”', '•', '–', '—', '˜', '™', 'š', '›', 'œ', '\0', 'ž', 'Ÿ',
    ];

    /// <summary>The standard face nearest a CSS font family list.</summary>
    public static BinkPdfFace FaceOf(string? family)
    {
        string f = (family ?? string.Empty).ToLowerInvariant();
        if (f.Contains("mono", StringComparison.Ordinal) || f.Contains("courier", StringComparison.Ordinal) || f.Contains("consol", StringComparison.Ordinal)) return BinkPdfFace.Courier;
        if (f.Contains("times", StringComparison.Ordinal) || f.Contains("georgia", StringComparison.Ordinal)
            || (f.Contains("serif", StringComparison.Ordinal) && !f.Contains("sans-serif", StringComparison.Ordinal))) return BinkPdfFace.Times;
        return BinkPdfFace.Helvetica;
    }

    /// <summary>Encodes a line in WinAnsiEncoding; false when it has a character the encoding lacks.</summary>
    public static bool TryEncode(string line, out byte[] bytes)
    {
        bytes = new byte[line.Length];
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\t') c = ' ';
            if (c is (>= ' ' and <= '~') or (>= ' ' and <= 'ÿ'))
            {
                bytes[i] = (byte)c;
                continue;
            }
            int index = c == '\0' ? -1 : Array.IndexOf(WinAnsiHigh, c);
            if (index < 0) return false;
            bytes[i] = (byte)(0x80 + index);
        }
        return true;
    }

    /// <summary>The width of encoded text in units of the font size.</summary>
    public static float Measure(ReadOnlySpan<byte> text, BinkPdfFace face, bool bold)
    {
        if (face == BinkPdfFace.Courier) return text.Length * 0.6f;
        short[] widths = face == BinkPdfFace.Times ? Times : bold ? HelveticaBold : Helvetica;
        short[] high = face == BinkPdfFace.Times ? TimesHigh : bold ? HelveticaBoldHigh : HelveticaHigh;
        int total = 0;
        foreach (byte b in text)
        {
            int width = b switch
            {
                >= 32 and <= 126 => widths[b - 32],
                >= 0x80 => high[b - 0x80],
                _ => 0,
            };
            // A byte the encoding has no character for is measured as a lower-case letter.
            total += width > 0 ? width : widths['a' - 32];
        }
        // Times bold is a little wider than Times; its own metrics are not carried, so it is estimated.
        return total / 1000f * (face == BinkPdfFace.Times && bold ? 1.04f : 1f);
    }

    /// <summary>Writes encoded text as a PDF string, escaping what a literal string cannot hold.</summary>
    public static StringBuilder WriteString(StringBuilder content, ReadOnlySpan<byte> text)
    {
        content.Append('(');
        foreach (byte b in text)
        {
            if (b is (byte)'(' or (byte)')' or (byte)'\\') content.Append('\\').Append((char)b);
            else if (b is < 32 or > 126) content.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
            else content.Append((char)b);
        }
        return content.Append(')');
    }
}
