using System.Runtime.CompilerServices;

namespace Bit.Balette;

/// <summary>
/// fzf-inspired fuzzy matcher using dynamic programming (Smith-Waterman style with affine gaps).
/// Rewards matches at word boundaries, camelCase humps, consecutive runs and the first pattern character;
/// penalizes gaps. Smart case: a lower-case query is case-insensitive, mixed case is case-sensitive.
/// Returns the positions of the optimal alignment for highlighting. A text longer than <see cref="MaxOptimalLength"/>
/// is aligned greedily in linear time instead, as fzf does.
/// </summary>
public sealed class BaletteFuzzyMatcher : IBaletteMatcher
{
    /// <summary>Score of every matched character.</summary>
    public const int ScoreMatch = 16;
    /// <summary>Penalty of the first skipped character of a gap.</summary>
    public const int ScoreGapStart = -3;
    /// <summary>Penalty of every further skipped character of a gap.</summary>
    public const int ScoreGapExtension = -1;
    /// <summary>Bonus for a match at the start of a word.</summary>
    public const int BonusBoundary = 8;
    /// <summary>Bonus for matching punctuation the query typed.</summary>
    public const int BonusNonWord = 8;
    /// <summary>Bonus for a camelCase hump or a letter-to-digit step.</summary>
    public const int BonusCamel = 7;
    /// <summary>Minimum bonus for a character that follows the previous match.</summary>
    public const int BonusConsecutive = 4;
    /// <summary>The first query character's bonus is multiplied by this.</summary>
    public const int BonusFirstCharMultiplier = 2;
    /// <summary>Bonus for matching the case exactly.</summary>
    public const int BonusCaseMatch = 1;

    /// <summary>
    /// The longest text aligned optimally, by the dynamic programming. A longer one - a long description - is aligned
    /// greedily instead, as fzf does: the first stretch of the text that holds the term, scored by the same rules, in
    /// linear time and without buffers. The choice depends on the text alone, so a field keeps its alignment, and its
    /// place in the ranking, as the query grows. A property rather than a constant, so code compiled against it reads
    /// the value of the library it runs with.
    /// </summary>
    public static int MaxOptimalLength => 4 * 1024;

    /// <summary>
    /// The most cells - (query length + 1) x (text length + 1) - the dynamic programming walks for one field: room for
    /// any term the palette parses (<see cref="BaletteQuery.MaxLength"/>) against any text up to
    /// <see cref="MaxOptimalLength"/>. Only a longer query, passed to <see cref="Match(string, string)"/> directly, goes
    /// past it, and is aligned greedily.
    /// </summary>
    internal static int MaxCells => (BaletteQuery.MaxLength + 1) * (MaxOptimalLength + 1);

    /// <summary>Shared default instance.</summary>
    public static BaletteFuzzyMatcher Default { get; } = new();

    /// <summary>Disable smart case and always match case-insensitively.</summary>
    public bool IgnoreCase { get; init; }

    /// <summary>
    /// Match letters the way people type them rather than the way they are stored (default true): <c>cafe</c> finds
    /// "Café" and <c>lodz</c> "Łódź", unless the query types a diacritic itself, as fzf does; and the Arabic and Persian
    /// forms of a letter (<c>ي</c>/<c>ی</c>, <c>ك</c>/<c>ک</c>), Arabic-Indic and Persian digits (<c>۱۲</c>/<c>12</c>) and
    /// fullwidth ASCII are always the same character. Set false to match every character literally, like fzf's
    /// <c>--literal</c>.
    /// </summary>
    public bool Normalize { get; init; } = true;

    /// <summary>Scores <paramref name="query"/> against <paramref name="text"/>, with the positions of the best alignment.</summary>
    public BaletteFieldMatch Match(string query, string text) => Match(query, text, greedy: false);

