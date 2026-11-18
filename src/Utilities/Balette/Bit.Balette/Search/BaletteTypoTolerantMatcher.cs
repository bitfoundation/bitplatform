namespace Bit.Balette;

/// <summary>
/// The fallback a query that matched nothing gets a second chance with (<see cref="BaletteOptions.TypoTolerance"/>):
/// the palette's matcher first, then an approximate match that allows one edit in a term of 4 to 7 characters and two
/// from 8 to 64 - a missing, extra or wrong letter, or two letters swapped (<c>settigns</c> finds "Settings"). Scored below
/// every exact match, and only ever asked when nothing matched exactly, so it adds no noise to a query that works.
/// </summary>
internal sealed class BaletteTypoTolerantMatcher(IBaletteMatcher inner) : IBaletteMatcher
{
    /// <summary>The shortest term that may have a typo: shorter ones match too much with one edit.</summary>
    public const int MinTermLength = 4;

    /// <summary>The longest term that may have a typo: one machine word of <see cref="Approximate"/>'s columns, and longer than any word.</summary>
    public const int MaxTermLength = 64;

    /// <summary>What each edit costs, on the scale of <see cref="BaletteFuzzyMatcher.ScoreMatch"/>.</summary>
    public const int EditPenalty = 12;

    public IBaletteMatcher Inner { get; } = inner;

    public static int MaxEdits(int termLength) => termLength > MaxTermLength ? 0 : termLength >= 8 ? 2 : termLength >= MinTermLength ? 1 : 0;

