
namespace Bit.Bink;

/// <summary>One undoable step.</summary>
public sealed record BinkHistoryEntry(BinkDrawingOperation Forward, BinkDrawingOperation Inverse, string Label, DateTimeOffset Timestamp);

/// <summary>
/// Undo/redo for an <see cref="BinkDocument"/>. Records the inverse of every local change as it happens, so no
/// document snapshots are ever taken: memory usage is proportional to what changed, and elements referenced by
/// history share their point data with the document.
/// </summary>
/// <remarks>
/// Attach one manager per document. Changes with origin <see cref="BinkChangeOrigin.Remote"/> or <see cref="BinkChangeOrigin.Load"/>
/// are not recorded (remote changes belong to the other user's history). Use <see cref="BeginGroup"/> to merge a
/// gesture's many operations (e.g. every frame of a drag) into a single undo step.
/// </remarks>
public sealed class BinkHistoryManager : IDisposable
{
    private readonly BinkDocument _document;
    private readonly List<BinkHistoryEntry> _undo = new();
    private readonly List<BinkHistoryEntry> _redo = new();
    private List<BinkHistoryEntry>? _group;
    private string? _groupLabel;
    private int _groupDepth;
    private bool _discardGroup;
    private bool _applying;
    private bool _disposed;
    private int _limit;
    private List<BinkHistoryEntry>? _consequences;
    private BinkHistoryEntry? _cause;

