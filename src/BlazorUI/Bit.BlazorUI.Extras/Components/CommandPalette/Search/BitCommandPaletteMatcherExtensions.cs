namespace Bit.BlazorUI;

/// <summary>Matches a query (possibly multi-term) against a whole command.</summary>
public static class BitCommandPaletteMatcherExtensions
{
    /// <summary>
    /// Matches every whitespace-separated term of <paramref name="query"/> against the command's fields.
    /// All terms must match (AND). Each term's score is the best weighted field score.
    /// Supports fzf-style operators: <c>'exact</c>, <c>^prefix</c>, <c>suffix$</c>, <c>!negate</c>,
    /// <c>"quoted phrases"</c> matched as one exact term, spaces included, and <c>a | b</c> alternatives, of which one
    /// has to match. See <see cref="BitCommandPaletteQuery.Parse"/>.
    /// </summary>
    public static BitCommandPaletteMatch? Match(this IBitCommandPaletteMatcher matcher, BitCommandPaletteCommand command, string query, BitCommandPaletteMatchWeights? weights = null)
        => matcher.Match(command, BitCommandPaletteQuery.Parse(query), weights ?? DefaultWeights, path: null);

    /// <summary>Same as the string overload, for a query parsed once with <see cref="BitCommandPaletteQuery.Parse"/> and matched against many commands.</summary>
    public static BitCommandPaletteMatch? Match(this IBitCommandPaletteMatcher matcher, BitCommandPaletteCommand command, BitCommandPaletteQuery query, BitCommandPaletteMatchWeights? weights = null)
        => matcher.Match(command, query, weights ?? DefaultWeights, path: null);

    /// <summary>The match itself, with the titles of a nested command's ancestors as one more field (<paramref name="path"/>).</summary>
    internal static BitCommandPaletteMatch? Match(this IBitCommandPaletteMatcher matcher, BitCommandPaletteCommand command, BitCommandPaletteQuery query, BitCommandPaletteMatchWeights weights, string? path)
    {
        var terms = query.TermArray;
        if (terms.Length == 0)
        {
            return query.IsTooLong ? null : new BitCommandPaletteMatch(command, 0, [], [], null);
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
                if (op == BitCommandPaletteQueryOperator.Negate)
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

        return new BitCommandPaletteMatch(command, total, SortedDistinct(titlePos, titleAll), SortedDistinct(subtitlePos, subtitleAll), matchedKeyword);
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
    private static double BestField(IBitCommandPaletteMatcher matcher, BitCommandPaletteCommand command, string term, BitCommandPaletteQueryOperator op, BitCommandPaletteMatchWeights weights, string? path, bool normalize,
        out int[]? bestTitle, out int[]? bestSubtitle, out string? bestKeyword)
    {
        var best = double.NegativeInfinity;
        (bestTitle, bestSubtitle, bestKeyword) = (null, null, null);

        // A literal term is searched in every field; whether it is ASCII is worked out once for all of them.
        var asciiTerm = op != BitCommandPaletteQueryOperator.Fuzzy && System.Text.Ascii.IsValid(term);

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
    internal static bool Normalizes(IBitCommandPaletteMatcher matcher) => matcher switch
    {
        BitCommandPaletteFuzzyMatcher fuzzy => fuzzy.Normalize,
        BitCommandPaletteSubstringMatcher substring => substring.Normalize,
        BitCommandPaletteTypoTolerantMatcher typo => Normalizes(typo.Inner),
        _ => true,
    };

    /// <summary>Scores one field; when it beats <paramref name="best"/>, updates it and returns the field's positions.</summary>
    private static bool Improves(IBitCommandPaletteMatcher matcher, string term, BitCommandPaletteQueryOperator op, bool asciiTerm, string? text, double weight, bool normalize, ref double best, out int[] positions)
    {
        positions = [];
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var m = op == BitCommandPaletteQueryOperator.Fuzzy ? matcher.Match(term, text) : ExactMatch(term, text, op, normalize, asciiTerm);
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

    private static readonly BitCommandPaletteMatchWeights DefaultWeights = new();

    internal static (string Term, BitCommandPaletteQueryOperator Op) ParseOperator(string raw)
    {
        if (raw.Length > 1 && raw[0] == '!')
        {
            return (raw[1..], BitCommandPaletteQueryOperator.Negate);
        }

        if (raw.Length > 1 && raw[0] == '\'')
        {
            return (raw[1..], BitCommandPaletteQueryOperator.Exact);
        }

        if (raw.Length > 1 && raw[0] == '^')
        {
            return (raw[1..], BitCommandPaletteQueryOperator.Prefix);
        }

        if (raw.Length > 1 && raw[^1] == '$')
        {
            return (raw[..^1], BitCommandPaletteQueryOperator.Suffix);
        }

        return (raw, BitCommandPaletteQueryOperator.Fuzzy);
    }

    private static bool AnyFieldContains(BitCommandPaletteCommand c, string term, string? path, bool normalize)
    {
        bool Contains(string? text) => text is not null && BitCommandPaletteTextFold.Find(text, term, BitCommandPaletteTextMatchKind.Anywhere, normalize) >= 0;

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

    private static BitCommandPaletteFieldMatch ExactMatch(string term, string text, BitCommandPaletteQueryOperator op, bool normalize, bool asciiTerm)
    {
        var kind = op switch
        {
            BitCommandPaletteQueryOperator.Prefix => BitCommandPaletteTextMatchKind.Prefix,
            BitCommandPaletteQueryOperator.Suffix => BitCommandPaletteTextMatchKind.Suffix,
            _ => BitCommandPaletteTextMatchKind.Anywhere,
        };
        var idx = BitCommandPaletteTextFold.Find(text, term, kind, normalize, asciiTerm);
        if (idx < 0)
        {
            return BitCommandPaletteFieldMatch.None;
        }

        // Exact matches score like a perfect consecutive fuzzy run plus a boundary bonus.
        var score = term.Length * (BitCommandPaletteFuzzyMatcher.ScoreMatch + BitCommandPaletteFuzzyMatcher.BonusConsecutive) + (idx == 0 ? BitCommandPaletteFuzzyMatcher.BonusBoundary * 2 : 0);
        return new BitCommandPaletteFieldMatch(true, score, Enumerable.Range(idx, term.Length).ToArray());
    }
}
