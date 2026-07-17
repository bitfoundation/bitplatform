// The font model: the parts needed to position and extract text.

using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Bit.BlazorUI;

/// <summary>
/// The information required to lay out and extract text for one PDF font:
/// glyph widths, the code-to-Unicode mapping, and the number of bytes per
/// character code. Supports simple (single-byte) fonts and Type0 composite
/// fonts using two-byte codes (Identity and CMap-based encodings).
/// </summary>
public sealed class BitPdfFont
{
    private readonly bool _isType0;
    private readonly int _firstChar;
    private readonly double[] _widths;          // simple-font widths, 1000-em
    private readonly Dictionary<int, double> _cidWidths; // Type0 widths, 1000-em
    private readonly double _defaultWidth;      // /DW or /MissingWidth, 1000-em
    private readonly BitPdfToUnicodeCMap? _toUnicode;
    private readonly Func<int, double>? _standardWidth; // Core-14 fallback metrics
    private readonly string[]? _encoding;       // simple-font code -> glyph name
    private BitPdfCMap _cidEncoding = BitPdfCMap.Identity;   // Type0 code -> CID mapping
    private double _glyphWidthScale = 1.0;      // maps raw widths to 1000-em (Type3)

    // Glyph-based painting via Private-Use-Area codepoints: the browser renders
    // the *exact* glyph (no shaping, no Unicode collisions). Simple fonts map
    // PUA = 0xE000 + code; Type0 (CID) fonts map PUA = 0xE000 + glyph-id. The real
    // Unicode is kept on a parallel invisible layer for selection/search.
    private HashSet<int>? _glyphMappedCodes;   // simple fonts
    private Func<int, int>? _gidForCid;         // Type0: CID -> glyph id
    private int _maxPuaGid;                     // exclusive upper bound of mapped gids

    /// <summary>The base of the Private-Use-Area range used for glyph mapping.</summary>
    internal const int GlyphPua = 0xE000;

    /// <summary>The size of the usable PUA range (0xE000..0xF8FF).</summary>
    internal const int GlyphPuaSize = 0xF900 - 0xE000;

    /// <summary><c>true</c> when this font paints glyphs via PUA codepoints.</summary>
    public bool UsesGlyphMap => _glyphMappedCodes is not null || _gidForCid is not null;

    /// <summary>
    /// The Private-Use-Area codepoint to paint for <paramref name="code"/> so its
    /// exact glyph renders, or -1 when the code isn't glyph-mapped (paint Unicode).
    /// </summary>
    public int GlyphPuaChar(int code)
    {
        if (_glyphMappedCodes is not null)
        {
            return _glyphMappedCodes.Contains(code) ? GlyphPua + code : -1;
        }
        if (_gidForCid is not null)
        {
            int gid = _gidForCid(_cidEncoding.Lookup(code));
            return gid >= 0 && gid < _maxPuaGid ? GlyphPua + gid : -1;
        }
        return -1;
    }

    private void SetGlyphMap(HashSet<int> codes) => _glyphMappedCodes = codes.Count > 0 ? codes : null;

    private void SetType0GlyphMap(Func<int, int> gidForCid, int maxGid)
    {
        _gidForCid = gidForCid;
        _maxPuaGid = maxGid;
    }

    /// <summary>Type3 glyph data (glyph procedures), when the font is a Type3 font.</summary>
    public BitPdfType3FontData? Type3 { get; private set; }

    /// <summary><c>true</c> for a Type3 font whose glyphs are content-stream procedures.</summary>
    public bool IsType3 => Type3 is not null;

    /// <summary>The PostScript base font name, when available.</summary>
    public string BaseFont { get; }

    /// <summary>The embedded font program bytes (TrueType/OpenType), if any.</summary>
    public byte[]? EmbeddedProgram { get; private set; }

    /// <summary>The CSS <c>@font-face</c> format of <see cref="EmbeddedProgram"/> ("truetype"/"opentype").</summary>
    public string? EmbeddedFormat { get; private set; }

    /// <summary>Whether the font is bold (from the base-font name or descriptor flags).</summary>
    public bool Bold { get; private set; }

