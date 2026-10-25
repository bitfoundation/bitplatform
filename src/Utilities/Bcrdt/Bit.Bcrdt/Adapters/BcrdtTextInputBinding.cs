namespace Bit.Bcrdt;

/// <summary>
/// What a plain-text control should be updated to, and what it must still contain for the update to
/// be safe.
/// </summary>
/// <param name="Value">The new value to show.</param>
/// <param name="Expected">
/// The value the control is believed to hold right now. If it holds anything else the user has typed
/// since this was produced; the update must be skipped and retried after that input is processed.
/// </param>
/// <param name="SelectionStart">The selection to restore, in <paramref name="Value"/> coordinates.</param>
/// <param name="SelectionEnd">The end of the selection to restore.</param>
/// <param name="Sequence">Identifies the document state <paramref name="Value"/> was taken from.</param>
public sealed record BcrdtTextControlUpdate(
    string Value,
    string Expected,
    int SelectionStart,
    int SelectionEnd,
    long Sequence)
{
    /// <summary>
    /// Identifies this update among the ones the binding has produced, increasing from one. A control
    /// remembers the identifier of the last update it applied and reports it back, so the binding can
    /// tell whether an update whose confirmation was lost was applied - see
    /// <see cref="BcrdtTextInputBinding.Resolve"/>.
    /// </summary>
    public long Id { get; init; }

    internal BcrdtTextProjection Projection { get; init; } = null!;
}

/// <summary>
/// Keeps a plain-text control and a collaborative text field in step while both change at once.
/// </summary>
/// <remarks>
/// <para>
/// A control reports the user's work as a whole new value, and the value it reports was computed
/// from whatever the control was showing at the time - which, during collaborative editing, is
/// routinely <i>older</i> than the document: a collaborator's change has been applied to the document
/// but not yet rendered. Diffing the reported value against the document would read that unrendered
/// change as something the user deleted, and erase it. Diffing against the last value the control
/// is known to show, and translating the result through everything the document has done since,
/// recovers exactly the user's edit.
/// </para>
/// <para>
/// The binding therefore tracks the control's content (<see cref="Value"/>) separately from the
/// document, along with the position in the document of every character the control shows. Changes
/// that did not come through the binding update those positions without touching the control's
/// content; <see cref="CreateUpdate"/> and <see cref="Acknowledge"/> bring the control up to date
/// when it is safe to do so.
/// </para>
/// <para>
/// Line endings are normalized to <c>"\n"</c> and embedded objects are hidden, as in a browser
/// <c>&lt;textarea&gt;</c>; positions are translated so neither shifts an edit.
/// </para>
/// <para>
/// Like the document, the binding is not thread-safe, and it must be used on the context the
/// document is changed on - in Blazor the renderer's, which is where a document opened through the
/// collaboration service applies network updates. Positions are moved synchronously, inside each
/// change notification, because they are only meaningful against the document state that change
/// produced; deferring that work would leave them describing a document that has since moved on.
/// Only <see cref="Changed"/> goes through the <c>dispatch</c> delegate.
/// </para>
/// </remarks>
public sealed class BcrdtTextInputBinding : IDisposable
{
    private readonly BcrdtText _text;
    private readonly object? _origin;
    private readonly Action<Action> _dispatch;
    private readonly IDisposable _subscription;
    private readonly List<(long Sequence, IReadOnlyList<BcrdtDeltaOperation> Delta)> _log = [];

    private List<BcrdtTextSegment> _segments;
    private string _value;
    private long _sequence;
    private BcrdtTransaction? _ownTransaction;
    private (string Value, int SelectionStart, int SelectionEnd)? _deferredInput;
    private BcrdtTextControlUpdate? _outstanding;
    private long _lastUpdateId;
    private bool _dirty;
    private bool _disposed;

    /// <summary>Binds a collaborative text field.</summary>
    /// <param name="text">The text field the control edits.</param>
    /// <param name="dispatch">
    /// Delivers <see cref="Changed"/> - for example onto a UI framework's dispatcher, so the handler
    /// can touch component state. Defaults to raising it inline. Everything else the binding does in
    /// response to a change happens synchronously, inside the change notification.
    /// </param>
    /// <param name="origin">
    /// The transaction origin for edits made through the binding. Defaults to <see langword="null"/>,
    /// the origin a <see cref="BcrdtUndoManager"/> tracks by default, so the user's typing is
    /// undoable.
    /// </param>
    public BcrdtTextInputBinding(BcrdtText text, Action<Action>? dispatch = null, object? origin = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        _text = text;
        _origin = origin;
        _dispatch = dispatch ?? (static work => work());

        BcrdtTextProjection projection = BcrdtTextProjection.Create(text, normalizeLineEndings: true);
        _value = projection.Display;
        _segments = projection.CreateSegments();
        _subscription = text.Observe(OnTextChanged);
    }

