// Standard PDF base encodings (code -> glyph name) per PDF 32000-1:2008 Annex D.
// Used to resolve simple-font character codes to glyph names (and thence to
// Unicode) when a font supplies a base encoding and/or a /Differences array.

namespace Bit.BlazorUI;

/// <summary>
/// The named base encodings used by simple fonts. Each array maps a byte code
/// (0..255) to a PostScript glyph name; empty entries are <c>.notdef</c>.
/// </summary>
internal static class BitPdfEncodings
{
    /// <summary>Returns the base encoding table for a named encoding, or <c>null</c>.</summary>
    public static string[]? ByName(string? name) => name switch
    {
        "StandardEncoding" => Standard,
        "WinAnsiEncoding" => WinAnsi,
        "MacRomanEncoding" => MacRoman,
        "PDFDocEncoding" => WinAnsi, // close enough for text extraction
        "Symbol" or "SymbolEncoding" or "SymbolSetEncoding" => Symbol,
        "ZapfDingbats" or "ZapfDingbatsEncoding" => ZapfDingbats,
        _ => null,
    };

    // Glyph name for the printable ASCII range 32..126, shared by all Latin
    // base encodings (with a couple of position-specific overrides applied below).
    private static readonly string[] Ascii =
    [
        "space", "exclam", "quotedbl", "numbersign", "dollar", "percent", "ampersand", "quotesingle",
        "parenleft", "parenright", "asterisk", "plus", "comma", "hyphen", "period", "slash",
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "colon", "semicolon", "less", "equal", "greater", "question", "at",
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
        "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
        "bracketleft", "backslash", "bracketright", "asciicircum", "underscore", "grave",
        "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
        "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
        "braceleft", "bar", "braceright", "asciitilde",
    ];

    private static string[] NewLatinBase()
    {
        var enc = new string[256];
        for (int i = 0; i < 256; i++)
        {
            enc[i] = "";
        }
        for (int i = 0; i < Ascii.Length; i++)
        {
            enc[32 + i] = Ascii[i];
        }
        return enc;
    }

    public static readonly string[] Standard = BuildStandard();
    public static readonly string[] WinAnsi = BuildWinAnsi();
    public static readonly string[] MacRoman = BuildMacRoman();
    public static readonly string[] Symbol = BuildSymbol();
    public static readonly string[] ZapfDingbats = BuildZapfDingbats();

    private static string[] BuildStandard()
    {
        var e = NewLatinBase();
        // StandardEncoding differs from ASCII in the quote glyphs.
        e[0x27] = "quoteright";
        e[0x60] = "quoteleft";
        // High range (PDF Annex D.2; codes shown in hexadecimal).
        e[0xA1] = "exclamdown"; e[0xA2] = "cent"; e[0xA3] = "sterling"; e[0xA4] = "fraction";
        e[0xA5] = "yen"; e[0xA6] = "florin"; e[0xA7] = "section"; e[0xA8] = "currency";
        e[0xA9] = "quotesingle"; e[0xAA] = "quotedblleft"; e[0xAB] = "guillemotleft";
        e[0xAC] = "guilsinglleft"; e[0xAD] = "guilsinglright"; e[0xAE] = "fi"; e[0xAF] = "fl";
        e[0xB1] = "endash"; e[0xB2] = "dagger"; e[0xB3] = "daggerdbl"; e[0xB4] = "periodcentered";
        e[0xB6] = "paragraph"; e[0xB7] = "bullet"; e[0xB8] = "quotesinglbase";
        e[0xB9] = "quotedblbase"; e[0xBA] = "quotedblright"; e[0xBB] = "guillemotright";
        e[0xBC] = "ellipsis"; e[0xBD] = "perthousand"; e[0xBF] = "questiondown";
        e[0xC1] = "grave"; e[0xC2] = "acute"; e[0xC3] = "circumflex"; e[0xC4] = "tilde";
        e[0xC5] = "macron"; e[0xC6] = "breve"; e[0xC7] = "dotaccent"; e[0xC8] = "dieresis";
        e[0xCA] = "ring"; e[0xCB] = "cedilla"; e[0xCD] = "hungarumlaut"; e[0xCE] = "ogonek";
        e[0xCF] = "caron"; e[0xD0] = "emdash"; e[0xE1] = "AE"; e[0xE3] = "ordfeminine";
        e[0xE8] = "Lslash"; e[0xE9] = "Oslash"; e[0xEA] = "OE"; e[0xEB] = "ordmasculine";
        e[0xF1] = "ae"; e[0xF5] = "dotlessi"; e[0xF8] = "lslash"; e[0xF9] = "oslash";
        e[0xFA] = "oe"; e[0xFB] = "germandbls";
        return e;
    }

