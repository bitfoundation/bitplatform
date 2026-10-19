using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// In-app undo/redo for the whole application state, independent of the Redux DevTools extension.
/// Snapshots the global state after each action and lets you step backwards/forwards. Restoring a
/// snapshot does not record a new action, so undo/redo never pollutes the history.
/// </summary>
/// <remarks>
/// Call <see cref="Start"/> once after all stores are registered to capture the baseline snapshot.
/// Single-threaded dispatch is assumed (Blazor's model).
/// </remarks>
public sealed class BlexHistory : IDisposable
{
    private readonly record struct BlexEntry(JsonObject State, string? Label);

    private readonly BlexManager _manager;
    private readonly List<BlexEntry> _undo = [];
    private readonly List<BlexEntry> _redo = [];
    private readonly Func<BlexActionContext, bool>? _filter;
    private readonly Func<BlexActionContext, string?>? _groupBy;
    private string? _lastGroupKey;
    private JsonObject? _present;

    // The state as of the last undo boundary (a recorded action, a checkpoint, a restore). It
    // trails _present across filtered actions, which advance the present without creating a step;
    // Checkpoint needs it to know whether anything has actually happened since the last boundary,
    // and which state that boundary should push.
    private JsonObject? _boundary;
    private string? _presentLabel;
    private bool _started;
    private bool _restoring;

    /// <summary>Creates a history bound to a manager. Set <paramref name="maxEntries"/> to cap memory.</summary>
    /// <param name="manager">The manager whose actions are recorded.</param>
    /// <param name="maxEntries">Maximum number of undo entries retained; older entries are discarded.</param>
    /// <param name="filter">
    /// Optional predicate deciding which actions get their own undo step (redux-undo's
    /// <c>filter</c>). Return <c>false</c> to keep an action out of the history: its state change
    /// still applies and still moves the present forward, it simply does not become a separate
    /// undo step, so the next <see cref="Undo"/> reverts it together with the recorded action
    /// before it. Use it to keep keystroke-level noise (<c>Set Query</c>, cursor moves, telemetry
    /// stores) out of an undo stack the user thinks in terms of.
    /// </param>
    /// <param name="groupBy">
    /// Optional key selector that merges <em>consecutive</em> actions into a single undo step
    /// (redux-undo's <c>groupBy</c>). Return the same non-<c>null</c> key for actions that belong
    /// together -- typing into one field, dragging one shape -- and they collapse into one step;
    /// return <c>null</c> to give the action its own step. Where <paramref name="filter"/> drops an
    /// action from the stack entirely, this keeps it but folds it into the step already open.
    /// </param>
    /// <example><code>
    /// new BlexHistory(manager, filter: ctx => !ctx.ActionName.StartsWith("Set "));
    ///
    /// // one undo step per edited field, however many keystrokes it took:
    /// new BlexHistory(manager, groupBy: ctx => ctx.ActionName.StartsWith("Set ") ? ctx.QualifiedName : null);
    /// </code></example>
    public BlexHistory(
        BlexManager manager,
        int maxEntries = 100,
        Func<BlexActionContext, bool>? filter = null,
        Func<BlexActionContext, string?>? groupBy = null)
    {
        ArgumentNullException.ThrowIfNull(manager);
        if (maxEntries < 1)
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        _manager = manager;
        MaxEntries = maxEntries;
        _filter = filter;
        _groupBy = groupBy;
    }

    /// <summary>Maximum number of undo entries retained; older entries are discarded.</summary>
    public int MaxEntries { get; }

    /// <summary>Raised whenever undo/redo availability changes (handy for binding button state).</summary>
    /// <remarks>
    /// Handlers are invoked one at a time with their exceptions isolated and reported through
    /// <see cref="BlexManager.OnError"/>: this event fires from inside the dispatch pipeline, where
    /// a throwing UI handler would otherwise starve the observers behind it (persistence included).
    /// </remarks>
    public event Action? Changed;

