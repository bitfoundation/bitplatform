// Assembles glyph outlines into an OpenType/CFF font the browser can load via
// @font-face. Outlines are re-encoded as Type2 charstrings inside a minimal CFF
// table, wrapped in an sfnt ('OTTO') with the tables OTS requires (cmap, head,
// hhea, hmtx, maxp, name, OS/2, post). Coordinates are normalized to a 1000-unit
// em so the default CFF FontMatrix applies (no CFF real-number encoding needed).
//
// Rendering, not hinting, is the goal: the cmap maps the Unicode codepoints the
// renderer emits (glyph-name → Unicode via the Adobe Glyph List) to glyph IDs.

using System.Text;

namespace Bit.BlazorUI;

internal static class BitPdfCffFontWriter
{
    private sealed class Glyph
    {
        public required string Name;
        public required int Advance;   // 1000-em units
        public required byte[] Charstring;
        public int Unicode = -1;
    }

    /// <summary>Builds an OpenType/CFF font from a parsed Type1 font, or null.</summary>
    public static byte[]? FromType1(BitPdfType1Font t1, string psName)
    {
        try
        {
            double[] fm = t1.FontMatrix;
            // Per-axis scale to normalize to a 1000-unit em (default CFF matrix).
            double sx = fm[0] * 1000.0, sy = fm[3] * 1000.0;
            double shx = fm[2] * 1000.0, shy = fm[1] * 1000.0; // skew terms (usually 0)

            var glyphs = new List<Glyph> { NotdefGlyph() };
            foreach (var name in t1.CharStrings.Keys)
            {
                if (name == ".notdef")
                {
                    continue;
                }
                BitPdfGlyphOutline? outline = t1.BuildOutline(name);
                if (outline is null)
                {
                    continue;
                }
                byte[] cs = BuildType2(outline, sx, sy, shx, shy, out int advance);
                int uni = UnicodeOf(name);
                glyphs.Add(new Glyph { Name = name, Advance = advance, Charstring = cs, Unicode = uni });
            }

            if (glyphs.Count <= 1)
            {
                return null;
            }

            byte[] cff = BuildCff(glyphs, psName);
            return BuildSfnt(glyphs, cff, psName);
        }
        catch
        {
            return null;
        }
    }

    private static Glyph NotdefGlyph()
        => new() { Name = ".notdef", Advance = 500, Charstring = new byte[] { 14 } /* endchar */, Unicode = -1 };

    private static int UnicodeOf(string name)
    {
        string u = BitPdfGlyphList.ToUnicode(name);
        return u.Length == 1 ? u[0] : (u.Length >= 1 ? char.ConvertToUtf32(u, 0) : -1);
    }

    // ----- Type2 charstring encoding -----

    private static byte[] BuildType2(BitPdfGlyphOutline outline, double sx, double sy, double shx, double shy, out int advance)
    {
        advance = (int)Math.Round(outline.AdvanceWidth * sx);

        var body = new List<byte>();
        double curX = 0, curY = 0;
        bool first = true;

        (int X, int Y) Map(double x, double y)
            => ((int)Math.Round(x * sx + y * shx), (int)Math.Round(x * shy + y * sy));

        foreach (var seg in outline.Segments)
        {
            switch (seg.Op)
            {
                case BitPdfPathSeg.Kind.Move:
                {
                    var (mx, my) = Map(seg.X1, seg.Y1);
                    if (first)
                    {
                        // Prepend the advance width (nominalWidthX = 0).
                        EncodeNum(body, advance);
                        first = false;
                    }
                    EncodeNum(body, mx - (int)curX);
                    EncodeNum(body, my - (int)curY);
                    body.Add(21); // rmoveto
                    curX = mx;
                    curY = my;
                    break;
                }
                case BitPdfPathSeg.Kind.Line:
                {
                    var (lx, ly) = Map(seg.X1, seg.Y1);
                    EncodeNum(body, lx - (int)curX);
                    EncodeNum(body, ly - (int)curY);
                    body.Add(5); // rlineto
                    curX = lx;
                    curY = ly;
                    break;
                }
                case BitPdfPathSeg.Kind.Curve:
                {
                    var (x1, y1) = Map(seg.X1, seg.Y1);
                    var (x2, y2) = Map(seg.X2, seg.Y2);
                    var (x3, y3) = Map(seg.X3, seg.Y3);
                    EncodeNum(body, x1 - (int)curX);
                    EncodeNum(body, y1 - (int)curY);
                    EncodeNum(body, x2 - x1);
                    EncodeNum(body, y2 - y1);
                    EncodeNum(body, x3 - x2);
                    EncodeNum(body, y3 - y2);
                    body.Add(8); // rrcurveto
                    curX = x3;
                    curY = y3;
                    break;
                }
                case BitPdfPathSeg.Kind.Close:
                    break; // Type2 closes implicitly on moveto/endchar
            }
        }

        if (first)
        {
            // Empty glyph: still emit the width.
            EncodeNum(body, advance);
        }
        body.Add(14); // endchar
        return body.ToArray();
    }

