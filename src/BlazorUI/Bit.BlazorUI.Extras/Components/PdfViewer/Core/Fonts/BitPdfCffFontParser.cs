// Minimal CFF (Compact Font Format) parser: recovers the glyph count and the
// glyph-id → glyph-name mapping (via the charset + String INDEX + the 391 CFF
// standard strings). Enough to wrap a bare CFF (/FontFile3 /Type1C) in an
// OpenType sfnt with a synthetic Unicode cmap, without interpreting charstrings.

namespace Bit.BlazorUI;

internal sealed class BitPdfCffFontParser
{
    public int NumGlyphs { get; private set; }

    /// <summary>The CFF FontMatrix (defaults to 1/1000-em).</summary>
    public double[] FontMatrix { get; private set; } = [0.001, 0, 0, 0.001, 0, 0];

    /// <summary>Glyph name → glyph id, built from the charset.</summary>
    public Dictionary<string, int> NameToGid { get; } = new();

    /// <summary>The font's built-in Encoding: character code → glyph id (custom
    /// encodings only; empty for the predefined Standard/Expert encodings).</summary>
    public Dictionary<int, int> CodeToGid { get; } = new();

    /// <summary>For a CID-keyed CFF: CID → glyph id (from the charset).</summary>
    public Dictionary<int, int> CidToGid { get; } = new();

    /// <summary>The raw CFF Encoding offset from the Top DICT (0/1 = predefined).</summary>
    public int EncodingOffset { get; private set; }

    /// <summary><c>true</c> for a CID-keyed CFF (ROS present) — unsupported here.</summary>
    public bool IsCid { get; private set; }

    public static BitPdfCffFontParser? Parse(byte[] cff)
    {
        try
        {
            var p = new BitPdfCffFontParser();
            return p.ParseCore(cff) ? p : null;
        }
        catch
        {
            return null;
        }
    }

    private bool ParseCore(byte[] d)
    {
        if (d.Length < 4)
        {
            return false;
        }
        int hdrSize = d[2];
        int pos = hdrSize;

        pos = SkipIndex(d, pos);                    // Name INDEX
        var top = ReadIndex(d, pos, out pos);       // Top DICT INDEX
        var strings = ReadIndex(d, pos, out pos);   // String INDEX
        pos = SkipIndex(d, pos);                     // Global Subr INDEX (unused)

        if (top.Count == 0)
        {
            return false;
        }
        var dict = ParseDict(d, top.Start(0), top.End(0));
        IsCid = dict.ContainsKey(0xC1E); // 12 30 = ROS marks a CID-keyed CFF

        if (dict.TryGetValue(0xC07, out var fm) && fm.Count >= 6) // 12 7 = FontMatrix
        {
            FontMatrix = fm.ToArray();
        }
        int charStringsOff = dict.TryGetValue(17, out var cs) && cs.Count > 0 ? (int)cs[0] : -1;
        int charsetOff = dict.TryGetValue(15, out var ch) && ch.Count > 0 ? (int)ch[0] : 0;
        int encodingOff = dict.TryGetValue(16, out var en) && en.Count > 0 ? (int)en[0] : 0;
        EncodingOffset = encodingOff;
        if (charStringsOff < 0)
        {
            return false;
        }

        var charStrings = ReadIndex(d, charStringsOff, out _);
        NumGlyphs = charStrings.Count;
        if (NumGlyphs == 0)
        {
            return false;
        }

        string SidName(int sid) => sid < StdStrings.Length
            ? StdStrings[sid]
            : (sid - StdStrings.Length < strings.Count
                ? System.Text.Encoding.Latin1.GetString(d, strings.Start(sid - StdStrings.Length), strings.Len(sid - StdStrings.Length))
                : "");

        // Charset: gid → SID (name-keyed) or gid → CID (CID-keyed). Predefined
        // offsets 0/1/2 map identity (SID/CID == gid).
        var gidToId = new int[NumGlyphs];
        if (charsetOff is 0 or 1 or 2)
        {
            for (int g = 0; g < NumGlyphs; g++)
            {
                gidToId[g] = g;
            }
        }
        else
        {
            ReadCharset(d, charsetOff, gidToId);
        }

        if (IsCid)
        {
            // CID-keyed: the charset entries are CIDs; invert to CID -> glyph id.
            for (int g = 0; g < NumGlyphs; g++)
            {
                CidToGid.TryAdd(gidToId[g], g);
            }
            return CidToGid.Count > 0;
        }

        for (int g = 0; g < NumGlyphs; g++)
        {
            string name = SidName(gidToId[g]);
            if (name.Length > 0 && !NameToGid.ContainsKey(name))
            {
                NameToGid[name] = g;
            }
        }

        // Built-in Encoding (custom formats only; predefined 0/1 rely on names).
        if (encodingOff > 1)
        {
            ReadEncoding(d, encodingOff, CodeToGid);
        }
        return NameToGid.Count > 0;
    }

