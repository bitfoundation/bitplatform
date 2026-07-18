// A conservative TrueType/OpenType (sfnt) sanitizer. Embedded PDF subset fonts
// frequently ship with an unsorted table directory, wrong table checksums, a
// wrong head.checkSumAdjustment, or unpadded tables - all of which strict font
// parsers (including the browser's OTS) reject, so the @font-face silently fails
// to load. This rebuilds a structurally valid sfnt: recognized tables are kept
// verbatim, the directory is re-sorted, tables are 4-byte aligned, and all
// checksums are recomputed. It does not repair broken table *contents*; when the
// input cannot be parsed it returns null and the caller keeps the raw bytes.

using System.Linq;

namespace Bit.BlazorUI;

internal static class BitPdfTrueTypeSanitizer
{
    /// <summary>
    /// Returns a structurally-normalized copy of an sfnt font, or <c>null</c> when
    /// the input is not a parseable sfnt (caller should keep the original bytes).
    /// </summary>
    public static byte[]? Sanitize(byte[] input) => Sanitize(input, null);

    /// <summary>
    /// As <see cref="Sanitize(byte[])"/>, but replaces the font's <c>cmap</c> with
    /// <paramref name="replacementCmap"/> when supplied - used to inject a clean
    /// synthetic Unicode cmap for subset fonts whose own cmap OTS rejects.
    /// </summary>
    public static byte[]? Sanitize(byte[] input, byte[]? replacementCmap)
    {
        if (input.Length < 12)
        {
            return null;
        }

        uint version = ReadU32(input, 0);
        // Accept TrueType (0x00010000 / 'true' / 'ttcf' not handled), and OpenType
        // with CFF ('OTTO'). Reject anything else.
        bool isKnown = version is 0x00010000 or 0x74727565 /*true*/ or 0x4F54544F /*OTTO*/;
        if (!isKnown)
        {
            return null;
        }

        int numTables = ReadU16(input, 4);
        if (numTables == 0 || numTables > 4096)
        {
            return null;
        }

        int dirOffset = 12;
        if (dirOffset + numTables * 16 > input.Length)
        {
            return null;
        }

        var tables = new List<(uint Tag, byte[] Data)>(numTables);
        var seen = new HashSet<uint>();
        for (int i = 0; i < numTables; i++)
        {
            int rec = dirOffset + i * 16;
            uint tag = ReadU32(input, rec);
            int off = (int)ReadU32(input, rec + 8);
            int len = (int)ReadU32(input, rec + 12);
            if (off < 0 || len < 0 || (long)off + len > input.Length)
            {
                return null; // corrupt directory entry
            }
            if (!seen.Add(tag))
            {
                continue; // drop duplicate tables
            }
            var data = new byte[len];
            Array.Copy(input, off, data, 0, len);
            tables.Add((tag, data));
        }

        // Require the tables every consumer needs to be present.
        if (!seen.Contains(Tag("head")) || !seen.Contains(Tag("maxp")) || !seen.Contains(Tag("hhea")))
        {
            return null;
        }

        // Replace the cmap with a clean synthetic one when provided (the font's
        // own cmap subtable is often one OTS refuses to load).
        if (replacementCmap is not null)
        {
            tables.RemoveAll(static t => t.Tag == Tag("cmap"));
            seen.Remove(Tag("cmap"));
            tables.Add((Tag("cmap"), replacementCmap));
            seen.Add(Tag("cmap"));
        }

        // OTS (the browser's sanitizer) additionally *requires* OS/2, name and
        // post for a downloadable font. Subset fonts frequently strip them, so
        // synthesize minimal, self-consistent versions when absent.
        SynthesizeRequired(tables, seen);

        // The sfnt spec requires the table directory sorted ascending by tag.
        tables.Sort(static (a, b) => a.Tag.CompareTo(b.Tag));

        return Serialize(version, tables);
    }

