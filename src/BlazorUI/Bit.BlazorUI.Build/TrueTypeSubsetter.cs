using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Cuts a TrueType font down to the glyphs of a set of characters: .notdef, the glyph each character maps to and every glyph
/// a composite among them is built of, renumbered in the font's own order.
/// </summary>
/// <remarks>
/// The tables read by glyph index are written again for the new numbering - glyf and loca, hmtx, cmap, maxp, hhea - and so
/// are head's bounding box and OS/2's character range. The ones that say nothing per glyph (name, and the hinting
/// programs the glyphs' own instructions call into) are copied as they are. The rest are left out: post keeps its header
/// and drops the glyph names, and a table indexing glyphs this does not rewrite (GDEF, GSUB, GPOS, kern, hdmx and the
/// like) is dropped rather than left pointing at the wrong ones - an icon font draws each glyph on its own.
/// </remarks>
public static class TrueTypeSubsetter
{
    private static readonly string[] Copied = ["name", "cvt ", "fpgm", "prep", "gasp"];

    public static TrueTypeFont Subset(TrueTypeFont font, IEnumerable<int> codepoints)
    {
        // A character the font has no glyph for draws nothing whether the font is cut or not, so it is left out - as
        // pyftsubset does - rather than keep the font whole for it (Bit.BlazorUI.Icons' ContactInfoMirrored is one).
        var map = ReadCmap(font.Table("cmap"));
        var characters = codepoints.Distinct().Where(map.ContainsKey).OrderBy(c => c).ToList();

        var kept = new SortedSet<int>();
        var pending = new Stack<int>(characters.Select(c => map[c]).Prepend(0));
        while (pending.Count > 0)
        {
            var glyph = pending.Pop();
            if (glyph >= font.Glyphs.Count) throw new FormatException($"the font refers to the glyph {glyph}, past its last one");
            if (kept.Add(glyph) is false) continue;
            foreach (var component in Components(font.Glyphs[glyph])) pending.Push(component.Glyph);
        }

        var order = kept.ToList();
        var renumbered = order.Select((glyph, index) => (glyph, index)).ToDictionary(g => g.glyph, g => g.index);
        var glyphs = order.Select(g => Renumber(font.Glyphs[g], renumbered)).ToList();

        var tables = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var tag in Copied)
        {
            if (font.Tables.TryGetValue(tag, out var table)) tables[tag] = table;
        }

        var (hhea, hmtx) = Metrics(font, order);
        tables["hhea"] = hhea;
        tables["hmtx"] = hmtx;

        var maxp = (byte[])font.Table("maxp").Clone();
        Big.Put16(maxp, 4, order.Count);
        tables["maxp"] = maxp;

        tables["cmap"] = WriteCmap(characters.Select(c => (c, renumbered[map[c]])).ToList());
        tables["post"] = Post(font.Table("post"));
        tables["head"] = Head(font.Table("head"), glyphs);

        if (font.Tables.TryGetValue("OS/2", out var os2))
        {
            os2 = (byte[])os2.Clone();
            // usFirstCharIndex and usLastCharIndex, which hold 0xFFFF for anything past the Basic Multilingual Plane.
            if (os2.Length >= 68 && characters.Count > 0)
            {
                Big.Put16(os2, 64, Math.Min(characters.First(), 0xFFFF));
                Big.Put16(os2, 66, Math.Min(characters.Last(), 0xFFFF));
            }
            tables["OS/2"] = os2;
        }

