namespace Bit.Balette;

/// <summary>Scores how well a query matches a text. Implement to plug in a custom algorithm.</summary>
public interface IBaletteMatcher
{
    /// <summary>
    /// Match <paramref name="query"/> against <paramref name="text"/>.
    /// Returns <see cref="BaletteFieldMatch.None"/> when the text does not match.
    /// Positions are indexes into <paramref name="text"/> to highlight.
    /// </summary>
    BaletteFieldMatch Match(string query, string text);
}

/// <summary>Field weights used when combining per-field scores into a command score.</summary>
public sealed class BaletteMatchWeights
{
    /// <summary>Weight of the title. Default 1.0.</summary>
    public double Title { get; set; } = 1.0;
    /// <summary>Weight of a keyword. Default 0.9.</summary>
    public double Keyword { get; set; } = 0.9;
    /// <summary>Weight of the subtitle. Default 0.7.</summary>
    public double Subtitle { get; set; } = 0.7;
    /// <summary>Weight of the group heading. Default 0.5.</summary>
    public double Group { get; set; } = 0.5;
    /// <summary>
    /// Weight of the titles of a nested command's ancestors, when it is found from the root page
    /// (<see cref="BaletteOptions.SearchNestedCommands"/>): <c>theme dark</c> finds "Dark" on the page of "Theme". Default 0.5.
    /// </summary>
    public double Path { get; set; } = 0.5;
    /// <summary>Weight of the description. Default 0.4.</summary>
    public double Description { get; set; } = 0.4;
    /// <summary>Weight of a tag. Default 0.3.</summary>
    public double Tag { get; set; } = 0.3;
}

/// <summary>Matches a query (possibly multi-term) against a whole command.</summary>
public static class BaletteMatcherExtensions
{
    /// <summary>
    /// Matches every whitespace-separated term of <paramref name="query"/> against the command's fields.
    /// All terms must match (AND). Each term's score is the best weighted field score.
    /// Supports fzf-style operators: <c>'exact</c>, <c>^prefix</c>, <c>suffix$</c>, <c>!negate</c>, and
    /// <c>"quoted phrases"</c> matched as one exact term, spaces included.
    /// </summary>
    public static BaletteMatch? Match(this IBaletteMatcher matcher, BaletteCommand command, string query, BaletteMatchWeights? weights = null)
        => matcher.Match(command, BaletteQuery.Parse(query), weights ?? DefaultWeights);