    /// <summary>Whether the font is italic/oblique.</summary>
    public bool Italic { get; private set; }

    /// <summary><c>true</c> when an embeddable font program is available.</summary>
    public bool HasEmbedded => EmbeddedProgram is { Length: > 0 } && EmbeddedFormat is not null;

    private string? _fontFaceFamily;

    /// <summary>
    /// A stable CSS family name derived from a content hash of the embedded font
    /// program. Using the program bytes (not <c>string.GetHashCode</c>, which is
    /// randomized per process) keeps <c>@font-face</c> names deterministic and
    /// avoids cross-font collisions.
    /// </summary>
    public string FontFaceFamily => _fontFaceFamily ??= $"bpf{StableFontHash():x8}";

    private uint StableFontHash()
    {
        // FNV-1a over the embedded program (or the base-font name when not
        // embedded). Deterministic across processes and WASM-safe.
        uint h = 2166136261;
        if (EmbeddedProgram is { Length: > 0 } p)
        {
            foreach (byte b in p)
            {
                h = (h ^ b) * 16777619;
            }
        }
        else
        {
            foreach (char c in BaseFont)
            {
                h = (h ^ (byte)c) * 16777619;
            }
        }
        return h;
    }

    /// <summary>A generic CSS family ("serif"/"sans-serif"/"monospace") inferred from the base font.</summary>
    public string GenericFamily => InferGenericFamily(BaseFont);

    /// <summary>Number of bytes per character code (1 for simple, 2 for Type0).</summary>
    public int BytesPerCode => _isType0 ? 2 : 1;

    private BitPdfFont(
        bool isType0, string baseFont, int firstChar, double[] widths,
        Dictionary<int, double> cidWidths, double defaultWidth, BitPdfToUnicodeCMap? toUnicode,
        Func<int, double>? standardWidth = null, string[]? encoding = null)
    {
        _isType0 = isType0;
        BaseFont = baseFont;
        _firstChar = firstChar;
        _widths = widths;
        _cidWidths = cidWidths;
        _defaultWidth = defaultWidth;
        _toUnicode = toUnicode;
        _standardWidth = standardWidth;
        _encoding = encoding;
    }

    /// <summary>Builds a <see cref="BitPdfFont"/> from a font dictionary.</summary>
    public static BitPdfFont Create(BitPdfDict fontDict, IBitPdfXRef xref)
    {
        string subtype = (fontDict.Get("Subtype") as BitPdfName)?.Value ?? "";
        string baseFont = (fontDict.Get("BaseFont") as BitPdfName)?.Value ?? "";
        BitPdfToUnicodeCMap? toUnicode = ReadToUnicode(fontDict);

        BitPdfFont font = subtype switch
        {
            "Type0" => CreateType0(fontDict, xref, baseFont, toUnicode),
            "Type3" => CreateType3(fontDict, xref, toUnicode),
            _ => CreateSimple(fontDict, baseFont, toUnicode),
        };

        // Locate the font descriptor (on the font, or its descendant for Type0).
        BitPdfDict? descriptor = fontDict.Get("FontDescriptor") as BitPdfDict;
        if (descriptor is null && fontDict.Get("DescendantFonts") is List<object?> desc && desc.Count > 0
            && xref.FetchIfRef(desc[0]) is BitPdfDict cidFont)
        {
            descriptor = cidFont.Get("FontDescriptor") as BitPdfDict;
        }

        (byte[]? program, string? format) = ExtractEmbedded(descriptor);

        // Sanitize an embedded TrueType now that the encoding is known: fix the
        // sfnt structure, add required tables, and inject a synthetic Unicode cmap
        // mapping the codepoints we emit to the right glyphs (so subset fonts whose
        // own cmap OTS rejects still render). Falls back to the raw bytes.
        if (format == "truetype" && program is not null)
        {
            program = font._isType0
                ? FixType0TrueType(program!, font, fontDict, xref) ?? program
                : FixTrueType(program!, font, descriptor) ?? program;
        }
        else if (format == "cff" && program is not null)
        {
            (program, format) = font._isType0
                ? FixType0Cff(program!, font)
                : FixCff(program!, font, fontDict);
        }

        font.EmbeddedProgram = program;
        font.EmbeddedFormat = format;
        font.Bold = InferBold(baseFont, descriptor);
        font.Italic = InferItalic(baseFont, descriptor);
        return font;
    }

