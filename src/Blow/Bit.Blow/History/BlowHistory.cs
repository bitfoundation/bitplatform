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
    private readonly List<BlowSnapshot> _past = [];
    private readonly List<BlowSnapshot> _future = [];
    private BlowSnapshot? _present;

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
    /// Records a new current state. Anything that had been undone is discarded - making a
    /// change after going back is what ends the redo branch, in this as in every editor.
    /// </summary>
    /// <returns>
    /// False when the snapshot describes the same graph as the current one and nothing was
    /// recorded. Capturing on every event and letting this filter the no-ops is a perfectly
    /// good way to use it, and it keeps the user from having to press undo twice to see
    /// anything happen.
    /// </returns>
    public bool Capture(BlowSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (_present is { } present && DescribesSameGraph(present, snapshot)) return false;

        if (_present is not null)
        {
            _past.Add(_present);
            if (_past.Count > Capacity) _past.RemoveAt(0);
        }

        _present = snapshot;
        _future.Clear();
        return true;
    }

    /// <summary>
    /// Steps back one state and returns it, or null when there is nothing to go back to.
    /// Restore the returned snapshot to apply it.
    /// </summary>
    public BlowSnapshot? Undo()
    {
        if (_past.Count == 0) return null;

        if (_present is not null) _future.Add(_present);

        _present = _past[^1];
        _past.RemoveAt(_past.Count - 1);
        return _present;
    }

    /// <summary>
    /// Steps forward one state and returns it, or null when nothing was undone.
    /// </summary>
    public BlowSnapshot? Redo()
    {
        if (_future.Count == 0) return null;

        if (_present is not null) _past.Add(_present);

        _present = _future[^1];
        _future.RemoveAt(_future.Count - 1);
        return _present;
    }

    /// <summary>Forgets everything, including the current state.</summary>
    public void Clear()
    {
        _past.Clear();
        _future.Clear();
        _present = null;
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

        for (var i = 0; i < a.Nodes.Count; i++)
        {
            if (!SameNode(a.Nodes[i], b.Nodes[i])) return false;
        }

        for (var i = 0; i < a.Edges.Count; i++)
        {
            if (!SameEdge(a.Edges[i], b.Edges[i])) return false;
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
           && x.Origin == y.Origin;

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