    /// <summary>The text field the control edits.</summary>
    public BcrdtText Text => _text;

    /// <summary>The value the control is believed to show.</summary>
    public string Value => _value;

    /// <summary>Whether the document has changed in ways the control does not show yet.</summary>
    public bool HasPendingChanges => _dirty;

    /// <summary>The start of the control's selection, in <see cref="Value"/> coordinates.</summary>
    public int SelectionStart { get; private set; }

    /// <summary>The end of the control's selection, in <see cref="Value"/> coordinates.</summary>
    public int SelectionEnd { get; private set; }

    /// <summary>
    /// Raised, through the dispatch delegate, when the document changed in a way the control does not
    /// show yet. Respond by sending the control an update from <see cref="CreateUpdate"/>.
    /// </summary>
    public event Action? Changed;

    /// <summary>Whether an update has been created and not yet acknowledged, rejected or resolved.</summary>
    public bool HasOutstandingUpdate => _outstanding is not null;

    /// <summary>
    /// Applies a new value reported by the control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is taken to be an edit of <see cref="Value"/>. If an update may have reached the
    /// control without its confirmation reaching the binding, call <see cref="Resolve"/> with what the
    /// control reports first, so the edit is read against the content the control really had.
    /// </para>
    /// <para>
    /// Called while a transaction is open - or while one is still being settled, from inside a
    /// change notification - the edit is applied once the document has settled, in a transaction
    /// of its own, rather than joining it. The tracked positions describe the document as of the
    /// last settled change, so an edit placed inside a transaction that has already changed the text
    /// would land in the wrong place, and one sharing a transaction with later changes could not be
    /// told apart from them. Until then <see cref="Value"/> still reports the previous value; a later
    /// call supersedes an input still waiting.
    /// </para>
    /// </remarks>
    /// <param name="value">The control's whole new value.</param>
    /// <param name="selectionStart">The control's selection start after the edit.</param>
    /// <param name="selectionEnd">The control's selection end after the edit; the caret.</param>
    /// <returns>
    /// <see langword="true"/> if the document changed - or, inside a transaction, will change once
    /// the document has settled.
    /// </returns>
    public bool ApplyInput(string value, int selectionStart, int selectionEnd)
    {
        ArgumentNullException.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_text.Doc.HasUnsettledTransactions)
        {
            if (_deferredInput is null) _text.Doc.Settled += OnDocumentSettled;
            _deferredInput = (value, selectionStart, selectionEnd);
            return !string.Equals(value, _value, StringComparison.Ordinal);
        }

        // Whatever was waiting described an older state of the control than this does.
        CancelDeferredInput();

        SetSelectionCore(value.Length, selectionStart, selectionEnd);
        if (string.Equals(value, _value, StringComparison.Ordinal)) return false;

        BcrdtTextEdit edit = BcrdtTextSynchronizer.Diff(_value, value, SelectionEnd);
        int end = edit.Index + edit.DeletedLength;

        SplitAt(edit.Index);
        SplitAt(end);
        int first = SegmentIndexAt(edit.Index);
        int last = SegmentIndexAt(end);

        // The document ranges holding the characters the user removed. Characters a collaborator has
        // already deleted hold nothing; text a collaborator inserted among them is not the user's to
        // delete and is not included.
        List<(int Start, int Length)> deletions = [];
        int deletedTotal = 0;
        for (int i = first; i < last; i++)
        {
            BcrdtTextSegment segment = _segments[i];
            if (segment.IsCollapsed) continue;
            deletedTotal += segment.DocumentLength;
            if (deletions.Count > 0 && deletions[^1].Start + deletions[^1].Length == segment.DocumentStart)
            {
                deletions[^1] = (deletions[^1].Start, deletions[^1].Length + segment.DocumentLength);
            }
            else
            {
                deletions.Add((segment.DocumentStart, segment.DocumentLength));
            }
        }

