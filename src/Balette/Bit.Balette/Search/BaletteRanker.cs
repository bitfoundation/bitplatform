namespace Bit.Balette;

/// <summary>Combines match score, priority and frecency into a final rank and sorts results.</summary>
public sealed class BaletteRanker
{
    private static readonly StringComparer TitleComparer = StringComparer.CurrentCultureIgnoreCase;

    private readonly BaletteOptions _options;

    /// <summary>Creates a ranker using the weights of <paramref name="options"/>.</summary>
    public BaletteRanker(BaletteOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Ranks matches. With a non-empty query: match score dominates, frecency and priority nudge, and equal ranks go
    /// by title. With an empty query: priority, then frecency, then the order the commands were registered or
    /// declared in - an author who lists Auto, Light and Dark meant that order.
    /// </summary>
    public List<BaletteMatch> Rank(IEnumerable<BaletteMatch> matches, bool hasQuery, Func<string, double> frecencyOf)
        => Rank(matches, hasQuery, frecencyOf, int.MaxValue);

    /// <summary>
    /// Same ordering as <see cref="Rank(IEnumerable{BaletteMatch}, bool, Func{string, double})"/>, keeping only the best
    /// <paramref name="limit"/> results: only those are sorted and copied, which keeps big registries cheap per keystroke.
    /// </summary>
    internal List<BaletteMatch> Rank(IEnumerable<BaletteMatch> matches, bool hasQuery, Func<string, double> frecencyOf, int limit)
    {
        var items = matches as IReadOnlyList<BaletteMatch> ?? matches.ToList();
        var n = items.Count;
        limit = Math.Max(0, limit);
        var ranks = new double[n];
        var frecencies = new double[n];
        for (var i = 0; i < n; i++)
        {
            var m = items[i];
            var frec = frecencyOf(m.Command.Id);
            ranks[i] = hasQuery
                ? m.Score + m.Command.Priority * _options.PriorityWeight + Math.Log(1 + frec) * _options.FrecencyWeight
                : m.Command.Priority * 1000 + frec;
            frecencies[i] = frec;
        }

        // Rank descending, then (while searching) title, then original position - a stable sort.
        int Compare(int a, int b)
        {
            var c = ranks[b].CompareTo(ranks[a]);
            if (c == 0 && hasQuery)
            {
                c = TitleComparer.Compare(items[a].Command.Title, items[b].Command.Title);
            }

            return c != 0 ? c : a.CompareTo(b);
        }

        int[] order;
        // A heap only pays for itself when it drops most of the matches: just above the limit, sorting
        // everything and keeping the front is cheaper than maintaining the heap.
        if (limit >= n / 2)
        {
            order = new int[n];
            for (var i = 0; i < n; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, Compare);
            if (limit < n)
            {
                order = order[..limit];
            }
        }
        else
        {
            // Keep the best `limit` indexes in a heap whose root is the worst one kept.
            var heap = new PriorityQueue<int, int>(limit, Comparer<int>.Create((a, b) => Compare(b, a)));
            for (var i = 0; i < n; i++)
            {
                if (heap.Count < limit)
                {
                    heap.Enqueue(i, i);
                }
                else if (limit > 0 && Compare(i, heap.Peek()) < 0)
                {
                    heap.DequeueEnqueue(i, i);
                }
            }

            order = new int[heap.Count];
            for (var i = 0; heap.TryDequeue(out var index, out _); i++)
            {
                order[i] = index;
            }

            Array.Sort(order, Compare);
        }

        var ranked = new List<BaletteMatch>(order.Length);
        foreach (var i in order)
        {
            ranked.Add(items[i] with { Rank = ranks[i], Frecency = frecencies[i] });
        }

        return ranked;
    }
}