    private static string[] BuildWinAnsi()
    {
        var e = NewLatinBase();
        e[0x80] = "Euro"; e[0x82] = "quotesinglbase"; e[0x83] = "florin"; e[0x84] = "quotedblbase";
        e[0x85] = "ellipsis"; e[0x86] = "dagger"; e[0x87] = "daggerdbl"; e[0x88] = "circumflex";
        e[0x89] = "perthousand"; e[0x8A] = "Scaron"; e[0x8B] = "guilsinglleft"; e[0x8C] = "OE";
        e[0x8E] = "Zcaron"; e[0x91] = "quoteleft"; e[0x92] = "quoteright"; e[0x93] = "quotedblleft";
        e[0x94] = "quotedblright"; e[0x95] = "bullet"; e[0x96] = "endash"; e[0x97] = "emdash";
        e[0x98] = "tilde"; e[0x99] = "trademark"; e[0x9A] = "scaron"; e[0x9B] = "guilsinglright";
        e[0x9C] = "oe"; e[0x9E] = "zcaron"; e[0x9F] = "Ydieresis"; e[0xA0] = "space";
        e[0xA1] = "exclamdown"; e[0xA2] = "cent"; e[0xA3] = "sterling"; e[0xA4] = "currency";
        e[0xA5] = "yen"; e[0xA6] = "brokenbar"; e[0xA7] = "section"; e[0xA8] = "dieresis";
        e[0xA9] = "copyright"; e[0xAA] = "ordfeminine"; e[0xAB] = "guillemotleft";
        e[0xAC] = "logicalnot"; e[0xAD] = "hyphen"; e[0xAE] = "registered"; e[0xAF] = "macron";
        e[0xB0] = "degree"; e[0xB1] = "plusminus"; e[0xB2] = "twosuperior"; e[0xB3] = "threesuperior";
        e[0xB4] = "acute"; e[0xB5] = "mu"; e[0xB6] = "paragraph"; e[0xB7] = "periodcentered";
        e[0xB8] = "cedilla"; e[0xB9] = "onesuperior"; e[0xBA] = "ordmasculine";
        e[0xBB] = "guillemotright"; e[0xBC] = "onequarter"; e[0xBD] = "onehalf";
        e[0xBE] = "threequarters"; e[0xBF] = "questiondown"; e[0xC0] = "Agrave"; e[0xC1] = "Aacute";
        e[0xC2] = "Acircumflex"; e[0xC3] = "Atilde"; e[0xC4] = "Adieresis"; e[0xC5] = "Aring";
        e[0xC6] = "AE"; e[0xC7] = "Ccedilla"; e[0xC8] = "Egrave"; e[0xC9] = "Eacute";
        e[0xCA] = "Ecircumflex"; e[0xCB] = "Edieresis"; e[0xCC] = "Igrave"; e[0xCD] = "Iacute";
        e[0xCE] = "Icircumflex"; e[0xCF] = "Idieresis"; e[0xD0] = "Eth"; e[0xD1] = "Ntilde";
        e[0xD2] = "Ograve"; e[0xD3] = "Oacute"; e[0xD4] = "Ocircumflex"; e[0xD5] = "Otilde";
        e[0xD6] = "Odieresis"; e[0xD7] = "multiply"; e[0xD8] = "Oslash"; e[0xD9] = "Ugrave";
        e[0xDA] = "Uacute"; e[0xDB] = "Ucircumflex"; e[0xDC] = "Udieresis"; e[0xDD] = "Yacute";
        e[0xDE] = "Thorn"; e[0xDF] = "germandbls"; e[0xE0] = "agrave"; e[0xE1] = "aacute";
        e[0xE2] = "acircumflex"; e[0xE3] = "atilde"; e[0xE4] = "adieresis"; e[0xE5] = "aring";
        e[0xE6] = "ae"; e[0xE7] = "ccedilla"; e[0xE8] = "egrave"; e[0xE9] = "eacute";
        e[0xEA] = "ecircumflex"; e[0xEB] = "edieresis"; e[0xEC] = "igrave"; e[0xED] = "iacute";
        e[0xEE] = "icircumflex"; e[0xEF] = "idieresis"; e[0xF0] = "eth"; e[0xF1] = "ntilde";
        e[0xF2] = "ograve"; e[0xF3] = "oacute"; e[0xF4] = "ocircumflex"; e[0xF5] = "otilde";
        e[0xF6] = "odieresis"; e[0xF7] = "divide"; e[0xF8] = "oslash"; e[0xF9] = "ugrave";
        e[0xFA] = "uacute"; e[0xFB] = "ucircumflex"; e[0xFC] = "udieresis"; e[0xFD] = "yacute";
        e[0xFE] = "thorn"; e[0xFF] = "ydieresis";
        return e;
    }