        return new TrueTypeFont(font.Flavor, tables, glyphs);
    }

    /// <summary>The character each codepoint of the font's Unicode cmap subtables maps to a glyph, the first subtable to say winning.</summary>
    public static Dictionary<int, int> ReadCmap(byte[] cmap)
    {
        var map = new Dictionary<int, int>();
        var count = Big.U16(cmap, 2);

        // Unicode subtables only: platform 0, and the Windows platform's Unicode BMP (1) and full repertoire (10) ones.
        var records = Enumerable.Range(0, count)
                                .Select(i => (Platform: Big.U16(cmap, 4 + i * 8), Encoding: Big.U16(cmap, 6 + i * 8), Offset: (int)Big.U32(cmap, 8 + i * 8)))
                                .Where(r => r.Platform == 0 || (r.Platform == 3 && r.Encoding is 1 or 10))
                                .OrderByDescending(r => Big.U16(cmap, r.Offset) == 12);

        foreach (var record in records)
        {
            var t = record.Offset;
            switch (Big.U16(cmap, t))
            {
                case 4:
                    var segCountX2 = Big.U16(cmap, t + 6);
                    var ends = t + 14;
                    var starts = ends + segCountX2 + 2;
                    var deltas = starts + segCountX2;
                    var rangeOffsets = deltas + segCountX2;
                    for (var s = 0; s < segCountX2 / 2; s++)
                    {
                        var start = Big.U16(cmap, starts + s * 2);
                        var end = Big.U16(cmap, ends + s * 2);
                        var delta = Big.U16(cmap, deltas + s * 2);
                        var rangeOffset = Big.U16(cmap, rangeOffsets + s * 2);
                        for (var c = start; c <= end && c != 0xFFFF; c++)
                        {
                            int glyph;
                            if (rangeOffset == 0)
                            {
                                glyph = (c + delta) & 0xFFFF;
                            }
                            else
                            {
                                glyph = Big.U16(cmap, rangeOffsets + s * 2 + rangeOffset + (c - start) * 2);
                                if (glyph != 0) glyph = (glyph + delta) & 0xFFFF;
                            }
                            if (glyph != 0 && map.ContainsKey(c) is false) map[c] = glyph;
                        }
                    }
                    break;

                case 12:
                    var groups = (int)Big.U32(cmap, t + 12);
                    for (var g = 0; g < groups; g++)
                    {
                        var start = (int)Big.U32(cmap, t + 16 + g * 12);
                        var end = (int)Big.U32(cmap, t + 20 + g * 12);
                        var first = (int)Big.U32(cmap, t + 24 + g * 12);
                        for (var c = start; c <= end; c++)
                        {
                            if (map.ContainsKey(c) is false) map[c] = first + c - start;
                        }
                    }
                    break;
            }
        }

        return map;
    }

    /// <summary>The glyphs a composite glyph is built of, each with where its index sits in the glyph; none for a simple one.</summary>
    public static List<(int Glyph, int Offset)> Components(byte[] glyph)
    {
        var components = new List<(int, int)>();
        if (glyph.Length < 10 || Big.I16(glyph, 0) >= 0) return components;

        var p = 10;
        int flags;
        do
        {
            if (p + 4 > glyph.Length) throw new FormatException("a composite glyph ends inside one of its components");
            flags = Big.U16(glyph, p);
            components.Add((Big.U16(glyph, p + 2), p + 2));
            p += 4 + ((flags & 0x0001) != 0 ? 4 : 2);
            if ((flags & 0x0008) != 0) p += 2;
            else if ((flags & 0x0040) != 0) p += 4;
            else if ((flags & 0x0080) != 0) p += 8;
        }
        while ((flags & 0x0020) != 0);

        return components;
    }

    private static byte[] Renumber(byte[] glyph, Dictionary<int, int> renumbered)
    {
        var components = Components(glyph);
        if (components.Count == 0) return glyph;

        var copy = (byte[])glyph.Clone();
        foreach (var (component, offset) in components) Big.Put16(copy, offset, renumbered[component]);
        return copy;
    }

    // A metric for every glyph kept, none shared through a last advance: few glyphs, so nothing to save.
    private static (byte[] Hhea, byte[] Hmtx) Metrics(TrueTypeFont font, List<int> order)
    {
        var hhea = (byte[])font.Table("hhea").Clone();
        var source = font.Table("hmtx");
        var longMetrics = Big.U16(hhea, 34);
        if (longMetrics == 0) throw new FormatException("the font's hhea has no metrics");

        var hmtx = new byte[order.Count * 4];
        var widest = 0;
        for (var i = 0; i < order.Count; i++)
        {
            var glyph = order[i];
            // A glyph past the long metrics has the last one's advance, and a bearing of its own after them.
            var advance = Big.U16(source, Math.Min(glyph, longMetrics - 1) * 4);
            var bearing = glyph < longMetrics ? Big.I16(source, glyph * 4 + 2) : Big.I16(source, longMetrics * 4 + (glyph - longMetrics) * 2);

            Big.Put16(hmtx, i * 4, advance);
            Big.Put16(hmtx, i * 4 + 2, bearing);
            widest = Math.Max(widest, advance);
        }

        Big.Put16(hhea, 10, widest);
        Big.Put16(hhea, 34, order.Count);
        return (hhea, hmtx);
    }

    // Version 3: the header, without the glyph names.
    private static byte[] Post(byte[] post)
    {
        if (post.Length < 32) throw new FormatException("the font's post table is shorter than its header");
        var header = new byte[32];
        Buffer.BlockCopy(post, 0, header, 0, 32);
        Big.Put32(header, 0, 0x00030000);
        return header;
    }

    // The font's bounding box, of the glyphs kept.
    private static byte[] Head(byte[] source, List<byte[]> glyphs)
    {
        var head = (byte[])source.Clone();
        var drawn = glyphs.Where(g => g.Length >= 10).ToList();
        if (drawn.Count == 0) return head;

        Big.Put16(head, 36, drawn.Min(g => Big.I16(g, 2)));
        Big.Put16(head, 38, drawn.Min(g => Big.I16(g, 4)));
        Big.Put16(head, 40, drawn.Max(g => Big.I16(g, 6)));
        Big.Put16(head, 42, drawn.Max(g => Big.I16(g, 8)));
        return head;
    }

    // A format 4 subtable for the Basic Multilingual Plane, under Unicode and Windows alike, and a format 12 one beside it for
    // anything past it. Each run of characters whose glyphs follow one another is one segment.
    private static byte[] WriteCmap(List<(int Character, int Glyph)> mappings)
    {
        var bmp = mappings.Where(m => m.Character < 0xFFFF).ToList();
        var full = mappings.Count > bmp.Count;

        var format4 = new MemoryStream();
        var segments = Runs(bmp).Select(r => (Start: r.Start, End: r.End, Delta: (r.Glyph - r.Start) & 0xFFFF)).ToList();
        segments.Add((0xFFFF, 0xFFFF, 1));

        var segCount = segments.Count;
        var power = 1;
        var log = 0;
        while (power * 2 <= segCount) { power *= 2; log++; }

        Big.Write16(format4, 4);
        Big.Write16(format4, 16 + 8 * segCount);
        Big.Write16(format4, 0);
        Big.Write16(format4, segCount * 2);
        Big.Write16(format4, power * 2);
        Big.Write16(format4, log);
        Big.Write16(format4, segCount * 2 - power * 2);
        foreach (var s in segments) Big.Write16(format4, s.End);
        Big.Write16(format4, 0);
        foreach (var s in segments) Big.Write16(format4, s.Start);
        foreach (var s in segments) Big.Write16(format4, s.Delta);
        foreach (var _ in segments) Big.Write16(format4, 0);

        var format12 = new MemoryStream();
        if (full)
        {
            var groups = Runs(mappings).ToList();
            Big.Write16(format12, 12);
            Big.Write16(format12, 0);
            Big.Write32(format12, (uint)(16 + 12 * groups.Count));
            Big.Write32(format12, 0);
            Big.Write32(format12, (uint)groups.Count);
            foreach (var g in groups)
            {
                Big.Write32(format12, (uint)g.Start);
                Big.Write32(format12, (uint)g.End);
                Big.Write32(format12, (uint)g.Glyph);
            }
        }

        var records = full
            ? new[] { (0, 3, false), (0, 4, true), (3, 1, false), (3, 10, true) }
            : new[] { (0, 3, false), (3, 1, false) };

        var cmap = new MemoryStream();
        var headerLength = 4 + 8 * records.Length;
        Big.Write16(cmap, 0);
        Big.Write16(cmap, records.Length);
        foreach (var (platform, encoding, isFull) in records)
        {
            Big.Write16(cmap, platform);
            Big.Write16(cmap, encoding);
            Big.Write32(cmap, (uint)(isFull ? headerLength + format4.Length : headerLength));
        }
        format4.WriteTo(cmap);
        format12.WriteTo(cmap);
        return cmap.ToArray();
    }

    private static IEnumerable<(int Start, int End, int Glyph)> Runs(List<(int Character, int Glyph)> mappings)
    {
        var i = 0;
        while (i < mappings.Count)
        {
            var j = i;
            while (j + 1 < mappings.Count && mappings[j + 1].Character == mappings[j].Character + 1 && mappings[j + 1].Glyph == mappings[j].Glyph + 1) j++;
            yield return (mappings[i].Character, mappings[j].Character, mappings[i].Glyph);
            i = j + 1;
        }
    }
}
