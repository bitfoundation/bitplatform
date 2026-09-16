using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Decodes HTML entity references (<c>&amp;copy;</c>) and numeric character references
/// (<c>&amp;#169;</c>, <c>&amp;#xA9;</c>), which CommonMark recognizes everywhere except
/// inside code spans and code blocks.
/// </summary>
/// <remarks>
/// Decoding happens while parsing, so the decoded text is an ordinary string that Blazor
/// escapes again on the way into the DOM. <c>&amp;lt;script&amp;gt;</c> therefore renders
/// as the visible text <c>&lt;script&gt;</c> and never as markup.
/// </remarks>
public static class BitMarkdownEntities
{
    /// <summary>The longest named reference recognized, used to bound the lookahead.</summary>
    private const int MaxNameLength = 32;

    // The HTML 4 named character references plus the handful of HTML5 additions that
    // appear in real documents. A full HTML5 table is ~2200 names; this covers what
    // Markdown actually uses without shipping the table.
    private static readonly Dictionary<string, string> Named = new(StringComparer.Ordinal)
    {
        // Markup-significant and whitespace
        ["amp"] = "&", ["lt"] = "<", ["gt"] = ">", ["quot"] = "\"", ["apos"] = "'",
        ["nbsp"] = " ", ["ensp"] = " ", ["emsp"] = " ", ["thinsp"] = " ",
        ["zwnj"] = "‌", ["zwj"] = "‍", ["lrm"] = "‎", ["rlm"] = "‏",
        // Latin-1 punctuation and symbols
        ["iexcl"] = "¡", ["cent"] = "¢", ["pound"] = "£", ["curren"] = "¤",
        ["yen"] = "¥", ["brvbar"] = "¦", ["sect"] = "§", ["uml"] = "¨",
        ["copy"] = "©", ["ordf"] = "ª", ["laquo"] = "«", ["not"] = "¬",
        ["shy"] = "­", ["reg"] = "®", ["macr"] = "¯", ["deg"] = "°",
        ["plusmn"] = "±", ["sup2"] = "²", ["sup3"] = "³", ["acute"] = "´",
        ["micro"] = "µ", ["para"] = "¶", ["middot"] = "·", ["cedil"] = "¸",
        ["sup1"] = "¹", ["ordm"] = "º", ["raquo"] = "»", ["frac14"] = "¼",
        ["frac12"] = "½", ["frac34"] = "¾", ["iquest"] = "¿", ["times"] = "×",
        ["divide"] = "÷",
        // Latin-1 letters
        ["Agrave"] = "À", ["Aacute"] = "Á", ["Acirc"] = "Â", ["Atilde"] = "Ã",
        ["Auml"] = "Ä", ["Aring"] = "Å", ["AElig"] = "Æ", ["Ccedil"] = "Ç",
        ["Egrave"] = "È", ["Eacute"] = "É", ["Ecirc"] = "Ê", ["Euml"] = "Ë",
        ["Igrave"] = "Ì", ["Iacute"] = "Í", ["Icirc"] = "Î", ["Iuml"] = "Ï",
        ["ETH"] = "Ð", ["Ntilde"] = "Ñ", ["Ograve"] = "Ò", ["Oacute"] = "Ó",
        ["Ocirc"] = "Ô", ["Otilde"] = "Õ", ["Ouml"] = "Ö", ["Oslash"] = "Ø",
        ["Ugrave"] = "Ù", ["Uacute"] = "Ú", ["Ucirc"] = "Û", ["Uuml"] = "Ü",
        ["Yacute"] = "Ý", ["THORN"] = "Þ", ["szlig"] = "ß",
        ["agrave"] = "à", ["aacute"] = "á", ["acirc"] = "â", ["atilde"] = "ã",
        ["auml"] = "ä", ["aring"] = "å", ["aelig"] = "æ", ["ccedil"] = "ç",
        ["egrave"] = "è", ["eacute"] = "é", ["ecirc"] = "ê", ["euml"] = "ë",
        ["igrave"] = "ì", ["iacute"] = "í", ["icirc"] = "î", ["iuml"] = "ï",
        ["eth"] = "ð", ["ntilde"] = "ñ", ["ograve"] = "ò", ["oacute"] = "ó",
        ["ocirc"] = "ô", ["otilde"] = "õ", ["ouml"] = "ö", ["oslash"] = "ø",
        ["ugrave"] = "ù", ["uacute"] = "ú", ["ucirc"] = "û", ["uuml"] = "ü",
        ["yacute"] = "ý", ["thorn"] = "þ", ["yuml"] = "ÿ",
        ["OElig"] = "Œ", ["oelig"] = "œ", ["Scaron"] = "Š", ["scaron"] = "š",
        ["Yuml"] = "Ÿ", ["fnof"] = "ƒ",
        // General punctuation
        ["ndash"] = "–", ["mdash"] = "—", ["lsquo"] = "‘", ["rsquo"] = "’",
        ["sbquo"] = "‚", ["ldquo"] = "“", ["rdquo"] = "”", ["bdquo"] = "„",
        ["dagger"] = "†", ["Dagger"] = "‡", ["bull"] = "•", ["hellip"] = "…",
        ["permil"] = "‰", ["prime"] = "′", ["Prime"] = "″", ["lsaquo"] = "‹",
        ["rsaquo"] = "›", ["oline"] = "‾", ["frasl"] = "⁄", ["euro"] = "€",
        ["trade"] = "™", ["image"] = "ℑ", ["real"] = "ℜ", ["weierp"] = "℘",
        ["alefsym"] = "ℵ",
        // Arrows and math
        ["larr"] = "←", ["uarr"] = "↑", ["rarr"] = "→", ["darr"] = "↓",
        ["harr"] = "↔", ["crarr"] = "↵", ["lArr"] = "⇐", ["uArr"] = "⇑",
        ["rArr"] = "⇒", ["dArr"] = "⇓", ["hArr"] = "⇔",
        ["forall"] = "∀", ["part"] = "∂", ["exist"] = "∃", ["empty"] = "∅",
        ["nabla"] = "∇", ["isin"] = "∈", ["notin"] = "∉", ["ni"] = "∋",
        ["prod"] = "∏", ["sum"] = "∑", ["minus"] = "−", ["lowast"] = "∗",
        ["radic"] = "√", ["prop"] = "∝", ["infin"] = "∞", ["ang"] = "∠",
        ["and"] = "∧", ["or"] = "∨", ["cap"] = "∩", ["cup"] = "∪",
        ["int"] = "∫", ["there4"] = "∴", ["sim"] = "∼", ["cong"] = "≅",
        ["asymp"] = "≈", ["ne"] = "≠", ["equiv"] = "≡", ["le"] = "≤",
        ["ge"] = "≥", ["sub"] = "⊂", ["sup"] = "⊃", ["nsub"] = "⊄",
        ["sube"] = "⊆", ["supe"] = "⊇", ["oplus"] = "⊕", ["otimes"] = "⊗",
        ["perp"] = "⊥", ["sdot"] = "⋅", ["lceil"] = "⌈", ["rceil"] = "⌉",
        ["lfloor"] = "⌊", ["rfloor"] = "⌋", ["lang"] = "〈", ["rang"] = "〉",
        ["loz"] = "◊", ["spades"] = "♠", ["clubs"] = "♣", ["hearts"] = "♥",
        ["diams"] = "♦",
        // Greek
        ["Alpha"] = "Α", ["Beta"] = "Β", ["Gamma"] = "Γ", ["Delta"] = "Δ",
        ["Epsilon"] = "Ε", ["Zeta"] = "Ζ", ["Eta"] = "Η", ["Theta"] = "Θ",
        ["Iota"] = "Ι", ["Kappa"] = "Κ", ["Lambda"] = "Λ", ["Mu"] = "Μ",
        ["Nu"] = "Ν", ["Xi"] = "Ξ", ["Omicron"] = "Ο", ["Pi"] = "Π",
        ["Rho"] = "Ρ", ["Sigma"] = "Σ", ["Tau"] = "Τ", ["Upsilon"] = "Υ",
        ["Phi"] = "Φ", ["Chi"] = "Χ", ["Psi"] = "Ψ", ["Omega"] = "Ω",
        ["alpha"] = "α", ["beta"] = "β", ["gamma"] = "γ", ["delta"] = "δ",
        ["epsilon"] = "ε", ["zeta"] = "ζ", ["eta"] = "η", ["theta"] = "θ",
        ["iota"] = "ι", ["kappa"] = "κ", ["lambda"] = "λ", ["mu"] = "μ",
        ["nu"] = "ν", ["xi"] = "ξ", ["omicron"] = "ο", ["pi"] = "π",
        ["rho"] = "ρ", ["sigmaf"] = "ς", ["sigma"] = "σ", ["tau"] = "τ",
        ["upsilon"] = "υ", ["phi"] = "φ", ["chi"] = "χ", ["psi"] = "ψ",
        ["omega"] = "ω", ["thetasym"] = "ϑ", ["upsih"] = "ϒ", ["piv"] = "ϖ",
        // Diacritics
        ["circ"] = "ˆ", ["tilde"] = "˜",
        // Checks and crosses (common HTML5 names in documentation)
        ["check"] = "✓", ["cross"] = "✗", ["star"] = "☆", ["starf"] = "★",
    };

