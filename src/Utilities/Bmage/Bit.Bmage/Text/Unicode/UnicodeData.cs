namespace Bit.Bmage.Text.Unicode;

/// <summary>Bidi_Class (UAX #9), in the order the generated table numbers it.</summary>
internal enum BidiClass : byte
{
    L, R, AL, EN, ES, ET, AN, CS, NSM, BN, B, S, WS, ON, LRE, LRO, RLE, RLO, PDF, LRI, RLI, FSI, PDI,
}

/// <summary>Joining_Type (the Arabic shaping property), in the order the generated table numbers it.</summary>
internal enum JoiningType : byte
{
    /// <summary>Non joining: a space, a Latin letter, ZERO WIDTH NON-JOINER.</summary>
    U,
    /// <summary>Joins only to the letter before it, as alef and reh do.</summary>
    R,
    /// <summary>Joins only to the letter after it; no Arabic letter does.</summary>
    L,
    /// <summary>Joins on both sides, as beh and seen do.</summary>
    D,
    /// <summary>Join causing: makes its neighbours join to it, as tatweel and ZERO WIDTH JOINER do.</summary>
    C,
    /// <summary>Transparent: skipped when deciding how letters join, as the harakat are.</summary>
    T,
}

/// <summary>The scripts the shaper chooses OpenType features by.</summary>
internal enum UnicodeScript : byte
{
    Common, Inherited, Latin, Greek, Cyrillic, Arabic, Hebrew, Other,
}

/// <summary>Lookups into <see cref="UnicodeTables"/>.</summary>
internal static class UnicodeData
{
    public static BidiClass GetBidiClass(int codePoint) => (BidiClass)Find(UnicodeTables.BidiClassRuns, codePoint, 5);

    public static JoiningType GetJoiningType(int codePoint) => (JoiningType)Find(UnicodeTables.JoiningTypeRuns, codePoint, 3);

    public static UnicodeScript GetScript(int codePoint) => (UnicodeScript)Find(UnicodeTables.ScriptRuns, codePoint, 3);

    /// <summary>Whether a code point has no visible glyph of its own: ZWNJ, ZWJ, the bidi controls, variation selectors.</summary>
    public static bool IsDefaultIgnorable(int codePoint) => Find(UnicodeTables.IgnorableRuns, codePoint, 1) == 1;

    /// <summary>The mirrored form of a code point drawn right to left, such as ) for (, or -1 when it has none.</summary>
    public static int GetMirror(int codePoint)
    {
        var pairs = UnicodeTables.MirrorPairs;
        int lo = 0, hi = pairs.Length / 2 - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            var c = (int)pairs[mid * 2];
            if (c == codePoint) return (int)pairs[mid * 2 + 1];
            if (c < codePoint) lo = mid + 1; else hi = mid - 1;
        }
        return -1;
    }

    /// <summary>The bracket a code point pairs with and whether it opens, or false when it is not a paired bracket.</summary>
    public static bool TryGetBracket(int codePoint, out int pair, out bool opening)
    {
        var triples = UnicodeTables.BracketTriples;
        int lo = 0, hi = triples.Length / 3 - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            var c = (int)triples[mid * 3];
            if (c == codePoint)
            {
                pair = (int)triples[mid * 3 + 1];
                opening = triples[mid * 3 + 2] == 1;
                return true;
            }
            if (c < codePoint) lo = mid + 1; else hi = mid - 1;
        }
        pair = 0;
        opening = false;
        return false;
    }

    /// <summary>The value of the run containing <paramref name="codePoint"/>.</summary>
    private static int Find(ReadOnlySpan<uint> runs, int codePoint, int bits)
    {
        if ((uint)codePoint > 0x10FFFF) return 0;
        var key = (uint)codePoint << bits;
        // The last run starting at or before the code point.
        int lo = 0, hi = runs.Length - 1;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) >>> 1;
            if ((runs[mid] & ~((1u << bits) - 1)) <= key) lo = mid; else hi = mid - 1;
        }
        return (int)(runs[lo] & ((1u << bits) - 1));
    }
}
