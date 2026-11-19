using System.Globalization;

namespace Bit.BevTools;

/// <summary>Parses the small query language shared by the panels: free text plus key:value tokens.</summary>
public sealed class BevToolsQueryFilter
{
    private BevToolsQueryFilter()
    {
    }

    public List<string> Terms { get; } = [];

    public Dictionary<string, string> Tokens { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static BevToolsQueryFilter Parse(string? query)
    {
        var filter = new BevToolsQueryFilter();
        if (string.IsNullOrWhiteSpace(query))
        {
            return filter;
        }

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

    public bool MatchesText(params string?[] fields)
    {
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

    public string? Token(string name) => Tokens.TryGetValue(name, out var value) ? value : null;

    public int? IntToken(string name) => int.TryParse(Token(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    public long? LongToken(string name) => long.TryParse(Token(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
}