    /// <summary>Same as the string overload, for a query parsed once and matched against many commands.</summary>
    internal static BaletteMatch? Match(this IBaletteMatcher matcher, BaletteCommand command, BaletteQuery query, BaletteMatchWeights weights, string? path = null)
    {
        var terms = query.Terms;
        if (terms.Length == 0)
        {
            return new BaletteMatch(command, 0, [], [], null);
        }

        var normalize = Normalizes(matcher);
        double total = 0;
        int[]? titlePos = null;
        int[]? subtitlePos = null;
        string? matchedKeyword = null;

        foreach (var (term, op) in terms)
        {
            if (op == TermOperator.Negate)
            {
                if (AnyFieldContains(command, term, path, normalize))
                {
                    return null;
                }

                continue;
            }

            var best = double.NegativeInfinity;
            int[]? bestTitle = null;
            int[]? bestSubtitle = null;
            string? bestKeyword = null;

            // The best weighted field wins; only title/subtitle positions are kept for highlighting.
            if (Improves(matcher, term, op, command.Title, weights.Title, normalize, ref best, out var positions))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (positions, null, null);
            }

            var keywords = command.Keywords;
            for (var i = 0; i < keywords.Count; i++)
            {
                if (Improves(matcher, term, op, keywords[i], weights.Keyword, normalize, ref best, out _))
                {
                    (bestTitle, bestSubtitle, bestKeyword) = (null, null, keywords[i]);
                }
            }

            if (Improves(matcher, term, op, command.Subtitle, weights.Subtitle, normalize, ref best, out positions))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, positions, null);
            }

            if (Improves(matcher, term, op, command.Group, weights.Group, normalize, ref best, out _))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
            }

            if (Improves(matcher, term, op, path, weights.Path, normalize, ref best, out _))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
            }

            if (Improves(matcher, term, op, command.Description, weights.Description, normalize, ref best, out _))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
            }

            var tags = command.Tags;
            for (var i = 0; i < tags.Count; i++)
            {
                if (Improves(matcher, term, op, tags[i], weights.Tag, normalize, ref best, out _))
                {
                    (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
                }
            }

            if (double.IsNegativeInfinity(best))
            {
                return null;
            }

            total += best;
            if (bestTitle is { Length: > 0 })
            {
                titlePos = titlePos is null ? bestTitle : [.. titlePos, .. bestTitle];
            }

            if (bestSubtitle is { Length: > 0 })
            {
                subtitlePos = subtitlePos is null ? bestSubtitle : [.. subtitlePos, .. bestSubtitle];
            }

            matchedKeyword ??= bestKeyword;
        }

        return new BaletteMatch(command, total, SortedDistinct(titlePos), SortedDistinct(subtitlePos), matchedKeyword);
    }

    /// <summary>Whether the operators fold diacritics and keyboard variants: as the matcher does, and by default.</summary>
    private static bool Normalizes(IBaletteMatcher matcher) => matcher switch
    {
        BaletteFuzzyMatcher fuzzy => fuzzy.Normalize,
        BaletteSubstringMatcher substring => substring.Normalize,
        BaletteTypoTolerantMatcher typo => Normalizes(typo.Inner),
        _ => true,
    };

    /// <summary>Scores one field; when it beats <paramref name="best"/>, updates it and returns the field's positions.</summary>
    private static bool Improves(IBaletteMatcher matcher, string term, TermOperator op, string? text, double weight, bool normalize, ref double best, out int[] positions)
    {
        positions = [];
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var m = op == TermOperator.Fuzzy ? matcher.Match(term, text) : ExactMatch(term, text, op, normalize);
        if (!m.Matched)
        {
            return false;
        }

        var s = m.Score * weight;
        if (!(s > best))
        {
            return false;
        }

        best = s;
        positions = m.Positions;
        return true;
    }

    /// <summary>A sorted, de-duplicated copy (matchers usually return ascending positions already).</summary>
    private static int[] SortedDistinct(int[]? positions)
    {
        if (positions is null)
        {
            return [];
        }

        var copy = (int[])positions.Clone();
        for (var i = 1; i < copy.Length; i++)
        {
            if (copy[i] <= copy[i - 1])
            {
                return [.. new HashSet<int>(copy).Order()];
            }
        }

        return copy;
    }

    private static readonly BaletteMatchWeights DefaultWeights = new();

    internal enum TermOperator { Fuzzy, Exact, Prefix, Suffix, Negate }

    internal static (string Term, TermOperator Op) ParseOperator(string raw)
    {
        if (raw.Length > 1 && raw[0] == '!')
        {
            return (raw[1..], TermOperator.Negate);
        }

        if (raw.Length > 1 && raw[0] == '\'')
        {
            return (raw[1..], TermOperator.Exact);
        }

        if (raw.Length > 1 && raw[0] == '^')
        {
            return (raw[1..], TermOperator.Prefix);
        }

        if (raw.Length > 1 && raw[^1] == '$')
        {
            return (raw[..^1], TermOperator.Suffix);
        }

        return (raw, TermOperator.Fuzzy);
    }

    private static bool AnyFieldContains(BaletteCommand c, string term, string? path, bool normalize)
    {
        bool Contains(string? text) => text is not null && BaletteTextFold.Find(text, term, BaletteTextMatchKind.Anywhere, normalize) >= 0;

        if (Contains(c.Title) || Contains(c.Subtitle) || Contains(c.Group) || Contains(path))
        {
            return true;
        }

        foreach (var k in c.Keywords)
        {
            if (Contains(k))
            {
                return true;
            }
        }

        return false;
    }

    private static BaletteFieldMatch ExactMatch(string term, string text, TermOperator op, bool normalize)
    {
        var kind = op switch
        {
            TermOperator.Prefix => BaletteTextMatchKind.Prefix,
            TermOperator.Suffix => BaletteTextMatchKind.Suffix,
            _ => BaletteTextMatchKind.Anywhere,
        };
        var idx = BaletteTextFold.Find(text, term, kind, normalize);
        if (idx < 0)
        {
            return BaletteFieldMatch.None;
        }

        // Exact matches score like a perfect consecutive fuzzy run plus a boundary bonus.
        var score = term.Length * (BaletteFuzzyMatcher.ScoreMatch + BaletteFuzzyMatcher.BonusConsecutive) + (idx == 0 ? BaletteFuzzyMatcher.BonusBoundary * 2 : 0);
        return new BaletteFieldMatch(true, score, Enumerable.Range(idx, term.Length).ToArray());
    }
}

