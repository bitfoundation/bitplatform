// Glyph-name to Unicode resolution based on the Adobe Glyph List. Covers the
// glyph names referenced by the standard Latin encodings plus the algorithmic
// "uniXXXX"/"uXXXXXX" forms.

using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Maps PostScript glyph names (e.g. "A", "quoteright", "eacute") to their
/// Unicode string. Falls back to the algorithmic Adobe rules for "uniXXXX",
/// "uXXXXXX" and single-character names.
/// </summary>
internal static class BitPdfGlyphList
{
    /// <summary>
    /// Resolves a glyph name to a Unicode string, or <c>string.Empty</c> when
    /// it cannot be mapped.
    /// </summary>
    public static string ToUnicode(string? name)
    {
        if (string.IsNullOrEmpty(name) || name == ".notdef")
        {
            return string.Empty;
        }

        // Strip a trailing ".variant" suffix (e.g. "a.sc", "f_f.alt").
        int dot = name.IndexOf('.');
        if (dot > 0)
        {
            name = name[..dot];
        }

        // A ligature/composite name is a sequence joined by "_".
        if (name.IndexOf('_') >= 0)
        {
            var parts = name.Split('_');
            var sb = new System.Text.StringBuilder();
            foreach (var part in parts)
            {
                sb.Append(ToUnicode(part));
            }
            return sb.ToString();
        }

        if (Map.TryGetValue(name, out string? mapped))
        {
            return mapped;
        }

        // Symbol (Greek/math) and ZapfDingbats glyph names, consulted after the
        // Latin AGL subset so shared names keep their Latin meaning.
        if (SymbolMap.TryGetValue(name, out string? sym))
        {
            return sym;
        }
        if (DingbatsMap.TryGetValue(name, out string? ding))
        {
            return ding;
        }

        // Single ASCII letters are their own glyph names in the Adobe Glyph List
        // (e.g. "A" -> U+0041); without this a /Differences remap to a letter
        // would be lost and fall back to the wrong base-encoding character.
        if (name.Length == 1)
        {
            char ch = name[0];
            if (ch is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))
            {
                return name;
            }
        }

        // "uniXXXX" - one or more 4-hex-digit UTF-16 code units.
        if (name.Length >= 7 && name.StartsWith("uni", StringComparison.Ordinal)
            && (name.Length - 3) % 4 == 0)
        {
            var sb = new System.Text.StringBuilder();
            bool ok = true;
            for (int i = 3; i + 4 <= name.Length; i += 4)
            {
                if (int.TryParse(name.AsSpan(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int cu))
                {
                    sb.Append((char)cu);
                }
                else
                {
                    ok = false;
                    break;
                }
            }
            if (ok)
            {
                return sb.ToString();
            }
        }

        // "uXXXX".."uXXXXXX" - a single code point of 4 to 6 hex digits.
        if (name.Length is >= 5 and <= 7 && name[0] == 'u'
            && int.TryParse(name.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int cp)
            && cp <= 0x10FFFF)
        {
            return char.ConvertFromUtf32(cp);
        }

        // "gXX" / "cidXX" / "indexXX" carry no Unicode meaning.
        return string.Empty;
    }

