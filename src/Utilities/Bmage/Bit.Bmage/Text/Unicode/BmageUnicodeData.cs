namespace Bit.Bmage;

/// <summary>Lookups into <see cref="BmageUnicodeTables"/>.</summary>
internal static class BmageUnicodeData
{
    public static BmageBidiClass GetBidiClass(int codePoint) => (BmageBidiClass)Find(BmageUnicodeTables.BidiClassRuns, codePoint, 5);

    public static BmageJoiningType GetJoiningType(int codePoint) => (BmageJoiningType)Find(BmageUnicodeTables.JoiningTypeRuns, codePoint, 3);

    public static BmageUnicodeScript GetScript(int codePoint) => (BmageUnicodeScript)Find(BmageUnicodeTables.ScriptRuns, codePoint, 3);

    /// <summary>Whether a code point has no visible glyph of its own: ZWNJ, ZWJ, the bidi controls, variation selectors.</summary>
    public static bool IsDefaultIgnorable(int codePoint) => Find(BmageUnicodeTables.IgnorableRuns, codePoint, 1) == 1;

    /// <summary>The mirrored form of a code point drawn right to left, such as ) for (, or -1 when it has none.</summary>
    public static int GetMirror(int codePoint)
    {
        var pairs = BmageUnicodeTables.MirrorPairs;
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
        var triples = BmageUnicodeTables.BracketTriples;
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
