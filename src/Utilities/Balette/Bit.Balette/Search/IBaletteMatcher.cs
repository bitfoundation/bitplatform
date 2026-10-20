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
    /// Supports fzf-style operators: <c>'exact</c>, <c>^prefix</c>, <c>suffix$</c>, <c>!negate</c>.
    /// </summary>
    public static BaletteMatch? Match(this IBaletteMatcher matcher, BaletteCommand command, string query, BaletteMatchWeights? weights = null)
        => matcher.Match(command, BaletteQuery.Parse(query), weights ?? DefaultWeights);

    /// <summary>Same as the string overload, for a query parsed once and matched against many commands.</summary>
    internal static BaletteMatch? Match(this IBaletteMatcher matcher, BaletteCommand command, BaletteQuery query, BaletteMatchWeights weights)
    {
        var terms = query.Terms;
        if (terms.Length == 0)
        {
            return new BaletteMatch(command, 0, [], [], null);
        }

        double total = 0;
        int[]? titlePos = null;
        int[]? subtitlePos = null;
        string? matchedKeyword = null;

        foreach (var (term, op) in terms)
        {
            if (op == TermOperator.Negate)
            {
                if (AnyFieldContains(command, term))
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
            if (Improves(matcher, term, op, command.Title, weights.Title, ref best, out var positions))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (positions, null, null);
            }

            var keywords = command.Keywords;
            for (var i = 0; i < keywords.Count; i++)
            {
                if (Improves(matcher, term, op, keywords[i], weights.Keyword, ref best, out _))
                {
                    (bestTitle, bestSubtitle, bestKeyword) = (null, null, keywords[i]);
                }
            }

            if (Improves(matcher, term, op, command.Subtitle, weights.Subtitle, ref best, out positions))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, positions, null);
            }

            if (Improves(matcher, term, op, command.Group, weights.Group, ref best, out _))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
            }

            if (Improves(matcher, term, op, command.Description, weights.Description, ref best, out _))
            {
                (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);
            }

            var tags = command.Tags;
            for (var i = 0; i < tags.Count; i++)
            {
                if (Improves(matcher, term, op, tags[i], weights.Tag, ref best, out _))
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

    /// <summary>Scores one field; when it beats <paramref name="best"/>, updates it and returns the field's positions.</summary>
    private static bool Improves(IBaletteMatcher matcher, string term, TermOperator op, string? text, double weight, ref double best, out int[] positions)
    {
        positions = [];
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var m = op == TermOperator.Fuzzy ? matcher.Match(term, text) : ExactMatch(term, text, op);
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

    private static bool AnyFieldContains(BaletteCommand c, string term)
    {
        var cmp = StringComparison.OrdinalIgnoreCase;
        return c.Title.Contains(term, cmp)
            || (c.Subtitle?.Contains(term, cmp) ?? false)
            || (c.Group?.Contains(term, cmp) ?? false)
            || c.Keywords.Any(k => k.Contains(term, cmp));
    }

    private static BaletteFieldMatch ExactMatch(string term, string text, TermOperator op)
    {
        var cmp = StringComparison.OrdinalIgnoreCase;
        int idx = op switch
        {
            TermOperator.Prefix => text.StartsWith(term, cmp) ? 0 : -1,
            TermOperator.Suffix => text.EndsWith(term, cmp) ? text.Length - term.Length : -1,
            _ => text.IndexOf(term, cmp),
        };

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

    public static BaletteQuery Parse(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Empty;
        }

        var terms = new List<(string, BaletteMatcherExtensions.TermOperator)>();
        foreach (var raw in query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var (term, op) = BaletteMatcherExtensions.ParseOperator(raw);
            if (term.Length > 0)
            {
                terms.Add((term, op));
            }
        }

        return new([.. terms]);
    }
}
