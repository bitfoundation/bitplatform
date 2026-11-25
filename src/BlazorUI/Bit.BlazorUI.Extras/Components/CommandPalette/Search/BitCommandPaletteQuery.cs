namespace Bit.BlazorUI;

/// <summary>
/// A query split into terms with their operators - what the palette parses once per keystroke and matches against every
/// command. Public for tools that explain a search and for matchers of your own.
/// </summary>
public sealed class BitCommandPaletteQuery
{
    /// <summary>The query with no terms, which every command matches.</summary>
    public static BitCommandPaletteQuery Empty { get; } = new([]);

    /// <summary>
    /// The longest query <see cref="Parse"/> searches, the spaces around it aside - in the palette, what follows a mode's
    /// prefix: matching costs grow with the query's length, and nobody types more than this to find a command. A longer
    /// one matches no command (<see cref="IsTooLong"/>), but it is not cut short: the palette still lists its
    /// <see cref="BitCommandPaletteCommand.AlwaysShow"/> commands, and async sources and handlers
    /// (<see cref="BitCommandPaletteCommandContext.Query"/>) still get all of it. A property rather than a constant, so code
    /// compiled against it reads the value of the library it runs with.
    /// </summary>
    public static int MaxLength => 256;

    private static readonly BitCommandPaletteQuery TooLong = new([]) { IsTooLong = true };

    private BitCommandPaletteQuery(BitCommandPaletteQueryTerm[] terms) => TermArray = terms;

    /// <summary>The terms, in the order they were typed.</summary>
    public IReadOnlyList<BitCommandPaletteQueryTerm> Terms => TermArray;

    /// <summary>The terms as an array, for the matching loop.</summary>
    internal BitCommandPaletteQueryTerm[] TermArray { get; }

    /// <summary>
    /// True when the query was longer than <see cref="MaxLength"/>: it has no <see cref="Terms"/>, like
    /// <see cref="Empty"/>, but where the empty query matches every command this one matches none.
    /// </summary>
    public bool IsTooLong { get; private init; }

    /// <summary>True when at least one term is matched by the matcher (not by an operator).</summary>
    internal bool HasFuzzyTerm(int minLength = 1)
    {
        foreach (var (text, op, _) in TermArray)
        {
            if (op == BitCommandPaletteQueryOperator.Fuzzy && text.Length >= minLength)
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
    /// them alternatives, as in fzf: <c>^go | ^open settings</c> is (starts with go, or with open) and settings. A query
    /// longer than <see cref="MaxLength"/> characters, surrounding spaces aside, has no terms and matches nothing
    /// (<see cref="IsTooLong"/>).
    /// </summary>
    public static BitCommandPaletteQuery Parse(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Empty;
        }

        if (query.AsSpan().Trim().Length > MaxLength)
        {
            return TooLong;
        }

        var terms = new List<BitCommandPaletteQueryTerm>();
        var group = -1;
        var orNext = false;

        void Add(string text, BitCommandPaletteQueryOperator op)
        {
            // An alternative joins the group of the term before it; anything else starts a group of its own.
            if (!(orNext && terms.Count > 0))
            {
                group++;
            }

            orNext = false;
            terms.Add(new BitCommandPaletteQueryTerm(text, op, group));
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
                        '!' => BitCommandPaletteQueryOperator.Negate,
                        '^' => BitCommandPaletteQueryOperator.Prefix,
                        _ when suffix => BitCommandPaletteQueryOperator.Suffix,
                        _ => BitCommandPaletteQueryOperator.Exact,
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

            var (term, op) = BitCommandPaletteMatcherExtensions.ParseOperator(query[start..i]);
            if (term.Length > 0)
            {
                Add(term, op);
            }
        }

        return new([.. terms]);
    }

    private static bool IsQuote(char c) => c is '"' or '\u201C' or '\u201D' or '\u201E';
}
