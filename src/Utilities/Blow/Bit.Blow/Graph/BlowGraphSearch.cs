using System.Globalization;
using System.Text;

namespace Bit.Blow;

/// <summary>
/// Finds nodes and edges by what they say - labels, ids, types, port names and any text of your own - for a search
/// box, a "go to" command or a command palette. Case and accents are ignored, every word of the query has to match
/// somewhere, and the best matches come first: a whole field equal to the query, then one starting with it, then a
/// word starting with it, then the query anywhere, then the words of the query found apart. Elements that match
/// equally well keep their order in the graph.
/// </summary>
/// <remarks>
/// Text is compared, never interpreted: the query is not a pattern, so nothing in it can make a search expensive.
/// </remarks>
public static class BlowGraphSearch
{
    /// <summary>The nodes matching <paramref name="query"/>, best matches first.</summary>
    /// <param name="nodes">The nodes to search.</param>
    /// <param name="query">What to look for. Empty or white space finds nothing.</param>
    /// <param name="text">
    /// More text to search for each node - a description in its data, a value it shows. Searched after the node's own
    /// fields: its aria label, label, id and type, and its ports' labels, ids and data types.
    /// </param>
    public static IReadOnlyList<BlowNode> FindNodes(IEnumerable<BlowNode> nodes, string query, Func<BlowNode, string?>? text = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return Find(nodes, query, node => NodeFields(node, text));
    }

    /// <summary>The edges matching <paramref name="query"/>, best matches first.</summary>
    /// <param name="edges">The edges to search.</param>
    /// <param name="query">What to look for. Empty or white space finds nothing.</param>
    /// <param name="nodeName">
    /// The name of a node from its id, so an edge can be found by the nodes it joins as they are called on screen;
    /// without it, by their ids.
    /// </param>
    /// <param name="text">More text to search for each edge, after its label, id and the names of its two nodes.</param>
    public static IReadOnlyList<BlowEdge> FindEdges(IEnumerable<BlowEdge> edges, string query,
        Func<string, string>? nodeName = null, Func<BlowEdge, string?>? text = null)
    {
        ArgumentNullException.ThrowIfNull(edges);
        return Find(edges, query, edge => EdgeFields(edge, nodeName, text));
    }

    private static IEnumerable<string?> NodeFields(BlowNode node, Func<BlowNode, string?>? text)
    {
        yield return node.AriaLabel;
        yield return node.Label;
        yield return node.Id;
        yield return node.Type;
        if (node.Ports is { } ports)
        {
            foreach (var port in ports)
            {
                yield return port.Label;
                yield return port.Id;
                yield return port.DataType;
            }
        }
        if (text is not null) yield return text(node);
    }

    private static IEnumerable<string?> EdgeFields(BlowEdge edge, Func<string, string>? nodeName, Func<BlowEdge, string?>? text)
    {
        yield return edge.Label;
        yield return edge.Id;
        yield return nodeName?.Invoke(edge.Source) ?? edge.Source;
        yield return nodeName?.Invoke(edge.Target) ?? edge.Target;
        if (text is not null) yield return text(edge);
    }

    private static List<T> Find<T>(IEnumerable<T> elements, string query, Func<T, IEnumerable<string?>> fields)
    {
        var folded = Fold(query ?? "").Trim();
        if (folded.Length == 0) return [];
        var words = folded.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var found = new List<(T Element, int Rank)>();
        foreach (var element in elements)
        {
            var texts = fields(element).Where(f => !string.IsNullOrEmpty(f)).Select(f => Fold(f!)).ToList();
            if (Rank(texts, folded, words) is { } rank) found.Add((element, rank));
        }

        // OrderBy is stable: equally good matches keep the graph's order.
        return found.OrderBy(f => f.Rank).Select(f => f.Element).ToList();
    }

    /// <summary>How well the fields match: 0 is best; null when they do not match at all.</summary>
    private static int? Rank(List<string> fields, string query, string[] words)
    {
        var best = int.MaxValue;
        foreach (var field in fields)
        {
            var at = field.IndexOf(query, StringComparison.Ordinal);
            if (at < 0) continue;

            var rank = field.Length == query.Length ? 0
                : at == 0 ? 1
                : StartsAWord(field, query) ? 2
                : 3;
            best = Math.Min(best, rank);
        }
        if (best != int.MaxValue) return best;

        // The words of the query, each found somewhere: "sum input" finds the node "Sum" with an "input" port.
        return words.Length > 1 && words.All(w => fields.Any(f => f.Contains(w, StringComparison.Ordinal))) ? 4 : null;
    }

    private static bool StartsAWord(string field, string query)
    {
        for (var at = field.IndexOf(query, StringComparison.Ordinal); at >= 0;
             at = field.IndexOf(query, at + 1, StringComparison.Ordinal))
        {
            if (at == 0 || !char.IsLetterOrDigit(field[at - 1])) return true;
        }
        return false;
    }

    /// <summary>Lower case without accents, so "Résumé", "resume" and "RESUME" are the same text.</summary>
    private static string Fold(string text)
    {
        string decomposed;
        try
        {
            decomposed = text.Normalize(NormalizationForm.FormD);
        }
        catch (PlatformNotSupportedException)
        {
            // A build with invariant globalization may have no normalization data; case is still folded.
            decomposed = text;
        }

        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }
}