    private static void ReadEncoding(byte[] d, int off, Dictionary<int, int> codeToGid)
    {
        if (off < 0 || off >= d.Length)
        {
            return;
        }
        int format = d[off];
        int p = off + 1;
        switch (format & 0x7F)
        {
            case 0:
            {
                int nCodes = d[p++];
                for (int i = 1; i <= nCodes && p < d.Length; i++)
                {
                    int code = d[p++];
                    codeToGid.TryAdd(code, i); // gid i for the i-th code
                }
                break;
            }
            case 1:
            {
                int nRanges = d[p++];
                int gid = 1;
                for (int r = 0; r < nRanges && p + 1 < d.Length; r++)
                {
                    int first = d[p++];
                    int nLeft = d[p++];
                    for (int i = 0; i <= nLeft; i++)
                    {
                        int code = first + i;
                        if (code <= 255)
                        {
                            codeToGid.TryAdd(code, gid);
                        }
                        gid++;
                    }
                }
                break;
            }
        }
    }

    private static void ReadCharset(byte[] d, int off, int[] gidToSid)
    {
        int n = gidToSid.Length;
        gidToSid[0] = 0; // .notdef
        int format = d[off];
        int p = off + 1;
        int gid = 1;
        switch (format)
        {
            case 0:
                for (; gid < n && p + 1 < d.Length; gid++)
                {
                    gidToSid[gid] = (d[p] << 8) | d[p + 1];
                    p += 2;
                }
                break;
            case 1:
                while (gid < n && p + 2 < d.Length)
                {
                    int first = (d[p] << 8) | d[p + 1];
                    int nLeft = d[p + 2];
                    p += 3;
                    for (int i = 0; i <= nLeft && gid < n; i++)
                    {
                        gidToSid[gid++] = first + i;
                    }
                }
                break;
            case 2:
                while (gid < n && p + 3 < d.Length)
                {
                    int first = (d[p] << 8) | d[p + 1];
                    int nLeft = (d[p + 2] << 8) | d[p + 3];
                    p += 4;
                    for (int i = 0; i <= nLeft && gid < n; i++)
                    {
                        gidToSid[gid++] = first + i;
                    }
                }
                break;
        }
    }

    // ----- CFF primitives -----

    private readonly struct Index
    {
        public int Count { get; init; }
        public int[] Offsets { get; init; } // Count+1 entries, relative to DataBase
        public int DataBase { get; init; }
        public int Start(int i) => DataBase + Offsets[i];
        public int End(int i) => DataBase + Offsets[i + 1];
        public int Len(int i) => Offsets[i + 1] - Offsets[i];
    }

    private static Index ReadIndex(byte[] d, int pos, out int next)
    {
        int count = (d[pos] << 8) | d[pos + 1];
        if (count == 0)
        {
            next = pos + 2;
            return new Index { Count = 0, Offsets = Array.Empty<int>(), DataBase = pos + 2 };
        }
        int offSize = d[pos + 2];
        int offArr = pos + 3;
        var offsets = new int[count + 1];
        for (int i = 0; i <= count; i++)
        {
            int o = 0;
            for (int k = 0; k < offSize; k++)
            {
                o = (o << 8) | d[offArr + i * offSize + k];
            }
            offsets[i] = o;
        }
        int dataBase = offArr + (count + 1) * offSize - 1; // offsets are 1-based
        next = dataBase + offsets[count];
        return new Index { Count = count, Offsets = offsets, DataBase = dataBase };
    }