    // The subset of the Adobe Glyph List covering every name used by the
    // Standard, WinAnsi and MacRoman encodings (PDF 32000-1:2008 Annex D).
    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        ["space"] = " ", ["exclam"] = "!", ["quotedbl"] = "\"", ["numbersign"] = "#",
        ["dollar"] = "$", ["percent"] = "%", ["ampersand"] = "&", ["quotesingle"] = "'",
        ["quoteright"] = "\u2019", ["parenleft"] = "(", ["parenright"] = ")", ["asterisk"] = "*",
        ["plus"] = "+", ["comma"] = ",", ["hyphen"] = "-", ["period"] = ".", ["slash"] = "/",
        ["zero"] = "0", ["one"] = "1", ["two"] = "2", ["three"] = "3", ["four"] = "4",
        ["five"] = "5", ["six"] = "6", ["seven"] = "7", ["eight"] = "8", ["nine"] = "9",
        ["colon"] = ":", ["semicolon"] = ";", ["less"] = "<", ["equal"] = "=", ["greater"] = ">",
        ["question"] = "?", ["at"] = "@",
        ["bracketleft"] = "[", ["backslash"] = "\\", ["bracketright"] = "]",
        ["asciicircum"] = "^", ["underscore"] = "_", ["grave"] = "`", ["quoteleft"] = "\u2018",
        ["braceleft"] = "{", ["bar"] = "|", ["braceright"] = "}", ["asciitilde"] = "~",
        ["exclamdown"] = "\u00A1", ["cent"] = "\u00A2", ["sterling"] = "\u00A3",
        ["fraction"] = "\u2044", ["yen"] = "\u00A5", ["florin"] = "\u0192",
        ["section"] = "\u00A7", ["currency"] = "\u00A4", ["quotedblleft"] = "\u201C",
        ["guillemotleft"] = "\u00AB", ["guilsinglleft"] = "\u2039", ["guilsinglright"] = "\u203A",
        ["fi"] = "fi", ["fl"] = "fl", ["endash"] = "\u2013", ["dagger"] = "\u2020",
        ["daggerdbl"] = "\u2021", ["periodcentered"] = "\u00B7", ["paragraph"] = "\u00B6",
        ["bullet"] = "\u2022", ["quotesinglbase"] = "\u201A", ["quotedblbase"] = "\u201E",
        ["quotedblright"] = "\u201D", ["guillemotright"] = "\u00BB", ["ellipsis"] = "\u2026",
        ["perthousand"] = "\u2030", ["questiondown"] = "\u00BF", ["acute"] = "\u00B4",
        ["circumflex"] = "\u02C6", ["tilde"] = "\u02DC", ["macron"] = "\u00AF",
        ["breve"] = "\u02D8", ["dotaccent"] = "\u02D9", ["dieresis"] = "\u00A8",
        ["ring"] = "\u02DA", ["cedilla"] = "\u00B8", ["hungarumlaut"] = "\u02DD",
        ["ogonek"] = "\u02DB", ["caron"] = "\u02C7", ["emdash"] = "\u2014",
        ["AE"] = "\u00C6", ["ordfeminine"] = "\u00AA", ["Lslash"] = "\u0141",
        ["Oslash"] = "\u00D8", ["OE"] = "\u0152", ["ordmasculine"] = "\u00BA",
        ["ae"] = "\u00E6", ["dotlessi"] = "\u0131", ["lslash"] = "\u0142",
        ["oslash"] = "\u00F8", ["oe"] = "\u0153", ["germandbls"] = "\u00DF",
        ["brokenbar"] = "\u00A6", ["copyright"] = "\u00A9", ["logicalnot"] = "\u00AC",
        ["registered"] = "\u00AE", ["degree"] = "\u00B0", ["plusminus"] = "\u00B1",
        ["twosuperior"] = "\u00B2", ["threesuperior"] = "\u00B3", ["mu"] = "\u00B5",
        ["onesuperior"] = "\u00B9", ["onequarter"] = "\u00BC", ["onehalf"] = "\u00BD",
        ["threequarters"] = "\u00BE", ["trademark"] = "\u2122", ["multiply"] = "\u00D7",
        ["divide"] = "\u00F7", ["Euro"] = "\u20AC", ["euro"] = "\u20AC",
        ["minus"] = "\u2212", ["partialdiff"] = "\u2202", ["infinity"] = "\u221E",
        ["lozenge"] = "\u25CA", ["notequal"] = "\u2260", ["lessequal"] = "\u2264",
        ["greaterequal"] = "\u2265", ["summation"] = "\u2211", ["product"] = "\u220F",
        ["pi"] = "\u03C0", ["integral"] = "\u222B", ["Omega"] = "\u03A9",
        ["radical"] = "\u221A", ["approxequal"] = "\u2248", ["Delta"] = "\u2206",
        // Accented Latin letters (WinAnsi / MacRoman high range).
        ["Agrave"] = "\u00C0", ["Aacute"] = "\u00C1", ["Acircumflex"] = "\u00C2",
        ["Atilde"] = "\u00C3", ["Adieresis"] = "\u00C4", ["Aring"] = "\u00C5",
        ["Ccedilla"] = "\u00C7", ["Egrave"] = "\u00C8", ["Eacute"] = "\u00C9",
        ["Ecircumflex"] = "\u00CA", ["Edieresis"] = "\u00CB", ["Igrave"] = "\u00CC",
        ["Iacute"] = "\u00CD", ["Icircumflex"] = "\u00CE", ["Idieresis"] = "\u00CF",
        ["Eth"] = "\u00D0", ["Ntilde"] = "\u00D1", ["Ograve"] = "\u00D2",
        ["Oacute"] = "\u00D3", ["Ocircumflex"] = "\u00D4", ["Otilde"] = "\u00D5",
        ["Odieresis"] = "\u00D6", ["Ugrave"] = "\u00D9", ["Uacute"] = "\u00DA",
        ["Ucircumflex"] = "\u00DB", ["Udieresis"] = "\u00DC", ["Yacute"] = "\u00DD",
        ["Thorn"] = "\u00DE", ["agrave"] = "\u00E0", ["aacute"] = "\u00E1",
        ["acircumflex"] = "\u00E2", ["atilde"] = "\u00E3", ["adieresis"] = "\u00E4",
        ["aring"] = "\u00E5", ["ccedilla"] = "\u00E7", ["egrave"] = "\u00E8",
        ["eacute"] = "\u00E9", ["ecircumflex"] = "\u00EA", ["edieresis"] = "\u00EB",
        ["igrave"] = "\u00EC", ["iacute"] = "\u00ED", ["icircumflex"] = "\u00EE",
        ["idieresis"] = "\u00EF", ["eth"] = "\u00F0", ["ntilde"] = "\u00F1",
        ["ograve"] = "\u00F2", ["oacute"] = "\u00F3", ["ocircumflex"] = "\u00F4",
        ["otilde"] = "\u00F5", ["odieresis"] = "\u00F6", ["ugrave"] = "\u00F9",
        ["uacute"] = "\u00FA", ["ucircumflex"] = "\u00FB", ["udieresis"] = "\u00FC",
        ["yacute"] = "\u00FD", ["thorn"] = "\u00FE", ["ydieresis"] = "\u00FF",
        ["Scaron"] = "\u0160", ["scaron"] = "\u0161", ["Zcaron"] = "\u017D",
        ["zcaron"] = "\u017E", ["Ydieresis"] = "\u0178",
    };

    // Adobe Symbol glyph names → Unicode (Greek alphabet + mathematical signs).
    private static readonly Dictionary<string, string> SymbolMap = new(StringComparer.Ordinal)
    {
        ["Alpha"] = "\u0391", ["Beta"] = "\u0392", ["Gamma"] = "\u0393", ["Delta"] = "\u0394",
        ["Epsilon"] = "\u0395", ["Zeta"] = "\u0396", ["Eta"] = "\u0397", ["Theta"] = "\u0398",
        ["Iota"] = "\u0399", ["Kappa"] = "\u039A", ["Lambda"] = "\u039B", ["Mu"] = "\u039C",
        ["Nu"] = "\u039D", ["Xi"] = "\u039E", ["Omicron"] = "\u039F", ["Pi"] = "\u03A0",
        ["Rho"] = "\u03A1", ["Sigma"] = "\u03A3", ["Tau"] = "\u03A4", ["Upsilon"] = "\u03A5",
        ["Phi"] = "\u03A6", ["Chi"] = "\u03A7", ["Psi"] = "\u03A8", ["Omega"] = "\u03A9",
        ["alpha"] = "\u03B1", ["beta"] = "\u03B2", ["gamma"] = "\u03B3", ["delta"] = "\u03B4",
        ["epsilon"] = "\u03B5", ["zeta"] = "\u03B6", ["eta"] = "\u03B7", ["theta"] = "\u03B8",
        ["iota"] = "\u03B9", ["kappa"] = "\u03BA", ["lambda"] = "\u03BB", ["nu"] = "\u03BD",
        ["xi"] = "\u03BE", ["omicron"] = "\u03BF", ["rho"] = "\u03C1", ["sigma"] = "\u03C3",
        ["tau"] = "\u03C4", ["upsilon"] = "\u03C5", ["chi"] = "\u03C7", ["psi"] = "\u03C8",
        ["omega"] = "\u03C9", ["theta1"] = "\u03D1", ["phi1"] = "\u03D5", ["sigma1"] = "\u03C2",
        ["omega1"] = "\u03D6", ["Upsilon1"] = "\u03D2", ["phi"] = "\u03C6",
        ["universal"] = "\u2200", ["existential"] = "\u2203", ["suchthat"] = "\u220B",
        ["congruent"] = "\u2245", ["therefore"] = "\u2234", ["perpendicular"] = "\u22A5",
        ["similar"] = "\u223C", ["minute"] = "\u2032", ["second"] = "\u2033",
        ["proportional"] = "\u221D", ["equivalence"] = "\u2261", ["aleph"] = "\u2135",
        ["circlemultiply"] = "\u2297", ["circleplus"] = "\u2295", ["emptyset"] = "\u2205",
        ["intersection"] = "\u2229", ["union"] = "\u222A", ["propersuperset"] = "\u2283",
        ["reflexsuperset"] = "\u2287", ["notsubset"] = "\u2284", ["propersubset"] = "\u2282",
        ["reflexsubset"] = "\u2286", ["element"] = "\u2208", ["notelement"] = "\u2209",
        ["angle"] = "\u2220", ["gradient"] = "\u2207", ["dotmath"] = "\u22C5",
        ["logicaland"] = "\u2227", ["logicalor"] = "\u2228", ["arrowboth"] = "\u2194",
        ["arrowleft"] = "\u2190", ["arrowup"] = "\u2191", ["arrowright"] = "\u2192",
        ["arrowdown"] = "\u2193", ["arrowdblboth"] = "\u21D4", ["arrowdblleft"] = "\u21D0",
        ["arrowdblup"] = "\u21D1", ["arrowdblright"] = "\u21D2", ["arrowdbldown"] = "\u21D3",
        ["weierstrass"] = "\u2118", ["Ifraktur"] = "\u2111", ["Rfraktur"] = "\u211C",
        ["club"] = "\u2663", ["diamond"] = "\u2666", ["heart"] = "\u2665", ["spade"] = "\u2660",
        ["asteriskmath"] = "\u2217", ["radicalex"] = "\u203E", ["carriagereturn"] = "\u21B5",
        ["registerserif"] = "\u00AE", ["copyrightserif"] = "\u00A9", ["trademarkserif"] = "\u2122",
        ["registersans"] = "\u00AE", ["copyrightsans"] = "\u00A9", ["trademarksans"] = "\u2122",
        ["angleleft"] = "\u2329", ["angleright"] = "\u232A",
    };

    // Adobe ZapfDingbats "aNNN" glyph names → Unicode dingbat characters.
    private static readonly Dictionary<string, string> DingbatsMap = new(StringComparer.Ordinal)
    {
        ["a1"] = "\u2701", ["a2"] = "\u2702", ["a3"] = "\u2704", ["a4"] = "\u260E",
        ["a5"] = "\u2706", ["a6"] = "\u271D", ["a7"] = "\u271E", ["a8"] = "\u271F",
        ["a9"] = "\u2720", ["a10"] = "\u2721", ["a11"] = "\u261B", ["a12"] = "\u261E",
        ["a13"] = "\u270C", ["a14"] = "\u270D", ["a15"] = "\u270E", ["a16"] = "\u270F",
        ["a17"] = "\u2711", ["a18"] = "\u2712", ["a19"] = "\u2713", ["a20"] = "\u2714",
        ["a21"] = "\u2715", ["a22"] = "\u2716", ["a23"] = "\u2717", ["a24"] = "\u2718",
        ["a25"] = "\u2719", ["a26"] = "\u271A", ["a27"] = "\u271B", ["a28"] = "\u271C",
        ["a29"] = "\u2722", ["a30"] = "\u2723", ["a31"] = "\u2724", ["a32"] = "\u2725",
        ["a33"] = "\u2726", ["a34"] = "\u2727", ["a35"] = "\u2605", ["a36"] = "\u2729",
        ["a37"] = "\u272A", ["a38"] = "\u272B", ["a39"] = "\u272C", ["a40"] = "\u272D",
        ["a41"] = "\u272E", ["a42"] = "\u272F", ["a43"] = "\u2730", ["a44"] = "\u2731",
        ["a45"] = "\u2732", ["a46"] = "\u2733", ["a47"] = "\u2734", ["a48"] = "\u2735",
        ["a49"] = "\u2736", ["a50"] = "\u2737", ["a51"] = "\u2738", ["a52"] = "\u2739",
        ["a53"] = "\u273A", ["a54"] = "\u273B", ["a55"] = "\u273C", ["a56"] = "\u273D",
        ["a57"] = "\u273E", ["a58"] = "\u273F", ["a59"] = "\u2740", ["a60"] = "\u2741",
        ["a61"] = "\u2742", ["a62"] = "\u2743", ["a63"] = "\u2744", ["a64"] = "\u2745",
        ["a65"] = "\u2746", ["a66"] = "\u2747", ["a67"] = "\u2748", ["a68"] = "\u2749",
        ["a69"] = "\u274A", ["a70"] = "\u274B", ["a71"] = "\u25CF", ["a72"] = "\u274D",
        ["a73"] = "\u25A0", ["a74"] = "\u274F", ["a75"] = "\u2751", ["a76"] = "\u25B2",
        ["a77"] = "\u25BC", ["a78"] = "\u25C6", ["a79"] = "\u2756", ["a81"] = "\u25D7",
        ["a82"] = "\u2758", ["a83"] = "\u2759", ["a84"] = "\u275A", ["a97"] = "\u275B",
        ["a98"] = "\u275C", ["a99"] = "\u275D", ["a100"] = "\u275E", ["a101"] = "\u2761",
        ["a102"] = "\u2762", ["a103"] = "\u2763", ["a104"] = "\u2764", ["a105"] = "\u2710",
        ["a106"] = "\u2765", ["a107"] = "\u2766", ["a108"] = "\u2767", ["a109"] = "\u2660",
        ["a110"] = "\u2665", ["a111"] = "\u2666", ["a112"] = "\u2663", ["a117"] = "\u2709",
        ["a118"] = "\u2708", ["a119"] = "\u2707", ["a120"] = "\u2460", ["a121"] = "\u2461",
        ["a122"] = "\u2462", ["a123"] = "\u2463", ["a124"] = "\u2464", ["a125"] = "\u2465",
        ["a126"] = "\u2466", ["a127"] = "\u2467", ["a128"] = "\u2468", ["a129"] = "\u2469",
        ["a130"] = "\u2776", ["a131"] = "\u2777", ["a132"] = "\u2778", ["a133"] = "\u2779",
        ["a134"] = "\u277A", ["a135"] = "\u277B", ["a136"] = "\u277C", ["a137"] = "\u277D",
        ["a138"] = "\u277E", ["a139"] = "\u277F", ["a140"] = "\u2780", ["a141"] = "\u2781",
        ["a142"] = "\u2782", ["a143"] = "\u2783", ["a144"] = "\u2784", ["a145"] = "\u2785",
        ["a146"] = "\u2786", ["a147"] = "\u2787", ["a148"] = "\u2788", ["a149"] = "\u2789",
        ["a150"] = "\u278A", ["a151"] = "\u278B", ["a152"] = "\u278C", ["a153"] = "\u278D",
        ["a154"] = "\u278E", ["a155"] = "\u278F", ["a156"] = "\u2790", ["a157"] = "\u2791",
        ["a158"] = "\u2792", ["a159"] = "\u2793", ["a160"] = "\u2794", ["a161"] = "\u2192",
        ["a162"] = "\u27A3", ["a163"] = "\u2194", ["a164"] = "\u2195", ["a165"] = "\u2799",
        ["a166"] = "\u279B", ["a167"] = "\u279C", ["a168"] = "\u279D", ["a169"] = "\u279E",
        ["a170"] = "\u279F", ["a171"] = "\u27A0", ["a172"] = "\u27A1", ["a173"] = "\u27A2",
        ["a174"] = "\u27A4", ["a175"] = "\u27A5", ["a176"] = "\u27A6", ["a177"] = "\u27A7",
        ["a178"] = "\u27A8", ["a179"] = "\u27A9", ["a180"] = "\u27AB", ["a181"] = "\u27AD",
        ["a182"] = "\u27AF", ["a183"] = "\u27B2", ["a184"] = "\u27B3", ["a185"] = "\u27B5",
        ["a186"] = "\u27B8", ["a187"] = "\u27BA", ["a188"] = "\u27BB", ["a189"] = "\u27BC",
        ["a190"] = "\u27BD", ["a191"] = "\u27BE", ["a192"] = "\u279A", ["a193"] = "\u27AA",
        ["a194"] = "\u27B6", ["a195"] = "\u27B9", ["a196"] = "\u2798", ["a197"] = "\u27B4",
        ["a198"] = "\u27B7", ["a199"] = "\u27AC", ["a200"] = "\u27AE", ["a201"] = "\u27B1",
        ["a202"] = "\u2703", ["a203"] = "\u2750", ["a204"] = "\u2752",
    };
}
