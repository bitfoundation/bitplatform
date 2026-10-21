using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Reads and writes WOFF2 (https://www.w3.org/TR/WOFF2/) for the .NET cut of an icon font: reads one, its transformed glyf
/// table put back into glyphs, and writes one with every table as it is (the null transform of glyf and loca), compressed
/// with Brotli. A TrueType font only, never a collection.
/// </summary>
public static class Woff2
{
    private const uint Signature = 0x774F4632; // wOF2
    private const int HeaderSize = 48;

    // The tags a table directory entry names by their index (WOFF2 5.1, the known table tags).
    private static readonly string[] KnownTags =
    [
        "cmap", "head", "hhea", "hmtx", "maxp", "name", "OS/2", "post", "cvt ", "fpgm", "glyf", "loca", "prep", "CFF ", "VORG", "EBDT",
        "EBLC", "gasp", "hdmx", "kern", "LTSH", "PCLT", "VDMX", "vhea", "vmtx", "BASE", "GDEF", "GPOS", "GSUB", "EBSC", "JSTF", "MATH",
        "CBDT", "CBLC", "COLR", "CPAL", "SVG ", "sbix", "acnt", "avar", "bdat", "bloc", "bsln", "cvar", "fdsc", "feat", "fmtx", "fvar",
        "gvar", "hsty", "just", "lcar", "mort", "morx", "opbd", "prop", "trak", "Zapf", "Silf", "Glat", "Gloc", "Feat", "Sill",
    ];

    public static TrueTypeFont Read(byte[] file)
    {
        if (file.Length < HeaderSize || Big.U32(file, 0) != Signature) throw new FormatException("not a WOFF2 font");

        var flavor = Big.U32(file, 4);
        if (flavor == 0x74746366) throw new NotSupportedException("the WOFF2 font is a collection"); // ttcf

        var numTables = Big.U16(file, 12);
        var compressedLength = (int)Big.U32(file, 20);

        var position = HeaderSize;
        var entries = new List<(string Tag, bool Transformed, int Length, int StoredLength)>(numTables);
        for (var i = 0; i < numTables; i++)
        {
            var flags = file[position++];
            string tag;
            if ((flags & 0x3F) == 63)
            {
                tag = Encoding.ASCII.GetString(file, position, 4);
                position += 4;
            }
            else
            {
                tag = KnownTags[flags & 0x3F];
            }

            var version = flags >> 6;
            var transformed = tag is TrueTypeFont.Glyf or TrueTypeFont.Loca ? version != 3 : version != 0;
            var length = ReadBase128(file, ref position);
            var storedLength = transformed ? ReadBase128(file, ref position) : length;
            entries.Add((tag, transformed, length, storedLength));
        }

        if (position + compressedLength > file.Length) throw new FormatException("the WOFF2 font's compressed data runs past its end");
        var data = Brotli.Decompress(file, position, compressedLength);
        if (data.Length < entries.Sum(e => (long)e.StoredLength)) throw new FormatException("the WOFF2 font's compressed data is shorter than its tables");

        var tables = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var transformedGlyf = (byte[]?)null;
        var offset = 0;
        foreach (var (tag, transformed, _, storedLength) in entries)
        {
            var table = new byte[storedLength];
            Buffer.BlockCopy(data, offset, table, 0, storedLength);
            offset += storedLength;

            if (transformed is false) tables[tag] = table;
            else if (tag == TrueTypeFont.Glyf) transformedGlyf = table;
            else if (tag != TrueTypeFont.Loca) throw new NotSupportedException($"the WOFF2 font's '{tag}' table is transformed");
        }

        if (transformedGlyf is null) return TrueTypeFont.FromTables(flavor, tables);

        var glyphs = ReadTransformedGlyphs(transformedGlyf);
        if (tables.TryGetValue("maxp", out var maxp) && Big.U16(maxp, 4) != glyphs.Count) throw new FormatException("the WOFF2 font's glyf and maxp disagree on how many glyphs it has");

        return new TrueTypeFont(flavor, tables, glyphs);
    }

    /// <summary>
    /// The font as WOFF2: every table as it is, glyf and loca included (their null transform), in tag order but with loca
    /// right after glyf, compressed together with Brotli at its best.
    /// </summary>
    public static byte[] Write(TrueTypeFont font)
    {
        var tables = font.ToTables();
        var order = tables.Keys.Where(t => t != TrueTypeFont.Loca).OrderBy(t => t, StringComparer.Ordinal).ToList();
        order.Insert(order.IndexOf(TrueTypeFont.Glyf) + 1, TrueTypeFont.Loca);

        var directory = new MemoryStream();
        var data = new MemoryStream();
        foreach (var tag in order)
        {
            var index = Array.IndexOf(KnownTags, tag);
            var transformVersion = tag is TrueTypeFont.Glyf or TrueTypeFont.Loca ? 3 : 0;
            directory.WriteByte((byte)((transformVersion << 6) | (index < 0 ? 63 : index)));
            if (index < 0) directory.Write(Encoding.ASCII.GetBytes(tag), 0, 4);
            WriteBase128(directory, tables[tag].Length);

            data.Write(tables[tag], 0, tables[tag].Length);
        }

        var compressed = Brotli.Compress(data.ToArray());
        var sfntSize = 12 + 16 * tables.Count + tables.Values.Sum(t => (t.Length + 3) & ~3);
        var length = (HeaderSize + (int)directory.Length + compressed.Length + 3) & ~3;

        var file = new byte[length];
        Big.Put32(file, 0, Signature);
        Big.Put32(file, 4, font.Flavor);
        Big.Put32(file, 8, (uint)length);
        Big.Put16(file, 12, tables.Count);
        Big.Put32(file, 16, (uint)sfntSize);
        Big.Put32(file, 20, (uint)compressed.Length);
        Big.Put16(file, 24, 1);
        directory.ToArray().CopyTo(file, HeaderSize);
        compressed.CopyTo(file, HeaderSize + (int)directory.Length);
        return file;
    }

    // The transformed glyf table (WOFF2 5.1): the glyphs taken apart into streams - contour counts, point counts, flags,
    // coordinate triplets, composites, bounding boxes, instructions - and put back together here as TrueType glyphs.
    private static List<byte[]> ReadTransformedGlyphs(byte[] table)
    {
        var optionFlags = Big.U16(table, 2);
        var numGlyphs = Big.U16(table, 4);

        var streams = new GlyfStream[7];
        var position = 36;
        for (var i = 0; i < 7; i++)
        {
            var size = (int)Big.U32(table, 8 + i * 4);
            if (position + size > table.Length) throw new FormatException("a stream of the transformed glyf table runs past its end");
            streams[i] = new GlyfStream(table, position, size);
            position += size;
        }

        var (nContours, nPoints, flags, glyphStream, composites, bboxes, instructions) = (streams[0], streams[1], streams[2], streams[3], streams[4], streams[5], streams[6]);

        var bboxBitmap = bboxes.Bytes(4 * ((numGlyphs + 31) / 32));
        var overlapBitmap = (optionFlags & 1) != 0 ? new GlyfStream(table, position, (numGlyphs + 7) / 8).Bytes((numGlyphs + 7) / 8) : null;

        var glyphs = new List<byte[]>(numGlyphs);
        for (var i = 0; i < numGlyphs; i++)
        {
            var contours = (short)nContours.U16();
            var hasBbox = (bboxBitmap[i >> 3] & (0x80 >> (i & 7))) != 0;

            if (contours == 0)
            {
                if (hasBbox) throw new FormatException($"the empty glyph {i} has a bounding box");
                glyphs.Add([]);
                continue;
            }

            var glyph = new MemoryStream();

            if (contours < 0)
            {
                // A composite: its components as they are, its bounding box always given, its instructions if any says so.
                if (hasBbox is false) throw new FormatException($"the composite glyph {i} has no bounding box");

                var start = composites.Position;
                var hasInstructions = false;
                int componentFlags;
                do
                {
                    componentFlags = composites.U16();
                    composites.Skip(2 + ((componentFlags & 0x0001) != 0 ? 4 : 2));
                    if ((componentFlags & 0x0008) != 0) composites.Skip(2);
                    else if ((componentFlags & 0x0040) != 0) composites.Skip(4);
                    else if ((componentFlags & 0x0080) != 0) composites.Skip(8);
                    hasInstructions |= (componentFlags & 0x0100) != 0;
                }
                while ((componentFlags & 0x0020) != 0);

                Big.Write16(glyph, -1);
                glyph.Write(bboxes.Bytes(8), 0, 8);
                glyph.Write(table, start, composites.Position - start);
                if (hasInstructions)
                {
                    var length = glyphStream.U255();
                    Big.Write16(glyph, length);
                    glyph.Write(instructions.Bytes(length), 0, length);
                }

                glyphs.Add(glyph.ToArray());
                continue;
            }

            // A simple glyph: the end points of its contours, then its points - each a flag of its own and a triplet of
            // the glyph stream holding the move from the point before.
            var endPoints = new int[contours];
            var total = 0;
            for (var c = 0; c < contours; c++)
            {
                total += nPoints.U255();
                endPoints[c] = total - 1;
            }

            var onCurve = new bool[total];
            var xs = new int[total];
            var ys = new int[total];
            int x = 0, y = 0;
            for (var p = 0; p < total; p++)
            {
                var flag = flags.Byte();
                onCurve[p] = (flag & 0x80) == 0;
                var (dx, dy) = DecodeTriplet(flag & 0x7F, glyphStream);
                xs[p] = x += dx;
                ys[p] = y += dy;
            }

            var instructionLength = glyphStream.U255();
            var instructionBytes = instructions.Bytes(instructionLength);

            Big.Write16(glyph, contours);
            if (hasBbox)
            {
                glyph.Write(bboxes.Bytes(8), 0, 8);
            }
            else
            {
                Big.Write16(glyph, total == 0 ? 0 : xs.Min());
                Big.Write16(glyph, total == 0 ? 0 : ys.Min());
                Big.Write16(glyph, total == 0 ? 0 : xs.Max());
                Big.Write16(glyph, total == 0 ? 0 : ys.Max());
            }
            foreach (var end in endPoints) Big.Write16(glyph, end);
            Big.Write16(glyph, instructionLength);
            glyph.Write(instructionBytes, 0, instructionLength);

            var overlap = overlapBitmap is not null && (overlapBitmap[i >> 3] & (0x80 >> (i & 7))) != 0;
            WritePoints(glyph, onCurve, xs, ys, overlap);

            glyphs.Add(glyph.ToArray());
        }

        return glyphs;
    }

    // The move a coordinate triplet encodes (WOFF2 5.2): the flag's index says how many bytes follow and how their bits
    // split between x and y, and its two lowest bits the signs.
    private static (int Dx, int Dy) DecodeTriplet(int flag, GlyfStream glyphs)
    {
        static int WithSign(int flag, int value) => (flag & 1) != 0 ? value : -value;

        if (flag < 10)
        {
            return (0, WithSign(flag, ((flag & 14) << 7) + glyphs.Byte()));
        }
        if (flag < 20)
        {
            return (WithSign(flag, (((flag - 10) & 14) << 7) + glyphs.Byte()), 0);
        }
        if (flag < 84)
        {
            var b0 = flag - 20;
            var b1 = glyphs.Byte();
            return (WithSign(flag, 1 + (b0 & 0x30) + (b1 >> 4)), WithSign(flag >> 1, 1 + ((b0 & 0x0C) << 2) + (b1 & 0x0F)));
        }
        if (flag < 120)
        {
            var b0 = flag - 84;
            var b1 = glyphs.Byte();
            var b2 = glyphs.Byte();
            return (WithSign(flag, 1 + ((b0 / 12) << 8) + b1), WithSign(flag >> 1, 1 + (((b0 % 12) >> 2) << 8) + b2));
        }
        if (flag < 124)
        {
            var b1 = glyphs.Byte();
            var b2 = glyphs.Byte();
            var b3 = glyphs.Byte();
            return (WithSign(flag, (b1 << 4) + (b2 >> 4)), WithSign(flag >> 1, ((b2 & 0x0F) << 8) + b3));
        }
        {
            var b1 = glyphs.Byte();
            var b2 = glyphs.Byte();
            var b3 = glyphs.Byte();
            var b4 = glyphs.Byte();
            return (WithSign(flag, (b1 << 8) + b2), WithSign(flag >> 1, (b3 << 8) + b4));
        }
    }

    // A simple glyph's flags and coordinates, the way TrueType writes them: each move in a byte where it fits, none where it
    // is zero, and a run of the same flag as one flag and a count.
    private static void WritePoints(Stream glyph, bool[] onCurve, int[] xs, int[] ys, bool overlap)
    {
        var flagBytes = new List<byte>();
        var xBytes = new MemoryStream();
        var yBytes = new MemoryStream();

        int lastX = 0, lastY = 0, lastFlag = -1, repeat = 0;
        for (var p = 0; p < xs.Length; p++)
        {
            var flag = onCurve[p] ? 0x01 : 0x00;
            if (p == 0 && overlap) flag |= 0x40;

            flag |= Coordinate(xs[p] - lastX, xBytes, shortFlag: 0x02, sameFlag: 0x10);
            flag |= Coordinate(ys[p] - lastY, yBytes, shortFlag: 0x04, sameFlag: 0x20);
            lastX = xs[p];
            lastY = ys[p];

            if (flag == lastFlag && repeat < 255)
            {
                if (repeat == 0) flagBytes[flagBytes.Count - 1] |= 0x08;
                if (repeat == 0) flagBytes.Add(1);
                else flagBytes[flagBytes.Count - 1]++;
                repeat++;
            }
            else
            {
                flagBytes.Add((byte)flag);
                lastFlag = flag;
                repeat = 0;
            }
        }

        glyph.Write([.. flagBytes], 0, flagBytes.Count);
        xBytes.WriteTo(glyph);
        yBytes.WriteTo(glyph);
    }

    private static int Coordinate(int delta, MemoryStream bytes, int shortFlag, int sameFlag)
    {
        if (delta == 0) return sameFlag;

        if (delta is > -256 and < 256)
        {
            bytes.WriteByte((byte)Math.Abs(delta));
            return shortFlag | (delta > 0 ? sameFlag : 0);
        }

        Big.Write16(bytes, delta);
        return 0;
    }

    private static int ReadBase128(byte[] b, ref int position)
    {
        uint value = 0;
        for (var i = 0; i < 5; i++)
        {
            var next = b[position++];
            if (i == 0 && next == 0x80) throw new FormatException("a WOFF2 number has a leading zero");
            if ((value & 0xFE000000) != 0) throw new FormatException("a WOFF2 number overflows");
            value = (value << 7) | (uint)(next & 0x7F);
            if ((next & 0x80) == 0) return value > int.MaxValue ? throw new FormatException("a WOFF2 number is too large") : (int)value;
        }
        throw new FormatException("a WOFF2 number is longer than five bytes");
    }

    private static void WriteBase128(Stream s, int value)
    {
        var bytes = new List<byte>();
        do
        {
            bytes.Insert(0, (byte)(value & 0x7F));
            value >>= 7;
        }
        while (value > 0);

        for (var i = 0; i < bytes.Count - 1; i++) bytes[i] |= 0x80;
        s.Write([.. bytes], 0, bytes.Count);
    }

    // One stream of the transformed glyf table, read from its start to its end and never past it.
    private sealed class GlyfStream(byte[] data, int start, int length)
    {
        private readonly int _end = start + length;

        public int Position { get; private set; } = start;

        public byte Byte() => Position < _end ? data[Position++] : throw Overrun();

        public int U16() => (Byte() << 8) | Byte();

        // 255UInt16 (WOFF2 6.1.1): one byte, or a marker and one or two more.
        public int U255()
        {
            var code = Byte();
            return code switch
            {
                253 => U16(),
                254 => 506 + Byte(),
                255 => 253 + Byte(),
                _ => code,
            };
        }

        public byte[] Bytes(int count)
        {
            if (Position + count > _end) throw Overrun();
            var bytes = new byte[count];
            Buffer.BlockCopy(data, Position, bytes, 0, count);
            Position += count;
            return bytes;
        }

        public void Skip(int count) => Bytes(count);

        private static FormatException Overrun() => new("a stream of the transformed glyf table is shorter than its glyphs");
    }
}
