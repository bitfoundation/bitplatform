// C# implementation of the CCITTFaxDecode filter (ITU-T T.4 / T.6), following
// the standard specification. The Huffman code tables below are factual values
// from the ITU recommendations.

namespace Bit.BlazorUI;

/// <summary>
/// Decodes CCITT Group 3 (1-D and 2-D) and Group 4 fax data into packed 1-bit
/// rows (MSB first), where a set bit denotes a black pixel before any
/// <c>BlackIs1</c> inversion is applied by the caller.
/// </summary>
internal static class BitPdfCcittFaxDecoder
{
    // Mode sentinels MUST sit outside the vertical-mode offset range [-3, 3]:
    // the vertical codes VL1/VL2/VL3 decode to -1/-2/-3, so using those values as
    // Pass/Horizontal/Eol markers made every vertical-left code decode as the
    // wrong mode and corrupted virtually all real G4 images.
    private const int Pass = 100;
    private const int Horizontal = 101;
    private const int Eol = 102;
    private const int Error = 103;

    public static byte[] Decode(byte[] data, BitPdfCcittParams p)
    {
        var reader = new BitReader(data);
        int columns = p.Columns <= 0 ? 1728 : p.Columns;
        int rowBytes = (columns + 7) / 8;
        var output = new List<byte>();

        // Reference line changing elements; initialized so the first row is
        // coded against an all-white line.
        var refChanges = new List<int> { columns, columns };
        int maxRows = p.Rows > 0 ? p.Rows : int.MaxValue;
        int rowCount = 0;

        while (rowCount < maxRows && !reader.AtEnd)
        {
            if (p.EncodedByteAlign)
            {
                reader.AlignToByte();
            }

            // For Group 3 (K >= 0), an EOL code may precede each row (with optional
            // fill bits). For mixed 1D/2D (K > 0) a tag bit after the EOL selects
            // the coding of the next row: 1 = 1D, 0 = 2D.
            bool use2D = p.K < 0;
            if (p.K >= 0)
            {
                SkipEol(reader);
                if (p.K > 0)
                {
                    int tag = reader.ReadBit();
                    if (tag < 0)
                    {
                        break;
                    }
                    use2D = tag == 0;
                }
            }

            var curChanges = new List<int>();
            bool ok = use2D
                ? Decode2DRow(reader, refChanges, curChanges, columns)
                : Decode1DRow(reader, curChanges, columns);

            if (!ok)
            {
                // Damaged row: try to resynchronize at the next EOL (Group 3) and
                // continue, matching pdf.js damaged-row recovery. For Group 4 there
                // are no EOLs, so stop.
                if (p.K >= 0 && SkipToNextEol(reader))
                {
                    continue;
                }
                break;
            }

            // Emit the packed row honoring BlackIs1 (filter-output semantics:
            // a black pixel becomes a 1 bit only when BlackIs1 is set).
            var row = new byte[rowBytes];
            int color = 0; // start white
            int pos = 0;
            foreach (int change in curChanges)
            {
                int end = Math.Min(change, columns);
                bool oneBit = (color == 1) == p.BlackIs1;
                if (oneBit)
                {
                    for (int x = pos; x < end; x++)
                    {
                        row[x >> 3] |= (byte)(0x80 >> (x & 7));
                    }
                }
                pos = end;
                color ^= 1;
            }
            output.AddRange(row);

            curChanges.Add(columns);
            curChanges.Add(columns);
            refChanges = curChanges;
            rowCount++;
        }

        return output.ToArray();
    }

    /// <summary>Consumes an EOL code (≥11 zero bits then a 1, plus any preceding
    /// fill bits) if one is present; leaves the reader untouched otherwise.</summary>
    private static bool SkipEol(BitReader r)
    {
        var save = r.Save();
        int zeros = 0;
        while (true)
        {
            int b = r.ReadBit();
            if (b < 0)
            {
                r.Restore(save);
                return false;
            }
            if (b == 0)
            {
                zeros++;
                continue;
            }
            if (zeros >= 11)
            {
                return true; // consumed a full EOL
            }
            r.Restore(save);
            return false;
        }
    }

