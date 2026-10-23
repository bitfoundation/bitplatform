namespace Bit.Balette;

/// <summary>
/// The character folding behind normalized matching. Every mapping is one UTF-16 character to one, so a position in the
/// folded text is the same position in the original - which is what highlighting needs.
/// </summary>
/// <remarks>
/// Two kinds of folding, applied the way fzf applies its normalization:
/// <list type="bullet">
/// <item>Latin letters lose their diacritics (<c>é</c> → <c>e</c>, <c>ł</c> → <c>l</c>, <c>Ø</c> → <c>O</c>) only when the
/// query term has none of its own: <c>cafe</c> finds "Café", while a user who types <c>é</c> means that letter.</item>
/// <item>Letters that only differ by the keyboard they were typed on are always the same letter: the Arabic and Persian
/// forms of yeh (<c>ي ى ئ</c> → <c>ی</c>) and kaf (<c>ك</c> → <c>ک</c>), alef with hamza or madda (<c>أ إ آ ٱ</c> →
/// <c>ا</c>), teh marbuta and heh with yeh (<c>ة ۀ</c> → <c>ه</c>), waw with hamza (<c>ؤ</c> → <c>و</c>); Arabic-Indic and
/// Persian digits (<c>٣ ۳</c> → <c>3</c>); and fullwidth ASCII (<c>Ａ</c> → <c>A</c>).</item>
/// </list>
/// </remarks>
internal static class BaletteTextFold
{
    // One base letter per code point of U+00C0..U+024F and U+1E00..U+1EFF, '\0' where nothing folds. Generated from the
    // Unicode decomposition data (NFD: an ASCII letter followed only by combining marks), plus the letters with a stroke
    // or a bar that do not decompose (ø đ ł ħ ı ŧ ...), the set fzf's own table covers.
    private const string LatinA =
        "AAAAAA\0CEEEEIIIIDNOOOOO\0OUUUUY\0\0aaaaaa\0ceeeeiiiidnooooo\0ouuuuy\0y" +
        "AaAaAaCcCcCcCcDdDdEeEeEeEeEeGgGgGgGgHhHhIiIiIiIiIi\0\0JjKk\0LlLlLl\0" +
        "\0LlNnNnNn\0\0\0OoOoOo\0\0RrRrRrSsSsSsSsTtTtTtUuUuUuUuUuUuWwYyYZzZzZz\0" +
        "b\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0I\0\0l\0\0\0\0\0Oo\0\0\0\0\0\0\0\0\0\0\0\0\0Uu\0\0\0\0Zz\0\0\0\0\0\0\0\0\0" +
        "\0\0\0\0\0\0\0\0\0\0\0\0\0AaIiOoUuUuUuUuUu\0AaAa\0\0\0\0GgKkOoOo\0\0j\0\0\0Gg\0\0NnAa\0\0\0\0" +
        "AaAaEeEeIiIiOoOoRrRrUuUuSsTt\0\0Hh\0\0\0\0\0\0AaEeOoOoOoOoYy\0\0\0\0\0\0\0\0\0\0\0\0" +
        "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

    private const string LatinExtendedAdditional =
        "AaBbBbBbCcDdDdDdDdDdEeEeEeEeEeFfGgHhHhHhHhHhIiIiKkKkKkLlLlLlLlMm" +
        "MmMmNnNnNnNnOoOoOoOoPpPpRrRrRrRrSsSsSsSsSsTtTtTtTtUuUuUuUuUuVvVv" +
        "WwWwWwWwWwXxXxYyZzZzZzhtwy\0\0\0\0\0\0AaAaAaAaAaAaAaAaAaAaAaAaEeEeEeEe" +
        "EeEeEeEeIiIiOoOoOoOoOoOoOoOoOoOoOoOoUuUuUuUuUuUuUuYyYyYyYy\0\0\0\0\0\0";

    /// <summary>The Latin letter <paramref name="c"/> without its diacritics, or <paramref name="c"/> itself.</summary>
    public static char StripDiacritic(char c)
    {
        int i;
        if ((i = c - 0x00C0) >= 0 && i < LatinA.Length)
        {
            var b = LatinA[i];
            return b == '\0' ? c : b;
        }

        if ((i = c - 0x1E00) >= 0 && i < LatinExtendedAdditional.Length)
        {
            var b = LatinExtendedAdditional[i];
            return b == '\0' ? c : b;
        }

        return c;
    }