    /// <summary>
    /// Tries to read an entity or numeric character reference starting at the <c>&amp;</c>
    /// found at <paramref name="index"/>.
    /// </summary>
    /// <param name="s">The text being scanned.</param>
    /// <param name="index">The index of the <c>&amp;</c> that opens the candidate reference.</param>
    /// <param name="value">The decoded text.</param>
    /// <param name="length">How many source characters the reference occupied.</param>
    public static bool TryDecodeAt(string s, int index, out string value, out int length)
    {
        value = string.Empty;
        length = 0;

        if (index >= s.Length || s[index] != '&') return false;

        int i = index + 1;
        if (i >= s.Length) return false;

        if (s[i] == '#')
        {
            i++;
            bool hex = i < s.Length && (s[i] is 'x' or 'X');
            if (hex) i++;

            int digitStart = i;
            while (i < s.Length && (hex ? Uri.IsHexDigit(s[i]) : char.IsAsciiDigit(s[i]))) i++;

            int digits = i - digitStart;
            // CommonMark bounds numeric references at 1-7 digits (hex references at 1-6).
            if (digits == 0 || digits > (hex ? 6 : 7)) return false;
            if (i >= s.Length || s[i] != ';') return false;

            var span = s.AsSpan(digitStart, digits);
            if (int.TryParse(span, hex ? NumberStyles.HexNumber : NumberStyles.None, CultureInfo.InvariantCulture, out int code) is false)
                return false;

            value = FromCodePoint(code);
            length = i + 1 - index;
            return true;
        }

        int nameStart = i;
        while (i < s.Length && i - nameStart <= MaxNameLength && char.IsAsciiLetterOrDigit(s[i])) i++;
        if (i == nameStart || i >= s.Length || s[i] != ';') return false;

        if (Named.TryGetValue(s[nameStart..i], out var named) is false) return false;

        value = named;
        length = i + 1 - index;
        return true;
    }

    /// <summary>
    /// Decodes every entity and numeric character reference in <paramref name="text"/>.
    /// Used for the places the inline scanner does not reach - link destinations and titles.
    /// </summary>
    public static string Decode(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('&') < 0) return text;

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length;)
        {
            if (text[i] == '&' && TryDecodeAt(text, i, out var value, out int length))
            {
                sb.Append(value);
                i += length;
                continue;
            }
            sb.Append(text[i++]);
        }
        return sb.ToString();
    }

    // Per the spec, NUL, surrogates and out-of-range code points become U+FFFD rather
    // than producing an invalid string.
    private static string FromCodePoint(int code)
    {
        if (code is 0 or > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF))
            return "�";

        return char.ConvertFromUtf32(code);
    }
}
