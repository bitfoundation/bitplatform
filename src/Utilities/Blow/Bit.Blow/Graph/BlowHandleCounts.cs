namespace Bit.Blow;

/// <summary>
/// How many edges each handle has, counted once over a list of edges: what a port's <c>MaxConnections</c> and
/// <c>MinConnections</c> are held to - by the connection rules, the canvas's handles and validation alike.
/// </summary>
internal sealed class BlowHandleCounts
{
    private readonly Dictionary<(string NodeId, BlowHandleType Type, string HandleId), int> _counts = [];

    /// <param name="edges">The edges to count.</param>
    /// <param name="except">An edge left out - the one being reconnected, which the new connection replaces.</param>
    public BlowHandleCounts(IEnumerable<BlowEdge> edges, BlowEdge? except = null)
    {
        foreach (var e in edges)
        {
            if (ReferenceEquals(e, except)) continue;
            Increment((e.Source, BlowHandleType.Source, e.SourceHandle ?? string.Empty));
            Increment((e.Target, BlowHandleType.Target, e.TargetHandle ?? string.Empty));
        }

        void Increment((string, BlowHandleType, string) key) => _counts[key] = _counts.GetValueOrDefault(key) + 1;
    }

    /// <summary>
    /// The edges attached to one handle: at the end of its own type - or, in <see cref="BlowConnectionMode.Loose"/>
    /// mode, where a handle can be either end of an edge, at either end with its id. A missing handle id and an empty
    /// one name the same, unnamed handle.
    /// </summary>
    public int At(string nodeId, BlowHandleType type, string? handleId, BlowConnectionMode mode = BlowConnectionMode.Strict)
    {
        var at = _counts.GetValueOrDefault((nodeId, type, handleId ?? string.Empty));
        if (mode != BlowConnectionMode.Loose) return at;

        var other = type == BlowHandleType.Source ? BlowHandleType.Target : BlowHandleType.Source;
        return at + _counts.GetValueOrDefault((nodeId, other, handleId ?? string.Empty));
    }
}
