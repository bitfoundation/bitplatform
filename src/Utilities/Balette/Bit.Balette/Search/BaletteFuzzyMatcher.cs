namespace Bit.Balette;

/// <summary>
/// fzf-inspired fuzzy matcher using dynamic programming (Smith-Waterman style with affine gaps).
/// Rewards matches at word boundaries, camelCase humps, consecutive runs and the first pattern character;
/// penalizes gaps. Smart case: a lower-case query is case-insensitive, mixed case is case-sensitive.
/// Returns the positions of the optimal alignment for highlighting.
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

    /// <summary>Shared default instance.</summary>
    public static BaletteFuzzyMatcher Default { get; } = new();

    /// <summary>Disable smart case and always match case-insensitively.</summary>
    public bool IgnoreCase { get; init; }

    /// <summary>Scores <paramref name="query"/> against <paramref name="text"/>, with the positions of the best alignment.</summary>
    public BaletteFieldMatch Match(string query, string text)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BaletteFieldMatch.Empty;
        }

        if (string.IsNullOrEmpty(text) || query.Length > text.Length)
        {
            return BaletteFieldMatch.None;
        }

        // Runs once per field per command on every keystroke (interpreted on WebAssembly), so it avoids
        // LINQ, per-call pooling and full-matrix initialization. Results are identical to the plain DP.
        var caseSensitive = !IgnoreCase && HasUpper(query);
        int p = query.Length, t = text.Length;
        var size = (p + 1) * (t + 1);
        (t_scratch ??= new Scratch()).Get(p, t, size, out var q, out var tx, out var m, out var d, out var consec, out var bonus);
        Fold(query, q, caseSensitive);
        Fold(text, tx, caseSensitive);

        // Fast reject: must be a subsequence.
        if (!IsSubsequence(q, p, tx, t))
        {
            return BaletteFieldMatch.None;
        }

        const int NegInf = int.MinValue / 2;
        const int Floor = int.MinValue / 4;
        var w = t + 1;

        ComputeBonus(text, bonus);

        // Column 0 is the only part of m/d read before being written; consec is only read behind a match guard.
        for (var i = 0; i <= p; i++)
        {
            m[i * w] = NegInf;
            d[i * w] = NegInf;
        }

        // Row-major (p + 1) x (t + 1) matrices: row i + 1 / column j + 1 hold query[i] / text[j].
        // m = best score with query[i] matched at text[j]; d = best score for query[..i] within text[..j];
        // consec = length of the consecutive run ending at (i, j).
        for (var i = 0; i < p; i++)
        {
            var qc = q[i];
            var prevRow = i * w;
            var curRow = prevRow + w;
            var inGapPrev = false;
            for (var j = 0; j < t; j++)
            {
                var cur = curRow + j + 1;
                var mv = NegInf;
                if (qc == tx[j])
                {
                    var prevScore = 0;
                    var run = 0;
                    var reachable = true;
                    if (i > 0)
                    {
                        prevScore = d[prevRow + j];
                        if (prevScore <= Floor)
                        {
                            reachable = false;
                        }
                        else if (m[prevRow + j] == prevScore)
                        {
                            run = consec[prevRow + j];
                        }
                    }

                    if (reachable)
                    {
                        var b = bonus[j];
                        if (run > 0)
                        {
                            // Carry the bonus that started the run so "Foo" inside "fooBar" still benefits.
                            var runStart = j - run;
                            var startBonus = runStart >= 0 ? bonus[runStart] : 0;
                            b = Math.Max(Math.Max(b, BonusConsecutive), startBonus);
                        }

                        if (i == 0)
                        {
                            b *= BonusFirstCharMultiplier;
                        }

                        if (caseSensitive || (query[i] == text[j] && char.IsUpper(text[j])))
                        {
                            b += BonusCaseMatch;
                        }

                        mv = prevScore + ScoreMatch + b;
                        consec[cur] = run + 1;
                    }
                }

                m[cur] = mv;
                var left = d[cur - 1];
                var gapScore = left <= Floor ? NegInf : left + (inGapPrev ? ScoreGapExtension : ScoreGapStart);
                if (mv >= gapScore)
                {
                    d[cur] = mv;
                    inGapPrev = false;
                }
                else
                {
                    d[cur] = gapScore;
                    inGapPrev = true;
                }
            }
        }

        // Best end position for the last pattern char.
        var best = NegInf;
        var bestJ = -1;
        var lastRow = p * w;
        for (var j = 0; j < t; j++)
        {
            var v = m[lastRow + j + 1];
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
            var row = i * w;
            while (jj >= 0 && d[row + jj + 1] != m[row + jj + 1])
            {
                jj--;
            }
        }

        // Tie-breakers (fractions of one bonus point): prefer shorter texts and earlier matches.
        var score = best - 0.02 * t - 0.01 * positions[0];
        return new BaletteFieldMatch(true, score, positions);
    }

    [ThreadStatic]
    private static Scratch? t_scratch;

    /// <summary>Per-thread DP buffers reused across calls. Oversized requests get throwaway arrays.</summary>
    private sealed class Scratch
    {
        private const int MaxRetainedCells = 16 * 1024;

        private int[] _m = new int[256];
        private int[] _d = new int[256];
        private int[] _consec = new int[256];
        private int[] _bonus = new int[64];
        private char[] _query = new char[32];
        private char[] _text = new char[64];

        public void Get(int p, int t, int size, out char[] query, out char[] text, out int[] m, out int[] d, out int[] consec, out int[] bonus)
        {
            if (size > MaxRetainedCells)
            {
                query = new char[p];
                text = new char[t];
                m = new int[size];
                d = new int[size];
                consec = new int[size];
                bonus = new int[t];
                return;
            }

            if (_m.Length < size)
            {
                var n = Math.Min(MaxRetainedCells, Math.Max(size, _m.Length * 2));
                _m = new int[n];
                _d = new int[n];
                _consec = new int[n];
            }

            if (_text.Length < t)
            {
                var n = Math.Max(t, _text.Length * 2);
                _text = new char[n];
                _bonus = new int[n];
            }

            if (_query.Length < p)
            {
                _query = new char[Math.Max(p, _query.Length * 2)];
            }

            query = _query;
            text = _text;
            m = _m;
            d = _d;
            consec = _consec;
            bonus = _bonus;
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

    /// <summary>Copies <paramref name="s"/> into <paramref name="dst"/>, lower-cased unless matching case-sensitively.</summary>
    private static void Fold(string s, char[] dst, bool caseSensitive)
    {
        if (caseSensitive)
        {
            s.CopyTo(0, dst, 0, s.Length);
            return;
        }

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            dst[i] = c < 128
                ? ((uint)(c - 'A') <= 'Z' - 'A' ? (char)(c | 0x20) : c)
                : char.ToLowerInvariant(c);
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

    /// <summary>Finds <paramref name="query"/> in <paramref name="text"/> ignoring case, scoring a match at the start higher.</summary>
    public BaletteFieldMatch Match(string query, string text)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BaletteFieldMatch.Empty;
        }

        var idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return BaletteFieldMatch.None;
        }

        var score = query.Length * BaletteFuzzyMatcher.ScoreMatch + (idx == 0 ? BaletteFuzzyMatcher.BonusBoundary * 2 : 0);
        return new BaletteFieldMatch(true, score, Enumerable.Range(idx, query.Length).ToArray());
    }
}