        // A replacement goes where the replaced text was. A pure insertion goes immediately after the
        // character to its left, wherever that character now is.
        int insertAt = deletions.Count > 0
            ? deletions[0].Start
            : first > 0 ? _segments[first - 1].DocumentEnd : 0;

        if (insertAt > _text.Count || (deletions.Count > 0 && deletions[^1].Start + deletions[^1].Length > _text.Count))
        {
            // The tracked positions no longer describe the document. That should not happen; if it
            // does, fall back to a plain diff against the document rather than editing blindly.
            Resynchronize(value, SelectionEnd);
            return true;
        }

        int inserted = edit.Inserted.Length;
        List<BcrdtTextSegment> next = new(_segments.Count + 1);
        for (int i = 0; i < first; i++) next.Add(_segments[i]);
        if (inserted > 0) AddMerged(next, new BcrdtTextSegment(inserted, insertAt, inserted));
        for (int i = last; i < _segments.Count; i++)
        {
            BcrdtTextSegment segment = _segments[i];
            AddMerged(next, segment with { DocumentStart = segment.DocumentStart - deletedTotal + inserted });
        }

        bool committed = false;
        try
        {
            _text.Doc.Transact(transaction =>
            {
                _ownTransaction = transaction;
                for (int i = deletions.Count - 1; i >= 0; i--) _text.Delete(deletions[i].Start, deletions[i].Length);
                if (inserted > 0) _text.Insert(insertAt, edit.Inserted);

                // Committed before the transaction settles, not after Transact returns: a change an
                // observer makes in response settles straight after this one, and its notification
                // has to move positions that already include this edit.
                _segments = next;
                _value = value;
                committed = true;
            }, _origin);
        }
        catch (Exception) when (!committed)
        {
            // The edit failed partway. A failure once it was committed - another subscriber throwing
            // while the transaction settled - leaves the edit standing, and is simply rethrown.
            Resynchronize(value, SelectionEnd);
            throw;
        }

