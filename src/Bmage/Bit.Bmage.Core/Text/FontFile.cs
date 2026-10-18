using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Bit.Bmage.Text;

/// <summary>
/// A parsed TrueType or OpenType font: character mapping, glyph outlines and horizontal metrics. Enough of the format is
/// implemented to lay out and rasterise text without any external dependency. CFF (PostScript) outlines and complex
/// script shaping are out of scope; use a browser-backed rasteriser for those.
/// </summary>
public sealed class FontFile
{
    private readonly byte[] _data;
    private readonly Dictionary<string, (int Offset, int Length)> _tables = new(StringComparer.Ordinal);
    private readonly Dictionary<int, ushort> _cmap = [];
    private ushort[] _advanceWidths = [];
    private short[] _leftSideBearings = [];
    private int _indexToLocFormat;
    private uint[] _loca = [];
    private int _glyfOffset, _glyfLength;
    private readonly Dictionary<ushort, GlyphOutline?> _glyphCache = [];
    private (ushort Left, ushort Right, short Value)[] _kerningPairs = [];

    private FontFile(byte[] data) => _data = data;

    /// <summary>Units per em from the head table; all glyph coordinates are in these units.</summary>
    public int UnitsPerEm { get; private set; } = 1000;

    /// <summary>Typographic ascender in font units.</summary>
    public int Ascender { get; private set; }

    /// <summary>Typographic descender in font units (usually negative).</summary>
    public int Descender { get; private set; }

    /// <summary>Recommended extra line gap in font units.</summary>
    public int LineGap { get; private set; }

    /// <summary>Number of glyphs in the font.</summary>
    public int GlyphCount { get; private set; }

    /// <summary>The font family name from the name table, when present.</summary>
    public string? FamilyName { get; private set; }

    /// <summary>The full font name from the name table, when present.</summary>
    public string? FullName { get; private set; }

    /// <summary>True when the font contains a kerning table that this parser understands.</summary>
    public bool HasKerning => _kerningPairs.Length > 0;

    /// <summary>Parses a font from its bytes. Throws <see cref="ImageDecodeException"/> for unsupported or corrupt files.</summary>
    public static FontFile Load(ReadOnlyMemory<byte> data)
    {
        if (data.Length < 12) throw new ImageDecodeException("Font data is too short.");
        var font = new FontFile(data.ToArray());
        font.Parse();
        return font;
    }

