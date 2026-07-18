// Builds a well-formed OpenType 'cmap' table (a single Windows Unicode BMP
// (3,1) format-4 subtable) from a Unicode→glyph-id map. Used both by the CFF
// font writer and the TrueType synthetic-cmap path so the browser's OTS always
// sees a supported subtable.

namespace Bit.BlazorUI;

internal static class BitPdfCmapBuilder
{
    public static byte[] BuildUnicodeCmap(IReadOnlyDictionary<int, int> unicodeToGid)
    {
        var map = new SortedDictionary<int, int>();
        foreach (var (u, g) in unicodeToGid)
        {
            if (u is > 0 and <= 0xFFFF && g > 0)
            {
                map[u] = g;
            }
        }

        // Contiguous runs where gid - code is constant become one format-4 segment.
        var segs = new List<(int Start, int End, int Delta)>();
        int? runStart = null, prevCode = null, prevGid = null;
        foreach (var (code, gid) in map)
        {
            if (runStart is null)
            {
                runStart = code; prevCode = code; prevGid = gid;
                continue;
            }
            if (code == prevCode + 1 && gid == prevGid + 1)
            {
                prevCode = code; prevGid = gid;
                continue;
            }
            segs.Add((runStart.Value, prevCode!.Value, (map[runStart.Value] - runStart.Value) & 0xFFFF));
            runStart = code; prevCode = code; prevGid = gid;
        }
        if (runStart is not null)
        {
            segs.Add((runStart.Value, prevCode!.Value, (map[runStart.Value] - runStart.Value) & 0xFFFF));
        }
        segs.Add((0xFFFF, 0xFFFF, 1)); // required terminating segment

        int segCount = segs.Count;
        var sub = new List<byte>();
        void U16(int v) { sub.Add((byte)(v >> 8)); sub.Add((byte)v); }

        U16(4);                    // format
        int lengthPos = sub.Count;
        U16(0);                    // length (patched)
        U16(0);                    // language
        int sc2 = segCount * 2;
        U16(sc2);                  // segCountX2
        int sr = 2, es = 0;
        while (sr * 2 <= sc2) { sr *= 2; es++; }
        U16(sr);                   // searchRange
        U16(es);                   // entrySelector
        U16(sc2 - sr);             // rangeShift
        foreach (var s in segs) U16(s.End);
        U16(0);                    // reservedPad
        foreach (var s in segs) U16(s.Start);
        foreach (var s in segs) U16(s.Delta);
        foreach (var _ in segs) U16(0); // idRangeOffset
        int len = sub.Count;
        sub[lengthPos] = (byte)(len >> 8);
        sub[lengthPos + 1] = (byte)len;

        var cmap = new List<byte>();
        void C16(int v) { cmap.Add((byte)(v >> 8)); cmap.Add((byte)v); }
        void C32(int v) { cmap.Add((byte)(v >> 24)); cmap.Add((byte)(v >> 16)); cmap.Add((byte)(v >> 8)); cmap.Add((byte)v); }
        C16(0);   // version
        C16(1);   // numTables
        C16(3);   // platform Windows
        C16(1);   // encoding Unicode BMP
        C32(12);  // offset
        cmap.AddRange(sub);
        return cmap.ToArray();
    }
}
