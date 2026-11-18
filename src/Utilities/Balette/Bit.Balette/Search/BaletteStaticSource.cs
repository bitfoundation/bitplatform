namespace Bit.Balette;

/// <summary>A source backed by an in-memory list, filtered with the palette's matcher (useful for large static datasets).</summary>
public sealed class BaletteStaticSource : IBaletteSource
{
    private readonly Func<IEnumerable<BaletteCommand>> _items;

    /// <summary>Creates a source over a fixed list.</summary>
    public BaletteStaticSource(string name, IEnumerable<BaletteCommand> items, int maxResults = 50)
        : this(name, () => items, maxResults)
    {
    }

    /// <summary>Creates a source over a list read again on every search.</summary>
    public BaletteStaticSource(string name, Func<IEnumerable<BaletteCommand>> items, int maxResults = 50)
    {
        Name = name;
        _items = items;
        MaxResults = maxResults;
    }

    /// <summary>The group heading of the source's results.</summary>
    public string Name { get; }

    /// <summary>Always 1.</summary>
    public int MinQueryLength => 1;

    /// <summary>The most results returned.</summary>
    public int MaxResults { get; }

    /// <summary>Always true: the list is matched and ranked by the palette's matcher.</summary>
    public bool ApplyMatcher => true;

    /// <summary>
    /// Matches the list with the palette's matcher and returns the best <see cref="MaxResults"/> of it, best first.
    /// The palette keeps only <see cref="MaxResults"/> of what a source returns, so handing it the unfiltered list
    /// would let only its first items ever be found.
    /// </summary>
    public Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken)
    {
        var options = context.Services.GetService(typeof(BaletteOptions)) as BaletteOptions;
        var matcher = options?.Matcher ?? BaletteFuzzyMatcher.Default;
        var weights = options?.Weights ?? new BaletteMatchWeights();
        var parsed = BaletteQuery.Parse(query);

        var matches = new List<BaletteMatch>();
        foreach (var command in _items())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (matcher.Match(command, parsed, weights) is { } match)
            {
                matches.Add(match);
            }
        }

        IReadOnlyList<BaletteCommand> best = [.. matches.OrderByDescending(m => m.Score).Take(MaxResults).Select(m => m.Command)];
        return Task.FromResult(best);
    }
}