    /// <summary>Scans forward to the next EOL for damaged-row resynchronization.</summary>
    private static bool SkipToNextEol(BitReader r)
    {
        int zeros = 0;
        while (true)
        {
            int b = r.ReadBit();
            if (b < 0)
            {
                return false;
            }
            if (b == 0)
            {
                zeros++;
            }
            else
            {
                if (zeros >= 11)
                {
                    return true;
                }
                zeros = 0;
            }
        }
    }

    private static bool Decode1DRow(BitReader reader, List<int> changes, int columns)
    {
        int a0 = 0;
        int color = 0; // white
        while (a0 < columns)
        {
            int run = ReadRun(reader, color == 0);
            if (run < 0)
            {
                return changes.Count > 0;
            }
            a0 = Math.Min(a0 + run, columns);
            changes.Add(a0);
            color ^= 1;
        }
        return true;
    }

    private static bool Decode2DRow(BitReader reader, List<int> refChanges, List<int> changes, int columns)
    {
        int a0 = -1;
        int color = 0; // white

        while (a0 < columns)
        {
            int mode = ReadMode(reader);
            if (mode == Eol || mode == Error)
            {
                return changes.Count > 0;
            }

            int b1 = FindB1(refChanges, a0, color, columns);
            int b2 = b1 < columns ? NextChange(refChanges, b1, columns) : columns;

            if (mode == Pass)
            {
                FillTo(changes, b2);
                a0 = b2;
            }
            else if (mode == Horizontal)
            {
                int start = a0 < 0 ? 0 : a0;
                int run1 = ReadRun(reader, color == 0);
                int run2 = ReadRun(reader, color != 0);
                if (run1 < 0 || run2 < 0)
                {
                    return changes.Count > 0;
                }
                int a1 = Math.Min(start + run1, columns);
                int a2 = Math.Min(a1 + run2, columns);
                changes.Add(a1);
                changes.Add(a2);
                a0 = a2;
            }
            else
            {
                // Vertical mode: mode is the offset from b1 (-3..3).
                int a1 = Math.Clamp(b1 + mode, 0, columns);
                changes.Add(a1);
                a0 = a1;
                color ^= 1;
            }
        }
        return true;
    }

    private static void FillTo(List<int> changes, int upto)
    {
        // Pass mode keeps the current color up to b2 without adding a transition.
        // The colour run is extended; nothing recorded because colour is unchanged.
        _ = changes;
        _ = upto;
    }

    private static int FindB1(List<int> refChanges, int a0, int color, int columns)
    {
        // b1 = first changing element on the reference line to the right of a0
        // and of opposite colour to a0's colour. Reference changes alternate
        // colour starting with white->index0.
        for (int i = 0; i < refChanges.Count; i++)
        {
            if (refChanges[i] > a0)
            {
                // Colour of the element at index i is white if i is even.
                int elemColor = i & 1; // 0 white-run end ... transitions to color (i%2)
                if (elemColor == color)
                {
                    return refChanges[i];
                }
            }
        }
        return columns;
    }

    private static int NextChange(List<int> refChanges, int afterValue, int columns)
    {
        foreach (int c in refChanges)
        {
            if (c > afterValue)
            {
                return c;
            }
        }
        return columns;
    }

    // ----- Mode and run-length code reading -----

    private static int ReadMode(BitReader r)
    {
        if (r.ReadBit() == 1) return 0;            // V0  : 1
        int b2 = r.ReadBit();
        if (b2 < 0) return Error;
        if (b2 == 1)                               // 01x
        {
            return r.ReadBit() == 1 ? 1 : -1;      // 011=VR1, 010=VL1
        }
        int b3 = r.ReadBit();
        if (b3 < 0) return Error;
        if (b3 == 1) return Horizontal;            // 001
        int b4 = r.ReadBit();
        if (b4 < 0) return Error;
        if (b4 == 1) return Pass;                  // 0001
        int b5 = r.ReadBit();
        if (b5 < 0) return Error;
        if (b5 == 1)                               // 00001x
        {
            return r.ReadBit() == 1 ? 2 : -2;      // 000011=VR2, 000010=VL2
        }
        int b6 = r.ReadBit();
        if (b6 < 0) return Error;
        if (b6 == 1)                               // 000001x
        {
            return r.ReadBit() == 1 ? 3 : -3;      // 0000011=VR3, 0000010=VL3
        }
        // 0000001... is the EOL / EOFB prefix (000000000001); end the row.
        return Eol;
    }

