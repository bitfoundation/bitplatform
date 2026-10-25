
namespace Bit.BevTools;

public sealed record BevCommandMatch(IBevToolsCommand Command, int Score, string? Argument);

/// <summary>Command palette backend with lightweight fuzzy matching.</summary>
internal sealed class CommandRegistry
{
    private readonly List<IBevToolsCommand> _commands = [];

    public IReadOnlyList<IBevToolsCommand> Commands => _commands;

    public void Add(IBevToolsCommand command)
    {
        if (_commands.All(c => c.Id != command.Id))
        {
            _commands.Add(command);
        }
    }

    public void AddRange(IEnumerable<IBevToolsCommand> commands)
    {
        foreach (var command in commands)
        {
            Add(command);
        }
    }

    public IBevToolsCommand? Find(string id) => _commands.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<BevCommandMatch> Search(string? query, int limit = 12)
    {
        query = query?.Trim() ?? string.Empty;
        if (query.StartsWith('>'))
        {
            query = query[1..].Trim();
        }

        if (query.Length == 0)
        {
            return _commands.OrderBy(c => c.Category).ThenBy(c => c.Title).Take(limit).Select(c => new BevCommandMatch(c, 0, null)).ToList();
        }

        var results = new List<BevCommandMatch>();
        foreach (var command in _commands)
        {
            var (score, argument) = Score(command, query);
            if (score > 0)
            {
                results.Add(new BevCommandMatch(command, score, argument));
            }
        }

        return results.OrderByDescending(r => r.Score).ThenBy(r => r.Command.Title).Take(limit).ToList();
    }

    private static (int Score, string? Argument) Score(IBevToolsCommand command, string query)
    {
        var title = command.Title;
        if (title.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return (1000, null);
        }

        if (title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return (800 - (title.Length - query.Length), null);
        }

        if (command.AcceptsArgument && query.StartsWith(title, StringComparison.OrdinalIgnoreCase) && query.Length > title.Length)
        {
            return (900, query[title.Length..].Trim());
        }

        if (command.AcceptsArgument)
        {
            // "find ProductList" matches "Find component" with argument "ProductList" when every title word prefix appears in order.
            var titleWords = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (queryWords.Length >= 2 && titleWords.Length > 0 && titleWords[0].StartsWith(queryWords[0], StringComparison.OrdinalIgnoreCase))
            {
                var consumed = 1;
                for (var i = 1; i < titleWords.Length && consumed < queryWords.Length - 1; i++)
                {
                    if (titleWords[i].StartsWith(queryWords[consumed], StringComparison.OrdinalIgnoreCase))
                    {
                        consumed++;
                    }
                }

                var argument = string.Join(' ', queryWords.Skip(consumed));
                if (argument.Length > 0)
                {
                    return (500, argument);
                }
            }
        }

        var best = 0;
        foreach (var word in query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var wordScore = 0;
            if (title.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                wordScore = 300;
            }
            else if (command.Category.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                wordScore = 200;
            }
            else if (command.Keywords.Any(k => k.Contains(word, StringComparison.OrdinalIgnoreCase)))
            {
                wordScore = 250;
            }
            else if (IsSubsequence(word, title))
            {
                wordScore = 100;
            }

            if (wordScore == 0)
            {
                return (0, null);
            }

            best += wordScore;
        }

        return (best, null);
    }

    internal static bool IsSubsequence(string needle, string haystack)
    {
        var n = 0;
        foreach (var ch in haystack)
        {
            if (n < needle.Length && char.ToLowerInvariant(ch) == char.ToLowerInvariant(needle[n]))
            {
                n++;
            }
        }

        return n == needle.Length;
    }
}