    private static void SynthesizeRequired(List<(uint Tag, byte[] Data)> tables, HashSet<uint> seen)
    {
        // Read metrics from head/hhea/maxp so the synthesized tables agree.
        byte[]? head = Find(tables, Tag("head"));
        byte[]? hhea = Find(tables, Tag("hhea"));
        byte[]? maxp = Find(tables, Tag("maxp"));

        int unitsPerEm = head is { Length: >= 20 } ? ReadU16(head, 18) : 1000;
        if (unitsPerEm <= 0)
        {
            unitsPerEm = 1000;
        }
        short ascent = hhea is { Length: >= 8 } ? ReadI16(hhea, 4) : (short)(unitsPerEm * 0.8);
        short descent = hhea is { Length: >= 8 } ? ReadI16(hhea, 6) : (short)(-unitsPerEm * 0.2);
        int numGlyphs = maxp is { Length: >= 6 } ? ReadU16(maxp, 4) : 0;

        if (!seen.Contains(Tag("OS/2")))
        {
            tables.Add((Tag("OS/2"), BuildOs2(ascent, descent, unitsPerEm)));
            seen.Add(Tag("OS/2"));
        }
        if (!seen.Contains(Tag("name")))
        {
            tables.Add((Tag("name"), BuildName()));
            seen.Add(Tag("name"));
        }
        if (!seen.Contains(Tag("post")))
        {
            tables.Add((Tag("post"), BuildPost()));
            seen.Add(Tag("post"));
        }
        _ = numGlyphs;
    }

    private static byte[]? Find(List<(uint Tag, byte[] Data)> tables, uint tag)
    {
        foreach (var (t, d) in tables)
        {
            if (t == tag)
            {
                return d;
            }
        }
        return null;
    }