    // Wraps a bare CFF (/FontFile3 /Type1C) in OpenType with a synthetic cmap;
    // returns (null, "cff") to fall back to a substitute when unsupported.
    private static (byte[]?, string) FixCff(byte[] cff, BitPdfFont font, BitPdfDict fontDict)
    {
        var parser = BitPdfCffFontParser.Parse(cff);
        if (parser is null || parser.IsCid || font._isType0)
        {
            return (null, "cff");
        }

        // Map each character code to a unique Private-Use-Area codepoint so that
        // distinct glyphs sharing one Unicode value (lowercase 'a' at 0x61 vs
        // small-cap 'A' at 0xFF) never collide. The renderer paints these PUA
        // codepoints; the real Unicode is preserved on the selection layer.
        var map = new Dictionary<int, int>();
        var mappedCodes = new HashSet<int>();
        var advances = new int[parser.NumGlyphs]; // glyph-id -> advance (1000-em)
        for (int code = 0; code < 256; code++)
        {
            string? name = font._encoding is not null && code < font._encoding.Length
                && font._encoding[code] is { Length: > 0 } nm ? nm : null;

            // Resolve the glyph id: the PDF encoding's name via the CFF charset
            // first (honors /Differences), then the font's built-in Encoding.
            int gid = -1;
            if (name is not null && parser.NameToGid.TryGetValue(name, out int g1))
            {
                gid = g1;
            }
            else if (parser.CodeToGid.TryGetValue(code, out int g2))
            {
                gid = g2;
            }
            if (gid <= 0)
            {
                continue;
            }

            map[GlyphPua + code] = gid;
            mappedCodes.Add(code);
            if (gid < advances.Length)
            {
                advances[gid] = (int)Math.Round(font.WidthFor(code)); // real per-glyph advance
            }
        }

        if (map.Count == 0)
        {
            return (null, "cff");
        }
        byte[] cmap = BitPdfCmapBuilder.BuildUnicodeCmap(map);
        byte[]? otf = BitPdfCffFontWriter.WrapBareCff(cff, parser.NumGlyphs, parser.FontMatrix, cmap, "BitPdfCff", advances);
        if (otf is null)
        {
            return (null, "cff");
        }
        font.SetGlyphMap(mappedCodes);
        return (otf, "opentype");
    }

    // Glyph-maps a Type0 CIDFontType2 (TrueType CID) font: paints each glyph by
    // its exact glyph id via a PUA cmap, so Arabic/Persian and other complex
    // scripts render the PDF's positioned glyphs instead of browser-reshaped text.
    private static byte[]? FixType0TrueType(byte[] program, BitPdfFont font, BitPdfDict fontDict, IBitPdfXRef xref)
    {
        if (fontDict.Get("DescendantFonts") is not List<object?> desc || desc.Count == 0
            || xref.FetchIfRef(desc[0]) is not BitPdfDict cidFont
            || (cidFont.Get("Subtype") as BitPdfName)?.Value != "CIDFontType2")
        {
            return null; // only TrueType-CID here; CIDFontType0 (CFF) is separate
        }

        int numGlyphs = BitPdfSfntGlyphMapper.ReadNumGlyphs(program);
        if (numGlyphs <= 0)
        {
            return null;
        }

        // CIDToGIDMap: /Identity (gid == CID) or a stream of 2-byte gids per CID.
        Func<int, int> gidForCid = static cid => cid;
        if (xref.FetchIfRef(cidFont.Get("CIDToGIDMap")) is BitPdfStream mapStream)
        {
            byte[] d = BitPdfStreamDecoder.Decode(mapStream);
            gidForCid = cid => cid >= 0 && cid * 2 + 1 < d.Length ? (d[cid * 2] << 8) | d[cid * 2 + 1] : 0;
        }

        int max = Math.Min(numGlyphs, GlyphPuaSize);
        byte[] cmap = BuildGidPuaCmap(max);
        byte[]? sanitized = BitPdfTrueTypeSanitizer.Sanitize(program, cmap);
        if (sanitized is null)
        {
            return null;
        }
        font.SetType0GlyphMap(gidForCid, max);
        return sanitized;
    }

