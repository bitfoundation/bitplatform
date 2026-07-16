// WinAnsiEncoding code-to-Unicode mapping per the PDF specification (Annex D).
// This is factual encoding data, re-implemented in C#.

using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Maps single-byte character codes to Unicode using WinAnsiEncoding
/// (effectively Windows-1252). Codes 0x80–0x9F differ from Latin-1 and are
/// listed explicitly; all other codes map through Latin-1.
/// </summary>
internal static class BitPdfWinAnsiEncoding
{
    private static readonly Dictionary<int, char> HighRange = new()
    {
        [0x80] = '\u20AC', [0x82] = '\u201A', [0x83] = '\u0192', [0x84] = '\u201E',
        [0x85] = '\u2026', [0x86] = '\u2020', [0x87] = '\u2021', [0x88] = '\u02C6',
        [0x89] = '\u2030', [0x8A] = '\u0160', [0x8B] = '\u2039', [0x8C] = '\u0152',
        [0x8E] = '\u017D', [0x91] = '\u2018', [0x92] = '\u2019', [0x93] = '\u201C',
        [0x94] = '\u201D', [0x95] = '\u2022', [0x96] = '\u2013', [0x97] = '\u2014',
        [0x98] = '\u02DC', [0x99] = '\u2122', [0x9A] = '\u0161', [0x9B] = '\u203A',
        [0x9C] = '\u0153', [0x9E] = '\u017E', [0x9F] = '\u0178',
    };

    public static string CodeToUnicode(int code)
    {
        if (code < 0)
        {
            return string.Empty;
        }
        if (HighRange.TryGetValue(code, out char mapped))
        {
            return mapped.ToString();
        }
        if (code is >= 0x80 and <= 0x9F)
        {
            return string.Empty; // undefined in WinAnsi
        }
        return Encoding.Latin1.GetString([(byte)(code & 0xFF)]);
    }
}