    // Rendered label lists, rebuilt on demand. Every mutation of the stacks ends in RaiseChanged,
    // so invalidating there is enough -- and it matters, because the natural way to render a
    // history panel reads these once per item, which without a cache is quadratic.
    private IReadOnlyList<string?>? _undoLabels;
    private IReadOnlyList<string?>? _redoLabels;

    private void RaiseChanged()
    {
        _undoLabels = null;
        _redoLabels = null;
        _manager.InvokeIsolated<object?>(Changed, null, "history", null);
    }

    /// <summary>Whether there is a prior state to undo to.</summary>
    public bool CanUndo => _undo.Count > 0;

    /// <summary>Whether there is a state to redo to.</summary>
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Number of states available to undo to.</summary>
    public int UndoCount => _undo.Count;

    /// <summary>Number of states available to redo to.</summary>
    public int RedoCount => _redo.Count;

    /// <summary>
    /// The qualified name of the action that <see cref="Undo"/> would revert (e.g. for an
    /// "Undo Increment" button label), or <c>null</c> when nothing can be undone.
    /// </summary>
    public string? NextUndoLabel => CanUndo ? _presentLabel : null;

    /// <summary>
    /// The qualified name of the action that <see cref="Redo"/> would re-apply, or <c>null</c>
    /// when nothing can be redone.
    /// </summary>
    public string? NextRedoLabel => _redo.Count > 0 ? _redo[^1].Label : null;

    /// <summary>
    /// The labels of the available undo steps, nearest first -- the list a history panel renders,
    /// where item <c>i</c> is what <c>JumpToPast(UndoCount - 1 - i)</c> (equivalently
    /// <c>Jump(-(i + 1))</c>) would revert.
    /// </summary>
    public IReadOnlyList<string?> UndoLabels
    {
        get
        {
            if (_undoLabels is not null)
                return _undoLabels;

            if (_undo.Count == 0)
                return _undoLabels = [];

            // Entry i holds the state *before* the action labelled by entry i+1 (the newest
            // boundary's label lives in _presentLabel), so stepping back reverts _presentLabel
            // first and the stack's labels after it.
            var labels = new string?[_undo.Count];
            labels[0] = _presentLabel;
            for (var i = 1; i < _undo.Count; i++)
                labels[i] = _undo[_undo.Count - i].Label;

            return _undoLabels = labels;
        }
    }

    /// <summary>
    /// The labels of the available redo steps, nearest first -- item <c>i</c> is what
    /// <c>JumpToFuture(i)</c> (equivalently <c>Jump(i + 1)</c>) would re-apply.
    /// </summary>
    public IReadOnlyList<string?> RedoLabels
    {
        get
        {
            if (_redoLabels is not null)
                return _redoLabels;

            if (_redo.Count == 0)
                return _redoLabels = [];

            var labels = new string?[_redo.Count];
            for (var i = 0; i < _redo.Count; i++)
                labels[i] = _redo[_redo.Count - 1 - i].Label;

            return _redoLabels = labels;
        }
    }

    /// <summary>Captures the current state as the baseline and begins recording. Idempotent.</summary>
    public void Start()
    {
        if (_started)
            return;
        _started = true;
        _present = _boundary = _manager.CaptureGlobalState();
        _presentLabel = null;
        _manager.ActionDispatched += OnAction;
        _manager.StateRestored += OnExternalRestore;
    }

    private void OnExternalRestore()
    {
        if (_restoring)
            return;

        try
        {
            // An external restore (DevTools time-travel, manual RestoreGlobalState) changed the
            // state without an action. Refresh the present snapshot so the next Undo diffs from
            // what the user actually sees rather than a stale pre-jump state.
            _present = _boundary = _manager.CaptureGlobalState();
            _presentLabel = null;
            _lastGroupKey = null;
            RaiseChanged();
        }
        catch (Exception ex)
        {
            // This handler sits directly on the pipeline events; an escaping exception would
            // break dispatch for observers registered after it.
            _manager.ReportError("history", ex);
        }
    }