    // Wraps a CID-keyed bare CFF (/FontFile3 /CIDFontType0C) in OpenType with a
    // per-glyph-id PUA cmap; resolves code -> CID -> glyph id via the CFF charset.
    private static (byte[]?, string) FixType0Cff(byte[] cff, BitPdfFont font)
    {
        var parser = BitPdfCffFontParser.Parse(cff);
        if (parser is null || !parser.IsCid || parser.NumGlyphs <= 0)
        {
            return (null, "cff");
        }
        int max = Math.Min(parser.NumGlyphs, GlyphPuaSize);
        byte[] cmap = BuildGidPuaCmap(max);

        // Per-glyph advances (glyph-id indexed, 1000-em) from the CID /W widths.
        var advances = new int[parser.NumGlyphs];
        foreach (var (cid, gid) in parser.CidToGid)
        {
            if (gid >= 0 && gid < advances.Length)
            {
                advances[gid] = (int)Math.Round(font._cidWidths.TryGetValue(cid, out var w) ? w : font._defaultWidth);
            }
        }

        byte[]? otf = BitPdfCffFontWriter.WrapBareCff(cff, parser.NumGlyphs, parser.FontMatrix, cmap, "BitPdfCidCff", advances);
        if (otf is null)
        {
            return (null, "cff");
        }
        var cidToGid = parser.CidToGid;
        font.SetType0GlyphMap(cid => cidToGid.GetValueOrDefault(cid, 0), max);
        return (otf, "opentype");
    }

    // A PUA codepoint per glyph id (0xE000 + gid), one contiguous cmap segment.
    private static byte[] BuildGidPuaCmap(int count)
    {
        var map = new Dictionary<int, int>(count);
        for (int gid = 0; gid < count; gid++)
        {
            map[GlyphPua + gid] = gid;
        }
        return BitPdfCmapBuilder.BuildUnicodeCmap(map);
    }

    private static byte[]? FixTrueType(byte[] program, BitPdfFont font, BitPdfDict? descriptor)
    {
        byte[]? syntheticCmap = null;
        if (!font._isType0 && font._encoding is not null)
        {
            bool symbolic = descriptor?.Get("Flags") is double fl && ((int)fl & 0x4) != 0;
            syntheticCmap = BitPdfSfntGlyphMapper.BuildSyntheticCmap(program, font._encoding, symbolic,
                font.UnicodeFor, out var mappedCodes);
            if (syntheticCmap is not null)
            {
                font.SetGlyphMap(mappedCodes);
            }
        }
        return BitPdfTrueTypeSanitizer.Sanitize(program, syntheticCmap);
    }