    private static byte[] BuildOs2(short ascent, short descent, int unitsPerEm)
    {
        var b = new byte[96]; // version 4
        void U16(int o, int v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
        void I16(int o, int v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
        void U32(int o, uint v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

        U16(0, 4);                          // version
        I16(2, unitsPerEm / 2);             // xAvgCharWidth
        U16(4, 400);                        // usWeightClass
        U16(6, 5);                          // usWidthClass
        U16(8, 0);                          // fsType (installable)
        // subscript/superscript/strikeout (10..30) left zero.
        I16(30, 0);                         // sFamilyClass
        U32(42, 1);                         // ulUnicodeRange1: Basic Latin
        for (int k = 0; k < 4; k++) { b[58 + k] = (byte)"BLZR"[k]; } // achVendID
        U16(62, 0x40);                      // fsSelection: REGULAR
        U16(64, 0x20);                      // usFirstCharIndex
        U16(66, 0xFFFF);                    // usLastCharIndex
        I16(68, ascent);                    // sTypoAscender
        I16(70, descent);                   // sTypoDescender
        I16(72, unitsPerEm / 5);            // sTypoLineGap
        U16(74, (ushort)Math.Clamp(ascent, 0, 0xFFFF));         // usWinAscent
        U16(76, (ushort)Math.Clamp(-descent, 0, 0xFFFF));       // usWinDescent
        U32(78, 1);                         // ulCodePageRange1: Latin 1
        I16(86, unitsPerEm / 2);            // sxHeight
        I16(88, (short)(unitsPerEm * 0.7)); // sCapHeight
        U16(90, 0);                         // usDefaultChar
        U16(92, 0x20);                      // usBreakChar
        U16(94, 0);                         // usMaxContext
        return b;
    }

    private static byte[] BuildName()
    {
        string[] vals = { "BitPdf", "Regular", "BitPdf", "BitPdf" };
        int[] ids = { 1, 2, 4, 6 };
        var strings = vals.Select(System.Text.Encoding.BigEndianUnicode.GetBytes).ToArray();
        int count = ids.Length;
        var b = new List<byte>();
        void U16(int v) { b.Add((byte)(v >> 8)); b.Add((byte)v); }
        U16(0);                             // format
        U16(count);                         // count
        int storageOffset = 6 + count * 12;
        U16(storageOffset);                 // stringOffset
        int off = 0;
        for (int i = 0; i < count; i++)
        {
            U16(3); U16(1); U16(0x409); U16(ids[i]); U16(strings[i].Length); U16(off);
            off += strings[i].Length;
        }
        foreach (var s in strings)
        {
            b.AddRange(s);
        }
        return b.ToArray();
    }

    private static byte[] BuildPost()
    {
        var b = new byte[32];
        b[0] = 0x00; b[1] = 0x03; b[2] = 0x00; b[3] = 0x00; // version 3.0
        return b;
    }

    private static byte[] Serialize(uint version, List<(uint Tag, byte[] Data)> tables)
    {
        int n = tables.Count;
        int headerSize = 12 + n * 16;

        // Lay out table bodies 4-byte aligned after the directory.
        var offsets = new int[n];
        int pos = headerSize;
        for (int i = 0; i < n; i++)
        {
            offsets[i] = pos;
            pos += (tables[i].Data.Length + 3) & ~3;
        }
        int total = pos;

        var output = new byte[total];

        // Offset table.
        WriteU32(output, 0, version);
        WriteU16(output, 4, (ushort)n);
        int entrySelector = 0;
        int searchRange = 16;
        while (searchRange * 2 <= n * 16)
        {
            searchRange *= 2;
            entrySelector++;
        }
        WriteU16(output, 6, (ushort)searchRange);
        WriteU16(output, 8, (ushort)entrySelector);
        WriteU16(output, 10, (ushort)(n * 16 - searchRange));

        // Directory + bodies with recomputed checksums.
        int headDirRec = -1;
        for (int i = 0; i < n; i++)
        {
            var (tag, data) = tables[i];
            Array.Copy(data, 0, output, offsets[i], data.Length);
            uint checksum = TableChecksum(output, offsets[i], data.Length);

            int rec = 12 + i * 16;
            WriteU32(output, rec, tag);
            WriteU32(output, rec + 4, checksum);
            WriteU32(output, rec + 8, (uint)offsets[i]);
            WriteU32(output, rec + 12, (uint)data.Length);

            if (tag == Tag("head"))
            {
                headDirRec = i;
            }
        }

        // head.checkSumAdjustment = 0xB1B0AFBA - checksum(whole file with the
        // adjustment field zeroed).
        if (headDirRec >= 0)
        {
            int headOffset = offsets[headDirRec];
            if (headOffset + 12 <= output.Length)
            {
                WriteU32(output, headOffset + 8, 0); // zero the adjustment field
                uint fileChecksum = TableChecksum(output, 0, output.Length);
                WriteU32(output, headOffset + 8, unchecked(0xB1B0AFBA - fileChecksum));
                // The head table's own directory checksum must reflect the zeroed
                // adjustment field per spec; recompute it with the field at zero.
                uint saved = ReadU32(output, headOffset + 8);
                WriteU32(output, headOffset + 8, 0);
                uint headChecksum = TableChecksum(output, headOffset, tables[headDirRec].Data.Length);
                WriteU32(output, 12 + headDirRec * 16 + 4, headChecksum);
                WriteU32(output, headOffset + 8, saved);
            }
        }

        return output;
    }

    private static uint TableChecksum(byte[] data, int offset, int length)
    {
        uint sum = 0;
        int i = offset;
        int end = offset + length;
        while (i + 4 <= end)
        {
            sum = unchecked(sum + ReadU32(data, i));
            i += 4;
        }
        // Trailing bytes are treated as a big-endian word padded with zeros.
        if (i < end)
        {
            uint last = 0;
            for (int b = 0; b < 4; b++)
            {
                last = (last << 8) | (uint)(i < end ? data[i] : 0);
                i++;
            }
            sum = unchecked(sum + last);
        }
        return sum;
    }

    private static uint Tag(string s) => ((uint)s[0] << 24) | ((uint)s[1] << 16) | ((uint)s[2] << 8) | s[3];

    private static uint ReadU32(byte[] d, int o) => ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];
    private static int ReadU16(byte[] d, int o) => (d[o] << 8) | d[o + 1];
    private static short ReadI16(byte[] d, int o) => (short)((d[o] << 8) | d[o + 1]);

    private static void WriteU32(byte[] d, int o, uint v)
    {
        d[o] = (byte)(v >> 24);
        d[o + 1] = (byte)(v >> 16);
        d[o + 2] = (byte)(v >> 8);
        d[o + 3] = (byte)v;
    }

    private static void WriteU16(byte[] d, int o, ushort v)
    {
        d[o] = (byte)(v >> 8);
        d[o + 1] = (byte)v;
    }
}
