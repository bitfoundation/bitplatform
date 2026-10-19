namespace Bit.Bmage.Editing;

/// <summary>One entry in the edit history.</summary>
public sealed record HistoryEntry(ImageDocument Document, string Label, DateTimeOffset Timestamp)
{
    /// <summary>Rough memory cost of this entry: documents are descriptions, not pixels, so this is small.</summary>
    public int EstimatedSize => 128 + Document.Pipeline.Count * 64 + Document.Annotations.Count * 256;
}

/// <summary>
/// Undo/redo over <see cref="ImageDocument"/> values.
/// </summary>
/// <remarks>
/// Because a document describes the edit rather than holding pixels, one history entry costs a few hundred bytes
/// instead of a full-resolution image. A 4K photo with fifty undo steps needs kilobytes, not gigabytes. The depth limit
/// exists to bound annotation-heavy sessions, not pixel memory.
/// </remarks>
public sealed class EditHistory
{
    private readonly List<HistoryEntry> _entries = [];
    private int _position = -1;
    private bool _transientOpen;

    /// <summary>Creates a history whose initial state is <paramref name="initial"/>.</summary>
    public EditHistory(ImageDocument initial, int maxDepth = 100, string label = "Original")
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (maxDepth < 1) throw new ArgumentOutOfRangeException(nameof(maxDepth));
        MaxDepth = maxDepth;
        _entries.Add(new HistoryEntry(initial, label, DateTimeOffset.UtcNow));
        _position = 0;
    }

    /// <summary>Maximum number of entries kept. Older entries are discarded first; the oldest is always the original.</summary>
    public int MaxDepth { get; }

    /// <summary>The current document.</summary>
    public ImageDocument Current => _entries[_position].Document;

    /// <summary>The current entry including its label.</summary>
    public HistoryEntry CurrentEntry => _entries[_position];

    /// <summary>All entries, oldest first.</summary>
    public IReadOnlyList<HistoryEntry> Entries => _entries;

    /// <summary>Index of the current entry in <see cref="Entries"/>.</summary>
    public int Position => _position;

    /// <summary>True when <see cref="Undo"/> would change the state.</summary>
    public bool CanUndo => _position > 0;

    /// <summary>True when <see cref="Redo"/> would change the state.</summary>
    public bool CanRedo => _position < _entries.Count - 1;

    /// <summary>
    /// True while the current entry was opened by <see cref="PushTransient"/> and has not been committed yet.
    /// </summary>
    public bool IsTransientOpen => _transientOpen && _position == _entries.Count - 1;

    /// <summary>Approximate memory held by the history, in bytes.</summary>
    public long EstimatedMemory => _entries.Sum(e => (long)e.EstimatedSize);

    /// <summary>Raised after any change to the current document.</summary>
    public event Action<ImageDocument>? Changed;

    /// <summary>
    /// Records a new state. Anything after the current position is discarded, which is what makes the history a linear
    /// timeline: editing after an undo starts a new branch and forgets the old one.
    /// </summary>
    public void Push(ImageDocument document, string label)
    {
        ArgumentNullException.ThrowIfNull(document);
        _transientOpen = false;
        if (_position < _entries.Count - 1) _entries.RemoveRange(_position + 1, _entries.Count - _position - 1);
        _entries.Add(new HistoryEntry(document, label, DateTimeOffset.UtcNow));
        // Keep the original at index 0, and never trim the entry just added: with a depth of 1 that would lose the edit.
        if (_entries.Count > Math.Max(2, MaxDepth)) _entries.RemoveAt(1);
        _position = _entries.Count - 1;
        Changed?.Invoke(Current);
    }

    /// <summary>
    /// Replaces the current entry instead of adding one. Use while a value is being dragged so a slider does not create
    /// hundreds of undo steps; push once when the interaction ends.
    /// </summary>
    public void Amend(ImageDocument document, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_position == 0)
        {
            // Never overwrite the original state.
            Push(document, label ?? "Edit");
            return;
        }
        _entries[_position] = _entries[_position] with { Document = document, Label = label ?? _entries[_position].Label };
        Changed?.Invoke(Current);
    }

    /// <summary>
    /// Records a state that is still being dragged. The first call of an interaction adds a new entry, so the state
    /// before the interaction stays reachable by undo; later calls replace that entry. Finish the interaction with
    /// <see cref="CommitTransient"/>, or with <see cref="Push"/>, which starts a new entry after it.
    /// </summary>
    public void PushTransient(ImageDocument document, string label)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (IsTransientOpen)
        {
            _entries[_position] = _entries[_position] with { Document = document };
            Changed?.Invoke(Current);
            return;
        }
        Push(document, label);
        _transientOpen = true;
    }

    /// <summary>
    /// Finishes the interaction opened by <see cref="PushTransient"/>, storing its final state under
    /// <paramref name="label"/>. Returns false, changing nothing, when no transient entry is open.
    /// </summary>
    public bool CommitTransient(ImageDocument document, string label)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!IsTransientOpen) return false;
        _transientOpen = false;
        var changed = !ReferenceEquals(document, Current);
        _entries[_position] = _entries[_position] with { Document = document, Label = label };
        if (changed) Changed?.Invoke(Current);
        return true;
    }

    /// <summary>Steps back one entry. Returns the new current document.</summary>
    public ImageDocument Undo()
    {
        if (CanUndo)
        {
            _transientOpen = false;
            _position--;
            Changed?.Invoke(Current);
        }
        return Current;
    }

    /// <summary>Steps forward one entry. Returns the new current document.</summary>
    public ImageDocument Redo()
    {
        if (CanRedo)
        {
            _transientOpen = false;
            _position++;
            Changed?.Invoke(Current);
        }
        return Current;
    }

    /// <summary>Jumps to an arbitrary entry, e.g. from a history panel.</summary>
    public ImageDocument GoTo(int index)
    {
        if (index < 0 || index >= _entries.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (index != _position)
        {
            _transientOpen = false;
            _position = index;
            Changed?.Invoke(Current);
        }
        return Current;
    }

    /// <summary>Returns to the original state, discarding every edit but keeping the redo trail.</summary>
    public ImageDocument Reset() => GoTo(0);

    /// <summary>Discards all entries and starts again from <paramref name="document"/>.</summary>
    public void Clear(ImageDocument document, string label = "Original")
    {
        ArgumentNullException.ThrowIfNull(document);
        _transientOpen = false;
        _entries.Clear();
        _entries.Add(new HistoryEntry(document, label, DateTimeOffset.UtcNow));
        _position = 0;
        Changed?.Invoke(Current);
    }
}