    private static string[] BuildMacRoman()
    {
        var e = NewLatinBase();
        e[0x80] = "Adieresis"; e[0x81] = "Aring"; e[0x82] = "Ccedilla"; e[0x83] = "Eacute";
        e[0x84] = "Ntilde"; e[0x85] = "Odieresis"; e[0x86] = "Udieresis"; e[0x87] = "aacute";
        e[0x88] = "agrave"; e[0x89] = "acircumflex"; e[0x8A] = "adieresis"; e[0x8B] = "atilde";
        e[0x8C] = "aring"; e[0x8D] = "ccedilla"; e[0x8E] = "eacute"; e[0x8F] = "egrave";
        e[0x90] = "ecircumflex"; e[0x91] = "edieresis"; e[0x92] = "iacute"; e[0x93] = "igrave";
        e[0x94] = "icircumflex"; e[0x95] = "idieresis"; e[0x96] = "ntilde"; e[0x97] = "oacute";
        e[0x98] = "ograve"; e[0x99] = "ocircumflex"; e[0x9A] = "odieresis"; e[0x9B] = "otilde";
        e[0x9C] = "uacute"; e[0x9D] = "ugrave"; e[0x9E] = "ucircumflex"; e[0x9F] = "udieresis";
        e[0xA0] = "dagger"; e[0xA1] = "degree"; e[0xA2] = "cent"; e[0xA3] = "sterling";
        e[0xA4] = "section"; e[0xA5] = "bullet"; e[0xA6] = "paragraph"; e[0xA7] = "germandbls";
        e[0xA8] = "registered"; e[0xA9] = "copyright"; e[0xAA] = "trademark"; e[0xAB] = "acute";
        e[0xAC] = "dieresis"; e[0xAD] = "notequal"; e[0xAE] = "AE"; e[0xAF] = "Oslash";
        e[0xB0] = "infinity"; e[0xB1] = "plusminus"; e[0xB2] = "lessequal"; e[0xB3] = "greaterequal";
        e[0xB4] = "yen"; e[0xB5] = "mu"; e[0xB6] = "partialdiff"; e[0xB7] = "summation";
        e[0xB8] = "product"; e[0xB9] = "pi"; e[0xBA] = "integral"; e[0xBB] = "ordfeminine";
        e[0xBC] = "ordmasculine"; e[0xBD] = "Omega"; e[0xBE] = "ae"; e[0xBF] = "oslash";
        e[0xC0] = "questiondown"; e[0xC1] = "exclamdown"; e[0xC2] = "logicalnot"; e[0xC3] = "radical";
        e[0xC4] = "florin"; e[0xC5] = "approxequal"; e[0xC6] = "Delta"; e[0xC7] = "guillemotleft";
        e[0xC8] = "guillemotright"; e[0xC9] = "ellipsis"; e[0xCA] = "space"; e[0xCB] = "Agrave";
        e[0xCC] = "Atilde"; e[0xCD] = "Otilde"; e[0xCE] = "OE"; e[0xCF] = "oe"; e[0xD0] = "endash";
        e[0xD1] = "emdash"; e[0xD2] = "quotedblleft"; e[0xD3] = "quotedblright"; e[0xD4] = "quoteleft";
        e[0xD5] = "quoteright"; e[0xD6] = "divide"; e[0xD7] = "lozenge"; e[0xD8] = "ydieresis";
        e[0xD9] = "Ydieresis"; e[0xDA] = "fraction"; e[0xDB] = "currency"; e[0xDC] = "guilsinglleft";
        e[0xDD] = "guilsinglright"; e[0xDE] = "fi"; e[0xDF] = "fl"; e[0xE0] = "daggerdbl";
        e[0xE1] = "periodcentered"; e[0xE2] = "quotesinglbase"; e[0xE3] = "quotedblbase";
        e[0xE4] = "perthousand"; e[0xE5] = "Acircumflex"; e[0xE6] = "Ecircumflex"; e[0xE7] = "Aacute";
        e[0xE8] = "Edieresis"; e[0xE9] = "Egrave"; e[0xEA] = "Iacute"; e[0xEB] = "Icircumflex";
        e[0xEC] = "Idieresis"; e[0xED] = "Igrave"; e[0xEE] = "Oacute"; e[0xEF] = "Ocircumflex";
        e[0xF1] = "Ograve"; e[0xF2] = "Uacute"; e[0xF3] = "Ucircumflex"; e[0xF4] = "Ugrave";
        e[0xF0] = "apple"; // MacRoman 0xF0 is the Apple logo glyph.
        e[0xF5] = "dotlessi"; e[0xF6] = "circumflex"; e[0xF7] = "tilde"; e[0xF8] = "macron";
        e[0xF9] = "breve"; e[0xFA] = "dotaccent"; e[0xFB] = "ring"; e[0xFC] = "cedilla";
        e[0xFD] = "hungarumlaut"; e[0xFE] = "ogonek"; e[0xFF] = "caron";
        return e;
    }

