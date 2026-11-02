namespace Bit.Blow;

/// <summary>
/// Undo/redo over flow snapshots. Hand it a snapshot after each change worth undoing, and it
/// gives you back the one to restore when the user asks to go back.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately something you drive rather than something the canvas does for you.
/// Only the application knows what counts as one undoable step - dragging five nodes is one
/// step, not five hundred pointer moves, and typing in a node's text field may be several
/// steps or none. So capture at the moments that mean something: a drag ended, an edge was
/// connected, a delete happened.
/// </para>
/// <para>
/// Snapshots are held as taken. Pair it with <c>BlowCanvas.ToObject()</c>, whose default is a
/// deep copy, so the history keeps describing the graph as it was rather than following along
/// as the user keeps editing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var history = new BlowHistory();
/// history.Capture(flow.ToObject());                      // the starting point
///
/// void OnChanged() =&gt; history.Capture(flow.ToObject());
/// async Task Undo() { if (history.Undo() is { } s) await flow.RestoreAsync(s); }
/// </code>
/// </example>
public sealed class BlowHistory
{
    private readonly List<(BlowSnapshot Snapshot, string? Label)> _past = [];
    private readonly List<(BlowSnapshot Snapshot, string? Label)> _future = [];
    private BlowSnapshot? _present;
    private string? _presentLabel;

