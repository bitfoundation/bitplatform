
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    // Whether every change recorded in the open group so far replaced elements in place, which is what dragging,
    // resizing and rotating produce. Such a group is compacted as it grows; see TryCompact.
    private bool _groupCompactable;

    /// <summary>Entries an open group collects before it is compacted again.</summary>
    private const int CompactionInterval = 16;
    private bool _applying;
    private bool _disposed;
    private int _limit;
    private long _budget;
    private long _memory;
    // Kept only while a budget is set, so recording costs nothing extra without one: how many references the steps
    // hold to each payload (point data, text, an image) and its size. A payload several steps share, such as the
    // points of a stroke dragged for a hundred frames, is counted once.
    private readonly Dictionary<object, Payload> _payloads = new(ReferenceEqualityComparer.Instance);
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
            int remove = _undo.Count - Math.Max(_limit, 0);
            for (int i = 0; i < remove; i++) Untrack(_undo[i]);
            _undo.RemoveRange(0, remove);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// The most memory, in estimated bytes, the undo and redo steps may hold; 0, the default, for no limit besides
    /// <see cref="Limit"/>. The oldest steps are dropped past it, the furthest redo steps after them, but never the
    /// most recent step. Lowering it trims at once.
    /// </summary>
    /// <remarks>
    /// A step holds the elements it would put back: deleting a photo keeps the photo alive for undo, and a few hundred
    /// such steps of large embedded images can hold gigabytes. The estimate counts every element a step refers to, and
    /// the point data, text and image data they carry by size, once however many steps share it, as the steps of a
    /// drag share a stroke's points. Data the document itself still holds is counted too, so it errs high.
    /// </remarks>
    public long MemoryBudget
    {
        get => _budget;
        set
        {
            long budget = Math.Max(value, 0);
            if (budget > 0 && _budget == 0)
            {
                // Accounting starts now, from the steps already recorded.
                _payloads.Clear();
                _memory = 0;
                foreach (BinkHistoryEntry entry in _undo) _memory += Account(entry, _payloads, 1);
                foreach (BinkHistoryEntry entry in _redo) _memory += Account(entry, _payloads, 1);
            }
            else if (budget == 0)
            {
                _payloads.Clear();
                _payloads.TrimExcess();
                _memory = 0;
            }
            _budget = budget;
            if (TrimToBudget()) Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// The memory the undo and redo steps are estimated to hold, in bytes; see <see cref="MemoryBudget"/>. With a
    /// budget it is kept as steps come and go; without one it is worked out when read, by going through every step.
    /// </summary>
    public long EstimatedMemory
    {
        get
        {
            if (_budget > 0) return _memory;
            var payloads = new Dictionary<object, Payload>(ReferenceEqualityComparer.Instance);
            long total = 0;
            foreach (BinkHistoryEntry entry in _undo) total += Account(entry, payloads, 1);
            foreach (BinkHistoryEntry entry in _redo) total += Account(entry, payloads, 1);
            return total;
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
        _payloads.Clear();
        _memory = 0;
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
            _groupCompactable = true;
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
        BinkHistoryEntry merged = (_groupCompactable && entries.Count > 1 ? TryCompact(entries, _groupLabel) : null) ?? Merge(entries, _groupLabel);
        Push(merged);
    }

    /// <summary>
    /// Folds a group that only replaced elements in place into one replacement per element: the state it ended
    /// in going forward, and the state it started from going back. A drag records every frame, so without this a
    /// two-second drag of a thousand strokes kept over a hundred thousand operations, and undoing it replayed all
    /// of them to arrive where the first frame's inverse alone would have.
    /// </summary>
    /// <remarks>
    /// An element the group added and then kept replacing, which is what dragging a fresh copy does, folds into its
    /// addition: forward it is added as it ended up, and going back it is removed.
    /// </remarks>
    /// <returns>The compacted entry, or null when the group holds anything else.</returns>
    private static BinkHistoryEntry? TryCompact(List<BinkHistoryEntry> entries, string? label)
    {
        var steps = new List<(BinkDrawingOperation Forward, BinkDrawingOperation Inverse)>();
        var slots = new Dictionary<Guid, int>();
        var forward = new List<BinkDrawingOperation>();
        var inverse = new List<BinkDrawingOperation>();
        foreach (BinkHistoryEntry entry in entries)
        {
            forward.Clear();
            inverse.Clear();
            if (!Flatten(entry.Forward, forward) || !Flatten(entry.Inverse, inverse) || forward.Count != inverse.Count) return null;
            // An inverse runs its parts in reverse order, so the i-th forward operation pairs with the i-th from
            // the end of the inverse. A pair that does not name the same element means the shapes differ and the
            // group is left as recorded rather than guessed at.
            for (int i = 0; i < forward.Count; i++)
            {
                BinkDrawingOperation next = forward[i];
                BinkDrawingOperation undo = inverse[forward.Count - 1 - i];
                switch (next)
                {
                    case BinkReplaceElementOperation replace when undo is BinkReplaceElementOperation previous && previous.Element.Id == replace.Element.Id:
                        if (!slots.TryGetValue(replace.Element.Id, out int slot))
                        {
                            slots[replace.Element.Id] = steps.Count;
                            steps.Add((replace, previous));
                        }
                        // A later state of an element added in this group is still an addition, at its new state.
                        else if (steps[slot].Forward is BinkAddElementOperation added) steps[slot] = (added with { Element = replace.Element }, steps[slot].Inverse);
                        else steps[slot] = (replace, steps[slot].Inverse);
                        break;
                    case BinkAddElementOperation add when undo is BinkRemoveElementOperation remove && remove.ElementId == add.Element.Id && !slots.ContainsKey(add.Element.Id):
                        slots[add.Element.Id] = steps.Count;
                        steps.Add((add, remove));
                        break;
                    default:
                        return null;
                }
            }
        }
        if (steps.Count == 0) return null;

        var forwardOps = new BinkDrawingOperation[steps.Count];
        var inverseOps = new BinkDrawingOperation[steps.Count];
        for (int i = 0; i < steps.Count; i++)
        {
            forwardOps[i] = steps[i].Forward;
            inverseOps[steps.Count - 1 - i] = steps[i].Inverse;
        }
        string l = label ?? entries[0].Label;
        return new BinkHistoryEntry(new BinkBatchOperation(forwardOps, l), new BinkBatchOperation(inverseOps, l), l, entries[^1].Timestamp);
    }

    private static bool Flatten(BinkDrawingOperation operation, List<BinkDrawingOperation> into)
    {
        switch (operation)
        {
            case BinkReplaceElementOperation or BinkAddElementOperation or BinkRemoveElementOperation:
                into.Add(operation);
                return true;
            case BinkBatchOperation batch:
                foreach (BinkDrawingOperation inner in batch.Operations)
                {
                    if (!Flatten(inner, into)) return false;
                }
                return true;
            default:
                return false;
        }
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
            // Compacted as the gesture runs, not only when it ends, so a long drag does not hold every frame in
            // memory until the pointer lifts. Once anything other than a replacement joins, the group is kept as
            // recorded for good.
            if (_groupCompactable && _group.Count >= CompactionInterval)
            {
                if (TryCompact(_group, _groupLabel) is { } compacted)
                {
                    _group.Clear();
                    _group.Add(compacted);
                }
                else _groupCompactable = false;
            }
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
            Untrack(cause);
            _undo[^1] = Merge([cause, .. pending], cause.Label);
            Track(_undo[^1]);
            TrimToBudget();
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }
        // A consequence of a change history did not record, such as a remote edit, is a step of its own.
        Record(pending.Count == 1 ? pending[0] : Merge(pending, pending[0].Label));
    }

    private void Push(BinkHistoryEntry entry)
    {
        foreach (BinkHistoryEntry redo in _redo) Untrack(redo);
        _redo.Clear();
        _undo.Add(entry);
        Track(entry);
        if (_undo.Count > Limit)
        {
            int remove = _undo.Count - Limit;
            for (int i = 0; i < remove; i++) Untrack(_undo[i]);
            _undo.RemoveRange(0, remove);
        }
        TrimToBudget();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Drops the oldest steps, then the furthest redo steps, while over the budget; never the newest step.</summary>
    /// <returns>True when anything was dropped.</returns>
    private bool TrimToBudget()
    {
        bool trimmed = false;
        while (_budget > 0 && _memory > _budget && _undo.Count > 1)
        {
            Untrack(_undo[0]);
            _undo.RemoveAt(0);
            trimmed = true;
        }
        while (_budget > 0 && _memory > _budget && _redo.Count > 0 && _undo.Count + _redo.Count > 1)
        {
            Untrack(_redo[0]);
            _redo.RemoveAt(0);
            trimmed = true;
        }
        return trimmed;
    }

    private void Track(BinkHistoryEntry entry)
    {
        if (_budget > 0) _memory += Account(entry, _payloads, 1);
    }

    private void Untrack(BinkHistoryEntry entry)
    {
        if (_budget > 0) _memory += Account(entry, _payloads, -1);
    }

    /// <summary>
    /// Adds a step to the accounting (<paramref name="delta"/> 1) or takes it out (-1), returning how the estimate
    /// changes: the step's own records, and each payload it refers to when it is the first reference or the last.
    /// </summary>
    private static long Account(BinkHistoryEntry entry, Dictionary<object, Payload> payloads, int delta)
        => 64L * delta + Account(entry.Forward, payloads, delta) + Account(entry.Inverse, payloads, delta);

    private static long Account(BinkDrawingOperation operation, Dictionary<object, Payload> payloads, int delta)
    {
        switch (operation)
        {
            case BinkAddElementOperation add: return 48L * delta + Account(add.Element, payloads, delta);
            case BinkReplaceElementOperation replace: return 48L * delta + Account(replace.Element, payloads, delta);
            case BinkAddLayerOperation { Elements: { } elements }:
            {
                long total = 96L * delta;
                foreach (BinkElement element in elements) total += Account(element, payloads, delta);
                return total;
            }
            case BinkBatchOperation batch:
            {
                long total = 48L * delta;
                IReadOnlyList<BinkDrawingOperation> inner = batch.Operations;
                for (int i = 0; i < inner.Count; i++) total += Account(inner[i], payloads, delta);
                return total;
            }
            default:
                return 48L * delta;
        }
    }

    private static long Account(BinkElement element, Dictionary<object, Payload> payloads, int delta)
    {
        // The element record itself, which every version has of its own.
        long change = 160L * delta;
        (object? payload, long size) = element switch
        {
            // Positions, pressure and time as floats, and tilt and twist where the device produced them.
            BinkStroke stroke => (stroke.Points, stroke.Points.Count * 28L),
            BinkImageElement image => (image.Source, image.Source.Uri.Length * 2L),
            BinkTextElement text => (text.Text, text.Text.Length * 2L),
            BinkCalloutElement callout => (callout.Text, callout.Text.Length * 2L),
            BinkPolygonShape polygon => (polygon.Points, polygon.Points.Count * 8L),
            BinkPathElement path => (path.Path, path.Path.VerbCount + path.Path.Coordinates.Length * 4L),
            BinkHighlightElement highlight => (highlight.Rects, highlight.Rects.Count * 16L),
            BinkUnknownElement unknown => (unknown.RawJson, unknown.RawJson.Length * 2L),
            _ => ((object?)null, 0L),
        };
        if (payload is null || size == 0) return change;
        if (delta > 0)
        {
            ref Payload counted = ref CollectionsMarshal.GetValueRefOrAddDefault(payloads, payload, out bool exists);
            if (!exists) change += size;
            counted.References++;
            counted.Size = size;
        }
        else
        {
            ref Payload counted = ref CollectionsMarshal.GetValueRefOrNullRef(payloads, payload);
            if (Unsafe.IsNullRef(ref counted)) return change;
            if (--counted.References == 0)
            {
                change -= counted.Size;
                payloads.Remove(payload);
            }
        }
        return change;
    }

    /// <summary>How many references the counted steps hold to a payload, and its estimated size.</summary>
    private struct Payload
    {
        public int References;
        public long Size;
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