    private void OnAction(BlexActionContext context)
    {
        if (_restoring)
            return;

        try
        {
            var record = Allows(context);

            // A grouped action joins the step already open instead of starting a new one, so a
            // whole burst (every keystroke in one field) costs a single undo.
            var groupKey = record ? GroupKey(context) : null;
            var merge = groupKey is not null && groupKey == _lastGroupKey;

            if (record && !merge && _present is not null)
            {
                _undo.Add(new BlexEntry(_present, _presentLabel));
                if (_undo.Count > MaxEntries)
                    _undo.RemoveAt(0);
            }

            // The present always advances -- a filtered action changed the state just as much as a
            // recorded one, and leaving a stale present behind would make the next Undo restore a
            // snapshot the user never saw. Only the undo *step* is skipped. The label is left alone
            // for the same reason: it names the boundary the next Undo actually returns to.
            _present = context.GlobalState;
            if (record)
            {
                _presentLabel = context.QualifiedName;
                _boundary = _present;
                _lastGroupKey = groupKey;
            }

            _redo.Clear();
            RaiseChanged();
        }
        catch (Exception ex)
        {
            // A throwing Changed handler (user UI code) must not break the dispatch pipeline
            // for observers registered after history (e.g. persistence).
            _manager.ReportError("history", ex, context.QualifiedName);
        }
    }

    /// <summary>
    /// Whether the filter admits this action into the history. A throwing filter fails open (the
    /// action is recorded): silently dropping undo steps would be far harder to notice than an
    /// extra one, and the failure is reported either way.
    /// </summary>
    private bool Allows(BlexActionContext context)
    {
        if (_filter is null)
            return true;

        try
        {
            return _filter(context);
        }
        catch (Exception ex)
        {
            _manager.ReportError("history", ex, $"history filter for '{context.QualifiedName}'");
            return true;
        }
    }

    /// <summary>
    /// The group key for an action, or <c>null</c> when it gets its own step. A throwing selector
    /// falls back to <c>null</c>: an extra undo step is far easier to notice -- and to live with --
    /// than silently swallowing a step into the one before it.
    /// </summary>
    private string? GroupKey(BlexActionContext context)
    {
        if (_groupBy is null)
            return null;

        try
        {
            return _groupBy(context);
        }
        catch (Exception ex)
        {
            _manager.ReportError("history", ex, $"history groupBy for '{context.QualifiedName}'");
            return null;
        }
    }

    /// <summary>
    /// Commits the current state as an undo boundary, as if an action named
    /// <paramref name="label"/> had just been recorded. Use it together with a filter: the filter
    /// keeps keystroke-level actions out of the stack, and this marks the points the user would
    /// actually want to step back to (a pause in typing, a field losing focus, a wizard step).
    /// </summary>
    /// <param name="label">
    /// The label <see cref="NextUndoLabel"/> reports for this boundary. Defaults to
    /// <c>"Checkpoint"</c>.
    /// </param>
    /// <remarks>
    /// Without this, a filter that drops every keystroke leaves a whole edit with no boundary of its
    /// own, so the next <see cref="Undo"/> discards the edit together with the recorded action
    /// before it. Consecutive checkpoints with nothing in between are collapsed: a debounced
    /// "user stopped typing" timer can call this freely without growing the stack while the state
    /// sits still. Like <see cref="Clear"/>, it works whether or not recording has
    /// <see cref="Start">started</see>.
    /// </remarks>
    public void Checkpoint(string? label = null)
    {
        _present = _manager.CaptureGlobalState();

        // An explicit checkpoint always closes whatever group is open -- including the no-op case
        // below. The caller is naming a boundary; the next action must start a fresh step even
        // when this one had no state change of its own to commit.
        _lastGroupKey = null;

        // Nothing moved since the last boundary -- a checkpoint here would be an undo step that
        // reverts to the state it was taken from. Compared as text rather than with
        // JsonNode.DeepEquals, which only exists from .NET 9; both trees come from the same
        // CaptureGlobalState, so their property order matches.
        if (_boundary is not null && _boundary.ToJsonString() == _present.ToJsonString())
            return;

        if (_boundary is not null)
        {
            // The boundary, not the present: everything between them is the work this checkpoint
            // is turning into one undoable step.
            _undo.Add(new BlexEntry(_boundary, _presentLabel));
            if (_undo.Count > MaxEntries)
                _undo.RemoveAt(0);
        }

        _boundary = _present;
        _presentLabel = label ?? "Checkpoint";
        _redo.Clear();
        RaiseChanged();
    }