    private static void EncodeNum(List<byte> b, int v)
    {
        if (v is >= -107 and <= 107)
        {
            b.Add((byte)(v + 139));
        }
        else if (v is >= 108 and <= 1131)
        {
            v -= 108;
            b.Add((byte)((v >> 8) + 247));
            b.Add((byte)(v & 0xFF));
        }
        else if (v is >= -1131 and <= -108)
        {
            v = -v - 108;
            b.Add((byte)((v >> 8) + 251));
            b.Add((byte)(v & 0xFF));
        }
        else if (v is >= -32768 and <= 32767)
        {
            b.Add(28);
            b.Add((byte)(v >> 8));
            b.Add((byte)v);
        }
        else
        {
            b.Add(29);
            b.Add((byte)(v >> 24));
            b.Add((byte)(v >> 16));
            b.Add((byte)(v >> 8));
            b.Add((byte)v);
        }
    }

    // ----- CFF table -----

    private static byte[] BuildCff(List<Glyph> glyphs, string psName)
    {
        int nGlyphs = glyphs.Count;

        // String INDEX: every non-.notdef glyph name gets a custom SID (391 + i).
        var strings = new List<string>();
        var sidOf = new int[nGlyphs];
        for (int g = 1; g < nGlyphs; g++)
        {
            sidOf[g] = 391 + strings.Count;
            strings.Add(glyphs[g].Name);
        }
        // The font name itself also needs a string for the Name INDEX (SID unused there).

        // Charset (format 0): SIDs for gid 1..n-1.
        var charset = new List<byte> { 0 };
        for (int g = 1; g < nGlyphs; g++)
        {
            charset.Add((byte)(sidOf[g] >> 8));
            charset.Add((byte)(sidOf[g] & 0xFF));
        }

        // CharStrings INDEX.
        byte[] charStringsIndex = BuildIndex(glyphs.Select(g => g.Charstring).ToList());

        // Private DICT: defaultWidthX (0) nominalWidthX (0). Operators 20 / 21.
        var priv = new List<byte>();
        EncodeDictInt(priv, 0);
        priv.Add(20); // defaultWidthX
        EncodeDictInt(priv, 0);
        priv.Add(21); // nominalWidthX
        byte[] privateDict = priv.ToArray();

        byte[] nameIndex = BuildIndex(new List<byte[]> { Encoding1252(psName) });
        byte[] stringIndex = BuildIndex(strings.Select(Encoding1252).ToList());
        byte[] globalSubrs = BuildIndex(new List<byte[]>()); // empty

        // Layout: header | Name INDEX | TopDICT INDEX | String INDEX | GlobalSubrs
        //         | charset | CharStrings | Private DICT
        // Top DICT holds absolute offsets to charset, CharStrings and Private, so
        // build with fixed 5-byte integer operands to keep its size stable.
        const int header = 4;

        // First compute the Top DICT size with placeholder offsets (always 5 bytes).
        byte[] topDict = BuildTopDict(0, 0, privateDict.Length, 0);
        byte[] topIndex = BuildIndex(new List<byte[]> { topDict });

        int off = header + nameIndex.Length + topIndex.Length + stringIndex.Length + globalSubrs.Length;
        int charsetOff = off;
        off += charset.Count;
        int charStringsOff = off;
        off += charStringsIndex.Length;
        int privateOff = off;

        // Rebuild Top DICT with real offsets (same size → offsets stay valid).
        topDict = BuildTopDict(charsetOff, charStringsOff, privateDict.Length, privateOff);
        topIndex = BuildIndex(new List<byte[]> { topDict });

        var cff = new List<byte>
        {
            1, 0, 4, 2, // major, minor, hdrSize, offSize
        };
        cff.AddRange(nameIndex);
        cff.AddRange(topIndex);
        cff.AddRange(stringIndex);
        cff.AddRange(globalSubrs);
        cff.AddRange(charset);
        cff.AddRange(charStringsIndex);
        cff.AddRange(privateDict);
        return cff.ToArray();
    }

