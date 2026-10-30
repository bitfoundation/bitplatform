// Parsing of embedded CMap streams used as the /Encoding of Type0 (composite)
// fonts: maps multi-byte character codes to CIDs. Identity encodings map code
// to CID directly; an embedded CMap supplies explicit codespace ranges and
// cidrange/cidchar mappings.

namespace Bit.BlazorUI;

/// <summary>
/// A character-code → CID mapping for a composite font, built from an embedded
/// CMap stream (or the Identity mapping for Identity-H/Identity-V).
/// </summary>
public sealed class BitPdfCMap
{
    private readonly List<(long Lo, long Hi, int Cid)> _ranges = new();
    private readonly Dictionary<long, int> _chars = new();
    private (long Lo, long Hi, int Cid)[]? _segments;

    /// <summary>The byte length of a character code (1 or 2 for the common cases).</summary>
    public int CodeLength { get; private set; } = 2;

    /// <summary><c>true</c> for an Identity mapping (CID == code).</summary>
    public bool IsIdentity { get; private set; }

    /// <summary>The Identity mapping (2-byte codes, CID == code).</summary>
    public static BitPdfCMap Identity { get; } = new() { IsIdentity = true, CodeLength = 2 };

    /// <summary>Maps a character code to a CID.</summary>
    public int Lookup(long code)
    {
        if (IsIdentity)
        {
            return (int)code;
        }
        if (_chars.TryGetValue(code, out int cid))
        {
            return cid;
        }
        _segments ??= Flatten(_ranges);
        int low = 0, high = _segments.Length - 1;
        while (low <= high)
        {
            int mid = (low + high) >>> 1;
            if (_segments[mid].Lo <= code)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        if (high >= 0 && code <= _segments[high].Hi)
        {
            return (int)(_segments[high].Cid + (code - _segments[high].Lo));
        }
        return (int)code; // fall back to identity for unmapped codes
    }

    private static (long Lo, long Hi, int Cid)[] Flatten(List<(long Lo, long Hi, int Cid)> ranges)
    {
        var points = new List<(long At, int Index, bool Start)>(ranges.Count * 2);
        for (int i = 0; i < ranges.Count; i++)
        {
            if (ranges[i].Lo <= ranges[i].Hi)
            {
                points.Add((ranges[i].Lo, i, true));
                points.Add((ranges[i].Hi == long.MaxValue ? long.MaxValue : ranges[i].Hi + 1, i, false));
            }
        }
        points.Sort((a, b) => a.At.CompareTo(b.At));

        var active = new PriorityQueue<int, int>();
        var ended = new bool[ranges.Count];
        var segments = new List<(long Lo, long Hi, int Cid)>();
        int p = 0;
        while (p < points.Count)
        {
            long at = points[p].At;
            for (; p < points.Count && points[p].At == at; p++)
            {
                if (points[p].Start)
                {
                    active.Enqueue(points[p].Index, points[p].Index);
                }
                else
                {
                    ended[points[p].Index] = true;
                }
            }
            while (active.TryPeek(out int top, out _) && ended[top])
            {
                active.Dequeue();
            }
            if (p < points.Count && active.TryPeek(out int owner, out _))
            {
                var range = ranges[owner];
                segments.Add((at, points[p].At - 1, (int)(range.Cid + (at - range.Lo))));
            }
        }
        return [.. segments];
    }

    /// <summary>Parses an embedded CMap stream into a <see cref="BitPdfCMap"/>.</summary>
    public static BitPdfCMap Parse(byte[] data)
    {
        var cmap = new BitPdfCMap();
        var lexer = new BitPdfLexer(new BitPdfStream(data));
        int codeLen = 0;

        // Two-token queue so we can read "value value operator" style groups.
        object prev2 = BitPdfPrimitives.EOF;
        object prev1 = BitPdfPrimitives.EOF;

        while (true)
        {
            object tok = lexer.GetObj();
            if (ReferenceEquals(tok, BitPdfPrimitives.EOF))
            {
                break;
            }

            if (tok is BitPdfCmd cmd)
            {
                switch (cmd.Value)
                {
                    case "begincodespacerange":
                        codeLen = ReadCodespace(lexer, ref cmap);
                        if (codeLen > 0)
                        {
                            cmap.CodeLength = codeLen;
                        }
                        break;
                    case "begincidrange":
                    case "beginbfrange":
                        ReadCidRanges(lexer, cmap);
                        break;
                    case "begincidchar":
                    case "beginbfchar":
                        ReadCidChars(lexer, cmap);
                        break;
                }
            }

            prev2 = prev1;
            prev1 = tok;
            _ = prev2; // reserved for usecmap handling
        }

        return cmap;
    }

    private static int ReadCodespace(BitPdfLexer lexer, ref BitPdfCMap cmap)
    {
        int len = 0;
        while (true)
        {
            object tok = lexer.GetObj();
            if (ReferenceEquals(tok, BitPdfPrimitives.EOF) || (tok is BitPdfCmd { Value: "endcodespacerange" }))
            {
                break;
            }
            if (tok is BitPdfString lo)
            {
                _ = lexer.GetObj(); // the high bound of the range
                len = Math.Max(len, lo.Bytes.Length);
            }
        }
        return len;
    }

    private static void ReadCidRanges(BitPdfLexer lexer, BitPdfCMap cmap)
    {
        while (true)
        {
            object a = lexer.GetObj();
            if (ReferenceEquals(a, BitPdfPrimitives.EOF) || a is BitPdfCmd)
            {
                break; // endcidrange / endbfrange
            }
            object b = lexer.GetObj();
            object c = lexer.GetObj();
            if (a is BitPdfString lo && b is BitPdfString hi)
            {
                long loCode = ToCode(lo.Bytes);
                long hiCode = ToCode(hi.Bytes);
                int cid = c switch
                {
                    double d => (int)d,
                    BitPdfString s => (int)ToCode(s.Bytes),
                    _ => 0,
                };
                cmap._ranges.Add((loCode, hiCode, cid));
            }
        }
    }

    private static void ReadCidChars(BitPdfLexer lexer, BitPdfCMap cmap)
    {
        while (true)
        {
            object a = lexer.GetObj();
            if (ReferenceEquals(a, BitPdfPrimitives.EOF) || a is BitPdfCmd)
            {
                break; // endcidchar / endbfchar
            }
            object b = lexer.GetObj();
            if (a is BitPdfString code)
            {
                int cid = b switch
                {
                    double d => (int)d,
                    BitPdfString s => (int)ToCode(s.Bytes),
                    _ => 0,
                };
                cmap._chars[ToCode(code.Bytes)] = cid;
            }
        }
    }

    private static long ToCode(byte[] bytes)
    {
        long value = 0;
        foreach (byte b in bytes)
        {
            value = (value << 8) | b;
        }
        return value;
    }
}
