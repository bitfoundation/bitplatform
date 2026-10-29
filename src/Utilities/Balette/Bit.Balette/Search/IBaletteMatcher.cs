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
    /// Supports fzf-style operators: <c>'exact</c>, <c>^prefix</c>, <c>suffix$</c>, <c>!negate</c>,
    /// <c>"quoted phrases"</c> matched as one exact term, spaces included, and <c>a | b</c> alternatives, of which one
    /// has to match. See <see cref="BaletteQuery.Parse"/>.
    /// </summary>
    public static BaletteMatch? Match(this IBaletteMatcher matcher, BaletteCommand command, string query, BaletteMatchWeights? weights = null)
        => matcher.Match(command, BaletteQuery.Parse(query), weights ?? DefaultWeights, path: null);

    /// <summary>Same as the string overload, for a query parsed once with <see cref="BaletteQuery.Parse"/> and matched against many commands.</summary>
    public static BaletteMatch? Match(this IBaletteMatcher matcher, BaletteCommand command, BaletteQuery query, BaletteMatchWeights? weights = null)
        => matcher.Match(command, query, weights ?? DefaultWeights, path: null);

    /// <summary>The match itself, with the titles of a nested command's ancestors as one more field (<paramref name="path"/>).</summary>
    internal static BaletteMatch? Match(this IBaletteMatcher matcher, BaletteCommand command, BaletteQuery query, BaletteMatchWeights weights, string? path)
    {
        var terms = query.TermArray;
        if (terms.Length == 0)
        {
            return query.IsTooLong ? null : new BaletteMatch(command, 0, [], [], null);
        }

        var normalize = Normalizes(matcher);
        double total = 0;
        int[]? titlePos = null;
        int[]? subtitlePos = null;
        List<int>? titleAll = null;
        List<int>? subtitleAll = null;
        string? matchedKeyword = null;

        for (var start = 0; start < terms.Length;)
        {
            // One term, or alternatives joined by " | ": the best one that holds counts, and none holding fails the
            // command. A negated term holds when the text is absent, adding nothing.
            var end = start + 1;
            while (end < terms.Length && terms[end].Group == terms[start].Group)
            {
                end++;
            }

            var holds = false;
            var groupBest = double.NegativeInfinity;
            int[]? groupTitle = null;
            int[]? groupSubtitle = null;
            string? groupKeyword = null;
            for (var t = start; t < end; t++)
            {
                var (text, op, _) = terms[t];
                double score;
                int[]? termTitle = null;
                int[]? termSubtitle = null;
                string? termKeyword = null;
                if (op == BaletteQueryOperator.Negate)
                {
                    if (AnyFieldContains(command, text, path, normalize))
                    {
                        continue;
                    }

                    score = 0;
                }
                else
                {
                    score = BestField(matcher, command, text, op, weights, path, normalize, out termTitle, out termSubtitle, out termKeyword);
                    if (double.IsNegativeInfinity(score))
                    {
                        continue;
                    }
                }

                if (!holds || score > groupBest)
                {
                    (holds, groupBest, groupTitle, groupSubtitle, groupKeyword) = (true, score, termTitle, termSubtitle, termKeyword);
                }
            }

            if (!holds)
            {
                return null;
            }

            total += groupBest;
            if (groupTitle is { Length: > 0 })
            {
                Collect(ref titlePos, ref titleAll, groupTitle);
            }

            if (groupSubtitle is { Length: > 0 })
            {
                Collect(ref subtitlePos, ref subtitleAll, groupSubtitle);
            }

            matchedKeyword ??= groupKeyword;
            start = end;
        }

        return new BaletteMatch(command, total, SortedDistinct(titlePos, titleAll), SortedDistinct(subtitlePos, subtitleAll), matchedKeyword);
    }

    private static void Collect(ref int[]? first, ref List<int>? all, int[] positions)
    {
        if (first is null)
        {
            first = positions;
            return;
        }

        (all ??= [.. first]).AddRange(positions);
    }

    /// <summary>
    /// The best weighted score of one term over the command's fields, or negative infinity when none matches; only
    /// title and subtitle positions are kept, for highlighting, and the keyword that won, for the row to show.
    /// </summary>
    private static double BestField(IBaletteMatcher matcher, BaletteCommand command, string term, BaletteQueryOperator op, BaletteMatchWeights weights, string? path, bool normalize,
        out int[]? bestTitle, out int[]? bestSubtitle, out string? bestKeyword)
    {
        var best = double.NegativeInfinity;
        (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);

        // A literal term is searched in every field; whether it is ASCII is worked out once for all of them.
        var asciiTerm = op != BaletteQueryOperator.Fuzzy && System.Text.Ascii.IsValid(term);

        if (Improves(matcher, term, op, asciiTerm, command.Title, weights.Title, normalize, ref best, out var positions))
        {
            (bestTitle, bestSubtitle, bestKeyword) = (positions, null, null);
        }

        var keywords = command.Keywords;
        for (var i = 0; i < keywords.Count; i++)
        {
            if (Improves(matcher, term, op, asciiTerm, keywords[i], weights.Keyword, normalize, ref best, out _))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, null, keywords[i]);
            }
        }

        if (Improves(matcher, term, op, asciiTerm, command.Subtitle, weights.Subtitle, normalize, ref best, out positions))
        {
            (bestTitle, bestSubtitle, bestKeyword) = (null, positions, null);
        }

        if (Improves(matcher, term, op, asciiTerm, command.Group, weights.Group, normalize, ref best, out _))
        {
            (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
        }

        if (Improves(matcher, term, op, asciiTerm, path, weights.Path, normalize, ref best, out _))
        {
            (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
        }

        if (Improves(matcher, term, op, asciiTerm, command.Description, weights.Description, normalize, ref best, out _))
        {
            (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
        }

        var tags = command.Tags;
        for (var i = 0; i < tags.Count; i++)
        {
            if (Improves(matcher, term, op, asciiTerm, tags[i], weights.Tag, normalize, ref best, out _))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
            }
        }

        return best;
    }

    /// <summary>Whether the operators fold diacritics and keyboard variants: as the matcher does, and by default.</summary>
    internal static bool Normalizes(IBaletteMatcher matcher) => matcher switch
    {
        BaletteFuzzyMatcher fuzzy => fuzzy.Normalize,
        BaletteSubstringMatcher substring => substring.Normalize,
        BaletteTypoTolerantMatcher typo => Normalizes(typo.Inner),
        _ => true,
    };

    /// <summary>Scores one field; when it beats <paramref name="best"/>, updates it and returns the field's positions.</summary>
    private static bool Improves(IBaletteMatcher matcher, string term, BaletteQueryOperator op, bool asciiTerm, string? text, double weight, bool normalize, ref double best, out int[] positions)
    {
        positions = [];
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var m = op == BaletteQueryOperator.Fuzzy ? matcher.Match(term, text) : ExactMatch(term, text, op, normalize, asciiTerm);
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
    private static int[] SortedDistinct(int[]? positions, List<int>? all)
    {
        if (positions is null)
        {
            return [];
        }

        var copy = all is null ? (int[])positions.Clone() : all.ToArray();
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

    internal static (string Term, BaletteQueryOperator Op) ParseOperator(string raw)
    {
        if (raw.Length > 1 && raw[0] == '!')
        {
            return (raw[1..], BaletteQueryOperator.Negate);
        }

        if (raw.Length > 1 && raw[0] == '\'')
        {
            return (raw[1..], BaletteQueryOperator.Exact);
        }

        if (raw.Length > 1 && raw[0] == '^')
        {
            return (raw[1..], BaletteQueryOperator.Prefix);
        }

        if (raw.Length > 1 && raw[^1] == '$')
        {
            return (raw[..^1], BaletteQueryOperator.Suffix);
        }

        return (raw, BaletteQueryOperator.Fuzzy);
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

    private static BaletteFieldMatch ExactMatch(string term, string text, BaletteQueryOperator op, bool normalize, bool asciiTerm)
    {
        var kind = op switch
        {
            BaletteQueryOperator.Prefix => BaletteTextMatchKind.Prefix,
            BaletteQueryOperator.Suffix => BaletteTextMatchKind.Suffix,
            _ => BaletteTextMatchKind.Anywhere,
        };
        var idx = BaletteTextFold.Find(text, term, kind, normalize, asciiTerm);
        if (idx < 0)
        {
            return BaletteFieldMatch.None;
        }

        // Exact matches score like a perfect consecutive fuzzy run plus a boundary bonus.
        var score = term.Length * (BaletteFuzzyMatcher.ScoreMatch + BaletteFuzzyMatcher.BonusConsecutive) + (idx == 0 ? BaletteFuzzyMatcher.BonusBoundary * 2 : 0);
        return new BaletteFieldMatch(true, score, Enumerable.Range(idx, term.Length).ToArray());
    }
}

/// <summary>How a term of a query is matched.</summary>
public enum BaletteQueryOperator
{
    /// <summary>By the palette's matcher - fuzzy by default.</summary>
    Fuzzy,

    /// <summary><c>'term</c> or <c>"a phrase"</c>: the text itself, anywhere in a field.</summary>
    Exact,

    /// <summary><c>^term</c>: a field starts with it.</summary>
    Prefix,

    /// <summary><c>term$</c>: a field ends with it.</summary>
    Suffix,

    /// <summary><c>!term</c>: no field contains it.</summary>
    Negate,
}

/// <summary>
/// One term of a parsed query: its <paramref name="Text"/> without the operator characters, how it is matched, and its
/// <paramref name="Group"/> - terms sharing a group are alternatives (<c>a | b</c>), of which one has to hold.
/// </summary>
public readonly record struct BaletteQueryTerm(string Text, BaletteQueryOperator Operator, int Group);

/// <summary>
/// A query split into terms with their operators - what the palette parses once per keystroke and matches against every
/// command. Public for tools that explain a search and for matchers of your own.
/// </summary>
public sealed class BaletteQuery
{
    /// <summary>The query with no terms, which every command matches.</summary>
    public static BaletteQuery Empty { get; } = new([]);

    internal const int MaxLength = 256;

    private static readonly BaletteQuery TooLong = new([]) { IsTooLong = true };

    private BaletteQuery(BaletteQueryTerm[] terms) => TermArray = terms;

    /// <summary>The terms, in the order they were typed.</summary>
    public IReadOnlyList<BaletteQueryTerm> Terms => TermArray;

    /// <summary>The terms as an array, for the matching loop.</summary>
    internal BaletteQueryTerm[] TermArray { get; }

    internal bool IsTooLong { get; private init; }

    /// <summary>True when at least one term is matched by the matcher (not by an operator).</summary>
    internal bool HasFuzzyTerm(int minLength = 1)
    {
        foreach (var (text, op, _) in TermArray)
        {
            if (op == BaletteQueryOperator.Fuzzy && text.Length >= minLength)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Splits on whitespace into terms with fzf's operators: <c>'exact</c>, <c>^prefix</c>, <c>suffix$</c> and
    /// <c>!negate</c>. A double-quoted phrase - straight or curly quotes, which some keyboards type - is one exact term with
    /// its spaces; <c>!"..."</c> excludes it, <c>^"..."</c> and <c>"..."$</c> anchor it. A quote left open takes the rest of
    /// the query, so typing one never shows "no results" midway. A <c>|</c> standing on its own between two terms makes
    /// them alternatives, as in fzf: <c>^go | ^open settings</c> is (starts with go, or with open) and settings.
    /// </summary>
    public static BaletteQuery Parse(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Empty;
        }

        if (query.Length > MaxLength)
        {
            return TooLong;
        }

        var terms = new List<BaletteQueryTerm>();
        var group = -1;
        var orNext = false;

        void Add(string text, BaletteQueryOperator op)
        {
            // An alternative joins the group of the term before it; anything else starts a group of its own.
            if (!(orNext && terms.Count > 0))
            {
                group++;
            }

            orNext = false;
            terms.Add(new BaletteQueryTerm(text, op, group));
        }

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
                    Add(phrase, prefix switch
                    {
                        '!' => BaletteQueryOperator.Negate,
                        '^' => BaletteQueryOperator.Prefix,
                        _ when suffix => BaletteQueryOperator.Suffix,
                        _ => BaletteQueryOperator.Exact,
                    });
                }

                continue;
            }

            var start = i;
            while (i < query.Length && !char.IsWhiteSpace(query[i]))
            {
                i++;
            }

            if (i - start == 1 && query[start] == '|')
            {
                // Between two terms: the next one is an alternative to the last. Leading or trailing, it is ignored.
                orNext = terms.Count > 0;
                continue;
            }

            var (term, op) = BaletteMatcherExtensions.ParseOperator(query[start..i]);
            if (term.Length > 0)
            {
                Add(term, op);
            }
        }

        return new([.. terms]);
    }

    private static bool IsQuote(char c) => c is '"' or '\u201C' or '\u201D' or '\u201E';
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
        return maxEdits == 0 ? BaletteFieldMatch.None : Approximate(query, text, maxEdits, BaletteMatcherExtensions.Normalizes(Inner));
    }

    /// <summary>
    /// The best alignment of the whole of <paramref name="query"/> with any part of <paramref name="text"/> by optimal
    /// string alignment (Levenshtein plus adjacent transpositions), with free start and end in the text. Positions are
    /// the text's characters that matched a query character exactly, swapped pairs included.
    /// </summary>
    internal static BaletteFieldMatch Approximate(string query, string text, int maxEdits, bool normalize)
    {
        int p = query.Length, t = text.Length;
        if (p - maxEdits > t || (long)(p + 1) * (t + 1) > BaletteFuzzyMatcher.MaxCells)
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
