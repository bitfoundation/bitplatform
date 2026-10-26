using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bit.BlazorUI.Build;

/// <summary>
/// A TrueType font as the font cut works on it: its tables by tag, with the glyph outlines (glyf and loca) held as one byte
/// array per glyph - its glyf entry, unpadded, empty for a glyph with no outline.
/// </summary>
public sealed class TrueTypeFont(uint flavor, Dictionary<string, byte[]> tables, List<byte[]> glyphs)
{
    public const string Glyf = "glyf";
    public const string Loca = "loca";

    /// <summary>The sfnt version: 0x00010000 for TrueType outlines.</summary>
    public uint Flavor { get; } = flavor;

    /// <summary>Every table but glyf and loca, which <see cref="Glyphs"/> stand for.</summary>
    public Dictionary<string, byte[]> Tables { get; } = tables;

    public List<byte[]> Glyphs { get; } = glyphs;

    public byte[] Table(string tag) => Tables.TryGetValue(tag, out var table) ? table : throw new FormatException($"the font has no '{tag}' table");

    /// <summary>A font out of its tables, glyf and loca split into the glyphs (head says how loca is written, maxp how many).</summary>
    public static TrueTypeFont FromTables(uint flavor, Dictionary<string, byte[]> tables)
    {
        if (tables.ContainsKey("CFF ") || tables.ContainsKey("CFF2")) throw new NotSupportedException("the font has PostScript outlines (CFF), not TrueType ones");
        if (tables.TryGetValue(Glyf, out var glyf) is false || tables.TryGetValue(Loca, out var loca) is false) throw new FormatException("the font has no glyf or loca table");

        var numGlyphs = Big.U16(tables.TryGetValue("maxp", out var maxp) ? maxp : throw new FormatException("the font has no 'maxp' table"), 4);
        var longOffsets = Big.I16(tables.TryGetValue("head", out var head) ? head : throw new FormatException("the font has no 'head' table"), 50) != 0;

        var glyphs = new List<byte[]>(numGlyphs);
        for (var i = 0; i < numGlyphs; i++)
        {
            var start = longOffsets ? (int)Big.U32(loca, i * 4) : Big.U16(loca, i * 2) * 2;
            var end = longOffsets ? (int)Big.U32(loca, (i + 1) * 4) : Big.U16(loca, (i + 1) * 2) * 2;
            if (start > end || end > glyf.Length) throw new FormatException($"the loca entry of glyph {i} points outside glyf");

            var glyph = new byte[end - start];
            Buffer.BlockCopy(glyf, start, glyph, 0, glyph.Length);
            glyphs.Add(glyph);
        }

        var rest = tables.Where(t => t.Key is not Glyf and not Loca).ToDictionary(t => t.Key, t => t.Value, StringComparer.Ordinal);
        return new TrueTypeFont(flavor, rest, glyphs);
    }

    /// <summary>
    /// Every table, glyf and loca written out of the glyphs - each glyph 4-byte aligned, loca in its long form, which head
    /// is set to say - and head's checkSumAdjustment computed over the font as an sfnt would hold it.
    /// </summary>
    public Dictionary<string, byte[]> ToTables()
    {
        var glyf = new MemoryStream();
        var loca = new byte[(Glyphs.Count + 1) * 4];
        for (var i = 0; i < Glyphs.Count; i++)
        {
            Big.Put32(loca, i * 4, (uint)glyf.Length);
            glyf.Write(Glyphs[i], 0, Glyphs[i].Length);
            while (glyf.Length % 4 != 0) glyf.WriteByte(0);
        }
        Big.Put32(loca, Glyphs.Count * 4, (uint)glyf.Length);

        var tables = new Dictionary<string, byte[]>(Tables, StringComparer.Ordinal)
        {
            [Glyf] = glyf.ToArray(),
            [Loca] = loca,
        };

        var head = (byte[])Table("head").Clone();
        Big.Put16(head, 50, 1);
        Big.Put32(head, 8, 0);
        tables["head"] = head;

        // The font's checksum is the sum of every table's, plus the sfnt header's and the table directory's.
        var sum = SfntHeaderChecksum(tables);
        foreach (var table in tables.Values) sum += Checksum(table);
        Big.Put32(head, 8, 0xB1B0AFBA - sum);

        return tables;
    }

    /// <summary>The sum of a table's big-endian 32-bit words, the last one zero-padded.</summary>
    public static uint Checksum(byte[] data)
    {
        uint sum = 0;
        for (var i = 0; i < data.Length; i += 4)
        {
            uint word = 0;
            for (var j = 0; j < 4; j++) word = (word << 8) | (i + j < data.Length ? data[i + j] : 0u);
            sum += word;
        }
        return sum;
    }

    private uint SfntHeaderChecksum(Dictionary<string, byte[]> tables)
    {
        var count = tables.Count;
        var header = new byte[12 + 16 * count];
        var power = 1;
        var log = 0;
        while (power * 2 <= count) { power *= 2; log++; }

        Big.Put32(header, 0, Flavor);
        Big.Put16(header, 4, count);
        Big.Put16(header, 6, power * 16);
        Big.Put16(header, 8, log);
        Big.Put16(header, 10, count * 16 - power * 16);

        var offset = (uint)header.Length;
        var i = 0;
        foreach (var table in tables.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            var entry = 12 + 16 * i++;
            Encoding.ASCII.GetBytes(table.Key, 0, 4, header, entry);
            Big.Put32(header, entry + 4, Checksum(table.Value));
            Big.Put32(header, entry + 8, offset);
            Big.Put32(header, entry + 12, (uint)table.Value.Length);
            offset += (uint)((table.Value.Length + 3) & ~3);
        }

        return Checksum(header);
    }
}

/// <summary>Big-endian reads and writes, which every field of a font is.</summary>
internal static class Big
{
    public static int U16(byte[] b, int i) => (b[i] << 8) | b[i + 1];

    public static short I16(byte[] b, int i) => (short)U16(b, i);

    public static uint U32(byte[] b, int i) => ((uint)b[i] << 24) | ((uint)b[i + 1] << 16) | ((uint)b[i + 2] << 8) | b[i + 3];

    public static void Put16(byte[] b, int i, int value)
    {
        b[i] = (byte)(value >> 8);
        b[i + 1] = (byte)value;
    }

    public static void Put32(byte[] b, int i, uint value)
    {
        b[i] = (byte)(value >> 24);
        b[i + 1] = (byte)(value >> 16);
        b[i + 2] = (byte)(value >> 8);
        b[i + 3] = (byte)value;
    }

    public static void Write16(Stream s, int value)
    {
        s.WriteByte((byte)(value >> 8));
        s.WriteByte((byte)value);
    }

    public static void Write32(Stream s, uint value)
    {
        Write16(s, (int)(value >> 16));
        Write16(s, (int)value);
    }
}