    private static int SkipIndex(byte[] d, int pos)
    {
        ReadIndex(d, pos, out int next);
        return next;
    }

    // Parses a CFF DICT into operator → operands. Two-byte operators (12 x) are
    // keyed as 0xC00 | x.
    private static Dictionary<int, List<double>> ParseDict(byte[] d, int start, int end)
    {
        var dict = new Dictionary<int, List<double>>();
        var operands = new List<double>();
        int p = start;
        while (p < end)
        {
            int b0 = d[p];
            if (b0 <= 21) // operator
            {
                int op = b0;
                p++;
                if (b0 == 12)
                {
                    op = 0xC00 | d[p];
                    p++;
                }
                dict[op] = new List<double>(operands);
                operands.Clear();
            }
            else if (b0 == 28)
            {
                operands.Add((short)((d[p + 1] << 8) | d[p + 2]));
                p += 3;
            }
            else if (b0 == 29)
            {
                operands.Add((d[p + 1] << 24) | (d[p + 2] << 16) | (d[p + 3] << 8) | d[p + 4]);
                p += 5;
            }
            else if (b0 == 30) // real
            {
                p++;
                var sb = new System.Text.StringBuilder();
                bool doneReal = false;
                while (p < end && !doneReal)
                {
                    int by = d[p++];
                    foreach (int nib in new[] { by >> 4, by & 0xF })
                    {
                        if (nib <= 9) sb.Append((char)('0' + nib));
                        else if (nib == 0xa) sb.Append('.');
                        else if (nib == 0xb) sb.Append('E');
                        else if (nib == 0xc) sb.Append("E-");
                        else if (nib == 0xe) sb.Append('-');
                        else if (nib == 0xf) { doneReal = true; break; }
                    }
                }
                double.TryParse(sb.ToString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double rv);
                operands.Add(rv);
            }
            else if (b0 is >= 32 and <= 246)
            {
                operands.Add(b0 - 139);
                p++;
            }
            else if (b0 is >= 247 and <= 250)
            {
                operands.Add((b0 - 247) * 256 + d[p + 1] + 108);
                p += 2;
            }
            else if (b0 is >= 251 and <= 254)
            {
                operands.Add(-(b0 - 251) * 256 - d[p + 1] - 108);
                p += 2;
            }
            else
            {
                p++;
            }
        }
        return dict;
    }