    public BaletteFieldMatch Match(string query, string text)
    {
        var exact = Inner.Match(query, text);
        if (exact.Matched || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
        {
            return exact;
        }

        var maxEdits = MaxEdits(query.Length);
        return maxEdits == 0 ? BaletteFieldMatch.None : Approximate(query, text, maxEdits, BaletteMatcherExtensions.Normalizes(Inner));
    }

    /// <summary>
    /// The best alignment of the whole of <paramref name="query"/> with any part of <paramref name="text"/> by optimal
    /// string alignment (Levenshtein plus adjacent transpositions), with free start and end in the text. Positions are
    /// the text's characters that matched a query character exactly, swapped pairs included.
    /// <para>
    /// The table - a row per query character, a column per text character - is never stored: its columns are computed a
    /// machine word at a time (Myers' bit-vector algorithm, with Hyyrö's term for transpositions), so a long text costs
    /// time in proportion to its length and no memory. Only the few columns the best alignment can span are written
    /// out, to walk back for its characters. A query longer than <see cref="MaxTermLength"/> matches nothing.
    /// </para>
    /// </summary>
    internal static BaletteFieldMatch Approximate(string query, string text, int maxEdits, bool normalize)
    {
        int p = query.Length, t = text.Length;
        if (p > MaxTermLength || p - maxEdits > t)
        {
            return BaletteFieldMatch.None;
        }

        var strip = normalize && !BaletteTextFold.HasDiacritic(query);
        Span<char> q = stackalloc char[p];

        // Bit i of a character's mask: query[i] is that character. ASCII comes from a table, any other character - rarely
        // more than a few - from a list.
        Span<ulong> ascii = stackalloc ulong[128];
        Span<char> otherChars = stackalloc char[p];
        Span<ulong> otherMasks = stackalloc ulong[p];
        var others = 0;
        for (var i = 0; i < p; i++)
        {
            var c = q[i] = BaletteTextFold.Fold(query[i], true, normalize, strip);
            if (c < 128)
            {
                ascii[c] |= 1UL << i;
                continue;
            }

            var k = otherChars[..others].IndexOf(c);
            if (k < 0)
            {
                k = others++;
                otherChars[k] = c;
            }

            otherMasks[k] |= 1UL << i;
        }

        otherChars = otherChars[..others];
        otherMasks = otherMasks[..others];

        // Column 0 counts the query characters left out, so every row is one more than the row above.
        var last = 1UL << (p - 1);
        ulong vp = ~0UL, vn = 0, d0 = 0, prevEq = 0;
        int distance = p, edits = int.MaxValue, end = 0;
        for (var j = 0; j < t && edits > 0; j++)
        {
            distance += Step(MaskOf(FoldAt(text, j, normalize, strip), ascii, otherChars, otherMasks), ref prevEq, ref d0, ref vp, ref vn, last);
            if (distance < edits)
            {
                edits = distance;
                end = j + 1;
            }
        }

        if (edits > maxEdits)
        {
            return BaletteFieldMatch.None;
        }

        // The best alignment ends at column end and spans at most p + edits text characters, and the walk back reads two
        // columns behind where it starts: those columns are computed again and written out, column after column.
        var from = Math.Max(0, end - p - edits - 2);
        var h = p + 1;
        var d = new int[(end - from + 1) * h];
        vp = ~0UL;
        vn = 0;
        d0 = 0;
        prevEq = 0;
        for (var j = 0; ; j++)
        {
            if (j >= from)
            {
                var column = (j - from) * h;
                for (var i = 0; i < p; i++)
                {
                    d[column + i + 1] = d[column + i] + (int)((vp >> i) & 1) - (int)((vn >> i) & 1);
                }
            }

            if (j == end)
            {
                break;
            }

            Step(MaskOf(FoldAt(text, j, normalize, strip), ascii, otherChars, otherMasks), ref prevEq, ref d0, ref vp, ref vn, last);
        }

        // Walk the alignment back to collect the matched characters and where it starts.
        var positions = new List<int>(p);
        int qi = p, tj = end;
        while (qi > 0 && tj > 0)
        {
            var at = (tj - from) * h;
            var cur = d[at + qi];
            var x = FoldAt(text, tj - 1, normalize, strip);
            if (q[qi - 1] == x && cur == d[at - h + qi - 1])
            {
                positions.Add(tj - 1);
                qi--;
                tj--;
            }
            else if (qi > 1 && tj > 1 && q[qi - 1] == FoldAt(text, tj - 2, normalize, strip) && q[qi - 2] == x && cur == d[at - (2 * h) + qi - 2] + 1)
            {
                positions.Add(tj - 1);
                positions.Add(tj - 2);
                qi -= 2;
                tj -= 2;
            }
            else if (cur == d[at - h + qi - 1] + 1)
            {
                qi--;
                tj--;
            }
            else if (cur == d[at + qi - 1] + 1)
            {
                qi--;
            }
            else
            {
                tj--;
            }
        }

        positions.Sort();
        var atWordStart = tj == 0 || !char.IsLetterOrDigit(text[tj - 1]);
        var score = ((p - edits) * BaletteFuzzyMatcher.ScoreMatch) - (edits * EditPenalty)
            + (atWordStart ? BaletteFuzzyMatcher.BonusBoundary * 2 : 0) - (0.02 * t);
        return new BaletteFieldMatch(true, score, [.. positions]);
    }

    /// <summary>
    /// Moves <see cref="Approximate"/>'s columns on by one text character, whose mask is <paramref name="eq"/>, and says how
    /// the last row changed. Bit i of <paramref name="vp"/> and <paramref name="vn"/> is set when row i + 1 of the column is
    /// one more, or one less, than row i; of <paramref name="d0"/>, when it is what row i was in the column before. A
    /// transposition needs that column's <paramref name="d0"/> and mask. Row 0 stays 0: an alignment may start anywhere.
    /// </summary>
    private static int Step(ulong eq, ref ulong prevEq, ref ulong d0, ref ulong vp, ref ulong vn, ulong last)
    {
        var transposed = ((~d0 & eq) << 1) & prevEq;
        d0 = (((eq & vp) + vp) ^ vp) | eq | vn | transposed;
        var hp = vn | ~(d0 | vp);
        var hn = vp & d0;
        var change = (hp & last) != 0 ? 1 : (hn & last) != 0 ? -1 : 0;
        hp <<= 1;
        hn <<= 1;
        vp = hn | ~(d0 | hp);
        vn = hp & d0;
        prevEq = eq;
        return change;
    }

    private static ulong MaskOf(char c, ReadOnlySpan<ulong> ascii, ReadOnlySpan<char> otherChars, ReadOnlySpan<ulong> otherMasks)
    {
        if (c < 128)
        {
            return ascii[c];
        }

        var k = otherChars.IndexOf(c);
        return k < 0 ? 0 : otherMasks[k];
    }

    private static char FoldAt(string text, int j, bool normalize, bool strip) => BaletteTextFold.Fold(text[j], true, normalize, strip);
}