    /// <param name="capacity">
    /// How many steps back the user can go. The oldest step is dropped once the limit is
    /// reached, so a long editing session cannot grow without bound.
    /// </param>
    public BlowHistory(int capacity = 100)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "History needs room for at least one step.");
        Capacity = capacity;
    }

    /// <summary>How many steps back the history keeps.</summary>
    public int Capacity { get; }

    /// <summary>True when there is an earlier state to go back to.</summary>
    public bool CanUndo => _past.Count > 0;

    /// <summary>True when a state was undone and has not been superseded by a new change.</summary>
    public bool CanRedo => _future.Count > 0;

    /// <summary>The state the history believes the flow is currently in, or null before the first capture.</summary>
    public BlowSnapshot? Current => _present;

    /// <summary>Number of steps that can be undone.</summary>
    public int UndoDepth => _past.Count;

    /// <summary>Number of steps that can be redone.</summary>
    public int RedoDepth => _future.Count;

    /// <summary>
    /// What undoing would take back: the label the current state was captured with, or null when there is nothing
    /// to undo or the step has no label. The canvas labels its own steps with a short code - <c>move</c>,
    /// <c>resize</c>, <c>connect</c>, <c>delete</c>, ... - for an "Undo move" button to translate.
    /// </summary>
    public string? UndoLabel => CanUndo ? _presentLabel : null;

    /// <summary>What redoing would bring back: the label of the next state, or null.</summary>
    public string? RedoLabel => CanRedo ? _future[^1].Label : null;

    /// <summary>
    /// Records a new current state. Anything that had been undone is discarded - making a
    /// change after going back is what ends the redo branch, in this as in every editor.
    /// </summary>
    /// <returns>
    /// False when the snapshot describes the same graph as the current one and nothing was
    /// recorded. Capturing on every event and letting this filter the no-ops is a perfectly
    /// good way to use it, and it keeps the user from having to press undo twice to see
    /// anything happen.
    /// </returns>
    public bool Capture(BlowSnapshot snapshot) => Capture(snapshot, null);

    /// <summary>
    /// Records a new current state, labelled with what changed to produce it - what <see cref="UndoLabel"/> reports
    /// while it is the current state.
    /// </summary>
    /// <returns>False when the snapshot describes the same graph as the current one and nothing was recorded.</returns>
    public bool Capture(BlowSnapshot snapshot, string? label)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (_present is { } present && DescribesSameGraph(present, snapshot)) return false;

        if (_present is not null)
        {
            _past.Add((_present, _presentLabel));
            if (_past.Count > Capacity) _past.RemoveAt(0);
        }

        _present = snapshot;
        _presentLabel = label;
        _future.Clear();
        return true;
    }

    /// <summary>
    /// Records the given live graph as a new current state, copying only the nodes and edges that changed since the
    /// current state and sharing the copies of the rest with it - so a step costs memory in proportion to what
    /// changed rather than to the size of the graph.
    /// </summary>
    /// <remarks>
    /// The lists are read, never kept: what is stored is copies, so later edits to the live graph cannot reach back
    /// into the history. A copy is shared between steps only while it describes its element exactly - by the rules
    /// <see cref="Capture(BlowSnapshot, string?)"/> uses to drop a no-op, and in the state that does not count as a
    /// change but comes back on undo all the same: selection, measured size, handle anchors. That is what makes
    /// sharing safe: the history never modifies what it holds, and <c>BlowCanvas.RestoreAsync</c> copies a snapshot
    /// again before handing it to the live graph.
    /// </remarks>
    /// <returns>False when nothing changed since the current state and nothing was recorded.</returns>
    public bool Capture(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, BlowViewport viewport, string? label = null)
        => Capture(Copy(nodes, edges, viewport, _present), label);

    /// <summary>
    /// Writes a change that must not be undone into every state the history holds - another user's edit, applied by a
    /// collaboration adapter, or a server's correction. Each node and edge the live graph has added, changed or
    /// removed since <see cref="Current"/> is added, replaced or removed in every step, back and forward, and the live
    /// graph becomes the current state. Undo and redo then take back the local steps around the change, never the
    /// change itself.
    /// </summary>
    /// <remarks>
    /// The change wins: a local step that edited the same element no longer changes it, and a step that no longer
    /// changes anything is dropped. An edge left without one of its nodes in an older step is dropped from that step.
    /// Before the first capture there is no history to rewrite, and the graph simply becomes the starting point.
    /// </remarks>
    /// <returns>True when the history changed; false when the graph does not differ from the current state.</returns>
    public bool Rebase(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, BlowViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        if (_present is not { } present) return Capture(nodes, edges, viewport);

        var next = Copy(nodes, edges, viewport, present);
        if (DescribesSameGraph(present, next)) return false;

        var nodeChanges = Changes(present.Nodes, next.Nodes, static n => n.Id, SameNode);
        var edgeChanges = Changes(present.Edges, next.Edges, static e => e.Id, SameEdge);

        // Every state in order, oldest first, with the label of the step that led to it.
        var states = new List<(BlowSnapshot Snapshot, string? Label)>(_past.Count + 1 + _future.Count);
        foreach (var (snapshot, label) in _past) states.Add((Patch(snapshot, nodeChanges, edgeChanges), label));
        var presentAt = states.Count;
        states.Add((next, _presentLabel));
        for (var i = _future.Count - 1; i >= 0; i--)
            states.Add((Patch(_future[i].Snapshot, nodeChanges, edgeChanges), _future[i].Label));

        // A step whose whole effect the change has overwritten would make an undo that does nothing.
        for (var i = 1; i < states.Count;)
        {
            if (!DescribesSameGraph(states[i - 1].Snapshot, states[i].Snapshot))
            {
                i++;
                continue;
            }

            if (i == presentAt)
            {
                // The current state stays; it takes over the label of the step that led to the one dropped.
                states[i] = (states[i].Snapshot, states[i - 1].Label);
                states.RemoveAt(i - 1);
                presentAt--;
            }
            else
            {
                states.RemoveAt(i);
                if (i < presentAt) presentAt--;
            }
        }

        _past.Clear();
        _past.AddRange(states.Take(presentAt));
        (_present, _presentLabel) = states[presentAt];
        _future.Clear();
        for (var i = states.Count - 1; i > presentAt; i--) _future.Add(states[i]);
        return true;
    }

    /// <summary>The live graph as a snapshot, sharing the copies of everything unchanged since <paramref name="previous"/>.</summary>
    private static BlowSnapshot Copy(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, BlowViewport viewport,
        BlowSnapshot? previous)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        var nodeCopies = Share(nodes, previous?.Nodes, static n => n.Id,
            static (x, y) => SameNode(x, y) && SameNodeState(x, y), static n => n.Clone());
        var edgeCopies = Share(edges, previous?.Edges, static e => e.Id,
            static (x, y) => SameEdge(x, y) && x.Selected == y.Selected, static e => e.Clone());
        return new BlowSnapshot(nodeCopies, edgeCopies, viewport);
    }

    /// <summary>
    /// What a node carries that is not an edit but is restored with it, and so has to match for a copy to be shared:
    /// a stale selection would come back selected on undo, and a copy taken before the node was measured would come
    /// back without its size and handle anchors.
    /// </summary>
    private static bool SameNodeState(BlowNode x, BlowNode y)
    {
        if (x.Selected != y.Selected || x.Measured != y.Measured || x.Handles.Count != y.Handles.Count) return false;
        foreach (var (key, bounds) in x.Handles)
        {
            if (!y.Handles.TryGetValue(key, out var other) || other != bounds) return false;
        }
        return true;
    }

    /// <summary>
    /// The elements <paramref name="after"/> adds or changes (mapped to their new copy) and removes (mapped to null)
    /// relative to <paramref name="before"/>, in the order of <paramref name="after"/>.
    /// </summary>
    private static Dictionary<string, T?> Changes<T>(IReadOnlyList<T> before, IReadOnlyList<T> after, Func<T, string> id,
        Func<T, T, bool> same) where T : class
    {
        var old = new Dictionary<string, T>(before.Count, StringComparer.Ordinal);
        foreach (var element in before) old.TryAdd(id(element), element);

        var changes = new Dictionary<string, T?>(StringComparer.Ordinal);
        var kept = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in after)
        {
            var key = id(element);
            kept.Add(key);
            if (!old.TryGetValue(key, out var was) || !same(was, element)) changes[key] = element;
        }
        foreach (var key in old.Keys)
        {
            if (!kept.Contains(key)) changes[key] = null;
        }
        return changes;
    }

    /// <summary>One recorded state with the changes written in, and the edges they left without a node dropped.</summary>
    private static BlowSnapshot Patch(BlowSnapshot snapshot, Dictionary<string, BlowNode?> nodeChanges,
        Dictionary<string, BlowEdge?> edgeChanges)
    {
        var nodes = Apply(snapshot.Nodes, nodeChanges, static n => n.Id);
        var ids = new HashSet<string>(nodes.Select(n => n.Id), StringComparer.Ordinal);
        var edges = Apply(snapshot.Edges, edgeChanges, static e => e.Id);
        edges.RemoveAll(e => !ids.Contains(e.Source) || !ids.Contains(e.Target));
        return new BlowSnapshot(nodes, edges, snapshot.Viewport);

        static List<T> Apply<T>(IReadOnlyList<T> list, Dictionary<string, T?> changes, Func<T, string> id) where T : class
        {
            var result = new List<T>(list.Count);
            var replaced = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in list)
            {
                if (!changes.TryGetValue(id(element), out var replacement))
                {
                    result.Add(element);
                    continue;
                }
                replaced.Add(id(element));
                if (replacement is not null) result.Add(replacement);
            }
            foreach (var (key, replacement) in changes)
            {
                if (replacement is not null && !replaced.Contains(key)) result.Add(replacement);
            }
            return result;
        }
    }

    /// <summary>
    /// Copies of the live elements, reusing the previous step's copy of every element that has not changed. The
    /// previous step is matched by position first - the common case, where nothing was added or removed before it -
    /// and by id when the lists have shifted.
    /// </summary>
    private static List<T> Share<T>(IReadOnlyList<T> live, IReadOnlyList<T>? previous, Func<T, string> id,
        Func<T, T, bool> same, Func<T, T> copy) where T : class
    {
        var result = new List<T>(live.Count);
        Dictionary<string, T>? byId = null;
        for (var i = 0; i < live.Count; i++)
        {
            var element = live[i];
            T? candidate = null;
            if (previous is not null)
            {
                if (i < previous.Count && id(previous[i]) == id(element)) candidate = previous[i];
                else
                {
                    if (byId is null)
                    {
                        byId = new Dictionary<string, T>(previous.Count, StringComparer.Ordinal);
                        foreach (var p in previous) byId.TryAdd(id(p), p);
                    }
                    candidate = byId.GetValueOrDefault(id(element));
                }
            }

            result.Add(candidate is not null && same(candidate, element) ? candidate : copy(element));
        }
        return result;
    }

    /// <summary>
    /// Steps back one state and returns it, or null when there is nothing to go back to.
    /// Restore the returned snapshot to apply it.
    /// </summary>
    public BlowSnapshot? Undo()
    {
        if (_past.Count == 0) return null;

        if (_present is not null) _future.Add((_present, _presentLabel));

        (_present, _presentLabel) = _past[^1];
        _past.RemoveAt(_past.Count - 1);
        return _present;
    }

    /// <summary>
    /// Steps forward one state and returns it, or null when nothing was undone.
    /// </summary>
    public BlowSnapshot? Redo()
    {
        if (_future.Count == 0) return null;

        if (_present is not null) _past.Add((_present, _presentLabel));

        (_present, _presentLabel) = _future[^1];
        _future.RemoveAt(_future.Count - 1);
        return _present;
    }

    /// <summary>Forgets everything, including the current state.</summary>
    public void Clear()
    {
        _past.Clear();
        _future.Clear();
        _present = null;
        _presentLabel = null;
    }

    /// <summary>
    /// Whether two snapshots describe the same graph, so an unchanged capture can be ignored.
    /// </summary>
    /// <remarks>
    /// Compares the structure and geometry - what an undo would visibly restore - and not
    /// transient state like selection, since selecting a node is not a step a user expects to
    /// undo. Done exactly rather than by hash: a collision would silently swallow a real change.
    /// </remarks>
    private static bool DescribesSameGraph(BlowSnapshot a, BlowSnapshot b)
    {
        if (a.Nodes.Count != b.Nodes.Count || a.Edges.Count != b.Edges.Count) return false;

        // A copy shared from one step to the next is the same object in both: nothing to compare.
        for (var i = 0; i < a.Nodes.Count; i++)
        {
            if (!ReferenceEquals(a.Nodes[i], b.Nodes[i]) && !SameNode(a.Nodes[i], b.Nodes[i])) return false;
        }

        for (var i = 0; i < a.Edges.Count; i++)
        {
            if (!ReferenceEquals(a.Edges[i], b.Edges[i]) && !SameEdge(a.Edges[i], b.Edges[i])) return false;
        }

        return true;
    }

    // Everything a user can change about an element counts, so restyling an edge or replacing a
    // node's data is a step like any other. Selection does not: clicking around is not an edit,
    // and recording it would bury the real steps under dozens of no-ops. Nor does anything the
    // canvas maintains for itself (measured size, drag and resize state).
    private static bool SameNode(BlowNode x, BlowNode y)
        => x.Id == y.Id
           && x.Position == y.Position
           && x.Type == y.Type
           && x.Label == y.Label
           && Equals(x.Data, y.Data)
           && x.Width == y.Width
           && x.Height == y.Height
           && x.Draggable == y.Draggable
           && x.Selectable == y.Selectable
           && x.Connectable == y.Connectable
           && x.Hidden == y.Hidden
           && x.Collapsed == y.Collapsed
           && x.Deletable == y.Deletable
           && x.SourcePosition == y.SourcePosition
           && x.TargetPosition == y.TargetPosition
           && x.UseCustomDragHandle == y.UseCustomDragHandle
           && x.Focusable == y.Focusable
           && x.AriaLabel == y.AriaLabel
           && x.ParentId == y.ParentId
           && x.ExtentParent == y.ExtentParent
           && x.Extent == y.Extent
           && x.ExpandParent == y.ExpandParent
           && x.Class == y.Class
           && x.Style == y.Style
           && x.ZIndex == y.ZIndex
           && x.Origin == y.Origin
           && SamePorts(x.Ports, y.Ports);

    private static bool SamePorts(List<BlowPort>? x, List<BlowPort>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if ((x?.Count ?? 0) != (y?.Count ?? 0)) return false;
        if (x is null || y is null) return true; // both empty, one of them as null
        for (var i = 0; i < x.Count; i++)
        {
            if (!BlowPort.SameValues(x[i], y[i])) return false;
        }
        return true;
    }

    private static bool SameEdge(BlowEdge x, BlowEdge y)
        => x.Id == y.Id
           && x.Source == y.Source
           && x.Target == y.Target
           && x.SourceHandle == y.SourceHandle
           && x.TargetHandle == y.TargetHandle
           && x.Type == y.Type
           && BlowEdgePathOptions.SameValues(x.PathOptions, y.PathOptions)
           && x.Label == y.Label
           && x.LabelStyle == y.LabelStyle
           && x.LabelShowBg == y.LabelShowBg
           && x.LabelBgStyle == y.LabelBgStyle
           && x.LabelBgPaddingX == y.LabelBgPaddingX
           && x.LabelBgPaddingY == y.LabelBgPaddingY
           && x.LabelBgBorderRadius == y.LabelBgBorderRadius
           && x.Animated == y.Animated
           && x.Hidden == y.Hidden
           && x.Selectable == y.Selectable
           && x.Focusable == y.Focusable
           && x.Deletable == y.Deletable
           && x.Reconnectable == y.Reconnectable
           && x.StrokeWidth == y.StrokeWidth
           && x.Stroke == y.Stroke
           && x.MarkerEnd == y.MarkerEnd
           && x.MarkerStart == y.MarkerStart
           && x.MarkerType == y.MarkerType
           && x.MarkerColor == y.MarkerColor
           && x.MarkerWidth == y.MarkerWidth
           && x.MarkerHeight == y.MarkerHeight
           && x.Class == y.Class
           && x.Style == y.Style
           && x.InteractionWidth == y.InteractionWidth
           && x.ZIndex == y.ZIndex
           && Equals(x.Data, y.Data);
}
