namespace Bit.Bcrdt;

/// <summary>One reversible step on an undo or redo stack.</summary>
/// <remarks>
/// A step records two ranges of clock space rather than a copy of anything: what the action created,
/// and what it removed. Undoing is then a matter of deleting the first and re-creating the second,
/// which touches only this user's own changes and leaves concurrent work by others untouched.
/// </remarks>
public sealed class BcrdtUndoStackItem(BcrdtDeleteSet deletions, BcrdtDeleteSet insertions)
{
    /// <summary>What the action deleted, and what undoing it will restore.</summary>
    public BcrdtDeleteSet Deletions { get; internal set; } = deletions;

    /// <summary>What the action created, and what undoing it will remove.</summary>
    public BcrdtDeleteSet Insertions { get; internal set; } = insertions;

    /// <summary>Arbitrary caller state, for attaching cursor positions or labels to a step.</summary>
    public Dictionary<string, object?> Meta { get; } = new(StringComparer.Ordinal);
}

/// <summary>Settings for a <see cref="BcrdtUndoManager"/>.</summary>
public sealed class BcrdtUndoManagerOptions
{
    /// <summary>
    /// How long consecutive changes keep merging into one undo step.
    /// </summary>
    /// <remarks>
    /// Typing a word should be one undo step, not one per keystroke. Set to zero to make every
    /// transaction its own step.
    /// </remarks>
    public TimeSpan CaptureTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Which transaction origins this manager is responsible for.
    /// </summary>
    /// <remarks>
    /// Defaults to untagged local changes, which is what application code produces. Remote updates
    /// are tagged by their sync provider and therefore excluded - undo must never revert somebody
    /// else's work.
    /// </remarks>
    public HashSet<object?> TrackedOrigins { get; init; } = [null];

    /// <summary>An optional veto on deleting a particular block while undoing.</summary>
    public Func<BcrdtItem, bool>? DeleteFilter { get; init; }

    /// <summary>
    /// How many steps each stack keeps; the oldest is dropped when another is added. Zero keeps every
    /// step.
    /// </summary>
    /// <remarks>
    /// Every step pins the content it deleted so that undoing it can bring that content back, which is
    /// memory a long session never gets back otherwise. A dropped step releases what it pinned, and the
    /// content is reclaimed.
    /// </remarks>
    public int MaxStackItems { get; init; } = 100;

    /// <summary>
    /// Whether a map key redone locally should ignore a concurrent remote write to the same key.
    /// </summary>
    /// <remarks>
    /// Off by default: if somebody else has since written the key, redoing over their value would
    /// silently discard it, so the redo is abandoned instead.
    /// </remarks>
    public bool IgnoreRemoteMapChanges { get; init; }
}

/// <summary>Describes a change to an undo or redo stack.</summary>
/// <param name="Item">The step that was added or updated.</param>
/// <param name="IsRedoStack">Whether the step landed on the redo stack rather than the undo stack.</param>
/// <param name="Origin">The origin of the transaction responsible.</param>
public readonly record struct BcrdtUndoStackEventArgs(BcrdtUndoStackItem Item, bool IsRedoStack, object? Origin);

/// <summary>
/// Selective undo and redo with collaborative semantics.
/// </summary>
/// <remarks>
/// <para>
/// Undo in a collaborative document cannot mean "restore the document to how it looked before",
/// because that would throw away everything other people did in the meantime. What users actually
/// expect is <i>selective</i> undo: reverse my last action, leave everyone else's alone. That is
/// what this implements.
/// </para>
/// <para>
/// Undoing is itself an ordinary edit - new operations that delete what the action inserted and
/// re-insert what it deleted. It therefore synchronizes, merges and can itself be undone like any
/// other change, and a peer receiving it sees a normal update rather than a special case.
/// </para>
/// <para>
/// Scope matters: a manager tracks a specific set of types, so a form field's undo stack does not
/// swallow edits to its siblings. Origins matter too: only transactions tagged with a tracked origin
/// are captured, which is how remote changes stay out of the local stack.
/// </para>
/// </remarks>
public sealed class BcrdtUndoManager : IDisposable
{
    private readonly BcrdtDocument _doc;
    private readonly List<BcrdtAbstractType> _scope = [];
    private readonly BcrdtUndoManagerOptions _options;
    private readonly List<BcrdtUndoStackItem> _undoStack = [];
    private readonly List<BcrdtUndoStackItem> _redoStack = [];

