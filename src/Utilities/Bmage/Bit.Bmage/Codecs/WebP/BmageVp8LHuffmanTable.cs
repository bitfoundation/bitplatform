namespace Bit.Bmage;

/// <summary>
/// A canonical prefix code with a two level lookup table: codes up to the root length resolve in one lookup, longer
/// ones through a second table per root prefix. Bits are read least significant first, so codes are stored reversed.
/// </summary>
internal sealed class BmageVp8LHuffmanTable
{
    private const int MaxLength = 15;

    // Each entry: symbol in the upper bits and the number of bits it consumes in the low 4; for a root entry that leads
    // to a second level table, the table's offset and its index width instead, flagged by a length above the root.
    private readonly int[] _entries;
    private readonly int _rootBits;

    private BmageVp8LHuffmanTable(int[] entries, int rootBits, int singleSymbol)
    {
        _entries = entries;
        _rootBits = rootBits;
        SingleSymbol = singleSymbol;
    }

    /// <summary>The only symbol when the code has just one, which then costs no bits; otherwise -1.</summary>
    public int SingleSymbol { get; }

    /// <summary>Builds the table, or returns null when the lengths do not form a complete prefix code.</summary>
    public static BmageVp8LHuffmanTable? Build(ReadOnlySpan<byte> lengths, int rootBits)
    {
        Span<int> count = stackalloc int[MaxLength + 1];
        var symbols = 0;
        var last = -1;
        for (var s = 0; s < lengths.Length; s++)
        {
            if (lengths[s] == 0) continue;
            count[lengths[s]]++;
            symbols++;
            last = s;
        }
        if (symbols == 0) return null;
        if (symbols == 1) return new BmageVp8LHuffmanTable([last << 4], 0, last);

        // A complete code fills the code space exactly.
        var space = 0;
        for (var len = 1; len <= MaxLength; len++) space += count[len] << (MaxLength - len);
        if (space != 1 << MaxLength) return null;

        Span<int> nextCode = stackalloc int[MaxLength + 2];
        var code = 0;
        for (var len = 1; len <= MaxLength; len++)
        {
            code = (code + count[len - 1]) << 1;
            nextCode[len] = code;
        }

        // Size the second level tables: for every root prefix, the longest code that starts with it.
        var rootSize = 1 << rootBits;
        var subBits = new int[rootSize];
        var codes = new int[lengths.Length];
        for (var s = 0; s < lengths.Length; s++)
        {
            int len = lengths[s];
            if (len == 0) continue;
            var reversed = Reverse(nextCode[len]++, len);
            codes[s] = reversed;
            if (len > rootBits)
            {
                var root = reversed & (rootSize - 1);
                subBits[root] = Math.Max(subBits[root], len - rootBits);
            }
        }
        var total = rootSize;
        var offsets = new int[rootSize];
        for (var r = 0; r < rootSize; r++)
        {
            if (subBits[r] == 0) continue;
            offsets[r] = total;
            total += 1 << subBits[r];
        }

        var entries = new int[total];
        for (var r = 0; r < rootSize; r++)
            if (subBits[r] != 0) entries[r] = (offsets[r] << 8) | ((rootBits + subBits[r]) << 4) | 0xF;
        for (var s = 0; s < lengths.Length; s++)
        {
            int len = lengths[s];
            if (len == 0) continue;
            var reversed = codes[s];
            if (len <= rootBits)
            {
                for (var i = reversed; i < rootSize; i += 1 << len) entries[i] = (s << 4) | len;
            }
            else
            {
                var root = reversed & (rootSize - 1);
                var width = subBits[root];
                var rest = reversed >> rootBits;
                var subLen = len - rootBits;
                for (var i = rest; i < 1 << width; i += 1 << subLen) entries[offsets[root] + i] = (s << 4) | subLen;
            }
        }
        return new BmageVp8LHuffmanTable(entries, rootBits, -1);
    }

    private static int Reverse(int code, int length)
    {
        var r = 0;
        for (var i = 0; i < length; i++) { r = (r << 1) | (code & 1); code >>= 1; }
        return r;
    }

    public int ReadSymbol(BmageVp8LBitReader br)
    {
        if (SingleSymbol >= 0) return SingleSymbol;
        var bits = br.Peek();
        var e = _entries[bits & ((1u << _rootBits) - 1)];
        if ((e & 0xF) != 0xF)
        {
            br.Skip(e & 0xF);
            return e >> 4;
        }
        var width = ((e >> 4) & 0xF) - _rootBits;
        br.Skip(_rootBits);
        var sub = _entries[(e >> 8) + (int)((bits >> _rootBits) & ((1u << width) - 1))];
        br.Skip(sub & 0xF);
        return sub >> 4;
    }
}