    // A valid, non-empty PostScript-style name for the generated font (subset
    // prefix stripped, punctuation removed).
    private static string Sanitize(string name)
    {
        int plus = name.IndexOf('+');
        string n = plus == 6 ? name[(plus + 1)..] : name;
        var chars = n.Where(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').ToArray();
        return chars.Length > 0 ? new string(chars) : "BitPdfType1";
    }

    private static (byte[]?, string?) ExtractEmbedded(BitPdfDict? descriptor)
    {
        if (descriptor is null)
        {
            return (null, null);
        }
        try
        {
            // TrueType / CIDFontType2 program. Sanitizing (with a synthetic cmap)
            // happens in Create once the font's encoding is known.
            if (descriptor.Get("FontFile2") is BitPdfStream ttf)
            {
                return (BitPdfStreamDecoder.Decode(ttf), "truetype");
            }
            // FontFile3: OpenType passes through; Type1C/CIDFontType0C is bare CFF,
            // wrapped in Create once the encoding is known.
            if (descriptor.Get("FontFile3") is BitPdfStream ff3 && ff3.Dict is not null)
            {
                byte[] data = BitPdfStreamDecoder.Decode(ff3);
                return (ff3.Dict.Get("Subtype") as BitPdfName)?.Value == "OpenType"
                    ? (data, "opentype")
                    : (data, "cff");
            }
            // Type1 program (/FontFile): parse it and build an OpenType/CFF font the
            // browser can load. On any failure fall through to a substitute font -
            // a rejected @font-face simply falls back to the generic family.
            if (descriptor.Get("FontFile") is BitPdfStream t1Stream)
            {
                byte[] raw = BitPdfStreamDecoder.Decode(t1Stream);
                if (BitPdfType1Font.Parse(raw) is { } t1)
                {
                    string psName = (descriptor.Get("FontName") as BitPdfName)?.Value ?? "BitPdfType1";
                    if (BitPdfCffFontWriter.FromType1(t1, Sanitize(psName)) is { } otf)
                    {
                        return (otf, "opentype");
                    }
                }
            }
            // Bare CFF (FontFile3 /Type1C) still needs a CFF parser; substituted for now.
        }
        catch
        {
            // Ignore malformed font programs and fall back to substitute fonts.
        }
        return (null, null);
    }

    private static BitPdfFont CreateSimple(BitPdfDict fontDict, string baseFont, BitPdfToUnicodeCMap? toUnicode)
    {
        int firstChar = ToInt(fontDict.Get("FirstChar"), 0);
        var widths = new List<double>();
        if (fontDict.Get("Widths") is List<object?> widthArr)
        {
            foreach (var w in widthArr)
            {
                widths.Add(w is double d ? d : 0);
            }
        }

        double missingWidth = 0;
        bool symbolic = false;
        if (fontDict.Get("FontDescriptor") is BitPdfDict descriptor)
        {
            missingWidth = ToDouble(descriptor.Get("MissingWidth"), 0);
            // /Flags bit 3 (value 4) marks a symbolic font.
            symbolic = descriptor.Get("Flags") is double flags && ((int)flags & 0x4) != 0;
        }

        // Resolve Core-14 metrics whenever the base font is a standard font, so
        // they can fill in codes outside the explicit /Widths range too (not only
        // when /Widths is entirely absent).
        Func<int, double>? standardWidth = BitPdfStandardFonts.Resolve(baseFont);

        string[]? encoding = BuildSimpleEncoding(fontDict, baseFont, symbolic);

        return new BitPdfFont(
            isType0: false,
            baseFont,
            firstChar,
            widths.ToArray(),
            new Dictionary<int, double>(),
            missingWidth,
            toUnicode,
            standardWidth,
            encoding);
    }

    /// <summary>
    /// Builds the code-to-glyph-name table for a simple font from its
    /// <c>/Encoding</c> (a base-encoding name and/or a <c>/Differences</c> array).
    /// </summary>
    private static string[]? BuildSimpleEncoding(BitPdfDict fontDict, string baseFont, bool symbolic = false)
    {
        object? enc = fontDict.Get("Encoding");

        // The default base encoding: the named symbolic fonts (Symbol /
        // ZapfDingbats) carry their own built-in encoding; a font flagged
        // Symbolic in its descriptor uses the program's built-in encoding (which
        // we don't decode here), so start from an empty table rather than
        // imposing WinAnsi glyph names on codes that aren't WinAnsi. WinAnsi is
        // the pragmatic default for ordinary non-symbolic text.
        string[] baseTable = symbolic && !IsNamedSymbolFont(baseFont)
            ? EmptyEncodingTable()
            : DefaultBaseEncoding(baseFont);
        List<object?>? differences = null;

        switch (enc)
        {
            case BitPdfName name:
                baseTable = BitPdfEncodings.ByName(name.Value) ?? baseTable;
                break;
            case BitPdfDict dict:
                if (dict.Get("BaseEncoding") is BitPdfName baseName)
                {
                    baseTable = BitPdfEncodings.ByName(baseName.Value) ?? baseTable;
                }
                differences = dict.Get("Differences") as List<object?>;
                break;
            case null:
                // No /Encoding: nothing to override; keep the default table.
                return (string[])baseTable.Clone();
        }

        var table = (string[])baseTable.Clone();
        if (differences is not null)
        {
            int code = 0;
            foreach (var item in differences)
            {
                if (item is double d)
                {
                    code = (int)d;
                }
                else if (item is BitPdfName glyphName && code is >= 0 and < 256)
                {
                    table[code] = glyphName.Value;
                    code++;
                }
            }
        }
        return table;
    }

    /// <summary>
    /// Chooses the default base encoding for a simple font from its base-font
    /// name: the built-in encoding for Symbol/ZapfDingbats, WinAnsi otherwise.
    /// </summary>
    private static string[] DefaultBaseEncoding(string baseFont)
    {
        // Strip a subset prefix such as "ABCDEF+Symbol".
        int plus = baseFont.IndexOf('+');
        string name = plus >= 0 ? baseFont[(plus + 1)..] : baseFont;

        if (name.Contains("ZapfDingbats", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Dingbats", StringComparison.OrdinalIgnoreCase))
        {
            return BitPdfEncodings.ZapfDingbats;
        }
        if (name.Contains("Symbol", StringComparison.OrdinalIgnoreCase))
        {
            return BitPdfEncodings.Symbol;
        }
        return BitPdfEncodings.WinAnsi;
    }

    /// <summary>True when the base-font name is one of the built-in symbol fonts
    /// (Symbol / ZapfDingbats), which have their own well-known encoding table.</summary>
    private static bool IsNamedSymbolFont(string baseFont)
    {
        int plus = baseFont.IndexOf('+');
        string name = plus >= 0 ? baseFont[(plus + 1)..] : baseFont;
        return name.Contains("Symbol", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Dingbats", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] EmptyEncodingTable()
    {
        var table = new string[256];
        Array.Fill(table, string.Empty);
        return table;
    }

    private static BitPdfFont CreateType3(BitPdfDict fontDict, IBitPdfXRef xref, BitPdfToUnicodeCMap? toUnicode)
    {
        int firstChar = ToInt(fontDict.Get("FirstChar"), 0);
        var widths = new List<double>();
        if (fontDict.Get("Widths") is List<object?> widthArr)
        {
            foreach (var w in widthArr)
            {
                // /Widths elements may be indirect references (Type3 details, 1.20).
                widths.Add(BitPdfPrimitives.ResolveNumber(xref, w));
            }
        }

        // Type3 glyphs live in glyph space; /FontMatrix maps them to text space.
        BitPdfMatrix fontMatrix = ReadFontMatrix(fontDict);
        // A Type3 font defines its own glyphs via /CharProcs keyed by the names in
        // /Encoding /Differences; there is no standard base encoding, so start from
        // an empty table (symbolic) rather than imposing WinAnsi names.
        string[]? encoding = BuildSimpleEncoding(fontDict, "", symbolic: true);

        var font = new BitPdfFont(
            isType0: false,
            baseFont: "",
            firstChar,
            widths.ToArray(),
            new Dictionary<int, double>(),
            defaultWidth: 0,
            toUnicode,
            standardWidth: null,
            encoding)
        {
            // Widths are in glyph space; scale them to the 1000-em text space the
            // layout code expects (advance_text = width_glyph * fontMatrix.a).
            _glyphWidthScale = fontMatrix.A * 1000.0,
        };

        BitPdfDict? charProcs = fontDict.Get("CharProcs") as BitPdfDict;
        BitPdfDict? resources = fontDict.Get("Resources") as BitPdfDict;
        font.Type3 = new BitPdfType3FontData(fontMatrix, charProcs, resources, encoding, xref);
        return font;
    }

    private static BitPdfMatrix ReadFontMatrix(BitPdfDict fontDict)
    {
        if (fontDict.Get("FontMatrix") is List<object?> m && m.Count >= 6)
        {
            return new BitPdfMatrix(ToDouble(m[0], 0.001), ToDouble(m[1], 0), ToDouble(m[2], 0),
                ToDouble(m[3], 0.001), ToDouble(m[4], 0), ToDouble(m[5], 0));
        }
        return new BitPdfMatrix(0.001, 0, 0, 0.001, 0, 0);
    }

    private static BitPdfFont CreateType0(BitPdfDict fontDict, IBitPdfXRef xref, string baseFont, BitPdfToUnicodeCMap? toUnicode)
    {
        var cidWidths = new Dictionary<int, double>();
        double defaultWidth = 1000;

        if (fontDict.Get("DescendantFonts") is List<object?> descendants && descendants.Count > 0
            && xref.FetchIfRef(descendants[0]) is BitPdfDict cidFont)
        {
            defaultWidth = ToDouble(cidFont.Get("DW"), 1000);
            if (cidFont.Get("W") is List<object?> w)
            {
                ReadCidWidths(w, cidWidths, xref);
            }
        }

        var font = new BitPdfFont(
            isType0: true,
            baseFont,
            firstChar: 0,
            [],
            cidWidths,
            defaultWidth,
            toUnicode);

        // The Type0 /Encoding maps character codes to CIDs: Identity-H/V map
        // directly, an embedded CMap stream supplies explicit ranges.
        object? enc = fontDict.Get("Encoding");
        if (enc is BitPdfStream cmapStream)
        {
            try
            {
                font._cidEncoding = BitPdfCMap.Parse(BitPdfStreamDecoder.Decode(cmapStream));
            }
            catch
            {
                font._cidEncoding = BitPdfCMap.Identity;
            }
        }
        return font;
    }

    private static void ReadCidWidths(List<object?> w, Dictionary<int, double> cidWidths, IBitPdfXRef xref)
    {
        // Two forms: "c [w1 w2 ...]" and "cFirst cLast w". Array elements (and the
        // inner width list) may be indirect references, so resolve each.
        int i = 0;
        while (i < w.Count)
        {
            if (xref.FetchIfRef(w[i]) is not double first)
            {
                break;
            }
            object? second = i + 1 < w.Count ? xref.FetchIfRef(w[i + 1]) : null;
            if (second is List<object?> list)
            {
                int cid = (int)first;
                foreach (var item in list)
                {
                    if (xref.FetchIfRef(item) is double width)
                    {
                        cidWidths[cid] = width;
                    }
                    cid++;
                }
                i += 2;
            }
            else if (second is double last && i + 2 < w.Count && xref.FetchIfRef(w[i + 2]) is double width)
            {
                for (int cid = (int)first; cid <= (int)last; cid++)
                {
                    cidWidths[cid] = width;
                }
                i += 3;
            }
            else
            {
                break;
            }
        }
    }

    private static BitPdfToUnicodeCMap? ReadToUnicode(BitPdfDict fontDict)
    {
        if (fontDict.Get("ToUnicode") is BitPdfStream stream)
        {
            try
            {
                return BitPdfToUnicodeCMap.Parse(BitPdfStreamDecoder.Decode(stream));
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Decodes a show-text operand into its glyphs. The glyph count is known up
    /// front (one per byte for simple fonts, one per <c>step</c> bytes for Type0),
    /// so the result is materialized into an exactly-sized list filled through a
    /// span — no lazy iterator state machine and no intermediate re-grows.
    /// </summary>
    public List<BitPdfGlyph> Decode(byte[] bytes)
    {
        var src = new ReadOnlySpan<byte>(bytes);

        if (_isType0)
        {
            int step = _cidEncoding.CodeLength >= 1 ? _cidEncoding.CodeLength : 2;
            var glyphs = NewList(src.Length / step, out var dst);

            if (step == 2)
            {
                // Dominant case (Identity-H/V and most CMaps): read the big-endian
                // code in one shot instead of shifting byte-by-byte.
                for (int i = 0, n = 0; i + 2 <= src.Length; i += 2, n++)
                {
                    int code = BinaryPrimitives.ReadUInt16BigEndian(src.Slice(i));
                    int cid = _cidEncoding.Lookup(code);
                    // Text is keyed by the original code for ToUnicode lookup.
                    dst[n] = new BitPdfGlyph(code, UnicodeFor(code), WidthForCid(cid), isSpace: false);
                }
            }
            else
            {
                for (int i = 0, n = 0; i + step <= src.Length; i += step, n++)
                {
                    long code = 0;
                    for (int k = 0; k < step; k++)
                    {
                        code = (code << 8) | src[i + k];
                    }
                    int cid = _cidEncoding.Lookup(code);
                    dst[n] = new BitPdfGlyph((int)code, UnicodeFor((int)code), WidthForCid(cid), isSpace: false);
                }
            }
            return glyphs;
        }
        else
        {
            var glyphs = NewList(src.Length, out var dst);
            for (int i = 0; i < src.Length; i++)
            {
                int code = src[i];
                dst[i] = new BitPdfGlyph(code, UnicodeFor(code), WidthFor(code), isSpace: code == 0x20);
            }
            return glyphs;
        }
    }

    // A list pre-sized to exactly count elements, exposing its backing storage as
    // a span so callers fill by index without per-item Add bounds/grow checks.
    private static List<BitPdfGlyph> NewList(int count, out Span<BitPdfGlyph> storage)
    {
        var list = new List<BitPdfGlyph>(count);
        CollectionsMarshal.SetCount(list, count);
        storage = CollectionsMarshal.AsSpan(list);
        return list;
    }

    private double WidthForCid(int cid) => _cidWidths.TryGetValue(cid, out var w) ? w : _defaultWidth;

    private double WidthFor(int code)
    {
        int index = code - _firstChar;
        if (index >= 0 && index < _widths.Length)
        {
            // An explicit width of 0 is valid (e.g. combining marks) - use it
            // rather than falling through to a substitute metric.
            return _widths[index] * _glyphWidthScale;
        }
        // Code outside [FirstChar, FirstChar+len): prefer Core-14 metrics, then
        // the /MissingWidth default (never silently 0 when metrics exist).
        if (_standardWidth is not null)
        {
            return _standardWidth(code);
        }
        return _defaultWidth * _glyphWidthScale;
    }

    private string UnicodeFor(int code)
    {
        string? mapped = _toUnicode?.Lookup(code);
        if (!string.IsNullOrEmpty(mapped))
        {
            return mapped;
        }
        if (_isType0)
        {
            // Without a ToUnicode map a CID cannot be reliably mapped; emit nothing.
            return string.Empty;
        }
        // Resolve via the font's encoding (base encoding + /Differences).
        if (_encoding is not null && code is >= 0 and < 256 && _encoding[code].Length > 0)
        {
            string viaName = BitPdfGlyphList.ToUnicode(_encoding[code]);
            if (!string.IsNullOrEmpty(viaName))
            {
                return viaName;
            }
        }
        return BitPdfWinAnsiEncoding.CodeToUnicode(code);
    }

    private static string InferGenericFamily(string baseFont)
    {
        int plus = baseFont.IndexOf('+');
        string name = (plus >= 0 ? baseFont[(plus + 1)..] : baseFont).ToLowerInvariant();
        if (name.Contains("courier") || name.Contains("mono"))
        {
            return "monospace";
        }
        if (name.Contains("times") || name.Contains("serif") || name.Contains("roman")
            || name.Contains("georgia") || name.Contains("minion") || name.Contains("garamond"))
        {
            return "serif";
        }
        return "sans-serif";
    }

    private static bool InferBold(string baseFont, BitPdfDict? descriptor)
    {
        if (baseFont.Contains("Bold", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (descriptor?.Get("StemV") is double stemV && stemV >= 140)
        {
            return true;
        }
        // FontDescriptor /Flags bit 19 (0x40000) marks ForceBold.
        return descriptor?.Get("Flags") is double flags && ((int)flags & 0x40000) != 0;
    }

    private static bool InferItalic(string baseFont, BitPdfDict? descriptor)
    {
        if (baseFont.Contains("Italic", StringComparison.OrdinalIgnoreCase)
            || baseFont.Contains("Oblique", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (descriptor?.Get("ItalicAngle") is double angle && Math.Abs(angle) > 0.5)
        {
            return true;
        }
        // FontDescriptor /Flags bit 7 (0x40) marks Italic.
        return descriptor?.Get("Flags") is double flags && ((int)flags & 0x40) != 0;
    }

    private static int ToInt(object? value, int fallback) => value is double d ? (int)d : fallback;
    private static double ToDouble(object? value, double fallback) => value is double d ? d : fallback;
}
