namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowClipboard.Paste"/>.</summary>
public sealed class BlowPasteOptions
{
    /// <summary>
    /// How far the pasted copy is offset from the original, so it does not land exactly on top
    /// of what it was copied from and look like nothing happened.
    /// </summary>
    public BlowXYPosition Offset { get; set; } = new(24, 24);

    /// <summary>
    /// Ids already in use, so the paste can avoid colliding with them. Pass the current graph's
    /// ids; without them a second paste would produce duplicates of the first.
    /// </summary>
    public IEnumerable<string>? ExistingIds { get; set; }

    /// <summary>
    /// Names the copy of an element. Receives the original id and a de-duplication suffix that
    /// is only non-empty when the obvious name is taken. The default is <c>{id}-copy</c>,
    /// then <c>{id}-copy-2</c> and so on.
    /// </summary>
    public Func<string, int, string>? IdFactory { get; set; }

    /// <summary>
    /// Whether the pasted elements come back selected - usually what you want, since the user
    /// almost always wants to move or delete what they just pasted.
    /// </summary>
    public bool Select { get; set; } = true;
}

/// <summary>
/// Copy and paste for parts of a graph. Cloning a selection sounds trivial until the details
/// bite: ids have to stay unique, edges must be rewired to the copies rather than the originals,
/// a copied child has to end up inside the copied parent, and an edge that leaves the selection
/// must not come along at all.
/// </summary>
public static class BlowClipboard
{
    /// <summary>
    /// Takes a fragment out of a graph: deep copies of the given nodes, plus the edges that run
    /// between them.
    /// </summary>
    /// <remarks>
    /// The copies are detached, so the fragment keeps describing what was copied even as the
    /// user carries on editing - the clipboard should not change under you between Ctrl+C and
    /// Ctrl+V. Nothing in the fragment is selected, since selection belongs to the live graph.
    /// </remarks>
    public static BlowFragment Copy(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges)
        => Copy(nodes, edges, BlowXYPosition.Zero);

    /// <summary>
    /// Takes a fragment out of a graph, for a canvas whose nodes position themselves by
    /// <paramref name="nodeOrigin"/> rather than by their top-left corner.
    /// </summary>
    /// <param name="nodes">The nodes to copy.</param>
    /// <param name="edges">The graph's edges; those running between two copied nodes come along.</param>
    /// <param name="nodeOrigin">
    /// The canvas's <c>NodeOrigin</c>, used for nodes that do not set their own. A child copied
    /// without its parent becomes a root, and its new position has to name the same point of the
    /// node that the canvas will read it as.
    /// </param>
    public static BlowFragment Copy(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges, BlowXYPosition nodeOrigin)
    {
        var picked = nodes.ToList();
        if (picked.Count == 0) return BlowFragment.Empty;

        var ids = picked.Select(n => n.Id).ToHashSet();

        var copiedNodes = new List<BlowNode>(picked.Count);
        foreach (var n in picked)
        {
            var copy = n.Clone();
            copy.Selected = false;
            copy.Dragging = false;

            // A child whose parent was left behind becomes a root, and its position - which was
            // relative to that parent - has to become absolute or the copy jumps. The resolved
            // absolute position is the node's top-left; a root's Position names its origin point.
            if (copy.ParentId is { } parent && !ids.Contains(parent))
            {
                var origin = n.Origin ?? nodeOrigin;
                var size = n.EffectiveSize;
                var absolute = new BlowXYPosition(
                    n.AbsolutePosition.X + origin.X * size.Width,
                    n.AbsolutePosition.Y + origin.Y * size.Height);

                // An extent is written in the node's own coordinate space, which has just changed
                // from the parent's to the flow's; it moves with it. A parent extent has no parent
                // left to refer to.
                if (copy.Extent is { } extent)
                {
                    copy.Extent = extent with
                    {
                        X = extent.X + absolute.X - n.Position.X,
                        Y = extent.Y + absolute.Y - n.Position.Y,
                    };
                }
                copy.ExtentParent = false;

                copy.Position = absolute;
                copy.ParentId = null;
            }

            copiedNodes.Add(copy);
        }

        var copiedEdges = new List<BlowEdge>();
        foreach (var e in edges)
        {
            if (!ids.Contains(e.Source) || !ids.Contains(e.Target)) continue;
            var copy = e.Clone();
            copy.Selected = false;
            copiedEdges.Add(copy);
        }

        return new BlowFragment(copiedNodes, copiedEdges);
    }