    // Adobe Symbol font built-in encoding (PDF 32000-1:2008 Annex D.5).
    // Greek letters and math signs.
    private static string[] BuildSymbol()
    {
        var e = new string[256];
        for (int i = 0; i < 256; i++)
        {
            e[i] = "";
        }
        string[] low =
        [
            /*32*/ "space", "exclam", "universal", "numbersign", "existential", "percent",
            "ampersand", "suchthat", "parenleft", "parenright", "asteriskmath", "plus",
            "comma", "minus", "period", "slash", "zero", "one", "two", "three", "four",
            "five", "six", "seven", "eight", "nine", "colon", "semicolon", "less", "equal",
            "greater", "question", "congruent", "Alpha", "Beta", "Chi", "Delta", "Epsilon",
            "Phi", "Gamma", "Eta", "Iota", "theta1", "Kappa", "Lambda", "Mu", "Nu", "Omicron",
            "Pi", "Theta", "Rho", "Sigma", "Tau", "Upsilon", "sigma1", "Omega", "Xi", "Psi",
            "Zeta", "bracketleft", "therefore", "bracketright", "perpendicular", "underscore",
            "radicalex", "alpha", "beta", "chi", "delta", "epsilon", "phi", "gamma", "eta",
            "iota", "phi1", "kappa", "lambda", "mu", "nu", "omicron", "pi", "theta", "rho",
            "sigma", "tau", "upsilon", "omega1", "omega", "xi", "psi", "zeta", "braceleft",
            "bar", "braceright", "similar",
        ];
        for (int i = 0; i < low.Length; i++)
        {
            e[32 + i] = low[i];
        }
        string[] high =
        [
            /*161*/ "Upsilon1", "minute", "lessequal", "fraction", "infinity", "florin",
            "club", "diamond", "heart", "spade", "arrowboth", "arrowleft", "arrowup",
            "arrowright", "arrowdown", "degree", "plusminus", "second", "greaterequal",
            "multiply", "proportional", "partialdiff", "bullet", "divide", "notequal",
            "equivalence", "approxequal", "ellipsis", "arrowvertex", "arrowhorizex",
            "carriagereturn", "aleph", "Ifraktur", "Rfraktur", "weierstrass",
            "circlemultiply", "circleplus", "emptyset", "intersection", "union",
            "propersuperset", "reflexsuperset", "notsubset", "propersubset", "reflexsubset",
            "element", "notelement", "angle", "gradient", "registerserif", "copyrightserif",
            "trademarkserif", "product", "radical", "dotmath", "logicalnot", "logicaland",
            "logicalor", "arrowdblboth", "arrowdblleft", "arrowdblup", "arrowdblright",
            "arrowdbldown", "lozenge", "angleleft", "registersans", "copyrightsans",
            "trademarksans", "summation", "parenlefttp", "parenleftex", "parenleftbt",
            "bracketlefttp", "bracketleftex", "bracketleftbt", "bracelefttp", "braceleftmid",
            "braceleftbt", "barex",
        ];
        for (int i = 0; i < high.Length; i++)
        {
            e[161 + i] = high[i];
        }
        e[160] = "Euro"; // Adobe added the Euro glyph at 0xA0.
        // 240 is unused; 241..254 continue the delimiter-extension glyphs.
        string[] tail =
        [
            /*241*/ "angleright", "integral", "integraltp", "integralex", "integralbt",
            "parenrighttp", "parenrightex", "parenrightbt", "bracketrighttp",
            "bracketrightex", "bracketrightbt", "bracerighttp", "bracerightmid",
            "bracerightbt",
        ];
        for (int i = 0; i < tail.Length; i++)
        {
            e[241 + i] = tail[i];
        }
        return e;
    }

