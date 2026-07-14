using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// A PDF string object (literal <c>(...)</c> or hexadecimal <c>&lt;...&gt;</c>).
/// PDF strings are byte sequences, not Unicode text, so the raw bytes are kept
/// and interpreted by callers (e.g. for PDFDocEncoding or UTF-16BE text).
/// </summary>
public sealed class BitPdfString
{
    /// <summary>The raw decoded bytes of the string.</summary>
    public byte[] Bytes { get; }

    public BitPdfString(byte[] bytes) => Bytes = bytes ?? [];

    public int Length => Bytes.Length;

    /// <summary>Interprets the bytes as Latin-1.</summary>
    public string AsLatin1() => Encoding.Latin1.GetString(Bytes);

    /// <summary>
    /// Decodes the string as PDF text: UTF-16BE when a <c>FE FF</c> BOM is
    /// present, UTF-8 when an <c>EF BB BF</c> BOM is present (PDF 2.0), otherwise
    /// PDFDocEncoding (PDF 32000-1 Annex D.2).
    /// </summary>
    public string AsText()
    {
        if (Bytes.Length >= 2 && Bytes[0] == 0xFE && Bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(Bytes, 2, Bytes.Length - 2);
        }
        if (Bytes.Length >= 3 && Bytes[0] == 0xEF && Bytes[1] == 0xBB && Bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(Bytes, 3, Bytes.Length - 3);
        }
        var sb = new StringBuilder(Bytes.Length);
        foreach (byte b in Bytes)
        {
            sb.Append(PdfDocEncoding[b]);
        }
        return sb.ToString();
    }

    // PDFDocEncoding: identical to Latin-1 except for the special glyphs in
    // 0x18–0x1F and 0x80–0xA0 (bullets, dashes, smart quotes, ligatures, Euro).
    private static readonly char[] PdfDocEncoding = BuildPdfDocEncoding();

    private static char[] BuildPdfDocEncoding()
    {
        var t = new char[256];
        for (int i = 0; i < 256; i++)
        {
            t[i] = (char)i;
        }
        // 0x18–0x1F: accent glyphs.
        t[0x18] = '˘'; t[0x19] = 'ˇ'; t[0x1A] = 'ˆ'; t[0x1B] = '˙';
        t[0x1C] = '˝'; t[0x1D] = '˛'; t[0x1E] = '˚'; t[0x1F] = '˜';
        // 0x80–0x9F: punctuation, ligatures, accented capitals.
        t[0x80] = '•'; t[0x81] = '†'; t[0x82] = '‡'; t[0x83] = '…';
        t[0x84] = '—'; t[0x85] = '–'; t[0x86] = 'ƒ'; t[0x87] = '⁄';
        t[0x88] = '‹'; t[0x89] = '›'; t[0x8A] = '−'; t[0x8B] = '‰';
        t[0x8C] = '„'; t[0x8D] = '“'; t[0x8E] = '”'; t[0x8F] = '‘';
        t[0x90] = '’'; t[0x91] = '‚'; t[0x92] = '™'; t[0x93] = 'ﬁ';
        t[0x94] = 'ﬂ'; t[0x95] = 'Ł'; t[0x96] = 'Œ'; t[0x97] = 'Š';
        t[0x98] = 'Ÿ'; t[0x99] = 'Ž'; t[0x9A] = 'ı'; t[0x9B] = 'ł';
        t[0x9C] = 'œ'; t[0x9D] = 'š'; t[0x9E] = 'ž'; t[0xA0] = '€';
        return t;
    }

    public override string ToString() => AsText();
}