    /// <summary>
    /// Produces a fresh copy of a fragment, ready to add to a graph: new ids throughout, edges
    /// and parent links rewired to the copies, and everything shifted by the paste offset.
    /// </summary>
    /// <remarks>
    /// The returned nodes and edges are new instances, so the same fragment can be pasted over
    /// and over - each paste is independent of the last and of the clipboard itself.
    /// </remarks>
    public static BlowFragment Paste(BlowFragment fragment, BlowPasteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        var opts = options ?? new BlowPasteOptions();

        if (fragment.IsEmpty) return BlowFragment.Empty;

        var taken = opts.ExistingIds is null ? [] : new HashSet<string>(opts.ExistingIds);
        var factory = opts.IdFactory ?? DefaultId;

        // Every new id has to be reserved as it is minted, or two elements copied from the same
        // original would both be handed the same "obvious" name.
        var nodeIds = new Dictionary<string, string>(fragment.Nodes.Count);
        var nodes = new List<BlowNode>(fragment.Nodes.Count);

        foreach (var n in fragment.Nodes)
        {
            var id = Mint(n.Id, taken, factory);
            nodeIds[n.Id] = id;

            var copy = n.Clone(id);
            copy.Selected = opts.Select;
            nodes.Add(copy);
        }

        foreach (var copy in nodes)
        {
            // Everything in the paste lands offset by the same amount - a child because its
            // parent moved - so the resolved position moves with it. Until the canvas next lays
            // the graph out, that is what bounds and hit-tests on the pasted nodes go by.
            copy.AbsolutePosition = new BlowXYPosition(
                copy.AbsolutePosition.X + opts.Offset.X, copy.AbsolutePosition.Y + opts.Offset.Y);

            // A copied child follows its copied parent; its position is parent-relative and so
            // must not be offset as well, or the child drifts out of the parent.
            if (copy.ParentId is { } parent && nodeIds.TryGetValue(parent, out var newParent))
            {
                copy.ParentId = newParent;
                continue;
            }

            copy.ParentId = null;
            copy.Position = new BlowXYPosition(copy.Position.X + opts.Offset.X, copy.Position.Y + opts.Offset.Y);
        }

        var edges = new List<BlowEdge>(fragment.Edges.Count);
        foreach (var e in fragment.Edges)
        {
            if (!nodeIds.TryGetValue(e.Source, out var source)) continue;
            if (!nodeIds.TryGetValue(e.Target, out var target)) continue;

            var copy = e.Clone(Mint(e.Id, taken, factory));
            copy.Source = source;
            copy.Target = target;
            copy.Selected = opts.Select;
            edges.Add(copy);
        }

        return new BlowFragment(nodes, edges);
    }

    /// <summary>Picks an unused id and reserves it.</summary>
    /// <remarks>
    /// A custom factory that ignores the attempt number would offer the same taken id forever;
    /// after a generous number of tries a random id is used instead of hanging the UI thread.
    /// </remarks>
    private static string Mint(string original, HashSet<string> taken, Func<string, int, string> factory)
    {
        for (var attempt = 0; attempt < MaxMintAttempts; attempt++)
        {
            var candidate = factory(original, attempt);
            if (taken.Add(candidate)) return candidate;
        }

        string fallback;
        do fallback = $"{original}-{Guid.NewGuid():N}";
        while (!taken.Add(fallback));
        return fallback;
    }

    private const int MaxMintAttempts = 10_000;

    private static string DefaultId(string original, int attempt) =>
        attempt == 0 ? $"{original}-copy" : $"{original}-copy-{attempt + 1}";
}
