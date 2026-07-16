// Recovers glyph mappings from an embedded TrueType (sfnt) font and builds a
// synthetic (3,1) Unicode cmap. Subset fonts embedded in PDFs frequently carry a
// cmap subtable the browser's OTS rejects ("no supported subtables"), or one
// keyed by byte codes rather than Unicode. We re-derive code→glyph-id from the
// font's own cmap/post and re-emit a clean Unicode cmap keyed by the codepoints
// the renderer actually emits (PDF spec 9.6.6.4, simplified).

namespace Bit.BlazorUI;

internal static class BitPdfSfntGlyphMapper
{
    /// <summary>
    /// Builds a (3,1) Unicode cmap for the given font so the codepoints the
    /// renderer emits select the right glyphs. Returns null when nothing usable
    /// could be derived (caller keeps the font's own cmap).
    /// </summary>
    public static byte[]? BuildSyntheticCmap(byte[] font, string[]? encoding, bool symbolic,
        Func<int, string> codeToUnicode, out HashSet<int> mappedCodes)
    {
        mappedCodes = new HashSet<int>();
        try
        {
            var dir = ReadDirectory(font);
            if (dir is null)
            {
                return null;
            }

            var uni = ReadCmapSubtable(font, dir, 3, 1);   // Windows Unicode
            var sym = ReadCmapSubtable(font, dir, 3, 0);   // Windows Symbol
            var mac = ReadCmapSubtable(font, dir, 1, 0);   // Mac Roman
            var names = ReadPost(font, dir);               // glyph name → gid

            // Map each byte code to a unique Private-Use-Area codepoint (glyph
            // painting is by exact glyph id — no shaping, no Unicode collisions).
            var result = new Dictionary<int, int>();
            for (int code = 0; code < 256; code++)
            {
                string? glyphName = encoding is not null && code < encoding.Length
                    && encoding[code] is { Length: > 0 } name
                    ? name
                    : null;

                int gid = ResolveGid(code, glyphName, symbolic, uni, sym, mac, names);
                if (gid <= 0)
                {
                    continue;
                }
                result[BitPdfFont.GlyphPua + code] = gid;
                mappedCodes.Add(code);
            }

            if (result.Count == 0)
            {
                return null;
            }
            return BitPdfCmapBuilder.BuildUnicodeCmap(result);
        }
        catch
        {
            return null;
        }
    }

    private static int ResolveGid(int code, string? glyphName, bool symbolic,
        Dictionary<int, int>? uni, Dictionary<int, int>? sym, Dictionary<int, int>? mac,
        Dictionary<string, int>? names)
    {
        // Named glyph via post table is the most reliable for subset fonts.
        if (glyphName is not null && names is not null && names.TryGetValue(glyphName, out int g1))
        {
            return g1;
        }

        if (symbolic)
        {
            // Symbolic: the (3,0) subtable is keyed at 0xF000+code (or code).
            if (sym is not null && (sym.TryGetValue(0xF000 + code, out int g2) || sym.TryGetValue(code, out g2)))
            {
                return g2;
            }
            if (mac is not null && mac.TryGetValue(code, out int g3))
            {
                return g3;
            }
        }

        // Non-symbolic: map through the glyph name's Unicode in the (3,1) subtable.
        if (glyphName is not null && uni is not null)
        {
            string u = BitPdfGlyphList.ToUnicode(glyphName);
            if (u.Length >= 1 && uni.TryGetValue(char.ConvertToUtf32(u, 0), out int g4))
            {
                return g4;
            }
        }
        if (uni is not null && uni.TryGetValue(code, out int g5))
        {
            return g5;
        }
        if (mac is not null && mac.TryGetValue(code, out int g6))
        {
            return g6;
        }
        if (sym is not null && (sym.TryGetValue(0xF000 + code, out int g7) || sym.TryGetValue(code, out g7)))
        {
            return g7;
        }
        return 0;
    }

    /// <summary>Reads the glyph count from a TrueType font's maxp table (0 on failure).</summary>
    public static int ReadNumGlyphs(byte[] font)
    {
        try
        {
            var dir = ReadDirectory(font);
            if (dir is not null && dir.TryGetValue("maxp", out var m) && m.Off + 6 <= font.Length)
            {
                return U16(font, m.Off + 4);
            }
        }
        catch
        {
            // fall through
        }
        return 0;
    }

    // ----- sfnt parsing -----

