// ToUnicode CMap reader (bfchar / bfrange handling).

using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// A <c>/ToUnicode</c> CMap: maps character codes to Unicode strings using the
/// <c>bfchar</c> / <c>bfrange</c> entries embedded in the CMap stream.
/// </summary>
public sealed class BitPdfToUnicodeCMap
{
    private const int MaxRangeDestinationBytes = 512;

    // Each single mapping keeps how many ranges were written before it: a range written after it
    // overrides it, as a later entry does in a CMap read in stream order.
    private readonly Dictionary<int, (string Dst, int RangesBefore)> _single = new();
    private readonly List<(int Lo, int Hi, string Dst)> _ranges = new();
    private (long Lo, long Hi, int Owner)[]? _segments;

    /// <summary>Looks up the Unicode text for <paramref name="code"/>, or <c>null</c> if unmapped.</summary>
    public string? Lookup(int code)
    {
        bool hasSingle = _single.TryGetValue(code, out var single);
        if (hasSingle && single.RangesBefore == _ranges.Count)
        {
            return single.Dst;
        }
        _segments ??= BitPdfRangeIndex.Build(_ranges.Count, i => (_ranges[i].Lo, _ranges[i].Hi));
        int owner = BitPdfRangeIndex.Find(_segments, code);
        if (hasSingle && owner < single.RangesBefore)
        {
            return single.Dst;
        }
        if (owner < 0)
        {
            return null;
        }
        var (lo, _, dst) = _ranges[owner];
        // Increment the last UTF-16 unit by the offset, per the CMap rules.
        if (dst.Length == 0)
        {
            return string.Empty;
        }
        if (code == lo)
        {
            return dst;
        }
        var chars = dst.ToCharArray();
        chars[^1] = (char)(chars[^1] + (code - lo));
        return new string(chars);
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
                _single[CodeFromHex(s)] = (Utf16Be(d), _ranges.Count);
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
                if (dstS.Bytes.Length <= MaxRangeDestinationBytes)
                {
                    _ranges.Add((loCode, hiCode, Utf16Be(dstS)));
                }
            }
            else if (dst is List<object?> arr)
            {
                for (int i = 0; i < arr.Count && loCode + i <= hiCode; i++)
                {
                    if (arr[i] is BitPdfString item)
                    {
                        _single[loCode + i] = (Utf16Be(item), _ranges.Count);
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