/// <summary>A query split into terms with their operators: parsed once per keystroke, matched against every command.</summary>
internal sealed class BaletteQuery
{
    public static BaletteQuery Empty { get; } = new([]);

    private BaletteQuery((string Term, BaletteMatcherExtensions.TermOperator Op)[] terms) => Terms = terms;

    public (string Term, BaletteMatcherExtensions.TermOperator Op)[] Terms { get; }

    /// <summary>True when at least one term is matched by the matcher (not by an operator).</summary>
    public bool HasFuzzyTerm(int minLength = 1)
    {
        foreach (var (term, op) in Terms)
        {
            if (op == BaletteMatcherExtensions.TermOperator.Fuzzy && term.Length >= minLength)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Splits on whitespace into terms with fzf's operators. A double-quoted phrase - straight or curly quotes, which
    /// some keyboards type - is one exact term with its spaces; <c>!"..."</c> excludes it, <c>^"..."</c> and <c>"..."$</c>
    /// anchor it. A quote left open takes the rest of the query, so typing one never shows "no results" midway.
    /// </summary>
    public static BaletteQuery Parse(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Empty;
        }

        var terms = new List<(string, BaletteMatcherExtensions.TermOperator)>();
        var i = 0;
        while (i < query.Length)
        {
            while (i < query.Length && char.IsWhiteSpace(query[i]))
            {
                i++;
            }

            if (i == query.Length)
            {
                break;
            }

            // A phrase, possibly behind one operator character.
            var prefix = query[i] is '!' or '^' or '\'' && i + 1 < query.Length && IsQuote(query[i + 1]) ? query[i] : '\0';
            var open = prefix == '\0' ? i : i + 1;
            if (IsQuote(query[open]))
            {
                var close = open + 1;
                while (close < query.Length && !IsQuote(query[close]))
                {
                    close++;
                }

                var phrase = query[(open + 1)..close].Trim();
                i = close < query.Length ? close + 1 : close;
                var suffix = i < query.Length && query[i] == '$' && (i + 1 == query.Length || char.IsWhiteSpace(query[i + 1]));
                if (suffix)
                {
                    i++;
                }

                if (phrase.Length > 0)
                {
                    terms.Add((phrase, prefix switch
                    {
                        '!' => BaletteMatcherExtensions.TermOperator.Negate,
                        '^' => BaletteMatcherExtensions.TermOperator.Prefix,
                        _ when suffix => BaletteMatcherExtensions.TermOperator.Suffix,
                        _ => BaletteMatcherExtensions.TermOperator.Exact,
                    }));
                }

                continue;
            }

            var start = i;
            while (i < query.Length && !char.IsWhiteSpace(query[i]))
            {
                i++;
            }

            var (term, op) = BaletteMatcherExtensions.ParseOperator(query[start..i]);
            if (term.Length > 0)
            {
                terms.Add((term, op));
            }
        }

        return new([.. terms]);
    }

    private static bool IsQuote(char c) => c is '"' or '“' or '”' or '„';
}

/// <summary>
/// The fallback a query that matched nothing gets a second chance with (<see cref="BaletteOptions.TypoTolerance"/>):
/// the palette's matcher first, then an approximate match that allows one edit in a term of 4 to 7 characters and two
/// from 8 on - a missing, extra or wrong letter, or two letters swapped (<c>settigns</c> finds "Settings"). Scored below
/// every exact match, and only ever asked when nothing matched exactly, so it adds no noise to a query that works.
/// </summary>
internal sealed class BaletteTypoTolerantMatcher(IBaletteMatcher inner) : IBaletteMatcher
{
    /// <summary>The shortest term that may have a typo: shorter ones match too much with one edit.</summary>
    public const int MinTermLength = 4;