    private static Dictionary<string, (int Off, int Len)>? ReadDirectory(byte[] f)
    {
        if (f.Length < 12)
        {
            return null;
        }
        int n = U16(f, 4);
        if (12 + n * 16 > f.Length)
        {
            return null;
        }
        var dir = new Dictionary<string, (int, int)>();
        for (int i = 0; i < n; i++)
        {
            int rec = 12 + i * 16;
            string tag = $"{(char)f[rec]}{(char)f[rec + 1]}{(char)f[rec + 2]}{(char)f[rec + 3]}";
            int off = (int)U32(f, rec + 8);
            int len = (int)U32(f, rec + 12);
            if (off >= 0 && len >= 0 && (long)off + len <= f.Length)
            {
                dir[tag] = (off, len);
            }
        }
        return dir;
    }

    private static Dictionary<int, int>? ReadCmapSubtable(byte[] f, Dictionary<string, (int Off, int Len)> dir, int plat, int enc)
    {
        if (!dir.TryGetValue("cmap", out var c))
        {
            return null;
        }
        int baseOff = c.Off;
        int tableCount = U16(f, baseOff + 2);
        for (int i = 0; i < tableCount; i++)
        {
            int rec = baseOff + 4 + i * 8;
            if (U16(f, rec) == plat && U16(f, rec + 2) == enc)
            {
                int subOff = baseOff + (int)U32(f, rec + 4);
                return ParseSubtable(f, subOff);
            }
        }
        return null;
    }

    private static Dictionary<int, int>? ParseSubtable(byte[] f, int o)
    {
        if (o < 0 || o + 4 > f.Length)
        {
            return null;
        }
        int format = U16(f, o);
        var map = new Dictionary<int, int>();
        switch (format)
        {
            case 0: // byte encoding table
                for (int i = 0; i < 256 && o + 6 + i < f.Length; i++)
                {
                    int gid = f[o + 6 + i];
                    if (gid != 0)
                    {
                        map[i] = gid;
                    }
                }
                break;
            case 6: // trimmed table
            {
                int first = U16(f, o + 6);
                int count = U16(f, o + 8);
                for (int i = 0; i < count; i++)
                {
                    int gid = U16(f, o + 10 + i * 2);
                    if (gid != 0)
                    {
                        map[first + i] = gid;
                    }
                }
                break;
            }
            case 4: // segment mapping
            {
                int segX2 = U16(f, o + 6);
                int segCount = segX2 / 2;
                int endO = o + 14;
                int startO = endO + segX2 + 2;
                int deltaO = startO + segX2;
                int rangeO = deltaO + segX2;
                for (int s = 0; s < segCount; s++)
                {
                    int end = U16(f, endO + s * 2);
                    int start = U16(f, startO + s * 2);
                    int delta = U16(f, deltaO + s * 2);
                    int rangeOffset = U16(f, rangeO + s * 2);
                    for (int ch = start; ch <= end && ch != 0xFFFF; ch++)
                    {
                        int gid;
                        if (rangeOffset == 0)
                        {
                            gid = (ch + delta) & 0xFFFF;
                        }
                        else
                        {
                            int gi = rangeO + s * 2 + rangeOffset + (ch - start) * 2;
                            if (gi + 1 >= f.Length)
                            {
                                continue;
                            }
                            gid = U16(f, gi);
                            if (gid != 0)
                            {
                                gid = (gid + delta) & 0xFFFF;
                            }
                        }
                        if (gid != 0)
                        {
                            map[ch] = gid;
                        }
                    }
                }
                break;
            }
            case 12: // segmented coverage
            {
                int nGroups = (int)U32(f, o + 12);
                for (int gr = 0; gr < nGroups; gr++)
                {
                    int rec = o + 16 + gr * 12;
                    if (rec + 12 > f.Length)
                    {
                        break;
                    }
                    long start = U32(f, rec);
                    long end = U32(f, rec + 4);
                    long startGid = U32(f, rec + 8);
                    for (long ch = start; ch <= end && ch <= 0xFFFF; ch++)
                    {
                        map[(int)ch] = (int)(startGid + (ch - start));
                    }
                }
                break;
            }
            default:
                return null;
        }
        return map.Count > 0 ? map : null;
    }

    private static readonly string[] MacGlyphNames = BuildMacGlyphNames();