    private static byte[] BuildTopDict(int charsetOff, int charStringsOff, int privSize, int privOff)
    {
        var d = new List<byte>();
        // ROS-less, non-CID. version (SID) optional; omit. Set required offsets.
        EncodeDictInt5(d, charsetOff);
        d.Add(15); // charset
        EncodeDictInt5(d, charStringsOff);
        d.Add(17); // CharStrings
        // Private: size then offset (two operands), operator 18.
        EncodeDictInt5(d, privSize);
        EncodeDictInt5(d, privOff);
        d.Add(18); // Private
        return d.ToArray();
    }

    // CFF INDEX: count(2) offSize(1) offsets[(count+1)*offSize] data.
    private static byte[] BuildIndex(List<byte[]> items)
    {
        var outBytes = new List<byte>();
        int count = items.Count;
        outBytes.Add((byte)(count >> 8));
        outBytes.Add((byte)(count & 0xFF));
        if (count == 0)
        {
            return outBytes.ToArray();
        }

        int total = items.Sum(x => x.Length);
        int offSize = total + 1 <= 0xFF ? 1 : total + 1 <= 0xFFFF ? 2 : total + 1 <= 0xFFFFFF ? 3 : 4;
        outBytes.Add((byte)offSize);

        int offset = 1;
        void WriteOff(int o)
        {
            for (int k = offSize - 1; k >= 0; k--)
            {
                outBytes.Add((byte)(o >> (k * 8)));
            }
        }
        WriteOff(offset);
        foreach (var it in items)
        {
            offset += it.Length;
            WriteOff(offset);
        }
        foreach (var it in items)
        {
            outBytes.AddRange(it);
        }
        return outBytes.ToArray();
    }

    private static void EncodeDictInt(List<byte> b, int v)
    {
        if (v is >= -107 and <= 107)
        {
            b.Add((byte)(v + 139));
        }
        else if (v is >= 108 and <= 1131)
        {
            v -= 108;
            b.Add((byte)((v >> 8) + 247));
            b.Add((byte)(v & 0xFF));
        }
        else if (v is >= -1131 and <= -108)
        {
            v = -v - 108;
            b.Add((byte)((v >> 8) + 251));
            b.Add((byte)(v & 0xFF));
        }
        else
        {
            EncodeDictInt5(b, v);
        }
    }

    // Fixed 5-byte 32-bit integer operand (CFF operator 29).
    private static void EncodeDictInt5(List<byte> b, int v)
    {
        b.Add(29);
        b.Add((byte)(v >> 24));
        b.Add((byte)(v >> 16));
        b.Add((byte)(v >> 8));
        b.Add((byte)v);
    }

    private static byte[] Encoding1252(string s) => Encoding.Latin1.GetBytes(s);

    // ----- OpenType sfnt wrapper -----