    /// <summary>What each edit costs, on the scale of <see cref="BaletteFuzzyMatcher.ScoreMatch"/>.</summary>
    public const int EditPenalty = 12;

    public IBaletteMatcher Inner { get; } = inner;

    public static int MaxEdits(int termLength) => termLength >= 8 ? 2 : termLength >= MinTermLength ? 1 : 0;

    public BaletteFieldMatch Match(string query, string text)
    {
        var exact = Inner.Match(query, text);
        if (exact.Matched || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
        {
            return exact;
        }

        var maxEdits = MaxEdits(query.Length);
        return maxEdits == 0 ? BaletteFieldMatch.None : Approximate(query, text, maxEdits, Inner is not BaletteFuzzyMatcher { Normalize: false });
    }

    /// <summary>
    /// The best alignment of the whole of <paramref name="query"/> with any part of <paramref name="text"/> by optimal
    /// string alignment (Levenshtein plus adjacent transpositions), with free start and end in the text. Positions are
    /// the text's characters that matched a query character exactly, swapped pairs included.
    /// </summary>
    internal static BaletteFieldMatch Approximate(string query, string text, int maxEdits, bool normalize)
    {
        int p = query.Length, t = text.Length;
        if (p - maxEdits > t)
        {
            return BaletteFieldMatch.None;
        }

        var strip = normalize && !BaletteTextFold.HasDiacritic(query);
        var q = new char[p];
        var x = new char[t];
        for (var i = 0; i < p; i++)
        {
            q[i] = BaletteTextFold.Fold(query[i], true, normalize, strip);
        }

        for (var j = 0; j < t; j++)
        {
            x[j] = BaletteTextFold.Fold(text[j], true, normalize, strip);
        }

        var w = t + 1;
        var d = new int[(p + 1) * w];
        for (var i = 1; i <= p; i++)
        {
            d[i * w] = i;
            for (var j = 1; j <= t; j++)
            {
                var v = Math.Min(d[((i - 1) * w) + j] + 1, d[(i * w) + j - 1] + 1);
                v = Math.Min(v, d[((i - 1) * w) + j - 1] + (q[i - 1] == x[j - 1] ? 0 : 1));
                if (i > 1 && j > 1 && q[i - 1] == x[j - 2] && q[i - 2] == x[j - 1])
                {
                    v = Math.Min(v, d[((i - 2) * w) + j - 2] + 1);
                }

                d[(i * w) + j] = v;
            }
        }

        var edits = int.MaxValue;
        var end = 0;
        for (var j = 1; j <= t; j++)
        {
            if (d[(p * w) + j] < edits)
            {
                edits = d[(p * w) + j];
                end = j;
            }
        }

        if (edits > maxEdits)
        {
            return BaletteFieldMatch.None;
        }

        // Walk the alignment back to collect the matched characters and where it starts.
        var positions = new List<int>(p);
        int qi = p, tj = end;
        while (qi > 0 && tj > 0)
        {
            var cur = d[(qi * w) + tj];
            if (q[qi - 1] == x[tj - 1] && cur == d[((qi - 1) * w) + tj - 1])
            {
                positions.Add(tj - 1);
                qi--;
                tj--;
            }
            else if (qi > 1 && tj > 1 && q[qi - 1] == x[tj - 2] && q[qi - 2] == x[tj - 1] && cur == d[((qi - 2) * w) + tj - 2] + 1)
            {
                positions.Add(tj - 1);
                positions.Add(tj - 2);
                qi -= 2;
                tj -= 2;
            }
            else if (cur == d[((qi - 1) * w) + tj - 1] + 1)
            {
                qi--;
                tj--;
            }
            else if (cur == d[((qi - 1) * w) + tj] + 1)
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
}