    /// <summary>Parses a font from a stream.</summary>
    public static async ValueTask<FontFile> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        return Load(ms.ToArray());
    }

    private ReadOnlySpan<byte> Data => _data;

    private void Parse()
    {
        var d = Data;
        var tag = BinaryPrimitives.ReadUInt32BigEndian(d);
        var offset = 0;
        if (tag == 0x74746366) // 'ttcf': font collection, take the first font
        {
            if (d.Length < 16) throw new ImageDecodeException("Truncated font collection.");
            offset = (int)BinaryPrimitives.ReadUInt32BigEndian(d[12..]);
            if (offset < 0 || offset + 12 > d.Length) throw new ImageDecodeException("Invalid font collection offset.");
            tag = BinaryPrimitives.ReadUInt32BigEndian(d[offset..]);
        }
        if (tag is not (0x00010000 or 0x74727565 or 0x4F54544F)) // 1.0, 'true', 'OTTO'
            throw new ImageDecodeException("Unsupported font format. TrueType and OpenType (glyf) fonts are supported.");

        int numTables = BinaryPrimitives.ReadUInt16BigEndian(d[(offset + 4)..]);
        if (numTables is <= 0 or > 512) throw new ImageDecodeException("Invalid font table count.");
        for (var i = 0; i < numTables; i++)
        {
            var p = offset + 12 + i * 16;
            if (p + 16 > d.Length) throw new ImageDecodeException("Truncated font table directory.");
            var name = Encoding.ASCII.GetString(d.Slice(p, 4));
            var tableOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(d[(p + 8)..]);
            var tableLength = (int)BinaryPrimitives.ReadUInt32BigEndian(d[(p + 12)..]);
            if (tableOffset < 0 || tableLength < 0 || (long)tableOffset + tableLength > d.Length) continue;
            _tables[name] = (tableOffset, tableLength);
        }

        if (tag == 0x4F54544F && !_tables.ContainsKey("glyf"))
            throw new ImageDecodeException("This OpenType font uses CFF (PostScript) outlines, which are not supported. Supply a TrueType font or use a browser-backed text rasteriser.");

        ParseHead();
        ParseMaxp();
        ParseHhea();
        ParseHmtx();
        ParseLoca();
        ParseCmap();
        ParseName();
        ParseKern();
        if (_glyfLength == 0) throw new ImageDecodeException("Font has no glyph outlines (missing glyf table).");
    }

    private ReadOnlySpan<byte> Table(string name)
    {
        if (!_tables.TryGetValue(name, out var t)) return default;
        return Data.Slice(t.Offset, t.Length);
    }

    private void ParseHead()
    {
        var head = Table("head");
        if (head.Length < 54) throw new ImageDecodeException("Font is missing a valid head table.");
        UnitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(head[18..]);
        if (UnitsPerEm is <= 0) UnitsPerEm = 1000;
        _indexToLocFormat = BinaryPrimitives.ReadInt16BigEndian(head[50..]);
    }

    private void ParseMaxp()
    {
        var maxp = Table("maxp");
        if (maxp.Length < 6) throw new ImageDecodeException("Font is missing a valid maxp table.");
        GlyphCount = BinaryPrimitives.ReadUInt16BigEndian(maxp[4..]);
    }

    private void ParseHhea()
    {
        var hhea = Table("hhea");
        if (hhea.Length < 36) return;
        Ascender = BinaryPrimitives.ReadInt16BigEndian(hhea[4..]);
        Descender = BinaryPrimitives.ReadInt16BigEndian(hhea[6..]);
        LineGap = BinaryPrimitives.ReadInt16BigEndian(hhea[8..]);
        _numberOfHMetrics = BinaryPrimitives.ReadUInt16BigEndian(hhea[34..]);
        if (Ascender == 0 && Descender == 0)
        {
            Ascender = (int)(UnitsPerEm * 0.8);
            Descender = -(int)(UnitsPerEm * 0.2);
        }
    }

    private int _numberOfHMetrics;

    private void ParseHmtx()
    {
        var hmtx = Table("hmtx");
        var count = Math.Min(_numberOfHMetrics, GlyphCount);
        if (hmtx.IsEmpty || count <= 0)
        {
            _advanceWidths = [(ushort)(UnitsPerEm / 2)];
            _leftSideBearings = [0];
            return;
        }
        _advanceWidths = new ushort[count];
        _leftSideBearings = new short[GlyphCount];
        for (var i = 0; i < count && i * 4 + 4 <= hmtx.Length; i++)
        {
            _advanceWidths[i] = BinaryPrimitives.ReadUInt16BigEndian(hmtx[(i * 4)..]);
            _leftSideBearings[i] = BinaryPrimitives.ReadInt16BigEndian(hmtx[(i * 4 + 2)..]);
        }
        for (var i = count; i < GlyphCount; i++)
        {
            var p = count * 4 + (i - count) * 2;
            if (p + 2 <= hmtx.Length) _leftSideBearings[i] = BinaryPrimitives.ReadInt16BigEndian(hmtx[p..]);
        }
    }

    private void ParseLoca()
    {
        if (_tables.TryGetValue("glyf", out var glyf))
        {
            _glyfOffset = glyf.Offset;
            _glyfLength = glyf.Length;
        }
        var loca = Table("loca");
        if (loca.IsEmpty) return;
        var n = GlyphCount + 1;
        _loca = new uint[n];
        if (_indexToLocFormat == 0)
        {
            for (var i = 0; i < n && i * 2 + 2 <= loca.Length; i++) _loca[i] = (uint)BinaryPrimitives.ReadUInt16BigEndian(loca[(i * 2)..]) * 2;
        }
        else
        {
            for (var i = 0; i < n && i * 4 + 4 <= loca.Length; i++) _loca[i] = BinaryPrimitives.ReadUInt32BigEndian(loca[(i * 4)..]);
        }
    }

    private void ParseCmap()
    {
        var cmap = Table("cmap");
        if (cmap.Length < 4) return;
        int count = BinaryPrimitives.ReadUInt16BigEndian(cmap[2..]);
        var best = -1;
        var bestScore = -1;
        for (var i = 0; i < count; i++)
        {
            var p = 4 + i * 8;
            if (p + 8 > cmap.Length) break;
            int platform = BinaryPrimitives.ReadUInt16BigEndian(cmap[p..]);
            int encoding = BinaryPrimitives.ReadUInt16BigEndian(cmap[(p + 2)..]);
            var subtableOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(cmap[(p + 4)..]);
            if (subtableOffset < 0 || subtableOffset + 4 > cmap.Length) continue;
            // Prefer full Unicode (4,10 / 0,4-6), then BMP (3,1 / 0,3), then anything.
            var score = (platform, encoding) switch
            {
                (3, 10) => 100,
                (0, 4) or (0, 6) => 95,
                (3, 1) => 90,
                (0, 3) or (0, 2) or (0, 1) or (0, 0) => 85,
                (3, 0) => 50,
                (1, 0) => 10,
                _ => 1,
            };
            if (score > bestScore) { bestScore = score; best = subtableOffset; }
        }
        if (best < 0) return;
        ParseCmapSubtable(cmap[best..]);
    }

    private void ParseCmapSubtable(ReadOnlySpan<byte> t)
    {
        if (t.Length < 4) return;
        int format = BinaryPrimitives.ReadUInt16BigEndian(t);
        switch (format)
        {
            case 0:
            {
                if (t.Length < 262) return;
                for (var c = 0; c < 256; c++) if (t[6 + c] != 0) _cmap[c] = t[6 + c];
                break;
            }
            case 4:
            {
                if (t.Length < 14) return;
                int segX2 = BinaryPrimitives.ReadUInt16BigEndian(t[6..]);
                var seg = segX2 / 2;
                if (seg <= 0 || 16 + segX2 * 4 > t.Length) return;
                var endBase = 14;
                var startBase = endBase + segX2 + 2;
                var deltaBase = startBase + segX2;
                var rangeBase = deltaBase + segX2;
                for (var i = 0; i < seg; i++)
                {
                    int end = BinaryPrimitives.ReadUInt16BigEndian(t[(endBase + i * 2)..]);
                    int start = BinaryPrimitives.ReadUInt16BigEndian(t[(startBase + i * 2)..]);
                    var delta = BinaryPrimitives.ReadInt16BigEndian(t[(deltaBase + i * 2)..]);
                    int rangeOffset = BinaryPrimitives.ReadUInt16BigEndian(t[(rangeBase + i * 2)..]);
                    if (start > end) continue;
                    if (end - start > 0xFFFF) continue;
                    for (var c = start; c <= end && c <= 0xFFFF; c++)
                    {
                        ushort glyph;
                        if (rangeOffset == 0) glyph = (ushort)((c + delta) & 0xFFFF);
                        else
                        {
                            var idx = rangeBase + i * 2 + rangeOffset + (c - start) * 2;
                            if (idx + 2 > t.Length) continue;
                            glyph = BinaryPrimitives.ReadUInt16BigEndian(t[idx..]);
                            if (glyph != 0) glyph = (ushort)((glyph + delta) & 0xFFFF);
                        }
                        if (glyph != 0) _cmap[c] = glyph;
                    }
                }
                break;
            }
            case 6:
            {
                if (t.Length < 10) return;
                int first = BinaryPrimitives.ReadUInt16BigEndian(t[6..]);
                int n = BinaryPrimitives.ReadUInt16BigEndian(t[8..]);
                for (var i = 0; i < n && 10 + i * 2 + 2 <= t.Length; i++)
                {
                    var g = BinaryPrimitives.ReadUInt16BigEndian(t[(10 + i * 2)..]);
                    if (g != 0) _cmap[first + i] = g;
                }
                break;
            }
            case 12:
            {
                if (t.Length < 16) return;
                var groups = (int)BinaryPrimitives.ReadUInt32BigEndian(t[12..]);
                groups = Math.Min(groups, (t.Length - 16) / 12);
                for (var i = 0; i < groups; i++)
                {
                    var p = 16 + i * 12;
                    var start = BinaryPrimitives.ReadUInt32BigEndian(t[p..]);
                    var end = BinaryPrimitives.ReadUInt32BigEndian(t[(p + 4)..]);
                    var glyph = BinaryPrimitives.ReadUInt32BigEndian(t[(p + 8)..]);
                    // Code points end at 0x10FFFF. Bounding end also stops the uint loop below wrapping at 0xFFFFFFFF forever.
                    if (end < start || end > 0x10FFFF) continue;
                    for (var c = start; c <= end; c++)
                    {
                        var g = glyph + (c - start);
                        if (g is > 0 and <= 0xFFFF) _cmap[(int)c] = (ushort)g;
                    }
                }
                break;
            }
        }
    }

    private void ParseName()
    {
        var name = Table("name");
        if (name.Length < 6) return;
        int count = BinaryPrimitives.ReadUInt16BigEndian(name[2..]);
        int storage = BinaryPrimitives.ReadUInt16BigEndian(name[4..]);
        for (var i = 0; i < count; i++)
        {
            var p = 6 + i * 12;
            if (p + 12 > name.Length) break;
            int platform = BinaryPrimitives.ReadUInt16BigEndian(name[p..]);
            int nameId = BinaryPrimitives.ReadUInt16BigEndian(name[(p + 6)..]);
            int length = BinaryPrimitives.ReadUInt16BigEndian(name[(p + 8)..]);
            int offset = BinaryPrimitives.ReadUInt16BigEndian(name[(p + 10)..]);
            if (nameId is not (1 or 4)) continue;
            var start = storage + offset;
            if (start < 0 || start + length > name.Length) continue;
            var bytes = name.Slice(start, length);
            var text = platform == 1 ? Encoding.ASCII.GetString(bytes) : Encoding.BigEndianUnicode.GetString(bytes);
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (nameId == 1) FamilyName ??= text;
            else FullName ??= text;
        }
    }

    private void ParseKern()
    {
        var kern = Table("kern");
        if (kern.Length < 4) return;
        int nTables = BinaryPrimitives.ReadUInt16BigEndian(kern[2..]);
        var pos = 4;
        var pairs = new List<(ushort, ushort, short)>();
        for (var t = 0; t < nTables && pos + 6 <= kern.Length; t++)
        {
            var length = BinaryPrimitives.ReadUInt16BigEndian(kern[(pos + 2)..]);
            var coverage = BinaryPrimitives.ReadUInt16BigEndian(kern[(pos + 4)..]);
            var format = coverage >> 8;
            var horizontal = (coverage & 1) != 0;
            if (format == 0 && horizontal && pos + 14 <= kern.Length)
            {
                int nPairs = BinaryPrimitives.ReadUInt16BigEndian(kern[(pos + 6)..]);
                var pairBase = pos + 14;
                for (var i = 0; i < nPairs && pairBase + i * 6 + 6 <= kern.Length; i++)
                {
                    var p = pairBase + i * 6;
                    pairs.Add((BinaryPrimitives.ReadUInt16BigEndian(kern[p..]), BinaryPrimitives.ReadUInt16BigEndian(kern[(p + 2)..]), BinaryPrimitives.ReadInt16BigEndian(kern[(p + 4)..])));
                }
            }
            if (length <= 0) break;
            pos += length;
        }
        _kerningPairs = [.. pairs.OrderBy(p => ((uint)p.Item1 << 16) | p.Item2)];
    }

    /// <summary>Maps a Unicode code point to a glyph index. Returns 0 (the .notdef glyph) when unmapped.</summary>
    public ushort GetGlyphIndex(int codePoint) => _cmap.TryGetValue(codePoint, out var g) ? g : (ushort)0;

    /// <summary>True when the font has a glyph for the code point.</summary>
    public bool HasGlyph(int codePoint) => _cmap.ContainsKey(codePoint);

    /// <summary>Horizontal advance for a glyph, in font units.</summary>
    public int GetAdvanceWidth(ushort glyphIndex)
    {
        if (_advanceWidths.Length == 0) return UnitsPerEm / 2;
        return glyphIndex < _advanceWidths.Length ? _advanceWidths[glyphIndex] : _advanceWidths[^1];
    }

    /// <summary>Kerning adjustment between two glyphs, in font units. Zero when the font has no kerning data for the pair.</summary>
    public int GetKerning(ushort left, ushort right)
    {
        if (_kerningPairs.Length == 0) return 0;
        var key = ((uint)left << 16) | right;
        var lo = 0;
        var hi = _kerningPairs.Length - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            var k = ((uint)_kerningPairs[mid].Left << 16) | _kerningPairs[mid].Right;
            if (k == key) return _kerningPairs[mid].Value;
            if (k < key) lo = mid + 1; else hi = mid - 1;
        }
        return 0;
    }

    /// <summary>Returns the outline of a glyph in font units, or null when the glyph is blank.</summary>
    public GlyphOutline? GetGlyphOutline(ushort glyphIndex)
    {
        if (_glyphCache.TryGetValue(glyphIndex, out var cached)) return cached;
        var budget = MaxCompositeComponents;
        var outline = LoadGlyph(glyphIndex, 0, ref budget);
        _glyphCache[glyphIndex] = outline;
        return outline;
    }

    /// <summary>
    /// Total components one glyph may expand to across every nesting level. The depth limit alone still allows a
    /// composite that references itself many times to cost exponential time.
    /// </summary>
    private const int MaxCompositeComponents = 512;

    private GlyphOutline? LoadGlyph(ushort glyphIndex, int depth, ref int budget)
    {
        if (depth > 5 || glyphIndex >= _loca.Length - 1) return null;
        var start = _loca[glyphIndex];
        var end = _loca[glyphIndex + 1];
        if (end <= start || start >= (uint)_glyfLength) return null;
        var length = (int)Math.Min(end - start, (uint)(_glyfLength - start));
        if (length < 10) return null;
        var g = Data.Slice(_glyfOffset + (int)start, length);
        var numberOfContours = BinaryPrimitives.ReadInt16BigEndian(g);
        return numberOfContours >= 0 ? ParseSimpleGlyph(g, numberOfContours) : ParseCompositeGlyph(g, depth, ref budget);
    }

    private static GlyphOutline? ParseSimpleGlyph(ReadOnlySpan<byte> g, int numberOfContours)
    {
        if (numberOfContours == 0) return null;
        var p = 10;
        if (p + numberOfContours * 2 + 2 > g.Length) return null;
        var endPoints = new int[numberOfContours];
        for (var i = 0; i < numberOfContours; i++) endPoints[i] = BinaryPrimitives.ReadUInt16BigEndian(g[(p + i * 2)..]);
        p += numberOfContours * 2;
        var pointCount = endPoints[^1] + 1;
        if (pointCount <= 0 || pointCount > 10000) return null;
        int instructionLength = BinaryPrimitives.ReadUInt16BigEndian(g[p..]);
        p += 2 + instructionLength;
        if (p > g.Length) return null;

        var flags = new byte[pointCount];
        for (var i = 0; i < pointCount;)
        {
            if (p >= g.Length) return null;
            var f = g[p++];
            flags[i++] = f;
            if ((f & 8) != 0)
            {
                if (p >= g.Length) return null;
                var repeat = g[p++];
                for (var r = 0; r < repeat && i < pointCount; r++) flags[i++] = f;
            }
        }

        var xs = new int[pointCount];
        var x = 0;
        for (var i = 0; i < pointCount; i++)
        {
            var f = flags[i];
            if ((f & 2) != 0)
            {
                if (p >= g.Length) return null;
                int dx = g[p++];
                x += (f & 16) != 0 ? dx : -dx;
            }
            else if ((f & 16) == 0)
            {
                if (p + 2 > g.Length) return null;
                x += BinaryPrimitives.ReadInt16BigEndian(g[p..]);
                p += 2;
            }
            xs[i] = x;
        }
        var ys = new int[pointCount];
        var y = 0;
        for (var i = 0; i < pointCount; i++)
        {
            var f = flags[i];
            if ((f & 4) != 0)
            {
                if (p >= g.Length) return null;
                int dy = g[p++];
                y += (f & 32) != 0 ? dy : -dy;
            }
            else if ((f & 32) == 0)
            {
                if (p + 2 > g.Length) return null;
                y += BinaryPrimitives.ReadInt16BigEndian(g[p..]);
                p += 2;
            }
            ys[i] = y;
        }

        var contours = new List<GlyphContour>(numberOfContours);
        var startIndex = 0;
        foreach (var endIndex in endPoints)
        {
            if (endIndex < startIndex || endIndex >= pointCount) { startIndex = endIndex + 1; continue; }
            var n = endIndex - startIndex + 1;
            var points = new GlyphPoint[n];
            for (var i = 0; i < n; i++)
                points[i] = new GlyphPoint(xs[startIndex + i], ys[startIndex + i], (flags[startIndex + i] & 1) != 0);
            contours.Add(new GlyphContour(points));
            startIndex = endIndex + 1;
        }
        return contours.Count == 0 ? null : new GlyphOutline(contours);
    }

    private GlyphOutline? ParseCompositeGlyph(ReadOnlySpan<byte> g, int depth, ref int budget)
    {
        var p = 10;
        var contours = new List<GlyphContour>();
        while (p + 4 <= g.Length)
        {
            int flags = BinaryPrimitives.ReadUInt16BigEndian(g[p..]);
            var glyphIndex = BinaryPrimitives.ReadUInt16BigEndian(g[(p + 2)..]);
            p += 4;
            float dx, dy;
            if ((flags & 1) != 0) // ARG_1_AND_2_ARE_WORDS
            {
                if (p + 4 > g.Length) break;
                dx = BinaryPrimitives.ReadInt16BigEndian(g[p..]);
                dy = BinaryPrimitives.ReadInt16BigEndian(g[(p + 2)..]);
                p += 4;
            }
            else
            {
                if (p + 2 > g.Length) break;
                dx = (sbyte)g[p];
                dy = (sbyte)g[p + 1];
                p += 2;
            }
            if ((flags & 2) == 0) { dx = 0; dy = 0; } // offsets are point indices, not supported

            float a = 1, b = 0, c = 0, d = 1;
            if ((flags & 8) != 0) // WE_HAVE_A_SCALE
            {
                if (p + 2 > g.Length) break;
                a = d = F2Dot14(g, p); p += 2;
            }
            else if ((flags & 0x40) != 0) // X_AND_Y_SCALE
            {
                if (p + 4 > g.Length) break;
                a = F2Dot14(g, p); d = F2Dot14(g, p + 2); p += 4;
            }
            else if ((flags & 0x80) != 0) // TWO_BY_TWO
            {
                if (p + 8 > g.Length) break;
                a = F2Dot14(g, p); b = F2Dot14(g, p + 2); c = F2Dot14(g, p + 4); d = F2Dot14(g, p + 6); p += 8;
            }

            if (--budget < 0) break;
            var component = LoadGlyph(glyphIndex, depth + 1, ref budget);
            if (component is not null)
            {
                var m = new Matrix3x2(a, b, c, d, dx, dy);
                foreach (var contour in component.Contours)
                {
                    var points = new GlyphPoint[contour.Points.Count];
                    for (var i = 0; i < points.Length; i++)
                    {
                        var pt = contour.Points[i];
                        var v = Vector2.Transform(new Vector2(pt.X, pt.Y), m);
                        points[i] = new GlyphPoint(v.X, v.Y, pt.OnCurve);
                    }
                    contours.Add(new GlyphContour(points));
                }
            }
            if ((flags & 0x20) == 0) break; // MORE_COMPONENTS
        }
        return contours.Count == 0 ? null : new GlyphOutline(contours);
    }

    private static float F2Dot14(ReadOnlySpan<byte> d, int offset) => BinaryPrimitives.ReadInt16BigEndian(d[offset..]) / 16384f;
}