    private long _lastChangeTicks;
    private bool _undoing;
    private bool _redoing;
    private bool _disposed;

    /// <summary>Creates a manager tracking one type.</summary>
    public BcrdtUndoManager(BcrdtAbstractType scope, BcrdtUndoManagerOptions? options = null)
        : this((scope ?? throw new ArgumentNullException(nameof(scope))).Doc, [scope], options) { }

    /// <summary>Creates a manager tracking several types in the same document.</summary>
    public BcrdtUndoManager(BcrdtDocument doc, IEnumerable<BcrdtAbstractType> scope, BcrdtUndoManagerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(scope);

        _doc = doc;
        _options = options ?? new BcrdtUndoManagerOptions();
        _scope.AddRange(scope);

        // The manager tags its own transactions so that undoing pushes onto the redo stack instead
        // of being captured as a fresh undoable action. The set is copied rather than added to: an
        // options instance shared between two managers would otherwise make each one track - and
        // capture - the other's undo operations.
        _trackedOrigins = [.. _options.TrackedOrigins, this];

        _doc.AfterTransaction += OnAfterTransaction;
        _doc.UndoManagers.Add(this);
    }

    private readonly HashSet<object?> _trackedOrigins;

    /// <summary>Raised when a step is pushed onto a stack.</summary>
    public event Action<BcrdtUndoStackEventArgs>? StackItemAdded;

    /// <summary>Raised when a step absorbs a further change within the capture window.</summary>
    public event Action<BcrdtUndoStackEventArgs>? StackItemUpdated;

    /// <summary>Raised when a step is popped and applied.</summary>
    public event Action<BcrdtUndoStackEventArgs>? StackItemPopped;