    private static int ReadRun(BitReader r, bool white)
    {
        int total = 0;
        while (true)
        {
            int run = ReadCode(r, white);
            if (run < 0)
            {
                return total > 0 ? total : -1;
            }
            total += run;
            if (run < 64)
            {
                return total; // terminating code completes the run
            }
            // makeup code (>=64): continue accumulating
        }
    }

    private static int ReadCode(BitReader r, bool white)
    {
        var table = white ? WhiteCodes : BlackCodes;
        int code = 0;
        for (int len = 1; len <= 14; len++)
        {
            int bit = r.ReadBit();
            if (bit < 0)
            {
                return -1;
            }
            code = (code << 1) | bit;
            if (table.TryGetValue((len << 16) | code, out int run))
            {
                return run;
            }
        }
        return -1;
    }

    // Code tables keyed by (bitLength << 16 | code) -> run length.
    private static readonly Dictionary<int, int> WhiteCodes = BuildWhite();
    private static readonly Dictionary<int, int> BlackCodes = BuildBlack();

    private static void Add(Dictionary<int, int> t, int len, int code, int run)
        => t[(len << 16) | code] = run;

    private static Dictionary<int, int> BuildWhite()
    {
        var t = new Dictionary<int, int>();
        // Terminating codes 0..63: (run, len, code)
        int[][] term =
        [
            [0,8,0x35],[1,6,0x07],[2,4,0x07],[3,4,0x08],[4,4,0x0B],[5,4,0x0C],[6,4,0x0E],[7,4,0x0F],
            [8,5,0x13],[9,5,0x14],[10,5,0x07],[11,5,0x08],[12,6,0x08],[13,6,0x03],[14,6,0x34],[15,6,0x35],
            [16,6,0x2A],[17,6,0x2B],[18,7,0x27],[19,7,0x0C],[20,7,0x08],[21,7,0x17],[22,7,0x03],[23,7,0x04],
            [24,7,0x28],[25,7,0x2B],[26,7,0x13],[27,7,0x24],[28,7,0x18],[29,8,0x02],[30,8,0x03],[31,8,0x1A],
            [32,8,0x1B],[33,8,0x12],[34,8,0x13],[35,8,0x14],[36,8,0x15],[37,8,0x16],[38,8,0x17],[39,8,0x28],
            [40,8,0x29],[41,8,0x2A],[42,8,0x2B],[43,8,0x2C],[44,8,0x2D],[45,8,0x04],[46,8,0x05],[47,8,0x0A],
            [48,8,0x0B],[49,8,0x52],[50,8,0x53],[51,8,0x54],[52,8,0x55],[53,8,0x24],[54,8,0x25],[55,8,0x58],
            [56,8,0x59],[57,8,0x5A],[58,8,0x5B],[59,8,0x4A],[60,8,0x4B],[61,8,0x32],[62,8,0x33],[63,8,0x34],
        ];
        foreach (var e in term) Add(t, e[1], e[2], e[0]);

        int[][] makeup =
        [
            [64,5,0x1B],[128,5,0x12],[192,6,0x17],[256,7,0x37],[320,8,0x36],[384,8,0x37],[448,8,0x64],
            [512,8,0x65],[576,8,0x68],[640,8,0x67],[704,9,0xCC],[768,9,0xCD],[832,9,0xD2],[896,9,0xD3],
            [960,9,0xD4],[1024,9,0xD5],[1088,9,0xD6],[1152,9,0xD7],[1216,9,0xD8],[1280,9,0xD9],[1344,9,0xDA],
            [1408,9,0xDB],[1472,9,0x98],[1536,9,0x99],[1600,9,0x9A],[1664,6,0x18],[1728,9,0x9B],
        ];
        foreach (var e in makeup) Add(t, e[1], e[2], e[0]);
        AddExtended(t);
        return t;
    }