    /// <summary>Reverts to the previous state. No-op when <see cref="CanUndo"/> is false.</summary>
    public void Undo() => Jump(-1);

    /// <summary>Re-applies the most recently undone state. No-op when <see cref="CanRedo"/> is false.</summary>
    public void Redo() => Jump(1);

    /// <summary>
    /// Moves <paramref name="steps"/> through the history in one go -- negative to undo, positive
    /// to redo (redux-undo's <c>jump</c>). The intermediate states are never applied: the stacks are
    /// walked first and only the destination is restored, so a ten-step undo costs one render, one
    /// storage write and one <see cref="Changed"/> raise rather than ten of each.
    /// </summary>
    /// <param name="steps">How far to move. Clamped to what the stacks actually hold.</param>
    public void Jump(int steps)
    {
        if (steps == 0)
            return;

        var moved = 0;
        if (steps < 0)
        {
            for (var i = 0; i < -steps && _undo.Count > 0; i++)
            {
                StepBack();
                moved++;
            }
        }
        else
        {
            for (var i = 0; i < steps && _redo.Count > 0; i++)
            {
                StepForward();
                moved++;
            }
        }

        if (moved == 0 || _present is null)
            return;

        // Landing somewhere by hand ends whatever group was open; the next action must not be
        // folded into a step the user has already stepped away from.
        _lastGroupKey = null;
        Restore(_present);
        RaiseChanged();
    }

    /// <summary>
    /// Jumps straight to an entry in the undo stack, where <c>0</c> is the oldest retained state
    /// and <see cref="UndoCount"/><c> - 1</c> is the one a single <see cref="Undo"/> would reach --
    /// the index space of <see cref="UndoLabels"/> reversed, and of redux-undo's <c>jumpToPast</c>.
    /// </summary>
    public void JumpToPast(int index) => Jump(index - _undo.Count);

    /// <summary>
    /// Jumps straight to an entry in the redo stack, where <c>0</c> is the state a single
    /// <see cref="Redo"/> would reach (redux-undo's <c>jumpToFuture</c>) -- the index space of
    /// <see cref="RedoLabels"/>.
    /// </summary>
    public void JumpToFuture(int index) => Jump(index + 1);

    /// <summary>Moves the present one step back through the undo stack, without restoring.</summary>
    private void StepBack()
    {
        if (_present is not null)
            _redo.Add(new BlexEntry(_present, _presentLabel));

        var entry = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _present = _boundary = entry.State;
        _presentLabel = entry.Label;
    }

    /// <summary>Moves the present one step forward through the redo stack, without restoring.</summary>
    private void StepForward()
    {
        if (_present is not null)
            _undo.Add(new BlexEntry(_present, _presentLabel));

        var entry = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _present = _boundary = entry.State;
        _presentLabel = entry.Label;
    }

    /// <summary>Clears all undo/redo history, keeping the current state as the new baseline.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _present = _boundary = _manager.CaptureGlobalState();
        _presentLabel = null;
        _lastGroupKey = null;
        RaiseChanged();
    }

    private void Restore(JsonObject state)
    {
        _restoring = true;
        try
        {
            _manager.RestoreGlobalState(state);
        }
        finally
        {
            _restoring = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_started)
        {
            _manager.ActionDispatched -= OnAction;
            _manager.StateRestored -= OnExternalRestore;
            _started = false;
        }
    }
}
