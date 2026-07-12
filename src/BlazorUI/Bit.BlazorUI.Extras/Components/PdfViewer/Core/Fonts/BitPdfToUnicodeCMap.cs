// ToUnicode CMap reader (bfchar / bfrange handling).

using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// A <c>/ToUnicode</c> CMap: maps character codes to Unicode strings using the
/// <c>bfchar</c> / <c>bfrange</c> entries embedded in the CMap stream.
/// </summary>
public sealed class BitPdfToUnicodeCMap
{
    private readonly Dictionary<int, string> _single = new();
    private readonly List<(int Lo, int Hi, string Dst)> _ranges = new();

    /// <summary>Looks up the Unicode text for <paramref name="code"/>, or <c>null</c> if unmapped.</summary>
    public string? Lookup(int code)
    {
        if (_single.TryGetValue(code, out var s))
        {
            return s;
        }
        foreach (var (lo, hi, dst) in _ranges)
        {
            if (code >= lo && code <= hi)
            {
                // Increment the last UTF-16 unit by the offset, per the CMap rules.
                if (dst.Length == 0)
                {
                    return string.Empty;
                }
                var chars = dst.ToCharArray();
                chars[^1] = (char)(chars[^1] + (code - lo));
                return new string(chars);
            }
        }
        return null;
    }

    /// <summary>Parses a decoded ToUnicode CMap byte stream.</summary>
    public static BitPdfToUnicodeCMap Parse(byte[] data)
    {
        var map = new BitPdfToUnicodeCMap();
        var lexer = new BitPdfLexer(new BitPdfStream(data));

        object token = lexer.GetObj();
        while (!ReferenceEquals(token, BitPdfPrimitives.EOF))
        {
            if (token is BitPdfCmd cmd)
            {
                switch (cmd.Value)
                {
                    case "beginbfchar":
                        token = map.ReadBfChar(lexer);
                        continue;
                    case "beginbfrange":
                        token = map.ReadBfRange(lexer);
                        continue;
                }
            }
            token = lexer.GetObj();
        }

        return map;
    }

    private object ReadBfChar(BitPdfLexer lexer)
    {
        while (true)
        {
            object src = lexer.GetObj();
            if (src is BitPdfCmd { Value: "endbfchar" } || ReferenceEquals(src, BitPdfPrimitives.EOF))
            {
                return ReferenceEquals(src, BitPdfPrimitives.EOF) ? src : lexer.GetObj();
            }
            object dst = lexer.GetObj();
            if (src is BitPdfString s && dst is BitPdfString d)
            {
                _single[CodeFromHex(s)] = Utf16Be(d);
            }
        }
    }

    private object ReadBfRange(BitPdfLexer lexer)
    {
        while (true)
        {
            object lo = lexer.GetObj();
            if (lo is BitPdfCmd { Value: "endbfrange" } || ReferenceEquals(lo, BitPdfPrimitives.EOF))
            {
                return ReferenceEquals(lo, BitPdfPrimitives.EOF) ? lo : lexer.GetObj();
            }
            object hi = lexer.GetObj();
            object dst = lexer.GetObj();

            if (lo is not BitPdfString loS || hi is not BitPdfString hiS)
            {
                continue;
            }
            int loCode = CodeFromHex(loS);
            int hiCode = CodeFromHex(hiS);

            if (dst is BitPdfString dstS)
            {
                _ranges.Add((loCode, hiCode, Utf16Be(dstS)));
            }
            else if (dst is List<object?> arr)
            {
                for (int i = 0; i < arr.Count && loCode + i <= hiCode; i++)
                {
                    if (arr[i] is BitPdfString item)
                    {
                        _single[loCode + i] = Utf16Be(item);
                    }
                }
            }
        }
    }

    private static int CodeFromHex(BitPdfString s)
    {
        int value = 0;
        foreach (byte b in s.Bytes)
        {
            value = (value << 8) | b;
        }
        return value;
    }

    private static string Utf16Be(BitPdfString s)
    {
        byte[] bytes = s.Bytes;
        if (bytes.Length == 0)
        {
            return string.Empty;
        }
        // ToUnicode destination strings are UTF-16BE (occasionally one byte).
        if (bytes.Length == 1)
        {
            return ((char)bytes[0]).ToString();
        }
        return Encoding.BigEndianUnicode.GetString(bytes);
    }
}