    private static Dictionary<int, int> BuildBlack()
    {
        var t = new Dictionary<int, int>();
        int[][] term =
        [
            [0,10,0x37],[1,3,0x02],[2,2,0x03],[3,2,0x02],[4,3,0x03],[5,4,0x03],[6,4,0x02],[7,5,0x03],
            [8,6,0x05],[9,6,0x04],[10,7,0x04],[11,7,0x05],[12,7,0x07],[13,8,0x04],[14,8,0x07],[15,9,0x18],
            [16,10,0x17],[17,10,0x18],[18,10,0x08],[19,11,0x67],[20,11,0x68],[21,11,0x6C],[22,11,0x37],
            [23,11,0x28],[24,11,0x17],[25,11,0x18],[26,12,0xCA],[27,12,0xCB],[28,12,0xCC],[29,12,0xCD],
            [30,12,0x68],[31,12,0x69],[32,12,0x6A],[33,12,0x6B],[34,12,0xD2],[35,12,0xD3],[36,12,0xD4],
            [37,12,0xD5],[38,12,0xD6],[39,12,0xD7],[40,12,0x6C],[41,12,0x6D],[42,12,0xDA],[43,12,0xDB],
            [44,12,0x54],[45,12,0x55],[46,12,0x56],[47,12,0x57],[48,12,0x64],[49,12,0x65],[50,12,0x52],
            [51,12,0x53],[52,12,0x24],[53,12,0x37],[54,12,0x38],[55,12,0x27],[56,12,0x28],[57,12,0x58],
            [58,12,0x59],[59,12,0x2B],[60,12,0x2C],[61,12,0x5A],[62,12,0x66],[63,12,0x67],
        ];
        foreach (var e in term) Add(t, e[1], e[2], e[0]);

        int[][] makeup =
        [
            [64,10,0x0F],[128,12,0xC8],[192,12,0xC9],[256,12,0x5B],[320,12,0x33],[384,12,0x34],[448,12,0x35],
            [512,13,0x6C],[576,13,0x6D],[640,13,0x4A],[704,13,0x4B],[768,13,0x4C],[832,13,0x4D],[896,13,0x72],
            [960,13,0x73],[1024,13,0x74],[1088,13,0x75],[1152,13,0x76],[1216,13,0x77],[1280,13,0x52],
            [1344,13,0x53],[1408,13,0x54],[1472,13,0x55],[1536,13,0x5A],[1600,13,0x5B],[1664,13,0x64],[1728,13,0x65],
        ];
        foreach (var e in makeup) Add(t, e[1], e[2], e[0]);
        AddExtended(t);
        return t;
    }

    private static void AddExtended(Dictionary<int, int> t)
    {
        // Shared extended makeup codes (1792..2560).
        int[][] ext =
        [
            [1792,11,0x08],[1856,11,0x0C],[1920,11,0x0D],[1984,12,0x12],[2048,12,0x13],[2112,12,0x14],
            [2176,12,0x15],[2240,12,0x16],[2304,12,0x17],[2368,12,0x1C],[2432,12,0x1D],[2496,12,0x1E],[2560,12,0x1F],
        ];
        foreach (var e in ext) Add(t, e[1], e[2], e[0]);
    }

    private sealed class BitReader
    {
        private readonly byte[] _data;
        private int _bytePos;
        private int _bitPos;

        public BitReader(byte[] data) => _data = data;

        public bool AtEnd => _bytePos >= _data.Length;

        public int ReadBit()
        {
            if (_bytePos >= _data.Length)
            {
                return -1;
            }
            int bit = (_data[_bytePos] >> (7 - _bitPos)) & 1;
            if (++_bitPos == 8)
            {
                _bitPos = 0;
                _bytePos++;
            }
            return bit;
        }

        public void AlignToByte()
        {
            if (_bitPos != 0)
            {
                _bitPos = 0;
                _bytePos++;
            }
        }

        public (int, int) Save() => (_bytePos, _bitPos);

        public void Restore((int BytePos, int BitPos) state)
        {
            _bytePos = state.BytePos;
            _bitPos = state.BitPos;
        }
    }
}