    /// <summary>Whether there is anything to undo.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Whether there is anything to redo.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>How many undo steps are available.</summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>How many redo steps are available.</summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>Adds another type to the tracked scope.</summary>
    public void AddToScope(BcrdtAbstractType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!_scope.Contains(type)) _scope.Add(type);
    }

    /// <summary>
    /// Ends the current capture window, so the next change starts a new undo step.
    /// </summary>
    /// <remarks>
    /// Call this at a natural boundary - the user moved the caret, switched fields, or clicked
    /// something - to stop unrelated edits merging into one step.
    /// </remarks>
    public void StopCapturing() => _lastChangeTicks = 0;

    /// <summary>Reverses the most recent tracked action.</summary>
    /// <returns>The step that was applied, or <see langword="null"/> if there was nothing to undo.</returns>
    public BcrdtUndoStackItem? Undo()
    {
        _undoing = true;
        try
        {
            return Pop(_undoStack, isRedoStack: false);
        }
        finally
        {
            _undoing = false;
        }
    }

    /// <summary>Re-applies the most recently undone action.</summary>
    /// <returns>The step that was applied, or <see langword="null"/> if there was nothing to redo.</returns>
    public BcrdtUndoStackItem? Redo()
    {
        _redoing = true;
        try
        {
            return Pop(_redoStack, isRedoStack: true);
        }
        finally
        {
            _redoing = false;
        }
    }

    /// <summary>Discards both stacks.</summary>
    public void Clear(bool clearUndo = true, bool clearRedo = true)
    {
        // Runs after every tracked edit to drop the redo history, which is nearly always empty
        // already. Opening a transaction for nothing would raise a transaction's worth of events.
        if (!(clearUndo && _undoStack.Count > 0) && !(clearRedo && _redoStack.Count > 0)) return;

        _doc.Transact(transaction =>
        {
            if (clearUndo) Release(transaction, _undoStack, _undoStack.Count);
            if (clearRedo) Release(transaction, _redoStack, _redoStack.Count);
        }, origin: this);
    }

    /// <summary>
    /// Drops the oldest <paramref name="count"/> steps of a stack, releasing the deleted content they
    /// kept so the collector can reclaim it now rather than never.
    /// </summary>
    /// <remarks>
    /// The keep flag is shared: two managers over overlapping scopes keep the same deleted block, and one
    /// step's container is often another step's too. So a step releases what it pinned - each block it
    /// could restore and the containers above it - except what a step left on any manager of the
    /// document could still restore, or still needs a container of to restore into.
    /// </remarks>
    private void Release(BcrdtTransaction transaction, List<BcrdtUndoStackItem> stack, int count)
    {
        BcrdtUndoStackItem[] released = [.. stack.Take(count)];
        stack.RemoveRange(0, count);

        foreach (BcrdtUndoStackItem step in released) transaction.ReleaseForCollection(step.Deletions);

        HashSet<BcrdtItem> pinned = PinnedBy(transaction, released);
        if (pinned.Count == 0) return;

        // A leaf is still needed only if a remaining step deleted it, which is a cheap lookup. A container
        // is needed by whatever any remaining step deleted inside it, which takes walking those steps -
        // and that walk can split released blocks further, so they are gathered again after it.
        HashSet<BcrdtItem>? needed = null;
        if (pinned.Any(static item => item.Content is BcrdtContentType))
        {
            needed = StillPinned(transaction);
            pinned = PinnedBy(transaction, released);
        }

        BcrdtDeleteSet? containers = null;
        foreach (BcrdtItem item in pinned)
        {
            if (needed?.Contains(item) ?? IsStillNeeded(item)) continue;
            item.Keep = false;

            // A container deleted while it was pinned is not in any range the steps released, and the
            // collector looks at nothing else.
            if (item.Deleted && item.Content is BcrdtContentType)
            {
                (containers ??= new BcrdtDeleteSet()).Add(item.Id.Client, item.Id.Clock, item.Length);
            }
        }

        if (containers is not null) transaction.ReleaseForCollection(containers);
    }

    /// <summary>
    /// What the given steps keep: every block in scope they could restore, and every kept container above
    /// one.
    /// </summary>
    private HashSet<BcrdtItem> PinnedBy(BcrdtTransaction transaction, IEnumerable<BcrdtUndoStackItem> steps)
    {
        HashSet<BcrdtItem> pinned = [];
        foreach (BcrdtUndoStackItem step in steps)
        {
            step.Deletions.IterateDeletedStructs(transaction, block =>
            {
                if (block is not BcrdtItem target || !IsInScope(target)) return;

                // Stops at a container already gathered: the chain above it was gathered with it.
                for (BcrdtItem? item = target; item is { Keep: true } && pinned.Add(item); item = ContainerOf(item)) { }
            });
        }
        return pinned;
    }

    /// <summary>
    /// Every block a step left on any undo manager of this document could restore, with the containers
    /// above it.
    /// </summary>
    private HashSet<BcrdtItem> StillPinned(BcrdtTransaction transaction)
    {
        HashSet<BcrdtItem> needed = [];
        foreach (BcrdtUndoManager manager in _doc.UndoManagers)
        {
            foreach (BcrdtUndoStackItem step in manager._undoStack.Concat(manager._redoStack))
            {
                step.Deletions.IterateDeletedStructs(transaction, block =>
                {
                    if (block is not BcrdtItem target || !manager.IsInScope(target)) return;
                    for (BcrdtItem? item = target; item is not null && needed.Add(item); item = ContainerOf(item)) { }
                });
            }
        }
        return needed;
    }

    private static BcrdtItem? ContainerOf(BcrdtItem item) => (item.Parent as BcrdtAbstractType)?.ContainingItem;

    /// <summary>Whether any step of any undo manager on this document could still restore a block.</summary>
    private bool IsStillNeeded(BcrdtItem item)
    {
        foreach (BcrdtUndoManager manager in _doc.UndoManagers)
        {
            if (!manager.IsInScope(item)) continue;
            foreach (BcrdtUndoStackItem step in manager._undoStack)
            {
                if (step.Deletions.IsDeleted(item.Id)) return true;
            }
            foreach (BcrdtUndoStackItem step in manager._redoStack)
            {
                if (step.Deletions.IsDeleted(item.Id)) return true;
            }
        }
        return false;
    }

    private void OnAfterTransaction(BcrdtTransaction transaction)
    {
        if (_disposed) return;

        // Only transactions that touched the scope, and only the ones this manager is responsible
        // for. Anyone else's change to the scope - a collaborator's, typically - still ends the
        // current capture window, so one undo step never straddles somebody else's edit.
        if (!_scope.Any(transaction.ChangedParentTypes.ContainsKey)) return;
        if (!IsTrackedOrigin(transaction.Origin))
        {
            StopCapturing();
            return;
        }

        List<BcrdtUndoStackItem> stack = _undoing ? _redoStack : _undoStack;

        if (_undoing)
        {
            StopCapturing();
        }
        else if (!_redoing)
        {
            // A fresh action invalidates the redo history, exactly as in a single-user editor.
            Clear(clearUndo: false, clearRedo: true);
        }

        // Only what this replica created. A tracked transaction normally creates nothing else, but one
        // that had a peer's update applied inside it - an await inside an open transaction, say - also
        // advanced the peer's clock, and undoing that would delete the peer's work.
        var insertions = new BcrdtDeleteSet();
        long ownStart = transaction.BeforeState.Get(_doc.ClientId);
        long ownEnd = transaction.AfterState.Get(_doc.ClientId);
        if (ownEnd > ownStart) insertions.Add(_doc.ClientId, ownStart, ownEnd - ownStart);

        long now = DateTime.UtcNow.Ticks;
        bool withinCaptureWindow =
            _lastChangeTicks > 0 &&
            now - _lastChangeTicks < _options.CaptureTimeout.Ticks &&
            stack.Count > 0 &&
            !_undoing &&
            !_redoing;

        bool added;
        if (withinCaptureWindow)
        {
            BcrdtUndoStackItem last = stack[^1];
            last.Deletions = BcrdtDeleteSet.Merge([last.Deletions, transaction.DeleteSet]);
            last.Insertions = BcrdtDeleteSet.Merge([last.Insertions, insertions]);
            added = false;
        }
        else
        {
            stack.Add(new BcrdtUndoStackItem(transaction.DeleteSet.Clone(), insertions));
            added = true;
        }

        if (!_undoing && !_redoing) _lastChangeTicks = now;

        // Deleted content this step could restore must survive garbage collection until the step is
        // dropped, or undo would find nothing to bring back.
        transaction.DeleteSet.IterateDeletedStructs(transaction, block =>
        {
            if (block is BcrdtItem target && IsInScope(target)) SetKeep(target, true);
        });

        BcrdtUndoStackItem current = stack[^1];

        // The oldest step makes way. This transaction is still being settled, so what that step kept
        // is reclaimed in the same collection pass as anything the transaction deleted itself.
        if (_options.MaxStackItems > 0 && stack.Count > _options.MaxStackItems)
        {
            Release(transaction, stack, stack.Count - _options.MaxStackItems);
        }

        var args = new BcrdtUndoStackEventArgs(current, _undoing, transaction.Origin);
        if (added) StackItemAdded?.Invoke(args);
        else StackItemUpdated?.Invoke(args);
    }

    private BcrdtUndoStackItem? Pop(List<BcrdtUndoStackItem> stack, bool isRedoStack)
    {
        BcrdtUndoStackItem? applied = null;

        _doc.Transact(transaction =>
        {
            while (stack.Count > 0 && applied is null)
            {
                BcrdtUndoStackItem step = stack[^1];
                stack.RemoveAt(stack.Count - 1);

                var toRestore = new HashSet<BcrdtItem>();
                List<BcrdtItem> toDelete = [];
                bool changed = false;

                // Everything the action inserted should go away - following redo links, because a
                // previous undo/redo cycle may have replaced the original blocks with copies.
                step.Insertions.IterateDeletedStructs(transaction, block =>
                {
                    if (block is not BcrdtItem item) return;

                    if (item.Redone is not null)
                    {
                        (BcrdtItem resolved, long diff) = FollowRedone(_doc.Store, item.Id);
                        if (diff > 0)
                        {
                            resolved = BcrdtStructStore.GetItemCleanStart(
                                transaction, new BcrdtItemId(resolved.Id.Client, resolved.Id.Clock + diff));
                        }
                        item = resolved;
                    }

                    if (!item.Deleted && IsInScope(item)) toDelete.Add(item);
                });

                // Everything the action deleted should come back - except content that was both
                // created and deleted within the same step, which never existed as far as anyone
                // outside the step is concerned.
                step.Deletions.IterateDeletedStructs(transaction, block =>
                {
                    if (block is BcrdtItem item && IsInScope(item) && !step.Insertions.IsDeleted(item.Id))
                    {
                        toRestore.Add(item);
                    }
                });

                foreach (BcrdtItem item in toRestore)
                {
                    changed |= Restore(transaction, item, toRestore, step.Insertions) is not null;
                }

                // Children before parents, so that a delete filter still sees the full context.
                for (int i = toDelete.Count - 1; i >= 0; i--)
                {
                    BcrdtItem item = toDelete[i];
                    if (_options.DeleteFilter is not null && !_options.DeleteFilter(item)) continue;
                    item.Delete(transaction);
                    changed = true;
                }

                applied = changed ? step : null;
            }

            // Index caches are keyed on positions this operation just moved.
            foreach ((BcrdtAbstractType type, HashSet<string?> keys) in transaction.Changed)
            {
                if (keys.Contains(null)) type.InvalidateSearchMarkers();
            }
        }, origin: this);

        if (applied is not null)
        {
            StackItemPopped?.Invoke(new BcrdtUndoStackEventArgs(applied, isRedoStack, this));
        }
        return applied;
    }

    /// <summary>
    /// Re-inserts deleted content as a new block at the position it originally occupied.
    /// </summary>
    /// <remarks>
    /// The original block cannot simply be undeleted: deletion is a fact other replicas have already
    /// recorded, and un-recording it is not something a CRDT can do. Instead a fresh block carrying
    /// the same content is inserted between the original's surviving neighbours, and the original
    /// keeps a link to it so a later redo can find it again.
    /// </remarks>
    private BcrdtItem? Restore(BcrdtTransaction transaction, BcrdtItem item, HashSet<BcrdtItem> pending, BcrdtDeleteSet insertions)
    {
        if (item.Redone is { } existing) return BcrdtStructStore.GetItemCleanStart(transaction, existing);

        BcrdtItem? containerItem = (item.Parent as BcrdtAbstractType)?.ContainingItem;

        // If the container itself was deleted, it has to come back first or there is nowhere to put
        // this. If it is not part of this step, the restore is not possible.
        if (containerItem is { Deleted: true })
        {
            if (containerItem.Redone is null &&
                (!pending.Contains(containerItem) || Restore(transaction, containerItem, pending, insertions) is null))
            {
                return null;
            }

            while (containerItem?.Redone is { } redoneContainer)
            {
                containerItem = BcrdtStructStore.GetItemCleanStart(transaction, redoneContainer);
            }
        }

        BcrdtAbstractType? container = containerItem is null
            ? item.Parent as BcrdtAbstractType
            : (containerItem.Content as BcrdtContentType)?.Type;

        if (container is null) return null;

        BcrdtItem? left;
        BcrdtItem? right;

        if (item.ParentKey is null)
        {
            left = FollowToContainer(transaction, item.Left, containerItem, static block => block.Left);
            right = FollowToContainer(transaction, item, containerItem, static block => block.Right);
        }
        else
        {
            right = null;

            if (item.Right is not null && !_options.IgnoreRemoteMapChanges)
            {
                left = item;
                while (left?.Right is not null &&
                       (left.Right.Redone is not null || insertions.IsDeleted(left.Right.Id)))
                {
                    left = left.Right;
                    while (left.Redone is { } redone) left = BcrdtStructStore.GetItemCleanStart(transaction, redone);
                }

                // Somebody else has written this key since. Redoing over it would discard their
                // value, so abandon the step rather than lose data.
                if (left?.Right is not null) return null;
            }
            else
            {
                left = container.MapGetRaw(item.ParentKey);
            }
        }

        var restored = new BcrdtItem(
            new BcrdtItemId(_doc.ClientId, _doc.Store.GetState(_doc.ClientId)),
            left,
            left?.LastId,
            right,
            right?.Id,
            container,
            item.ParentKey,
            item.Content.Copy());

        item.Redone = restored.Id;
        SetKeep(restored, true);
        restored.Integrate(transaction, 0);
        return restored;
    }

    /// <summary>
    /// Walks outward from a block until it finds a neighbour that still lives in the target container.
    /// </summary>
    private static BcrdtItem? FollowToContainer(
        BcrdtTransaction transaction,
        BcrdtItem? start,
        BcrdtItem? containerItem,
        Func<BcrdtItem, BcrdtItem?> step)
    {
        BcrdtItem? cursor = start;
        while (cursor is not null)
        {
            BcrdtItem? trace = cursor;
            while (trace is not null && (trace.Parent as BcrdtAbstractType)?.ContainingItem != containerItem)
            {
                trace = trace.Redone is { } redone
                    ? BcrdtStructStore.GetItemCleanStart(transaction, redone)
                    : null;
            }

            if (trace is not null && (trace.Parent as BcrdtAbstractType)?.ContainingItem == containerItem) return trace;
            cursor = step(cursor);
        }
        return null;
    }

    private static (BcrdtItem Item, long Diff) FollowRedone(BcrdtStructStore store, BcrdtItemId id)
    {
        BcrdtItemId? next = id;
        long diff = 0;
        BcrdtItem item;

        do
        {
            BcrdtItemId target = next!.Value;
            if (diff > 0) target = new BcrdtItemId(target.Client, target.Clock + diff);

            if (store.Find(target) is not BcrdtItem found) throw new InvalidOperationException($"Redo chain broke at {target}.");

            item = found;
            diff = target.Clock - item.Id.Clock;
            next = item.Redone;
        }
        while (next is not null);

        return (item, diff);
    }

    private bool IsTrackedOrigin(object? origin)
    {
        if (_trackedOrigins.Contains(origin)) return true;
        return origin is not null && _trackedOrigins.Contains(origin.GetType());
    }

    private bool IsInScope(BcrdtItem item)
    {
        object? parent = item.Parent;
        while (parent is BcrdtAbstractType type)
        {
            if (_scope.Contains(type)) return true;
            parent = type.ContainingItem?.Parent;
        }
        return false;
    }

    /// <summary>
    /// Protects a block and its containers from collection, or releases that protection.
    /// </summary>
    /// <remarks>
    /// Protection propagates upward: restoring a block is pointless if the container it lived in has
    /// been reclaimed, so the whole chain is pinned together.
    /// </remarks>
    private static void SetKeep(BcrdtItem? item, bool keep)
    {
        while (item is not null && item.Keep != keep)
        {
            item.Keep = keep;
            item = ContainerOf(item);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _doc.AfterTransaction -= OnAfterTransaction;

        // Release what the stacks kept, so a document that outlives its undo manager can reclaim it -
        // except what another manager's steps still need.
        try
        {
            Clear();
        }
        catch (ObjectDisposedException)
        {
            // The document went first; there is nothing left to reclaim.
        }
        finally
        {
            // Unregistered whatever happened above, so the other managers stop keeping content for its steps.
            _doc.UndoManagers.Remove(this);

            _undoStack.Clear();
            _redoStack.Clear();
            StackItemAdded = null;
            StackItemUpdated = null;
            StackItemPopped = null;
        }
    }
}