    private static Dictionary<string, int>? ReadPost(byte[] f, Dictionary<string, (int Off, int Len)> dir)
    {
        if (!dir.TryGetValue("post", out var p))
        {
            return null;
        }
        int o = p.Off;
        if (o + 34 > f.Length)
        {
            return null;
        }
        uint version = U32(f, o);
        if (version != 0x00020000)
        {
            return null; // only version 2 carries glyph names
        }
        int numGlyphs = U16(f, o + 32);
        int idxO = o + 34;
        if (idxO + numGlyphs * 2 > f.Length)
        {
            return null;
        }
        var indices = new int[numGlyphs];
        int namesO = idxO + numGlyphs * 2;
        var extraNames = new List<string>();
        int q = namesO;
        while (q < p.Off + p.Len && q < f.Length)
        {
            int len = f[q++];
            if (q + len > f.Length)
            {
                break;
            }
            extraNames.Add(System.Text.Encoding.Latin1.GetString(f, q, len));
            q += len;
        }

        var map = new Dictionary<string, int>();
        for (int g = 0; g < numGlyphs; g++)
        {
            int idx = U16(f, idxO + g * 2);
            string? name = idx < 258 ? (idx < MacGlyphNames.Length ? MacGlyphNames[idx] : null)
                : (idx - 258 < extraNames.Count ? extraNames[idx - 258] : null);
            if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name!))
            {
                map[name!] = g;
            }
        }
        return map.Count > 0 ? map : null;
    }

    private static int U16(byte[] d, int o) => (d[o] << 8) | d[o + 1];
    private static uint U32(byte[] d, int o) => ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];

    // The 258 standard Macintosh glyph ordering (for post format 2 indices < 258).
    private static string[] BuildMacGlyphNames()
    {
        return
        [
            ".notdef", ".null", "nonmarkingreturn", "space", "exclam", "quotedbl", "numbersign", "dollar",
            "percent", "ampersand", "quotesingle", "parenleft", "parenright", "asterisk", "plus", "comma",
            "hyphen", "period", "slash", "zero", "one", "two", "three", "four", "five", "six", "seven",
            "eight", "nine", "colon", "semicolon", "less", "equal", "greater", "question", "at",
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S",
            "T", "U", "V", "W", "X", "Y", "Z", "bracketleft", "backslash", "bracketright", "asciicircum",
            "underscore", "grave", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n",
            "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "braceleft", "bar", "braceright",
            "asciitilde", "Adieresis", "Aring", "Ccedilla", "Eacute", "Ntilde", "Odieresis", "Udieresis",
            "aacute", "agrave", "acircumflex", "adieresis", "atilde", "aring", "ccedilla", "eacute",
            "egrave", "ecircumflex", "edieresis", "iacute", "igrave", "icircumflex", "idieresis", "ntilde",
            "oacute", "ograve", "ocircumflex", "odieresis", "otilde", "uacute", "ugrave", "ucircumflex",
            "udieresis", "dagger", "degree", "cent", "sterling", "section", "bullet", "paragraph",
            "germandbls", "registered", "copyright", "trademark", "acute", "dieresis", "notequal", "AE",
            "Oslash", "infinity", "plusminus", "lessequal", "greaterequal", "yen", "mu", "partialdiff",
            "summation", "product", "pi", "integral", "ordfeminine", "ordmasculine", "Omega", "ae",
            "oslash", "questiondown", "exclamdown", "logicalnot", "radical", "florin", "approxequal",
            "Delta", "guillemotleft", "guillemotright", "ellipsis", "nonbreakingspace", "Agrave", "Atilde",
            "Otilde", "OE", "oe", "endash", "emdash", "quotedblleft", "quotedblright", "quoteleft",
            "quoteright", "divide", "lozenge", "ydieresis", "Ydieresis", "fraction", "currency",
            "guilsinglleft", "guilsinglright", "fi", "fl", "daggerdbl", "periodcentered", "quotesinglbase",
            "quotedblbase", "perthousand", "Acircumflex", "Ecircumflex", "Aacute", "Edieresis", "Egrave",
            "Iacute", "Icircumflex", "Idieresis", "Igrave", "Oacute", "Ocircumflex", "apple", "Ograve",
            "Uacute", "Ucircumflex", "Ugrave", "dotlessi", "circumflex", "tilde", "macron", "breve",
            "dotaccent", "ring", "cedilla", "hungarumlaut", "ogonek", "caron", "Lslash", "lslash",
            "Scaron", "scaron", "Zcaron", "zcaron", "brokenbar", "Eth", "eth", "Yacute", "yacute", "Thorn",
            "thorn", "minus", "multiply", "onesuperior", "twosuperior", "threesuperior", "onehalf",
            "onequarter", "threequarters", "franc", "Gbreve", "gbreve", "Idotaccent", "Scedilla",
            "scedilla", "Cacute", "cacute", "Ccaron", "ccaron", "dcroat",
        ];
    }
}