    /// <summary>The match, by the greedy alignment alone when <paramref name="greedy"/> - what a text past <see cref="MaxOptimalLength"/> gets.</summary>
    internal BaletteFieldMatch Match(string query, string text, bool greedy)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BaletteFieldMatch.Empty;
        }

        if (string.IsNullOrEmpty(text) || query.Length > text.Length)
        {
            return BaletteFieldMatch.None;
        }

        var caseSensitive = !IgnoreCase && HasUpper(query);
        var strip = Normalize && !BaletteTextFold.HasDiacritic(query);
        int p = query.Length, t = text.Length;
        if (greedy || t > MaxOptimalLength || (long)(p + 1) * (t + 1) > MaxCells)
        {
            return AlignGreedily(query, text, caseSensitive, strip);
        }

        // Runs once per field per command on every keystroke (interpreted on WebAssembly), so it avoids
        // LINQ, per-call allocation and full-matrix initialization. Results are identical to the plain DP.
        var scratch = t_scratch ??= new Scratch();
        scratch.GetText(p, t, out var q, out var tx, out var bonus);
        Fold(query, q, caseSensitive, Normalize, strip);
        Fold(text, tx, caseSensitive, Normalize, strip);

        // Fast reject: must be a subsequence.
        if (!IsSubsequence(q, p, tx, t))
        {
            return BaletteFieldMatch.None;
        }

        ComputeBonus(text, bonus);
        scratch.GetRows(p, t, out var m, out var d, out var consec, out var fromMatch);

        const int NegInf = int.MinValue / 2;
        const int Floor = int.MinValue / 4;

        // Cell (i, j) is query[i] against text[j]: m = best score with query[i] matched at text[j]; d = best score for
        // query[..(i + 1)] within text[..(j + 1)]; consec = length of the consecutive run ending there. One row of each,
        // at index j + 1 (index 0 is the empty text), is overwritten in place, so the row above is read before it goes:
        // what is still there at j + 1, and the diagonal saved from the step before. For the backtrace every cell keeps
        // one bit - whether d came from m, a match rather than a gap.
        m[0] = NegInf;
        d[0] = NegInf;
        for (var i = 0; i < p; i++)
        {
            var qc = q[i];
            var row = i * t;
            int diagM = NegInf, diagD = NegInf, diagConsec = 0;
            var left = NegInf;
            var inGapPrev = false;
            for (var j = 0; j < t; j++)
            {
                var cur = j + 1;
                int upM = m[cur], upD = d[cur], upConsec = consec[cur];
                var mv = NegInf;
                if (qc == tx[j])
                {
                    var prevScore = 0;
                    var run = 0;
                    var reachable = true;
                    if (i > 0)
                    {
                        prevScore = diagD;
                        if (prevScore <= Floor)
                        {
                            reachable = false;
                        }
                        else if (diagM == prevScore)
                        {
                            run = diagConsec;
                        }
                    }

                    if (reachable)
                    {
                        mv = prevScore + MatchScore(bonus[j], run, run > 0 ? bonus[j - run] : 0, i == 0, CaseMatches(query, i, text, j, caseSensitive));
                        consec[cur] = run + 1;
                    }
                }

                m[cur] = mv;
                var gapScore = left <= Floor ? NegInf : left + (inGapPrev ? ScoreGapExtension : ScoreGapStart);
                if (mv >= gapScore)
                {
                    left = mv;
                    inGapPrev = false;
                    fromMatch[(row + j) >> 6] |= 1UL << ((row + j) & 63);
                }
                else
                {
                    left = gapScore;
                    inGapPrev = true;
                }

                d[cur] = left;
                diagM = upM;
                diagD = upD;
                diagConsec = upConsec;
            }
        }

        // Best end position for the last pattern char.
        var best = NegInf;
        var bestJ = -1;
        for (var j = 0; j < t; j++)
        {
            var v = m[j + 1];
            if (v > best)
            {
                best = v;
                bestJ = j;
            }
        }

        if (bestJ < 0 || best <= Floor)
        {
            return BaletteFieldMatch.None;
        }

        // Backtrace positions.
        var positions = new int[p];
        var jj = bestJ;
        for (var i = p - 1; i >= 0; i--)
        {
            positions[i] = jj;
            if (i == 0)
            {
                break;
            }

            // Previous match ends at the j' <= jj-1 where d == m (the alignment came from a match, not a gap).
            jj--;
            var row = (i - 1) * t;
            while (jj >= 0 && (fromMatch[(row + jj) >> 6] & (1UL << ((row + jj) & 63))) == 0)
            {
                jj--;
            }
        }

        return new BaletteFieldMatch(true, Score(best, t, positions[0]), positions);
    }

    /// <summary>
    /// fzf's linear-time alignment (its V1), for a text too long for the dynamic programming: the first stretch of the text
    /// that holds the query in order, narrowed from its end, with the query's characters matched left to right inside it.
    /// Scored by the rules the dynamic programming scores with, so it ranks alongside the others - it is just not always
    /// the best alignment there is. It folds and classifies only the characters it reads, reads nothing past the end of
    /// that stretch, and allocates nothing but the positions.
    /// </summary>
    private BaletteFieldMatch AlignGreedily(string query, string text, bool caseSensitive, bool strip)
    {
        int p = query.Length, t = text.Length;
        var ignoreCase = !caseSensitive;
        var normalize = Normalize;

        // Any term the palette parses fits on the stack.
        Span<char> q = p <= 256 ? stackalloc char[p] : new char[p];
        for (var i = 0; i < p; i++)
        {
            q[i] = BaletteTextFold.Fold(query[i], ignoreCase, normalize, strip);
        }

        // Forward to where the text first holds the whole query - which also tells whether it does at all -
        var end = -1;
        for (int i = 0, j = 0; j < t; j++)
        {
            if (BaletteTextFold.Fold(text[j], ignoreCase, normalize, strip) == q[i] && ++i == p)
            {
                end = j;
                break;
            }
        }

        if (end < 0)
        {
            return BaletteFieldMatch.None;
        }

        // back to the latest start that still holds it,
        var start = end;
        for (int i = p - 1, j = end; j >= 0; j--)
        {
            if (BaletteTextFold.Fold(text[j], ignoreCase, normalize, strip) == q[i] && --i < 0)
            {
                start = j;
                break;
            }
        }

        // and forward again, matching each query character as early as it comes.
        var positions = new int[p];
        for (int i = 0, j = start; i < p; j++)
        {
            if (BaletteTextFold.Fold(text[j], ignoreCase, normalize, strip) == q[i])
            {
                positions[i++] = j;
            }
        }

        var score = 0;
        var run = 0;
        for (var i = 0; i < p; i++)
        {
            var j = positions[i];
            if (i > 0 && j - positions[i - 1] > 1)
            {
                score += GapScore(j - positions[i - 1] - 1);
                run = 0;
            }

            score += MatchScore(BonusAt(text, j), run, run > 0 ? BonusAt(text, j - run) : 0, i == 0, CaseMatches(query, i, text, j, caseSensitive));
            run++;
        }

        return new BaletteFieldMatch(true, Score(score, t, positions[0]), positions);
    }

    /// <summary>
    /// What matching one query character earns, by the rules the dynamic programming and the greedy alignment share:
    /// <see cref="ScoreMatch"/> plus the character's bonus - inside a run at least <see cref="BonusConsecutive"/>, and no
    /// less than the bonus that started the run, so "Foo" inside "fooBar" still benefits - counted
    /// <see cref="BonusFirstCharMultiplier"/> times for the first query character, plus <see cref="BonusCaseMatch"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MatchScore(int bonus, int run, int runStartBonus, bool first, bool caseMatches)
    {
        var b = run > 0 ? Math.Max(Math.Max(bonus, BonusConsecutive), runStartBonus) : bonus;
        if (first)
        {
            b *= BonusFirstCharMultiplier;
        }

        if (caseMatches)
        {
            b += BonusCaseMatch;
        }

        return ScoreMatch + b;
    }

    /// <summary>Whether matching query[<paramref name="i"/>] at text[<paramref name="j"/>] earns <see cref="BonusCaseMatch"/>: always when matching case-sensitively, else for an upper-case letter typed as one.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CaseMatches(string query, int i, string text, int j, bool caseSensitive)
        => caseSensitive || (query[i] == text[j] && char.IsUpper(text[j]));

    /// <summary>What a gap of <paramref name="length"/> skipped characters costs: what the dynamic programming adds up a character at a time.</summary>
    private static int GapScore(int length) => ScoreGapStart + ((length - 1) * ScoreGapExtension);

    /// <summary>The alignment's score less the tie-breakers, fractions of one bonus point: prefer shorter texts and earlier matches.</summary>
    private static double Score(int alignment, int textLength, int firstPosition) => alignment - (0.02 * textLength) - (0.01 * firstPosition);

    [ThreadStatic]
    private static Scratch? t_scratch;

    /// <summary>
    /// Per-thread DP buffers reused across calls. Only a text up to <see cref="MaxOptimalLength"/> uses them, so they never
    /// outgrow that text, one row of scores for it and a bit for each of <see cref="MaxCells"/> cells - a couple of hundred
    /// kilobytes - and what they hold is never handed to other code.
    /// </summary>
    private sealed class Scratch
    {
        private int[] _m = new int[64];
        private int[] _d = new int[64];
        private int[] _consec = new int[64];
        private ulong[] _fromMatch = new ulong[16];
        private int[] _bonus = new int[64];
        private char[] _query = new char[32];
        private char[] _text = new char[64];

        public void GetText(int p, int t, out char[] query, out char[] text, out int[] bonus)
        {
            if (_text.Length < t)
            {
                var n = Math.Min(MaxOptimalLength, Math.Max(t, _text.Length * 2));
                _text = new char[n];
                _bonus = new int[n];
            }

            if (_query.Length < p)
            {
                _query = new char[Math.Min(MaxOptimalLength, Math.Max(p, _query.Length * 2))];
            }

            query = _query;
            text = _text;
            bonus = _bonus;
        }

        public void GetRows(int p, int t, out int[] m, out int[] d, out int[] consec, out ulong[] fromMatch)
        {
            if (_m.Length <= t)
            {
                var n = Math.Min(MaxOptimalLength + 1, Math.Max(t + 1, _m.Length * 2));
                _m = new int[n];
                _d = new int[n];
                _consec = new int[n];
            }

            // The rows are written before they are read (consec only behind a match guard); the bits are only ever set.
            var words = ((p * t) + 63) >> 6;
            if (_fromMatch.Length < words)
            {
                _fromMatch = new ulong[Math.Min((MaxCells >> 6) + 1, Math.Max(words, _fromMatch.Length * 2))];
            }

            Array.Clear(_fromMatch, 0, words);
            m = _m;
            d = _d;
            consec = _consec;
            fromMatch = _fromMatch;
        }
    }

    private static bool HasUpper(string s)
    {
        foreach (var c in s)
        {
            if (char.IsUpper(c))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Copies <paramref name="s"/> into <paramref name="dst"/>, lower-cased unless matching case-sensitively and folded (see
    /// <see cref="BaletteTextFold"/>) when normalizing. One character in, one out: positions stay those of the original.
    /// </summary>
    private static void Fold(string s, char[] dst, bool caseSensitive, bool normalize, bool strip)
    {
        if (caseSensitive && !normalize)
        {
            s.CopyTo(0, dst, 0, s.Length);
            return;
        }

        var ignoreCase = !caseSensitive;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            dst[i] = c < 128
                ? (ignoreCase && (uint)(c - 'A') <= 'Z' - 'A' ? (char)(c | 0x20) : c)
                : BaletteTextFold.Fold(c, ignoreCase, normalize, strip);
        }
    }

    private static bool IsSubsequence(char[] query, int queryLength, char[] text, int textLength)
    {
        var qi = 0;
        for (var ti = 0; ti < textLength; ti++)
        {
            if (text[ti] == query[qi] && ++qi == queryLength)
            {
                return true;
            }
        }

        return false;
    }

    private static void ComputeBonus(string text, int[] bonus)
    {
        var prev = CharClass.NonWord;
        for (var i = 0; i < text.Length; i++)
        {
            var cls = Classify(text[i]);
            bonus[i] = BonusFor(prev, cls);
            prev = cls;
        }
    }

    /// <summary>The bonus of text[<paramref name="j"/>] alone, as <see cref="ComputeBonus"/> computes it for every character.</summary>
    private static int BonusAt(string text, int j) => BonusFor(j == 0 ? CharClass.NonWord : Classify(text[j - 1]), Classify(text[j]));

    private enum CharClass { NonWord, Lower, Upper, Digit, Letter }

    private static CharClass Classify(char c)
    {
        if (c < 128)
        {
            if ((uint)(c - 'a') <= 'z' - 'a') return CharClass.Lower;
            if ((uint)(c - 'A') <= 'Z' - 'A') return CharClass.Upper;
            if ((uint)(c - '0') <= '9' - '0') return CharClass.Digit;
            return CharClass.NonWord;
        }

        if (char.IsLower(c)) return CharClass.Lower;
        if (char.IsUpper(c)) return CharClass.Upper;
        if (char.IsDigit(c)) return CharClass.Digit;
        if (char.IsLetter(c)) return CharClass.Letter;
        return CharClass.NonWord;
    }

    private static int BonusFor(CharClass prev, CharClass cur)
    {
        if (prev == CharClass.NonWord && cur != CharClass.NonWord)
        {
            return BonusBoundary; // start of word (after space, punctuation, or at index 0)
        }

        if (prev == CharClass.Lower && cur == CharClass.Upper)
        {
            return BonusCamel; // camelCase hump
        }

        if (prev != CharClass.Digit && cur == CharClass.Digit)
        {
            return BonusCamel; // letter→digit transition
        }

        if (cur == CharClass.NonWord)
        {
            return BonusNonWord;
        }

        return 0;
    }
}

/// <summary>Simple case-insensitive substring matcher. Faster, no typo tolerance.</summary>
public sealed class BaletteSubstringMatcher : IBaletteMatcher
{
    /// <summary>Shared default instance.</summary>
    public static BaletteSubstringMatcher Default { get; } = new();

    /// <summary>Fold diacritics and keyboard variants as <see cref="BaletteFuzzyMatcher.Normalize"/> does. Default true.</summary>
    public bool Normalize { get; init; } = true;

    /// <summary>Finds <paramref name="query"/> in <paramref name="text"/> ignoring case, scoring a match at the start higher.</summary>
    public BaletteFieldMatch Match(string query, string text)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BaletteFieldMatch.Empty;
        }

        var idx = BaletteTextFold.Find(text, query, BaletteTextMatchKind.Anywhere, Normalize);
        if (idx < 0)
        {
            return BaletteFieldMatch.None;
        }

        var score = query.Length * BaletteFuzzyMatcher.ScoreMatch + (idx == 0 ? BaletteFuzzyMatcher.BonusBoundary * 2 : 0);
        return new BaletteFieldMatch(true, score, Enumerable.Range(idx, query.Length).ToArray());
    }
}
