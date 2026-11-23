using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BevTools;

/// <summary>
/// Parses the small query language shared by the panels: free text plus key:value tokens, and one regular expression
/// between slashes (<c>/time(d|o)ut/</c>), which may hold spaces.
/// </summary>
public sealed class BevToolsQueryFilter
{
    /// <summary>
    /// How long one match of <see cref="Regex"/> may take: a pattern that backtracks without end must not freeze the
    /// panel, which in WebAssembly runs on the app's only thread. An entry it times out on does not match.
    /// </summary>
    public static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(50);

    private BevToolsQueryFilter()
    {
    }

    public List<string> Terms { get; } = [];

    public Dictionary<string, string> Tokens { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The regular expression written between slashes - <c>/pattern/</c>, starting a word and ending one - matched
    /// case-insensitively against the same text the free text terms are, and in addition to them. Null when there is none,
    /// or when it is not a valid one (<see cref="RegexError"/>).
    /// </summary>
    public Regex? Regex { get; private set; }

    /// <summary>Why the regular expression between slashes is not valid; while it is set, nothing matches the text.</summary>
    public string? RegexError { get; private set; }

    /// <summary>The regular expression as it was written, slashes included (<c>/pattern/</c>), valid or not.</summary>
    public string? RegexText { get; private set; }

    /// <summary>
    /// Whether the query filters by text: a free text term or a regular expression. Panels that show more when nothing is
    /// searched for (collapsed nodes, framework services) go by it.
    /// </summary>
    public bool HasText => Terms.Count > 0 || RegexText is not null;

    /// <summary>Whether <see cref="Regex"/> ran out of <see cref="RegexTimeout"/> on some text, which then did not match.</summary>
    public bool RegexTimedOut { get; private set; }

    public static BevToolsQueryFilter Parse(string? query)
    {
        var filter = new BevToolsQueryFilter();
        if (string.IsNullOrWhiteSpace(query))
        {
            return filter;
        }

        query = filter.TakeRegex(query);
        foreach (var part in query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = part.IndexOf(':');
            var gt = part.IndexOf('>');
            if (colon > 0 && colon < part.Length - 1 && !part.AsSpan(colon + 1).StartsWith("//", StringComparison.Ordinal))
            {
                filter.Tokens[part[..colon]] = part[(colon + 1)..];
            }
            else if (gt > 0 && gt < part.Length - 1)
            {
                filter.Tokens[part[..gt] + ">"] = part[(gt + 1)..];
            }
            else
            {
                filter.Terms.Add(part);
            }
        }

        return filter;
    }

    /// <summary>
    /// Takes the first <c>/pattern/</c> out of <paramref name="query"/>: a slash that starts a word, up to the last slash
    /// that ends one. A path such as <c>/counter</c> or <c>/api/orders</c> ends in no slash, and stays a search term.
    /// </summary>
    private string TakeRegex(string query)
    {
        for (var start = 0; start < query.Length; start++)
        {
            if (query[start] != '/' || (start > 0 && query[start - 1] != ' '))
            {
                continue;
            }

            for (var end = query.Length - 1; end > start + 1; end--)
            {
                if (query[end] != '/' || (end < query.Length - 1 && query[end + 1] != ' '))
                {
                    continue;
                }

                var pattern = query[(start + 1)..end];
                RegexText = query[start..(end + 1)];
                try
                {
                    Regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
                }
                catch (ArgumentException ex)
                {
                    RegexError = ex.Message;
                }

                return query[..start] + " " + query[(end + 1)..];
            }

            break;
        }

        return query;
    }

    public bool MatchesText(params string?[] fields)
    {
        if (RegexError is not null)
        {
            return false;
        }

        if (Regex is { } regex && !MatchesRegex(regex, fields))
        {
            return false;
        }

        if (Terms.Count == 0)
        {
            return true;
        }

        foreach (var term in Terms)
        {
            var found = false;
            foreach (var field in fields)
            {
                if (field is not null && field.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private bool MatchesRegex(Regex regex, string?[] fields)
    {
        foreach (var field in fields)
        {
            if (field is null)
            {
                continue;
            }

            try
            {
                if (regex.IsMatch(field))
                {
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                RegexTimedOut = true;
            }
        }

        return false;
    }

    public string? Token(string name) => Tokens.TryGetValue(name, out var value) ? value : null;

    public int? IntToken(string name) => int.TryParse(Token(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    public long? LongToken(string name) => long.TryParse(Token(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
}