    // Adobe ZapfDingbats built-in encoding (PDF 32000-1:2008 Annex D.6).
    private static string[] BuildZapfDingbats()
    {
        var e = new string[256];
        for (int i = 0; i < 256; i++)
        {
            e[i] = "";
        }
        string[] low =
        [
            /*32*/ "space", "a1", "a2", "a202", "a3", "a4", "a5", "a119", "a118", "a117",
            "a11", "a12", "a13", "a14", "a15", "a16", "a105", "a17", "a18", "a19", "a20",
            "a21", "a22", "a23", "a24", "a25", "a26", "a27", "a28", "a6", "a7", "a8", "a9",
            "a10", "a29", "a30", "a31", "a32", "a33", "a34", "a35", "a36", "a37", "a38",
            "a39", "a40", "a41", "a42", "a43", "a44", "a45", "a46", "a47", "a48", "a49",
            "a50", "a51", "a52", "a53", "a54", "a55", "a56", "a57", "a58", "a59", "a60",
            "a61", "a62", "a63", "a64", "a65", "a66", "a67", "a68", "a69", "a70", "a71",
            "a72", "a73", "a74", "a203", "a75", "a204", "a76", "a77", "a78", "a79", "a81",
            "a82", "a83", "a84", "a97", "a98", "a99", "a100",
        ];
        for (int i = 0; i < low.Length; i++)
        {
            e[32 + i] = low[i];
        }
        string[] high =
        [
            /*161*/ "a101", "a102", "a103", "a104", "a106", "a107", "a108", "a112", "a111",
            "a110", "a109", "a120", "a121", "a122", "a123", "a124", "a125", "a126", "a127",
            "a128", "a129", "a130", "a131", "a132", "a133", "a134", "a135", "a136", "a137",
            "a138", "a139", "a140", "a141", "a142", "a143", "a144", "a145", "a146", "a147",
            "a148", "a149", "a150", "a151", "a152", "a153", "a154", "a155", "a156", "a157",
            "a158", "a159", "a160", "a161", "a163", "a164", "a196", "a165", "a192", "a166",
            "a167", "a168", "a169", "a170", "a171", "a172", "a173", "a162", "a174", "a175",
            "a176", "a177", "a178", "a179", "a193", "a180", "a199", "a181", "a200", "a182",
        ];
        for (int i = 0; i < high.Length; i++)
        {
            e[161 + i] = high[i];
        }
        string[] tail =
        [
            /*241*/ "a201", "a183", "a184", "a197", "a185", "a194", "a198", "a186", "a195",
            "a187", "a188", "a189", "a190", "a191",
        ];
        for (int i = 0; i < tail.Length; i++)
        {
            e[241 + i] = tail[i];
        }
        return e;
    }
}