    /// <summary>
    /// Wraps an already-formed CFF ('/FontFile3 /Type1C') in an OpenType sfnt with
    /// a synthetic Unicode cmap, without re-encoding the charstrings.
    /// </summary>
    // advances1000 (optional): per-glyph advance widths, glyph-id indexed, in
    // 1000-em units, for the hmtx table so proportional spacing is preserved; a
    // uniform advance is used when null or an entry is 0.
    public static byte[]? WrapBareCff(byte[] cff, int numGlyphs, double[] fontMatrix, byte[] cmap, string psName,
        int[]? advances1000 = null)
    {
        try
        {
            if (numGlyphs <= 0)
            {
                return null;
            }
            int upm = fontMatrix.Length >= 1 && fontMatrix[0] != 0
                ? (int)Math.Round(1.0 / fontMatrix[0])
                : 1000;
            if (upm is < 16 or > 16384)
            {
                upm = 1000;
            }
            int defaultAdvance = upm / 2;

            byte[] hmtx = BuildHmtx(numGlyphs, advances1000, upm, defaultAdvance);
            int maxAdvance = MaxAdvance(hmtx, numGlyphs, defaultAdvance);

            var tables = new List<(string Tag, byte[] Data)>
            {
                ("CFF ", cff),
                ("cmap", cmap),
                ("head", BuildHead(upm)),
                ("hhea", BuildHheaFixed(numGlyphs, maxAdvance, upm)),
                ("hmtx", hmtx),
                ("maxp", BuildMaxp(numGlyphs)),
                ("name", BuildName(psName)),
                ("OS/2", BuildOs2(upm)),
                ("post", BuildPost()),
            };
            return AssembleSfnt(tables);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] BuildHmtx(int numGlyphs, int[]? advances1000, int upm, int defaultAdvance)
    {
        var b = new byte[numGlyphs * 4];
        for (int i = 0; i < numGlyphs; i++)
        {
            int adv = advances1000 is not null && i < advances1000.Length && advances1000[i] > 0
                ? (int)Math.Round(advances1000[i] * (double)upm / 1000)
                : defaultAdvance;
            WriteU16(b, i * 4, (ushort)Math.Clamp(adv, 0, 0xFFFF));
        }
        return b;
    }

    private static int MaxAdvance(byte[] hmtx, int numGlyphs, int fallback)
    {
        int max = fallback;
        for (int i = 0; i < numGlyphs; i++)
        {
            max = Math.Max(max, (hmtx[i * 4] << 8) | hmtx[i * 4 + 1]);
        }
        return max;
    }

    private static byte[] BuildSfnt(List<Glyph> glyphs, byte[] cff, string psName)
    {
        int n = glyphs.Count;
        var tables = new List<(string Tag, byte[] Data)>
        {
            ("CFF ", cff),
            ("cmap", BuildCmap(glyphs)),
            ("head", BuildHead(1000)),
            ("hhea", BuildHhea(glyphs)),
            ("hmtx", BuildHmtx(glyphs)),
            ("maxp", BuildMaxp(n)),
            ("name", BuildName(psName)),
            ("OS/2", BuildOs2(1000)),
            ("post", BuildPost()),
        };
        return AssembleSfnt(tables);
    }

    private static byte[] AssembleSfnt(List<(string Tag, byte[] Data)> tables)
    {
        tables.Sort((a, b) => string.CompareOrdinal(a.Tag, b.Tag));

        int numTables = tables.Count;
        int headerSize = 12 + numTables * 16;
        var offsets = new int[numTables];
        int pos = headerSize;
        for (int i = 0; i < numTables; i++)
        {
            offsets[i] = pos;
            pos += (tables[i].Data.Length + 3) & ~3;
        }

        var buf = new byte[pos];
        WriteU32(buf, 0, 0x4F54544F); // 'OTTO'
        WriteU16(buf, 4, (ushort)numTables);
        int sr = 16, es = 0;
        while (sr * 2 <= numTables * 16) { sr *= 2; es++; }
        WriteU16(buf, 6, (ushort)sr);
        WriteU16(buf, 8, (ushort)es);
        WriteU16(buf, 10, (ushort)(numTables * 16 - sr));

        int headOffset = -1;
        for (int i = 0; i < numTables; i++)
        {
            var (tag, data) = tables[i];
            Array.Copy(data, 0, buf, offsets[i], data.Length);
            int rec = 12 + i * 16;
            for (int k = 0; k < 4; k++)
            {
                buf[rec + k] = (byte)tag[k];
            }
            WriteU32(buf, rec + 4, Checksum(buf, offsets[i], data.Length));
            WriteU32(buf, rec + 8, (uint)offsets[i]);
            WriteU32(buf, rec + 12, (uint)data.Length);
            if (tag == "head")
            {
                headOffset = offsets[i];
            }
        }

        // head.checkSumAdjustment = 0xB1B0AFBA - checksum(whole file, field zeroed).
        if (headOffset >= 0)
        {
            WriteU32(buf, headOffset + 8, 0);
            uint fileSum = Checksum(buf, 0, buf.Length);
            WriteU32(buf, headOffset + 8, unchecked(0xB1B0AFBA - fileSum));
        }
        return buf;
    }

    private static byte[] BuildCmap(List<Glyph> glyphs)
    {
        // Collect BMP Unicode → gid, ascending.
        var map = new SortedDictionary<int, int>();
        for (int g = 1; g < glyphs.Count; g++)
        {
            int u = glyphs[g].Unicode;
            if (u is > 0 and <= 0xFFFF && !map.ContainsKey(u))
            {
                map[u] = g;
            }
        }

        // Build format-4 segments: contiguous runs where gid - code is constant.
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
        U16(0);                    // length (patched below)
        U16(0);                    // language
        int sc2 = segCount * 2;
        U16(sc2);                  // segCountX2
        int sr = 2, es = 0;
        while (sr * 2 <= sc2) { sr *= 2; es++; }
        U16(sr);                   // searchRange
        U16(es);                   // entrySelector
        U16(sc2 - sr);             // rangeShift
        foreach (var s in segs) U16(s.End);      // endCode
        U16(0);                    // reservedPad
        foreach (var s in segs) U16(s.Start);    // startCode
        foreach (var s in segs) U16(s.Delta);    // idDelta
        foreach (var _ in segs) U16(0);          // idRangeOffset (all 0)
        // Patch length.
        int len = sub.Count;
        sub[lengthPos] = (byte)(len >> 8);
        sub[lengthPos + 1] = (byte)len;

        // cmap header: version(0) numTables(1) + one (3,1) record.
        var cmap = new List<byte>();
        void C16(int v) { cmap.Add((byte)(v >> 8)); cmap.Add((byte)v); }
        void C32(int v) { cmap.Add((byte)(v >> 24)); cmap.Add((byte)(v >> 16)); cmap.Add((byte)(v >> 8)); cmap.Add((byte)v); }
        C16(0);
        C16(1);
        C16(3); // platform Windows
        C16(1); // encoding Unicode BMP
        C32(12); // offset to subtable
        cmap.AddRange(sub);
        return cmap.ToArray();
    }

    private static byte[] BuildHead(int upm)
    {
        var b = new byte[54];
        WriteU32(b, 0, 0x00010000);   // version
        WriteU32(b, 4, 0x00010000);   // fontRevision
        // checkSumAdjustment (8) patched after assembly.
        WriteU32(b, 12, 0x5F0F3CF5);  // magic
        WriteU16(b, 16, 0);           // flags
        WriteU16(b, 18, (ushort)upm); // unitsPerEm
        // created/modified (8+8) left zero.
        WriteI16(b, 36, (short)(-upm / 5));      // xMin
        WriteI16(b, 38, (short)(-upm / 5));      // yMin
        WriteI16(b, 40, (short)(upm * 6 / 5));   // xMax
        WriteI16(b, 42, (short)(upm * 9 / 10));  // yMax
        WriteU16(b, 44, 0);           // macStyle
        WriteU16(b, 46, 8);           // lowestRecPPEM
        WriteI16(b, 48, 2);           // fontDirectionHint
        WriteI16(b, 50, 0);           // indexToLocFormat
        WriteI16(b, 52, 0);           // glyphDataFormat
        return b;
    }

    private static byte[] BuildHheaFixed(int numGlyphs, int advance, int upm)
    {
        var b = new byte[36];
        WriteU32(b, 0, 0x00010000);
        WriteI16(b, 4, (short)(upm * 4 / 5));   // ascent
        WriteI16(b, 6, (short)(-upm / 5));      // descent
        WriteI16(b, 8, 0);                      // lineGap
        WriteU16(b, 10, (ushort)Math.Clamp(advance, 0, 0xFFFF)); // advanceWidthMax
        WriteI16(b, 16, (short)Math.Clamp(advance, 0, 0x7FFF));  // xMaxExtent
        WriteI16(b, 18, 1);                     // caretSlopeRise
        WriteU16(b, 34, (ushort)numGlyphs);     // numberOfHMetrics
        return b;
    }

    private static byte[] BuildHhea(List<Glyph> glyphs)
    {
        int maxAdv = glyphs.Max(g => g.Advance);
        var b = new byte[36];
        WriteU32(b, 0, 0x00010000);
        WriteI16(b, 4, 800);          // ascent
        WriteI16(b, 6, -200);         // descent
        WriteI16(b, 8, 0);            // lineGap
        WriteU16(b, 10, (ushort)Math.Clamp(maxAdv, 0, 0xFFFF)); // advanceWidthMax
        WriteI16(b, 12, 0);           // minLeftSideBearing
        WriteI16(b, 14, 0);           // minRightSideBearing
        WriteI16(b, 16, (short)Math.Clamp(maxAdv, 0, 0x7FFF)); // xMaxExtent
        WriteI16(b, 18, 1);           // caretSlopeRise
        WriteI16(b, 20, 0);           // caretSlopeRun
        // reserved (10 bytes) zero
        WriteI16(b, 32, 0);           // metricDataFormat
        WriteU16(b, 34, (ushort)glyphs.Count); // numberOfHMetrics
        return b;
    }

    private static byte[] BuildHmtx(List<Glyph> glyphs)
    {
        var b = new byte[glyphs.Count * 4];
        for (int i = 0; i < glyphs.Count; i++)
        {
            WriteU16(b, i * 4, (ushort)Math.Clamp(glyphs[i].Advance, 0, 0xFFFF));
            WriteI16(b, i * 4 + 2, 0); // lsb
        }
        return b;
    }

    private static byte[] BuildMaxp(int numGlyphs)
    {
        var b = new byte[6];
        WriteU32(b, 0, 0x00005000); // version 0.5 (CFF)
        WriteU16(b, 4, (ushort)numGlyphs);
        return b;
    }

    private static byte[] BuildName(string psName)
    {
        // Minimal name table: records 1 (family), 2 (subfamily), 4 (full), 6 (PS)
        // for platform (3,1,0x409). Strings are UTF-16BE.
        string[] vals = { psName, "Regular", psName, psName };
        int[] ids = { 1, 2, 4, 6 };
        var strings = vals.Select(v => Encoding.BigEndianUnicode.GetBytes(v)).ToArray();

        int count = ids.Length;
        var b = new List<byte>();
        void U16(int v) { b.Add((byte)(v >> 8)); b.Add((byte)v); }
        U16(0);                 // format
        U16(count);             // count
        int storageOffset = 6 + count * 12;
        U16(storageOffset);     // stringOffset
        int off = 0;
        for (int i = 0; i < count; i++)
        {
            U16(3);             // platform Windows
            U16(1);             // encoding Unicode
            U16(0x409);         // language en-US
            U16(ids[i]);        // nameID
            U16(strings[i].Length);
            U16(off);
            off += strings[i].Length;
        }
        foreach (var s in strings)
        {
            b.AddRange(s);
        }
        return b.ToArray();
    }

    private static byte[] BuildOs2(int upm)
    {
        var b = new byte[96]; // version 4
        WriteU16(b, 0, 4);            // version
        WriteI16(b, 2, (short)(upm / 2));   // xAvgCharWidth
        WriteU16(b, 4, 400);          // usWeightClass
        WriteU16(b, 6, 5);            // usWidthClass
        WriteU16(b, 8, 0);            // fsType (installable)
        // subscript/superscript/strikeout (10..30) left zero.
        WriteI16(b, 30, 0);           // sFamilyClass
        // panose (10 bytes @ 32) zero
        WriteU32(b, 42, 1); // ulUnicodeRange1: Basic Latin
        for (int k = 0; k < 4; k++) { b[58 + k] = (byte)"BLZR"[k]; } // achVendID
        WriteU16(b, 62, 0x40);        // fsSelection (REGULAR)
        WriteU16(b, 64, 0x20);        // usFirstCharIndex
        WriteU16(b, 66, 0xFFFF);      // usLastCharIndex
        WriteI16(b, 68, (short)(upm * 4 / 5));  // sTypoAscender
        WriteI16(b, 70, (short)(-upm / 5));     // sTypoDescender
        WriteI16(b, 72, (short)(upm / 5));      // sTypoLineGap
        WriteU16(b, 74, (ushort)upm); // usWinAscent
        WriteU16(b, 76, (ushort)(upm / 5)); // usWinDescent
        WriteU32(b, 78, 1);           // ulCodePageRange1: Latin 1
        WriteU32(b, 82, 0);           // ulCodePageRange2
        WriteI16(b, 86, (short)(upm / 2));      // sxHeight
        WriteI16(b, 88, (short)(upm * 7 / 10)); // sCapHeight
        WriteU16(b, 90, 0);           // usDefaultChar
        WriteU16(b, 92, 0x20);        // usBreakChar
        WriteU16(b, 94, 0);           // usMaxContext
        return b;
    }

    private static byte[] BuildPost()
    {
        var b = new byte[32];
        WriteU32(b, 0, 0x00030000); // version 3.0 (no glyph names)
        // italicAngle, underlinePosition/Thickness, isFixedPitch, mem usage: zero.
        return b;
    }

    private static uint Checksum(byte[] data, int offset, int length)
    {
        uint sum = 0;
        int i = offset;
        int end = offset + length;
        while (i + 4 <= end)
        {
            sum = unchecked(sum + (((uint)data[i] << 24) | ((uint)data[i + 1] << 16) | ((uint)data[i + 2] << 8) | data[i + 3]));
            i += 4;
        }
        if (i < end)
        {
            uint last = 0;
            for (int k = 0; k < 4; k++)
            {
                last = (last << 8) | (uint)(i < end ? data[i] : 0);
                i++;
            }
            sum = unchecked(sum + last);
        }
        return sum;
    }

    private static void WriteU32(byte[] b, int o, uint v)
    {
        b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v;
    }
    private static void WriteU16(byte[] b, int o, ushort v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
    private static void WriteI16(byte[] b, int o, short v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
}