    /// <summary>The form a keyboard-dependent variant (Arabic script letters and digits, fullwidth ASCII) is matched as.</summary>
    public static char FoldVariant(char c)
    {
        if (c < 0x0622)
        {
            return c;
        }

        if (c is >= '！' and <= '～')
        {
            return (char)(c - 0xFEE0); // fullwidth ASCII
        }

        return c switch
        {
            >= '٠' and <= '٩' => (char)('0' + (c - 0x0660)), // Arabic-Indic digits
            >= '۰' and <= '۹' => (char)('0' + (c - 0x06F0)), // Extended Arabic-Indic (Persian, Urdu) digits
            'ي' or 'ى' or 'ئ' => 'ی',              // ي ى ئ → Persian yeh ی
            'ك' => 'ک',                                     // ك → keheh ک
            'آ' or 'أ' or 'إ' or 'ٱ' => 'ا', // آ أ إ ٱ → alef ا
            'ة' or 'ۀ' => 'ه',                         // ة ۀ → heh ه
            'ؤ' => 'و',                                     // ؤ → waw و
            _ => c,
        };
    }

    /// <summary>True when the term has a Latin letter with a diacritic, which it then means literally.</summary>
    public static bool HasDiacritic(string term)
    {
        foreach (var c in term)
        {
            if (c >= 0x00C0 && StripDiacritic(c) != c)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Folds one character for matching: keyboard variants when normalizing, diacritics when also stripping them, and
    /// case when matching case-insensitively.
    /// </summary>
    public static char Fold(char c, bool ignoreCase, bool normalize, bool stripDiacritics)
    {
        if (c < 128)
        {
            return ignoreCase && (uint)(c - 'A') <= 'Z' - 'A' ? (char)(c | 0x20) : c;
        }

        if (normalize)
        {
            c = FoldVariant(c);
            if (stripDiacritics)
            {
                c = StripDiacritic(c);
            }

            if (c < 128)
            {
                return ignoreCase && (uint)(c - 'A') <= 'Z' - 'A' ? (char)(c | 0x20) : c;
            }
        }

        return ignoreCase ? char.ToLowerInvariant(c) : c;
    }

    /// <summary>
    /// Where <paramref name="term"/> occurs in <paramref name="text"/> - anywhere, at the start or at the end - comparing
    /// folded characters case-insensitively; -1 when it does not.
    /// </summary>
    public static int Find(string text, string term, BaletteTextMatchKind kind, bool normalize)
    {
        if (term.Length == 0 || term.Length > text.Length)
        {
            return term.Length == 0 ? 0 : -1;
        }

        // Most terms and texts are plain ASCII, where folding is only case and the runtime's vectorized ordinal search
        // is the whole job. A term that folds to something else than ASCII cannot occur in an ASCII text at all.
        var asciiText = System.Text.Ascii.IsValid(text);
        if (asciiText && System.Text.Ascii.IsValid(term))
        {
            return FindOrdinal(text, term, kind);
        }

        var strip = normalize && !HasDiacritic(term);
        Span<char> t = term.Length <= 128 ? stackalloc char[term.Length] : new char[term.Length];
        for (var i = 0; i < term.Length; i++)
        {
            t[i] = Fold(term[i], true, normalize, strip);
        }

        if (asciiText)
        {
            return System.Text.Ascii.IsValid(t) ? FindOrdinal(text, t, kind) : -1;
        }

        var first = kind == BaletteTextMatchKind.Suffix ? text.Length - term.Length : 0;
        var last = kind == BaletteTextMatchKind.Prefix ? 0 : text.Length - term.Length;
        for (var start = first; start <= last; start++)
        {
            var k = 0;
            while (k < t.Length && Fold(text[start + k], true, normalize, strip) == t[k])
            {
                k++;
            }

            if (k == t.Length)
            {
                return start;
            }
        }

        return -1;
    }

    private static int FindOrdinal(string text, ReadOnlySpan<char> term, BaletteTextMatchKind kind) => kind switch
    {
        BaletteTextMatchKind.Prefix => text.AsSpan().StartsWith(term, StringComparison.OrdinalIgnoreCase) ? 0 : -1,
        BaletteTextMatchKind.Suffix => text.AsSpan().EndsWith(term, StringComparison.OrdinalIgnoreCase) ? text.Length - term.Length : -1,
        _ => text.AsSpan().IndexOf(term, StringComparison.OrdinalIgnoreCase),
    };
}

/// <summary>Where a literal term has to occur in a text.</summary>
internal enum BaletteTextMatchKind
{
    Anywhere,
    Prefix,
    Suffix,
}