        return true;
    }

    /// <summary>
    /// Applies an edit the control reported as a change - <paramref name="removedLength"/> characters at
    /// <paramref name="index"/> replaced by <paramref name="inserted"/> - rather than as its whole value.
    /// </summary>
    /// <remarks>
    /// The change is read against what the control is believed to hold: <see cref="Value"/>, or the
    /// value of an input still waiting for the document to settle. A control that reports changes sends
    /// a few bytes per keystroke instead of the whole text, which is what keeps a long document editable
    /// over a connection with a message size limit - Blazor Server's is 32 KB by default.
    /// </remarks>
    /// <returns>
    /// <see langword="false"/> if the change does not fit the value the control is believed to hold,
    /// which means the two have drifted apart: have the control report its whole value instead, through
    /// <see cref="ApplyInput"/>. <see langword="true"/> otherwise, whether or not the document changed.
    /// </returns>
    public bool TryApplyInputChange(int index, int removedLength, string inserted, int selectionStart, int selectionEnd)
    {
        ArgumentNullException.ThrowIfNull(inserted);
        ObjectDisposedException.ThrowIf(_disposed, this);

        string current = _deferredInput?.Value ?? _value;
        if (index < 0 || removedLength < 0 || index > current.Length || removedLength > current.Length - index) return false;

        string value = string.Concat(current.AsSpan(0, index), inserted, current.AsSpan(index + removedLength));
        ApplyInput(value, selectionStart, selectionEnd);
        return true;
    }

    /// <summary>Records the control's selection.</summary>
    public void SetSelection(int selectionStart, int selectionEnd)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SetSelectionCore(_value.Length, selectionStart, selectionEnd);
    }

    /// <summary>
    /// The document position corresponding to a position in <see cref="Value"/>, for publishing a
    /// caret or a selection.
    /// </summary>
    public int ToDocumentIndex(int valueIndex)
    {
        if (valueIndex <= 0) return 0;

        int position = 0;
        foreach (BcrdtTextSegment segment in _segments)
        {
            if (valueIndex <= position + segment.DisplayLength)
            {
                int offset = valueIndex - position;
                if (segment.IsCollapsed) return segment.DocumentStart;
                if (segment.IsOneToOne) return segment.DocumentStart + offset;
                return segment.DocumentEnd;
            }
            position += segment.DisplayLength;
        }

        return _segments.Count > 0 ? _segments[^1].DocumentEnd : 0;
    }

    /// <summary>
    /// Describes the update that brings the control in line with the document.
    /// </summary>
    /// <remarks>
    /// Send it to the control only if the control still holds <see cref="BcrdtTextControlUpdate.Expected"/>,
    /// and have the control remember <see cref="BcrdtTextControlUpdate.Id"/> when it applies it. Then call
    /// <see cref="Acknowledge"/> or <see cref="Reject"/> - or, if the control's answer was lost,
    /// <see cref="Resolve"/> with the identifier the control reports. Only one update can be
    /// outstanding at a time.
    /// </remarks>
    /// <exception cref="InvalidOperationException">An earlier update has not been settled.</exception>
    public BcrdtTextControlUpdate CreateUpdate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_outstanding is not null)
        {
            throw new InvalidOperationException(
                "An update is already outstanding. Acknowledge, reject or resolve it before creating another.");
        }

        BcrdtTextProjection projection = BcrdtTextProjection.Create(_text, normalizeLineEndings: true);
        int start = projection.ToDisplay(ToDocumentIndex(SelectionStart));
        int end = projection.ToDisplay(ToDocumentIndex(SelectionEnd));

        // Everything logged so far is reflected in the projection; only what follows will need
        // replaying when the control confirms it has the new value.
        _log.Clear();

        _outstanding = new BcrdtTextControlUpdate(projection.Display, _value, Math.Min(start, end), Math.Max(start, end), _sequence)
        {
            Id = ++_lastUpdateId,
            Projection = projection,
        };
        return _outstanding;
    }

    /// <summary>
    /// Settles the outstanding update from what the control reports, for when the control's own
    /// answer was lost - a timed-out call, a dropped connection.
    /// </summary>
    /// <param name="lastAppliedUpdateId">
    /// The <see cref="BcrdtTextControlUpdate.Id"/> of the last update the control applied, or zero if it
    /// has applied none.
    /// </param>
    /// <remarks>
    /// If the control applied the outstanding update it is acknowledged; if the control reports an
    /// older one, it never applied it, and it is rejected. Without this, an update that was applied but
    /// never confirmed would leave the binding believing the control still showed the old value, and
    /// the user's next keystroke would be read as an edit of text they were no longer looking at.
    /// </remarks>
    public void Resolve(long lastAppliedUpdateId)
    {
        if (_disposed || _outstanding is not { } update) return;

        if (lastAppliedUpdateId == update.Id) Acknowledge(update);
        else if (lastAppliedUpdateId < update.Id) Reject(update);
    }

    /// <summary>
    /// Records that <paramref name="update"/> was not applied because the control no longer held
    /// <see cref="BcrdtTextControlUpdate.Expected"/>. Create a fresh update once the pending input has
    /// been processed. Ignored if the update has already been settled.
    /// </summary>
    public void Reject(BcrdtTextControlUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (!ReferenceEquals(update, _outstanding)) return;

        _outstanding = null;
        _log.Clear();
    }

    /// <summary>
    /// Records that the control now shows <paramref name="update"/>'s value. Ignored if the update
    /// has already been settled.
    /// </summary>
    public void Acknowledge(BcrdtTextControlUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!ReferenceEquals(update, _outstanding)) return;

        _outstanding = null;
        _value = update.Value;
        _segments = update.Projection.CreateSegments();

        bool replayed = false;
        foreach ((long sequence, IReadOnlyList<BcrdtDeltaOperation> delta) in _log)
        {
            if (sequence <= update.Sequence) continue;
            _segments = Transform(_segments, delta);
            replayed = true;
        }
        _log.Clear();

        _dirty = replayed;
        SetSelectionCore(_value.Length, update.SelectionStart, update.SelectionEnd);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CancelDeferredInput();
        _subscription.Dispose();
        Changed = null;
    }

    private void OnTextChanged(BcrdtEvent crdtEvent, BcrdtTransaction transaction)
    {
        if (_disposed) return;

        // The change is processed here, synchronously: the description is only valid while the
        // transaction is being settled, and the tracked positions must move in step with the very
        // document state this change produced. Only the notification is dispatched.
        IReadOnlyList<BcrdtDeltaOperation> delta = [.. (crdtEvent is BcrdtTextEvent textEvent ? textEvent.Delta : crdtEvent.Delta)];
        Log(delta);

        // The binding's own edit, which already moved the positions. Nothing else shares its
        // transaction: input that arrives while another transaction is open or settling waits for
        // the document to settle rather than joining it.
        if (ReferenceEquals(transaction, _ownTransaction))
        {
            _ownTransaction = null;
            return;
        }

        _segments = Transform(_segments, delta);
        _dirty = true;
        _dispatch(RaiseChanged);
    }

    private void OnDocumentSettled()
    {
        if (_deferredInput is not { } input) return;
        CancelDeferredInput();

        // Every change notification has moved the positions by now.
        if (!_disposed) ApplyInput(input.Value, input.SelectionStart, input.SelectionEnd);
    }

    private void CancelDeferredInput()
    {
        if (_deferredInput is null) return;

        _text.Doc.Settled -= OnDocumentSettled;
        _deferredInput = null;
    }

    private void RaiseChanged()
    {
        if (!_disposed) Changed?.Invoke();
    }

    private void Resynchronize(string controlValue, int caret)
    {
        // Last resort: make the document read what the control shows, diffing against the same
        // normalized view the control has so a "\r\n" elsewhere does not widen the edit to most of
        // the text. Then have the control refreshed from the document. As in ApplyInput, the positions
        // are rebuilt inside the transaction, so the change notifications that follow it see them.
        _text.Doc.Transact(transaction =>
        {
            _ownTransaction = transaction;

            BcrdtTextProjection current = BcrdtTextProjection.Create(_text, normalizeLineEndings: true);
            BcrdtTextSynchronizer.ApplyProjected(_text, current, controlValue, _origin, caret);

            BcrdtTextProjection projection = BcrdtTextProjection.Create(_text, normalizeLineEndings: true);
            _segments = projection.CreateSegments();
            _value = controlValue;
            _dirty = !string.Equals(projection.Display, controlValue, StringComparison.Ordinal);
        }, _origin);
    }

    private void Log(IReadOnlyList<BcrdtDeltaOperation> delta)
    {
        _sequence++;

        // Changes only need replaying when an update is on its way to the control; otherwise the
        // tracked positions are already current and keeping the history would only grow memory.
        if (_outstanding is not null) _log.Add((_sequence, delta));
    }

    private void SetSelectionCore(int length, int start, int end)
    {
        start = Math.Clamp(start, 0, length);
        end = Math.Clamp(end, 0, length);
        SelectionStart = Math.Min(start, end);
        SelectionEnd = Math.Max(start, end);
    }

    private int SegmentIndexAt(int displayPosition)
    {
        int position = 0;
        for (int i = 0; i < _segments.Count; i++)
        {
            if (position >= displayPosition) return i;
            position += _segments[i].DisplayLength;
        }
        return _segments.Count;
    }

    private void SplitAt(int displayPosition)
    {
        int position = 0;
        for (int i = 0; i < _segments.Count; i++)
        {
            BcrdtTextSegment segment = _segments[i];
            int end = position + segment.DisplayLength;
            if (displayPosition > position && displayPosition < end)
            {
                int left = displayPosition - position;
                int right = segment.DisplayLength - left;
                if (segment.IsCollapsed)
                {
                    _segments[i] = segment with { DisplayLength = left };
                    _segments.Insert(i + 1, segment with { DisplayLength = right });
                }
                else
                {
                    // Only one-to-one runs span several displayed characters.
                    _segments[i] = new BcrdtTextSegment(left, segment.DocumentStart, left);
                    _segments.Insert(i + 1, new BcrdtTextSegment(right, segment.DocumentStart + left, right));
                }
                return;
            }
            if (end >= displayPosition) return;
            position = end;
        }
    }

    private static void AddMerged(List<BcrdtTextSegment> segments, BcrdtTextSegment segment)
    {
        if (segment.DisplayLength == 0) return;

        if (segments.Count > 0)
        {
            BcrdtTextSegment last = segments[^1];
            if (last.IsOneToOne && segment.IsOneToOne && last.DocumentEnd == segment.DocumentStart && !last.IsCollapsed && !segment.IsCollapsed)
            {
                segments[^1] = new BcrdtTextSegment(
                    last.DisplayLength + segment.DisplayLength, last.DocumentStart, last.DocumentLength + segment.DocumentLength);
                return;
            }

            if (last.IsCollapsed && segment.IsCollapsed && last.DocumentStart == segment.DocumentStart)
            {
                segments[^1] = last with { DisplayLength = last.DisplayLength + segment.DisplayLength };
                return;
            }
        }

        segments.Add(segment);
    }

    /// <summary>Moves every tracked position through a change to the document.</summary>
    internal static List<BcrdtTextSegment> Transform(List<BcrdtTextSegment> segments, IReadOnlyList<BcrdtDeltaOperation> delta)
    {
        var cursor = new DeltaCursor(delta);
        List<BcrdtTextSegment> result = new(segments.Count + 2);

        foreach (BcrdtTextSegment segment in segments)
        {
            int start = Math.Max(segment.DocumentStart, cursor.OldPosition);

            // Content inserted exactly where the segment starts comes before it.
            cursor.AdvanceTo(start);

            if (segment.IsCollapsed)
            {
                AddMerged(result, segment with { DocumentStart = cursor.NewPosition });
                continue;
            }

            int end = start + segment.DocumentLength;

            if (segment.IsOneToOne)
            {
                while (cursor.OldPosition < end)
                {
                    int length = cursor.TakeRun(end, out bool deleted, out int runStart);
                    if (length == 0) break;
                    AddMerged(result, deleted
                        ? new BcrdtTextSegment(length, runStart, 0)
                        : new BcrdtTextSegment(length, runStart, length));
                }
                continue;
            }

            // A single displayed character over several document units: a normalized "\r\n".
            int retained = 0;
            int firstRetained = -1;
            int collapsedAt = cursor.NewPosition;
            while (cursor.OldPosition < end)
            {
                int length = cursor.TakeRun(end, out bool deleted, out int runStart);
                if (length == 0) break;
                if (deleted)
                {
                    if (firstRetained < 0) collapsedAt = runStart;
                    continue;
                }

                if (firstRetained < 0) firstRetained = runStart;
                else if (firstRetained + retained != runStart) break;
                retained += length;
            }

            AddMerged(result, firstRetained < 0
                ? new BcrdtTextSegment(1, collapsedAt, 0)
                : new BcrdtTextSegment(1, firstRetained, retained));
        }

        return result;
    }

    /// <summary>Walks a delta in document order, tracking positions before and after it.</summary>
    private sealed class DeltaCursor(IReadOnlyList<BcrdtDeltaOperation> operations)
    {
        private int _index;
        private int _offset;

        public int OldPosition { get; private set; }

        public int NewPosition { get; private set; }

        /// <summary>
        /// Advances to <paramref name="target"/> in the old document, consuming insertions located
        /// before or at it.
        /// </summary>
        public void AdvanceTo(int target)
        {
            while (true)
            {
                if (_index >= operations.Count)
                {
                    NewPosition += target - OldPosition;
                    OldPosition = target;
                    return;
                }

                BcrdtDeltaOperation operation = operations[_index];
                if (operation.Kind == BcrdtDeltaKind.Insert)
                {
                    NewPosition += InsertedLength(operation);
                    _index++;
                    continue;
                }

                int available = operation.Length - _offset;
                if (available <= 0)
                {
                    _index++;
                    _offset = 0;
                    continue;
                }

                if (OldPosition >= target) return;

                int step = Math.Min(available, target - OldPosition);
                if (operation.Kind == BcrdtDeltaKind.Retain) NewPosition += step;
                OldPosition += step;
                _offset += step;
            }
        }

        /// <summary>
        /// Takes the next run of old-document content up to <paramref name="limit"/>, after any
        /// insertions at the current position.
        /// </summary>
        /// <returns>The run's length; zero when nothing remains before the limit.</returns>
        public int TakeRun(int limit, out bool deleted, out int newStart)
        {
            while (true)
            {
                if (_index >= operations.Count)
                {
                    deleted = false;
                    newStart = NewPosition;
                    int rest = Math.Max(0, limit - OldPosition);
                    OldPosition += rest;
                    NewPosition += rest;
                    return rest;
                }

                BcrdtDeltaOperation operation = operations[_index];
                if (operation.Kind == BcrdtDeltaKind.Insert)
                {
                    NewPosition += InsertedLength(operation);
                    _index++;
                    continue;
                }

                int available = operation.Length - _offset;
                if (available <= 0)
                {
                    _index++;
                    _offset = 0;
                    continue;
                }

                int step = Math.Min(available, Math.Max(0, limit - OldPosition));
                deleted = operation.Kind == BcrdtDeltaKind.Delete;
                newStart = NewPosition;
                if (!deleted) NewPosition += step;
                OldPosition += step;
                _offset += step;
                return step;
            }
        }

        // Always the operation's length in document units: an embedded string occupies one position,
        // however many characters it holds.
        private static int InsertedLength(BcrdtDeltaOperation operation) => operation.Length;
    }
}