/// <summary>One point of a glyph contour. Off-curve points are quadratic Bézier control points.</summary>
public readonly record struct GlyphPoint(float X, float Y, bool OnCurve);

/// <summary>One closed contour of a glyph.</summary>
public sealed class GlyphContour
{
    public GlyphContour(IReadOnlyList<GlyphPoint> points) => Points = points;
    public IReadOnlyList<GlyphPoint> Points { get; }
}

/// <summary>A glyph's outline in font units, with the Y axis pointing up as in the font format.</summary>
public sealed class GlyphOutline
{
    public GlyphOutline(IReadOnlyList<GlyphContour> contours) => Contours = contours;
    public IReadOnlyList<GlyphContour> Contours { get; }

    /// <summary>
    /// Appends the glyph to a path builder, converting quadratic segments and flipping the Y axis so the result is in
    /// screen coordinates.
    /// </summary>
    public void AppendTo(Drawing.PathBuilder builder, Vector2 origin, float scale)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var contour in Contours)
        {
            var pts = contour.Points;
            var n = pts.Count;
            if (n == 0) continue;

            Vector2 Map(GlyphPoint p) => new(origin.X + p.X * scale, origin.Y - p.Y * scale);

            // Find a starting on-curve point; if there is none, synthesise one between two off-curve points.
            var startIndex = -1;
            for (var i = 0; i < n; i++) if (pts[i].OnCurve) { startIndex = i; break; }
            Vector2 startPoint;
            if (startIndex < 0)
            {
                startPoint = (Map(pts[0]) + Map(pts[n - 1])) / 2;
                // The synthesised start sits between the last and first points, so the walk begins at the last one
                // and its first step is to point 0.
                startIndex = n - 1;
                builder.MoveTo(startPoint);
                EmitFrom(builder, pts, n, startIndex, Map, startPoint, allOffCurve: true);
            }
            else
            {
                startPoint = Map(pts[startIndex]);
                builder.MoveTo(startPoint);
                EmitFrom(builder, pts, n, startIndex, Map, startPoint, allOffCurve: false);
            }
            builder.Close();
        }
    }

    private static void EmitFrom(Drawing.PathBuilder builder, IReadOnlyList<GlyphPoint> pts, int n, int startIndex, Func<GlyphPoint, Vector2> map, Vector2 startPoint, bool allOffCurve)
    {
        Vector2? pendingControl = null;
        var count = allOffCurve ? n : n - 1;
        for (var k = 1; k <= count; k++)
        {
            var p = pts[(startIndex + k) % n];
            var v = map(p);
            if (p.OnCurve)
            {
                if (pendingControl is { } c) { builder.QuadraticTo(c, v); pendingControl = null; }
                else builder.LineTo(v);
            }
            else
            {
                if (pendingControl is { } c)
                {
                    // Two consecutive off-curve points imply an on-curve point halfway between them.
                    var implied = (c + v) / 2;
                    builder.QuadraticTo(c, implied);
                }
                pendingControl = v;
            }
        }
        if (pendingControl is { } last) builder.QuadraticTo(last, startPoint);
    }
}