    /// <summary>Creates a manager and starts recording the document's changes.</summary>
    public BinkHistoryManager(BinkDocument document, int limit = 200)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
        Limit = limit;
        document.Changed += OnDocumentChanged;
        document.DispatchCompleted += OnDispatchCompleted;
    }

    /// <summary>
    /// Maximum number of undo steps kept; older steps are dropped. Zero disables history. Lowering it trims the
    /// stack immediately, so the elements those steps referenced are released rather than kept alive by a limit
    /// that only applies to future changes.
    /// </summary>
    public int Limit
    {
        get => _limit;
        set
        {
            _limit = value;
            if (_undo.Count <= _limit) return;
            _undo.RemoveRange(0, _undo.Count - Math.Max(_limit, 0));
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>True when there is something to undo.</summary>
    public bool CanUndo => _undo.Count > 0;

    /// <summary>True when there is something to redo.</summary>
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Number of undo steps.</summary>
    public int UndoCount => _undo.Count;

    /// <summary>Number of redo steps.</summary>
    public int RedoCount => _redo.Count;

    /// <summary>Label of the next undo step, or null.</summary>
    public string? UndoLabel => _undo.Count > 0 ? _undo[^1].Label : null;

    /// <summary>Label of the next redo step, or null.</summary>
    public string? RedoLabel => _redo.Count > 0 ? _redo[^1].Label : null;

    /// <summary>Undo steps, oldest first.</summary>
    public IReadOnlyList<BinkHistoryEntry> UndoStack => _undo;

    /// <summary>Redo steps, oldest first.</summary>
    public IReadOnlyList<BinkHistoryEntry> RedoStack => _redo;

    /// <summary>Raised whenever the undo or redo stack changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Undoes the most recent step. Returns false when there is nothing to undo.</summary>
    public bool Undo()
    {
        if (_undo.Count == 0 || _groupDepth > 0) return false;
        BinkHistoryEntry entry = _undo[^1];
        // The step is only removed once it has been applied. Dropping it first would lose it for good if the
        // document refused the change, for example because a collaborator already deleted the element.
        if (!ApplyEntry(entry.Inverse, BinkChangeOrigin.Undo)) return false;
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(entry);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Redoes the most recently undone step. Returns false when there is nothing to redo.</summary>
    public bool Redo()
    {
        if (_redo.Count == 0 || _groupDepth > 0) return false;
        BinkHistoryEntry entry = _redo[^1];
        if (!ApplyEntry(entry.Forward, BinkChangeOrigin.Redo)) return false;
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(entry);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Applies a history step. Returns false when the document could not accept it.</summary>
    private bool ApplyEntry(BinkDrawingOperation operation, BinkChangeOrigin origin)
    {
        _applying = true;
        try
        {
            _document.Apply(operation, origin);
            return true;
        }
        catch (InvalidOperationException)
        {
            // The document moved on underneath this step, typically because a collaborator removed something it
            // refers to. The step stays where it is rather than vanishing.
            return false;
        }
        finally
        {
            _applying = false;
        }
    }

    /// <summary>Clears both stacks.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Starts a group: all local changes until the returned scope is disposed become one undo step.
    /// Groups nest; the outermost one wins.
    /// </summary>
    public IDisposable BeginGroup(string? label = null)
    {
        if (_groupDepth == 0)
        {
            _group = new List<BinkHistoryEntry>();
            _groupLabel = label;
        }
        _groupDepth++;
        return new GroupScope(this);
    }

    private void EndGroup()
    {
        // A scope can be disposed after the group it belongs to was already closed from outside, by
        // EndOpenGroups or by Dispose when a gesture was interrupted. The tool that opened it still runs its
        // finally block, and closing an already-closed group has to be a no-op rather than a crash.
        if (_groupDepth == 0) return;
        if (--_groupDepth > 0) return;
        List<BinkHistoryEntry> entries = _group ?? [];
        _group = null;
        bool discard = _discardGroup;
        _discardGroup = false;
        if (discard || entries.Count == 0) return;
        BinkHistoryEntry merged = Merge(entries, _groupLabel);
        Push(merged);
    }

    /// <summary>
    /// Discards the open group instead of recording it. A cancelled gesture has already been reversed in the
    /// document, so recording it and its reversal would leave the user with an undo step that does nothing.
    /// </summary>
    public void CancelGroup()
    {
        if (_groupDepth == 0) return;
        _group?.Clear();
        _discardGroup = true;
    }

    private static BinkHistoryEntry Merge(List<BinkHistoryEntry> entries, string? label)
    {
        if (entries.Count == 1 && label is null) return entries[0];
        var forward = new List<BinkDrawingOperation>(entries.Count);
        var inverse = new List<BinkDrawingOperation>(entries.Count);
        foreach (BinkHistoryEntry e in entries) forward.Add(e.Forward);
        for (int i = entries.Count - 1; i >= 0; i--) inverse.Add(entries[i].Inverse);
        string l = label ?? entries[0].Label;
        return new BinkHistoryEntry(new BinkBatchOperation(forward, l), new BinkBatchOperation(inverse, l), l, entries[^1].Timestamp);
    }

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        if (_applying || e.Origin != BinkChangeOrigin.Local || Limit <= 0) return;
        var entry = new BinkHistoryEntry(e.Operation, e.Inverse, e.Operation.Label, DateTimeOffset.UtcNow);

        // A change an application applied while handling another one is a consequence: re-routing a connector
        // because the node it is attached to moved, say. It has to be recorded after its cause, and whether it
        // arrives before or after depends only on the order the handlers were subscribed in, so it is held back
        // until the whole notification has finished.
        if (e.Depth > 0)
        {
            (_consequences ??= []).Add(entry);
            return;
        }
        Record(entry);
        // Remembered so its consequences can join it when they are recorded, once the notification is over.
        if (_groupDepth == 0) _cause = entry;
    }

    private void Record(BinkHistoryEntry entry)
    {
        if (_groupDepth > 0)
        {
            _group!.Add(entry);
            return;
        }
        Push(entry);
    }

    private void OnDispatchCompleted()
    {
        BinkHistoryEntry? cause = _cause;
        _cause = null;
        if (_consequences is not { Count: > 0 }) return;
        List<BinkHistoryEntry> pending = _consequences;
        _consequences = null;

        // Consequences undo with their cause, or undoing once would revert the derived edit and leave the change
        // that caused it in place: a node back on its old connectors' route, but still at its new position. With
        // a group open the group already joins them. Without one, the cause is the step just pushed, and the
        // consequences are folded into it.
        if (_groupDepth == 0 && cause is not null && _undo.Count > 0 && ReferenceEquals(_undo[^1], cause))
        {
            _undo[^1] = Merge([cause, .. pending], cause.Label);
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }
        // A consequence of a change history did not record, such as a remote edit, is a step of its own.
        Record(pending.Count == 1 ? pending[0] : Merge(pending, pending[0].Label));
    }

    private void Push(BinkHistoryEntry entry)
    {
        _redo.Clear();
        _undo.Add(entry);
        if (_undo.Count > Limit) _undo.RemoveRange(0, _undo.Count - Limit);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class GroupScope(BinkHistoryManager owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            owner.EndGroup();
        }
    }

    /// <summary>
    /// Closes any group left open by a tool that was interrupted. A group that is never closed silently disables
    /// undo, so the host calls this whenever a gesture is abandoned.
    /// </summary>
    public void EndOpenGroups()
    {
        while (_groupDepth > 0)
        {
            _groupDepth = 1;
            EndGroup();
        }
    }

    /// <summary>Stops recording. Any group left open by a tool is closed, so history is never left disabled.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _document.Changed -= OnDocumentChanged;
        _document.DispatchCompleted -= OnDispatchCompleted;
        EndOpenGroups();
        _consequences = null;
        // The stacks are deliberately left intact: disposing stops recording, and a host that swaps documents
        // still reads the final state. Call Clear to release the elements the stacks reference.
    }
}