    // The 391 CFF standard strings (CFF spec Appendix A).
    private static readonly string[] StdStrings =
    {
        ".notdef", "space", "exclam", "quotedbl", "numbersign", "dollar", "percent", "ampersand", "quoteright",
        "parenleft", "parenright", "asterisk", "plus", "comma", "hyphen", "period", "slash", "zero", "one",
        "two", "three", "four", "five", "six", "seven", "eight", "nine", "colon", "semicolon", "less", "equal",
        "greater", "question", "at", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O",
        "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "bracketleft", "backslash", "bracketright",
        "asciicircum", "underscore", "quoteleft", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l",
        "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "braceleft", "bar", "braceright",
        "asciitilde", "exclamdown", "cent", "sterling", "fraction", "yen", "florin", "section", "currency",
        "quotesingle", "quotedblleft", "guillemotleft", "guilsinglleft", "guilsinglright", "fi", "fl", "endash",
        "dagger", "daggerdbl", "periodcentered", "paragraph", "bullet", "quotesinglbase", "quotedblbase",
        "quotedblright", "guillemotright", "ellipsis", "perthousand", "questiondown", "grave", "acute",
        "circumflex", "tilde", "macron", "breve", "dotaccent", "dieresis", "ring", "cedilla", "hungarumlaut",
        "ogonek", "caron", "emdash", "AE", "ordfeminine", "Lslash", "Oslash", "OE", "ordmasculine", "ae",
        "dotlessi", "lslash", "oslash", "oe", "germandbls", "onesuperior", "logicalnot", "mu", "trademark",
        "Eth", "onehalf", "plusminus", "Thorn", "onequarter", "divide", "brokenbar", "degree", "thorn",
        "threequarters", "twosuperior", "registered", "minus", "eth", "multiply", "threesuperior", "copyright",
        "Aacute", "Acircumflex", "Adieresis", "Agrave", "Aring", "Atilde", "Ccedilla", "Eacute", "Ecircumflex",
        "Edieresis", "Egrave", "Iacute", "Icircumflex", "Idieresis", "Igrave", "Ntilde", "Oacute", "Ocircumflex",
        "Odieresis", "Ograve", "Otilde", "Scaron", "Uacute", "Ucircumflex", "Udieresis", "Ugrave", "Yacute",
        "Ydieresis", "Zcaron", "aacute", "acircumflex", "adieresis", "agrave", "aring", "atilde", "ccedilla",
        "eacute", "ecircumflex", "edieresis", "egrave", "iacute", "icircumflex", "idieresis", "igrave", "ntilde",
        "oacute", "ocircumflex", "odieresis", "ograve", "otilde", "scaron", "uacute", "ucircumflex", "udieresis",
        "ugrave", "yacute", "ydieresis", "zcaron", "exclamsmall", "Hungarumlautsmall", "dollaroldstyle",
        "dollarsuperior", "ampersandsmall", "Acutesmall", "parenleftsuperior", "parenrightsuperior",
        "twodotenleader", "onedotenleader", "zerooldstyle", "oneoldstyle", "twooldstyle", "threeoldstyle",
        "fouroldstyle", "fiveoldstyle", "sixoldstyle", "sevenoldstyle", "eightoldstyle", "nineoldstyle",
        "commasuperior", "threequartersemdash", "periodsuperior", "questionsmall", "asuperior", "bsuperior",
        "centsuperior", "dsuperior", "esuperior", "isuperior", "lsuperior", "msuperior", "nsuperior", "osuperior",
        "rsuperior", "ssuperior", "tsuperior", "ff", "ffi", "ffl", "parenleftinferior", "parenrightinferior",
        "Circumflexsmall", "hyphensuperior", "Gravesmall", "Asmall", "Bsmall", "Csmall", "Dsmall", "Esmall",
        "Fsmall", "Gsmall", "Hsmall", "Ismall", "Jsmall", "Ksmall", "Lsmall", "Msmall", "Nsmall", "Osmall",
        "Psmall", "Qsmall", "Rsmall", "Ssmall", "Tsmall", "Usmall", "Vsmall", "Wsmall", "Xsmall", "Ysmall",
        "Zsmall", "colonmonetary", "onefitted", "rupiah", "Tildesmall", "exclamdownsmall", "centoldstyle",
        "Lslashsmall", "Scaronsmall", "Zcaronsmall", "Dieresissmall", "Brevesmall", "Caronsmall",
        "Dotaccentsmall", "Macronsmall", "figuredash", "hypheninferior", "Ogoneksmall", "Ringsmall",
        "Cedillasmall", "questiondownsmall", "oneeighth", "threeeighths", "fiveeighths", "seveneighths",
        "onethird", "twothirds", "zerosuperior", "foursuperior", "fivesuperior", "sixsuperior", "sevensuperior",
        "eightsuperior", "ninesuperior", "zeroinferior", "oneinferior", "twoinferior", "threeinferior",
        "fourinferior", "fiveinferior", "sixinferior", "seveninferior", "eightinferior", "nineinferior",
        "centinferior", "dollarinferior", "periodinferior", "commainferior", "Agravesmall", "Aacutesmall",
        "Acircumflexsmall", "Atildesmall", "Adieresissmall", "Aringsmall", "AEsmall", "Ccedillasmall",
        "Egravesmall", "Eacutesmall", "Ecircumflexsmall", "Edieresissmall", "Igravesmall", "Iacutesmall",
        "Icircumflexsmall", "Idieresissmall", "Ethsmall", "Ntildesmall", "Ogravesmall", "Oacutesmall",
        "Ocircumflexsmall", "Otildesmall", "Odieresissmall", "OEsmall", "Oslashsmall", "Ugravesmall",
        "Uacutesmall", "Ucircumflexsmall", "Udieresissmall", "Yacutesmall", "Thornsmall", "Ydieresissmall",
        "001.000", "001.001", "001.002", "001.003", "Black", "Bold", "Book", "Light", "Medium", "Regular",
        "Roman", "Semibold",
    };
}
