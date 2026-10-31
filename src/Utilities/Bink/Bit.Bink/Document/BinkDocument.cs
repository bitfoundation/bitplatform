using System.Diagnostics.CodeAnalysis;

namespace Bit.Bink;

/// <summary>Raised after a document changed.</summary>
public sealed class BinkDocumentChangedEventArgs : EventArgs
{
    internal BinkDocumentChangedEventArgs(BinkDrawingOperation operation, BinkDrawingOperation inverse, BinkChangeOrigin origin, long revision, int depth)
    {
        Operation = operation;
        Inverse = inverse;
        Origin = origin;
        Revision = revision;
        Depth = depth;
    }

    /// <summary>The operation that was applied.</summary>
    public BinkDrawingOperation Operation { get; }

    /// <summary>The operation that undoes it.</summary>
    public BinkDrawingOperation Inverse { get; }

    /// <summary>Who caused the change.</summary>
    public BinkChangeOrigin Origin { get; }

    /// <summary>The document revision after the change.</summary>
    public long Revision { get; }

    /// <summary>
    /// How deeply this change is nested inside another change's notification. Zero for a change applied on its
    /// own; one for a change a listener applied while handling that one, and so on. Use it to tell a consequence
    /// from its cause, for example to avoid re-deriving geometry that a handler is already deriving.
    /// </summary>
    public int Depth { get; }
}

/// <summary>
/// The vector drawing model: layers of elements plus settings and metadata. Independent of any UI; can be
/// created, edited, serialized and rendered on a server or in tests without a browser.
/// </summary>
/// <remarks>
/// <para>
/// Every mutation goes through <see cref="Apply(BinkDrawingOperation, BinkChangeOrigin)"/> (the convenience methods such as
/// <see cref="AddElement"/> build the operation for you). The document raises <see cref="Changed"/> with the operation
/// and its inverse, which is what <see cref="BinkHistoryManager"/> and collaboration sessions consume.
/// </para>
/// <para>
/// The document is not thread-safe; use it from one thread (the UI thread in Blazor).
/// </para>
/// </remarks>
public sealed partial class BinkDocument
{
    private readonly List<BinkLayer> _layers = new();
    private readonly Dictionary<Guid, BinkLayer> _elementLayers = new();
    private readonly Dictionary<Guid, BinkLayer> _layersById = new();
    private readonly Dictionary<string, string> _metadata = new(StringComparer.Ordinal);
    private List<BinkDrawingOperation>? _batch;
    private List<BinkDrawingOperation>? _batchInverses;
    private int _batchDepth;
    private BinkChangeOrigin _batchOrigin;
    private string? _batchLabel;
    private long _totalPoints;
    private int _dispatchDepth;
    private bool _keepRemoteVersions;

    /// <summary>Creates a document with default settings and one layer named "Drawing".</summary>
    public BinkDocument() : this(BinkDocumentSettings.Default) { }

    /// <summary>Creates a document with the given settings and one layer named "Drawing".</summary>
    public BinkDocument(BinkDocumentSettings settings) : this(settings, BinkDocumentLimits.Default) { }

    /// <summary>Creates a document with settings and limits and one layer named "Drawing".</summary>
    public BinkDocument(BinkDocumentSettings settings, BinkDocumentLimits limits)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(limits);
        Settings = settings;
        Limits = limits;
        InsertLayerCore(Guid.NewGuid(), new BinkLayerProperties { Name = "Drawing" }, null, null);
    }

    /// <summary>Creates an empty document without layers (used by deserialization).</summary>
    internal BinkDocument(Guid id, BinkDocumentSettings settings, BinkDocumentLimits limits, bool withoutLayers)
    {
        Id = id;
        Settings = settings;
        Limits = limits;
        if (!withoutLayers) InsertLayerCore(Guid.NewGuid(), new BinkLayerProperties { Name = "Drawing" }, null, null);
    }

    /// <summary>Document identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Page settings.</summary>
    public BinkDocumentSettings Settings { get; private set; }

    /// <summary>Resource limits enforced on mutation.</summary>
    public BinkDocumentLimits Limits { get; }

    /// <summary>Layers from bottom to top.</summary>
    public IReadOnlyList<BinkLayer> Layers => _layers;

    /// <summary>Application metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    /// <summary>Incremented on every applied operation (batches count once).</summary>
    public long Revision { get; private set; }

    /// <summary>The <see cref="Revision"/> the application last saved, as recorded by <see cref="MarkSaved()"/>.</summary>
    public long SavedRevision { get; private set; }

    /// <summary>
    /// True when the document changed since the application last called <see cref="MarkSaved()"/>: the state an
    /// "unsaved changes" prompt asks about. Any change counts, an undo included, so undoing back to what was saved
    /// still reads as changed.
    /// </summary>
    public bool IsDirty => Revision != SavedRevision;

    /// <summary>Records that the document as it is now has been saved, so <see cref="IsDirty"/> is false.</summary>
    public void MarkSaved() => SavedRevision = Revision;

    /// <summary>
    /// Records that the document as it was at <paramref name="revision"/> has been saved. Use it when saving is
    /// asynchronous: the document was serialized at that revision and may have changed while the bytes were written,
    /// and those later changes are still unsaved.
    /// </summary>
    public void MarkSaved(long revision) => SavedRevision = revision;

    /// <summary>Total number of elements across layers.</summary>
    public int ElementCount => _elementLayers.Count;

    /// <summary>Total number of stroke points in the document.</summary>
    public long TotalPointCount => _totalPoints;

    /// <summary>The top-most unlocked, visible layer, or the top-most layer when none qualifies.</summary>
    public BinkLayer ActiveLayerCandidate
    {
        get
        {
            for (int i = _layers.Count - 1; i >= 0; i--) if (!_layers[i].IsLocked && _layers[i].IsVisible) return _layers[i];
            return _layers[^1];
        }
    }

    /// <summary>Raised after every applied operation (or once per batch).</summary>
    public event EventHandler<BinkDocumentChangedEventArgs>? Changed;

    /// <summary>Union of the bounds of all elements on visible layers.</summary>
    public BinkRect ContentBounds
    {
        get
        {
            BinkRect bounds = BinkRect.Empty;
            foreach (BinkLayer layer in _layers)
            {
                if (!layer.IsVisible) continue;
                foreach (BinkElement e in layer.Elements) if (!(e is BinkStroke { IsEraser: true })) bounds = bounds.Union(e.Bounds);
            }
            return bounds;
        }
    }

    /// <summary>True when no layer contains elements.</summary>
    public bool IsEmpty => _elementLayers.Count == 0;

    /// <summary>All elements on all layers, bottom to top.</summary>
    public IEnumerable<BinkElement> AllElements
    {
        get
        {
            foreach (BinkLayer layer in _layers) foreach (BinkElement e in layer.Elements) yield return e;
        }
    }

    // ---- Queries -------------------------------------------------------------------------------------------------

    /// <summary>Gets a layer by id.</summary>
    public bool TryGetLayer(Guid layerId, [NotNullWhen(true)] out BinkLayer? layer) => _layersById.TryGetValue(layerId, out layer);

    /// <summary>Gets a layer by id or throws.</summary>
    public BinkLayer GetLayer(Guid layerId) => TryGetLayer(layerId, out BinkLayer? layer) ? layer : throw new KeyNotFoundException($"Layer {layerId} is not in the document.");

    /// <summary>True when an element with the id exists.</summary>
    public bool ContainsElement(Guid elementId) => _elementLayers.ContainsKey(elementId);

    /// <summary>Gets an element and its layer.</summary>
    public bool TryGetElement(Guid elementId, [NotNullWhen(true)] out BinkElement? element, [NotNullWhen(true)] out BinkLayer? layer)
    {
        element = null;
        if (!_elementLayers.TryGetValue(elementId, out layer)) return false;
        int index = layer.IndexOf(elementId);
        if (index < 0) return false;
        element = layer.Elements[index];
        return true;
    }

    /// <summary>Gets an element by id.</summary>
    public bool TryGetElement(Guid elementId, [NotNullWhen(true)] out BinkElement? element) => TryGetElement(elementId, out element, out _);

    /// <summary>Gets an element by id or throws.</summary>
    public BinkElement GetElement(Guid elementId) => TryGetElement(elementId, out BinkElement? e) ? e : throw new KeyNotFoundException($"Element {elementId} is not in the document.");

    /// <summary>Gets the layer containing an element.</summary>
    public BinkLayer? GetLayerOf(Guid elementId) => _elementLayers.GetValueOrDefault(elementId);

    /// <summary>
    /// Gets a layer for a mutation. A missing layer is an operation that cannot be applied, reported the way
    /// <see cref="Apply(BinkDrawingOperation, BinkChangeOrigin)"/> documents (<see cref="InvalidOperationException"/>), so history, tools and
    /// collaboration, which all refuse a stale operation by catching that, do not crash on a
    /// <see cref="KeyNotFoundException"/> when a collaborator or an undo removed the layer.
    /// </summary>
    private BinkLayer RequireLayer(Guid layerId)
        => TryGetLayer(layerId, out BinkLayer? layer) ? layer : throw new InvalidOperationException($"Layer {layerId} is not in the document.");

    // ---- Mutation API -----------------------------------------------------------------------------------------------

    /// <summary>Applies an operation and raises <see cref="Changed"/>. Returns the inverse operation.</summary>
    /// <param name="operation">The operation.</param>
    /// <param name="origin">Who caused it.</param>
    /// <exception cref="InvalidOperationException">The operation cannot be applied (missing element, limit exceeded, ...).</exception>
    public BinkDrawingOperation Apply(BinkDrawingOperation operation, BinkChangeOrigin origin = BinkChangeOrigin.Local)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (operation is BinkBatchOperation { IsEmpty: true }) return operation;

        // Version numbers are decided before the operation is applied, so the operation reported to listeners
        // carries the same version that ends up in the document. A collaborator receiving an operation whose
        // version is lower than what the sender actually stored would discard it, and the two replicas would
        // never agree again.
        //
        // A remote edit has already won last-writer-wins at the version it carries, so it is stored at exactly
        // that version even when it ties with the current one. Bumping it would leave this replica one version
        // ahead of the others for the same content, and the next edit from a third participant would then be
        // ordered differently here than everywhere else.
        bool remote = origin == BinkChangeOrigin.Remote;
        operation = AssignVersions(operation, remote, operation is BinkBatchOperation ? new Dictionary<Guid, int>() : null);
        bool keepRemoteVersions = _keepRemoteVersions;
        _keepRemoteVersions = remote;
        BinkDrawingOperation inverse;
        try
        {
            inverse = operation.ApplyCore(this);
        }
        finally
        {
            _keepRemoteVersions = keepRemoteVersions;
        }
        if (_batchDepth > 0)
        {
            _batch!.Add(operation);
            // Inverses run in reverse order; they are appended here and reversed once when the batch closes,
            // because inserting each at the front made a batch of n operations cost n squared.
            _batchInverses!.Add(inverse);
            return inverse;
        }
        Revision++;
        Raise(operation, inverse, origin);
        return inverse;
    }

    /// <summary>
    /// Reports a change. A listener may apply further changes while handling one, for example to keep derived
    /// geometry in sync; those are reported immediately, nested inside this one, and carry a greater
    /// <see cref="BinkDocumentChangedEventArgs.Depth"/> so a listener can tell a consequence from its cause.
    /// </summary>
    private void Raise(BinkDrawingOperation operation, BinkDrawingOperation inverse, BinkChangeOrigin origin)
    {
        _dispatchDepth++;
        try
        {
            Changed?.Invoke(this, new BinkDocumentChangedEventArgs(operation, inverse, origin, Revision, _dispatchDepth - 1));
        }
        finally
        {
            _dispatchDepth--;
            if (_dispatchDepth == 0) DispatchCompleted?.Invoke();
        }
    }

    /// <summary>
    /// Raised once the outermost <see cref="Changed"/> notification, and everything it caused, have been
    /// reported. History uses it to place a consequential change after the change that caused it whatever order
    /// the handlers ran in; without it an application that subscribed before the history manager would have the
    /// consequence recorded first, and undoing that step would leave its derived geometry behind.
    /// </summary>
    internal event Action? DispatchCompleted;

    /// <summary>
    /// Groups all operations applied until the returned scope is disposed into a single <see cref="BinkBatchOperation"/>
    /// that raises one <see cref="Changed"/> event and undoes as one step. Scopes nest.
    /// </summary>
    /// <remarks>
    /// A scope collects operations that have already been applied, so an exception thrown inside it leaves the
    /// operations that succeeded in place. This differs from applying a <see cref="BinkBatchOperation"/> directly,
    /// which rolls back on failure. Use the batch operation when the change must be all or nothing.
    /// </remarks>
    /// <param name="label">Label for the batch (used by history UIs).</param>
    /// <param name="origin">Origin reported for the batch.</param>
    public IDisposable BeginBatch(string? label = null, BinkChangeOrigin origin = BinkChangeOrigin.Local)
    {
        if (_batchDepth == 0)
        {
            _batch = new List<BinkDrawingOperation>();
            _batchInverses = new List<BinkDrawingOperation>();
            _batchOrigin = origin;
            _batchLabel = label;
        }
        _batchDepth++;
        return new BatchScope(this);
    }

    private void EndBatch()
    {
        if (--_batchDepth > 0) return;
        List<BinkDrawingOperation> ops = _batch!;
        List<BinkDrawingOperation> inverses = _batchInverses!;
        _batch = null;
        _batchInverses = null;
        if (ops.Count == 0) return;
        inverses.Reverse();
        Revision++;
        BinkDrawingOperation forward = ops.Count == 1 && _batchLabel is null ? ops[0] : new BinkBatchOperation(ops, _batchLabel);
        BinkDrawingOperation inverse = inverses.Count == 1 && _batchLabel is null ? inverses[0] : new BinkBatchOperation(inverses, _batchLabel);
        Raise(forward, inverse, _batchOrigin);
    }

    private sealed class BatchScope(BinkDocument document) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            document.EndBatch();
        }
    }

    /// <summary>Adds an element to a layer (the top-most editable layer when <paramref name="layerId"/> is null).</summary>
    public void AddElement(BinkElement element, Guid? layerId = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        Apply(new BinkAddElementOperation(layerId ?? ActiveLayerCandidate.Id, element));
    }

    /// <summary>Removes an element.</summary>
    public void RemoveElement(Guid elementId) => Apply(new BinkRemoveElementOperation(elementId));

    /// <summary>Removes several elements as one step.</summary>
    public void RemoveElements(IEnumerable<Guid> elementIds)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        var targets = new List<(int Layer, int Index, Guid Id)>();
        var seen = new HashSet<Guid>();
        // A repeated id would remove the element and then fail on the second attempt, rolling the batch back.
        foreach (Guid id in elementIds)
        {
            if (!seen.Add(id) || !_elementLayers.TryGetValue(id, out BinkLayer? layer)) continue;
            targets.Add((_layers.IndexOf(layer), layer.IndexOf(id), id));
        }
        if (targets.Count == 0) return;
        // Removed from the top of each layer down. Removing an element shifts everything above it, so removing in
        // selection order made every later lookup re-index the layer, and deleting a large selection quadratic.
        // Top-down, a removal never moves one still to come. The inverse re-inserts bottom-up at the recorded
        // indices, which restores the original order exactly.
        targets.Sort(static (a, b) => a.Layer != b.Layer ? b.Layer.CompareTo(a.Layer) : b.Index.CompareTo(a.Index));
        var ops = new List<BinkDrawingOperation>(targets.Count);
        foreach ((_, _, Guid id) in targets) ops.Add(new BinkRemoveElementOperation(id));
        Apply(new BinkBatchOperation(ops, "Delete"));
    }

    /// <summary>Replaces an element with a new version (same id).</summary>
    public void ReplaceElement(BinkElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Apply(new BinkReplaceElementOperation(element));
    }

    /// <summary>Replaces several elements as one step.</summary>
    public void ReplaceElements(IEnumerable<BinkElement> elements, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var ops = new List<BinkDrawingOperation>();
        foreach (BinkElement e in elements) ops.Add(new BinkReplaceElementOperation(e));
        if (ops.Count > 0) Apply(new BinkBatchOperation(ops, label ?? "Edit"));
    }

    /// <summary>Adds a layer on top and returns it.</summary>
    public BinkLayer AddLayer(string name) => AddLayer(new BinkLayerProperties { Name = name });

    /// <summary>Adds a layer on top and returns it.</summary>
    public BinkLayer AddLayer(BinkLayerProperties properties, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(properties);
        var id = Guid.NewGuid();
        Apply(new BinkAddLayerOperation(id, properties, index));
        return GetLayer(id);
    }

    /// <summary>Removes a layer and its elements. A document always keeps at least one layer.</summary>
    public void RemoveLayer(Guid layerId) => Apply(new BinkRemoveLayerOperation(layerId));

    /// <summary>
    /// Adds a copy of a layer directly above it, with its properties and copies of its elements, as one undoable
    /// step. The copies get new ids, and the groups among them become new groups, so editing the copy never reaches
    /// the original. Elements of types this build does not know are not copied: their stored form names their ids.
    /// </summary>
    /// <param name="layerId">The layer to copy.</param>
    /// <param name="name">The copy's name, or null for the original's name followed by "copy".</param>
    /// <returns>The new layer.</returns>
    /// <exception cref="KeyNotFoundException">The layer is not in the document.</exception>
    /// <exception cref="InvalidOperationException">The copy would exceed the document's limits.</exception>
    public BinkLayer DuplicateLayer(Guid layerId, string? name = null)
    {
        BinkLayer source = GetLayer(layerId);
        var groups = new HashSet<Guid>();
        foreach (BinkElement e in source.Elements) groups.UnionWith(e.GroupIds);
        var renamed = new Dictionary<Guid, Guid>();
        var copies = new List<BinkElement>(source.Count);
        foreach (BinkElement e in source.Elements)
        {
            if (e is BinkUnknownElement) continue;
            BinkElement copy = e with { Id = Guid.NewGuid(), Version = 0 };
            if (e.GroupIds.Count > 0) copy = copy with { GroupIds = RenameGroups(e.GroupIds, groups, renamed) };
            copies.Add(copy);
        }
        var id = Guid.NewGuid();
        Apply(new BinkAddLayerOperation(id, source.Properties with { Name = name ?? source.Name + " copy" }, _layers.IndexOf(source) + 1, copies));
        return GetLayer(id);
    }

    /// <summary>Updates a layer's properties.</summary>
    public void UpdateLayer(Guid layerId, Func<BinkLayerProperties, BinkLayerProperties> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        Apply(new BinkUpdateLayerOperation(layerId, update(GetLayer(layerId).Properties)));
    }

    /// <summary>Moves a layer to a new index.</summary>
    public void ReorderLayer(Guid layerId, int index) => Apply(new BinkReorderLayerOperation(layerId, index));

    /// <summary>Changes the settings.</summary>
    public void UpdateSettings(BinkDocumentSettings settings) => Apply(new BinkUpdateSettingsOperation(settings));

    /// <summary>Sets or removes (null) a metadata entry.</summary>
    public void SetMetadata(string key, string? value) => Apply(new BinkSetMetadataOperation(key, value));

    /// <summary>Removes all elements from all layers (layers are kept) as one undoable step.</summary>
    public void Clear()
    {
        var ops = new List<BinkDrawingOperation>();
        foreach (BinkLayer layer in _layers)
            for (int i = layer.Elements.Count - 1; i >= 0; i--) ops.Add(new BinkRemoveElementOperation(layer.Elements[i].Id));
        if (ops.Count > 0) Apply(new BinkBatchOperation(ops, "Clear"));
    }

    /// <summary>
    /// Changes the stacking order of elements within their layers as one undoable step. Elements keep their order
    /// relative to each other, and nothing moves to another layer.
    /// </summary>
    /// <param name="elementIds">The elements to move; ids not in the document are ignored.</param>
    /// <param name="arrangement">Where they go.</param>
    /// <returns>True when anything moved.</returns>
    public bool Arrange(IEnumerable<Guid> elementIds, BinkArrangement arrangement)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        var selected = new HashSet<Guid>();
        foreach (Guid id in elementIds) if (ContainsElement(id)) selected.Add(id);
        if (selected.Count == 0) return false;

        var ops = new List<BinkDrawingOperation>();
        var order = new List<Guid>();
        foreach (BinkLayer layer in _layers)
        {
            order.Clear();
            bool any = false;
            foreach (BinkElement e in layer.Elements)
            {
                order.Add(e.Id);
                any |= selected.Contains(e.Id);
            }
            if (any) ArrangeLayer(layer.Id, order, selected, arrangement, ops);
        }
        if (ops.Count == 0) return false;
        Apply(new BinkBatchOperation(ops, arrangement switch
        {
            BinkArrangement.BringToFront => "Bring to front",
            BinkArrangement.BringForward => "Bring forward",
            BinkArrangement.SendBackward => "Send backward",
            _ => "Send to back",
        }));
        return true;
    }

    /// <summary>
    /// Emits the moves for one layer. Only the chosen elements move, never the ones they pass: bringing one stroke to
    /// the front of a thousand is one operation, not a thousand.
    /// </summary>
    private static void ArrangeLayer(Guid layerId, List<Guid> order, HashSet<Guid> selected, BinkArrangement arrangement, List<BinkDrawingOperation> ops)
    {
        int n = order.Count;
        switch (arrangement)
        {
            case BinkArrangement.BringToFront:
            case BinkArrangement.SendToBack:
            {
                var chosen = new List<Guid>();
                foreach (Guid id in order) if (selected.Contains(id)) chosen.Add(id);
                bool front = arrangement == BinkArrangement.BringToFront;
                // Already there, in order: a no-op would still be an undo step and a message to every collaborator.
                int start = front ? n - chosen.Count : 0;
                bool inPlace = true;
                for (int k = 0; k < chosen.Count && inPlace; k++) inPlace = order[start + k] == chosen[k];
                if (inPlace) return;
                // Each in turn to the top (bottom-most first) or to the bottom (top-most first), which leaves them in
                // their original order at the destination.
                if (front) foreach (Guid id in chosen) ops.Add(new BinkReorderElementOperation(id, layerId, n - 1));
                else for (int k = chosen.Count - 1; k >= 0; k--) ops.Add(new BinkReorderElementOperation(chosen[k], layerId, 0));
                break;
            }
            case BinkArrangement.BringForward:
                // From the top down, each chosen element steps over the unchosen one above it; a block of chosen
                // elements therefore moves up by one as a block.
                for (int i = n - 2; i >= 0; i--)
                {
                    if (!selected.Contains(order[i]) || selected.Contains(order[i + 1])) continue;
                    (order[i], order[i + 1]) = (order[i + 1], order[i]);
                    ops.Add(new BinkReorderElementOperation(order[i + 1], layerId, i + 1));
                }
                break;
            case BinkArrangement.SendBackward:
                for (int i = 1; i < n; i++)
                {
                    if (!selected.Contains(order[i]) || selected.Contains(order[i - 1])) continue;
                    (order[i], order[i - 1]) = (order[i - 1], order[i]);
                    ops.Add(new BinkReorderElementOperation(order[i - 1], layerId, i - 1));
                }
                break;
        }
    }

    /// <summary>
    /// Adds copies of elements, offset by a document-space distance, as one undoable step. Each copy goes on top of
    /// its original's layer, and the copies keep their relative stacking order.
    /// </summary>
    /// <param name="elementIds">The elements to copy; ids not in the document are ignored.</param>
    /// <param name="offset">How far the copies are moved from the originals.</param>
    /// <returns>The copies, in the order they were added.</returns>
    public IReadOnlyList<BinkElement> DuplicateElements(IEnumerable<Guid> elementIds, System.Numerics.Vector2 offset)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        var wanted = new HashSet<Guid>(elementIds);
        // A group copied whole becomes a new group of the copies, so moving the copy does not drag the original
        // along. A group copied in part (a member duplicated while working inside its group) is kept: the copy joins
        // the original's group, next to the element it was copied from.
        HashSet<Guid> whole = ContainedGroups(wanted, selectableOnly: true);
        var renamed = new Dictionary<Guid, Guid>();
        var copies = new List<(Guid Layer, BinkElement Copy)>();
        foreach (BinkLayer layer in _layers)
        {
            foreach (BinkElement e in layer.Elements)
            {
                if (!wanted.Contains(e.Id) || e is BinkStroke { IsEraser: true } or BinkUnknownElement) continue;
                BinkElement copy = e.Translate(offset) with { Id = Guid.NewGuid(), Version = 0 };
                if (e.GroupIds.Count > 0) copy = copy with { GroupIds = RenameGroups(e.GroupIds, whole, renamed) };
                copies.Add((layer.Id, copy));
            }
        }
        if (copies.Count == 0) return [];
        var ops = new List<BinkDrawingOperation>(copies.Count);
        foreach ((Guid layer, BinkElement copy) in copies) ops.Add(new BinkAddElementOperation(layer, copy));
        Apply(new BinkBatchOperation(ops, "Duplicate"));
        return copies.ConvertAll(static c => c.Copy);
    }

    private static Guid[] RenameGroups(IReadOnlyList<Guid> chain, HashSet<Guid> whole, Dictionary<Guid, Guid> renamed)
    {
        var result = new Guid[chain.Count];
        for (int i = 0; i < chain.Count; i++)
        {
            Guid group = chain[i];
            if (!whole.Contains(group)) result[i] = group;
            else if (renamed.TryGetValue(group, out Guid name)) result[i] = name;
            else result[i] = renamed[group] = Guid.NewGuid();
        }
        return result;
    }

    /// <summary>
    /// The groups whose every member is among <paramref name="elementIds"/>. Those are the groups an edit of exactly
    /// these elements acts on as a whole; a group with members outside the set is only partly involved.
    /// </summary>
    /// <param name="elementIds">The elements.</param>
    /// <param name="selectableOnly">
    /// Whether only the members that can be selected count: not locked, and on a layer that is neither locked nor
    /// hidden. A selection never holds the others, so a group the user selected in full is whole to the edits made
    /// on the selection. Grouping counts every member, because a new group has to contain a group entirely or not at all.
    /// </param>
    private HashSet<Guid> ContainedGroups(HashSet<Guid> elementIds, bool selectableOnly)
    {
        var groups = new HashSet<Guid>();
        foreach (Guid id in elementIds)
        {
            if (TryGetElement(id, out BinkElement? e)) groups.UnionWith(e.GroupIds);
        }
        if (groups.Count == 0) return groups;
        foreach (BinkLayer layer in _layers)
        {
            bool layerEditable = layer.IsVisible && !layer.IsLocked;
            foreach (BinkElement e in layer.Elements)
            {
                if (e.GroupIds.Count == 0 || elementIds.Contains(e.Id)) continue;
                if (selectableOnly && !(layerEditable && e.CanBeEdited)) continue;
                foreach (Guid g in e.GroupIds) groups.Remove(g);
            }
        }
        return groups;
    }

    /// <summary>
    /// Groups elements, as one undoable step, so that picking any of them selects them all and they move, resize and
    /// copy together. The members keep their layers; within each layer they are brought together in the stacking
    /// order, just below the top-most of them, so nothing else is sandwiched between them.
    /// </summary>
    /// <remarks>
    /// Groups nest. Groups the elements make up entirely end up inside the new group, and the new group sits inside
    /// any group the elements all belong to only in part, which is what grouping while working inside a group means.
    /// An element that is the only one picked from a group the others are not in leaves that group, because a group
    /// cannot overlap another without containing it.
    /// </remarks>
    /// <param name="elementIds">The elements to group; ids not in the document, and eraser strokes, are ignored.</param>
    /// <returns>The new group's id, or null when fewer than two elements could be grouped.</returns>
    /// <exception cref="InvalidOperationException">The grouping would nest an element more than <see cref="BinkElement.MaxGroupDepth"/> groups deep.</exception>
    public Guid? GroupElements(IEnumerable<Guid> elementIds)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        var ids = new HashSet<Guid>();
        var members = new List<BinkElement>();
        foreach (Guid id in elementIds)
        {
            if (!TryGetElement(id, out BinkElement? e) || e is BinkStroke { IsEraser: true } or BinkUnknownElement || !ids.Add(id)) continue;
            members.Add(e);
        }
        if (members.Count < 2) return null;

        // Each chain splits into the groups the members make up whole, which go inside the new group, and the
        // groups they share with elements outside, which stay outside it. The outer part has to be the same for
        // every member, so only what all of them share is kept.
        HashSet<Guid> whole = ContainedGroups(ids, selectableOnly: false);
        List<Guid>? shared = null;
        foreach (BinkElement e in members)
        {
            var outer = new List<Guid>();
            foreach (Guid g in e.GroupIds) if (!whole.Contains(g)) outer.Add(g);
            if (shared is null) shared = outer;
            else
            {
                // The longest common suffix: groups are listed innermost first, so the outermost ones line up at the end.
                int common = 0;
                while (common < shared.Count && common < outer.Count && shared[^(common + 1)] == outer[^(common + 1)]) common++;
                shared.RemoveRange(0, shared.Count - common);
            }
        }

        var group = Guid.NewGuid();
        var ops = new List<BinkDrawingOperation>(members.Count * 2);
        foreach (BinkElement e in members)
        {
            var chain = new List<Guid>(e.GroupIds.Count + 1);
            foreach (Guid g in e.GroupIds) if (whole.Contains(g)) chain.Add(g);
            chain.Add(group);
            chain.AddRange(shared!);
            if (chain.Count > BinkElement.MaxGroupDepth) throw new InvalidOperationException($"Groups can be nested at most {BinkElement.MaxGroupDepth} deep.");
            ops.Add(new BinkReplaceElementOperation(e with { GroupIds = chain }));
        }
        GatherInStackingOrder(ids, ops);
        Apply(new BinkBatchOperation(ops, "Group"));
        return group;
    }

    /// <summary>
    /// Moves the given elements of each layer next to each other, just below the top-most of them, without changing
    /// their order relative to each other. Only the elements that are not already in place move.
    /// </summary>
    private void GatherInStackingOrder(HashSet<Guid> ids, List<BinkDrawingOperation> ops)
    {
        foreach (BinkLayer layer in _layers)
        {
            int anchor = -1;
            for (int i = layer.Elements.Count - 1; i >= 0; i--)
            {
                if (!ids.Contains(layer.Elements[i].Id)) continue;
                // Working down from the top-most member, each one goes directly below the previous. A member below
                // the anchor is unaffected by the moves made above it, so its index is still the one read here.
                if (anchor >= 0 && i != anchor - 1)
                {
                    ops.Add(new BinkReorderElementOperation(layer.Elements[i].Id, layer.Id, anchor - 1));
                    anchor--;
                }
                else anchor = i;
            }
        }
    }

    /// <summary>
    /// Ungroups, as one undoable step: dissolves the outermost group each element belongs to that the given elements
    /// make up entirely. Selecting a group and ungrouping it therefore takes it apart one level, leaving any groups
    /// nested inside it intact, and a group that is only partly given is left alone.
    /// </summary>
    /// <remarks>
    /// Members that cannot be selected (locked, or on a locked or hidden layer) do not need to be given, since a
    /// selection never holds them; they leave the dissolved group too, so no group of one is left behind.
    /// </remarks>
    /// <param name="elementIds">The elements, typically the selection.</param>
    /// <returns>True when any group was dissolved.</returns>
    public bool UngroupElements(IEnumerable<Guid> elementIds)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        var ids = new HashSet<Guid>(elementIds);
        HashSet<Guid> whole = ContainedGroups(ids, selectableOnly: true);
        if (whole.Count == 0) return false;
        var dissolved = new HashSet<Guid>();
        foreach (Guid id in ids)
        {
            if (!TryGetElement(id, out BinkElement? e) || e is BinkUnknownElement) continue;
            for (int i = e.GroupIds.Count - 1; i >= 0; i--)
            {
                if (!whole.Contains(e.GroupIds[i])) continue;
                dissolved.Add(e.GroupIds[i]);
                break;
            }
        }
        if (dissolved.Count == 0) return false;
        // Members of one group share the chain from that group outwards, so the outermost group given whole is the
        // same for all of them, and removing every dissolved group from every chain removes exactly that one.
        var ops = new List<BinkDrawingOperation>();
        foreach (BinkLayer layer in _layers)
        {
            foreach (BinkElement e in layer.Elements)
            {
                // An element of a type this version does not know is saved exactly as it was read, so it is not changed.
                if (e.GroupIds.Count == 0 || e is BinkUnknownElement) continue;
                var chain = new List<Guid>(e.GroupIds.Count);
                foreach (Guid g in e.GroupIds) if (!dissolved.Contains(g)) chain.Add(g);
                if (chain.Count != e.GroupIds.Count) ops.Add(new BinkReplaceElementOperation(e with { GroupIds = chain }));
            }
        }
        Apply(new BinkBatchOperation(ops, "Ungroup"));
        return true;
    }

    /// <summary>
    /// Locks or unlocks elements, as one undoable step. A locked element is drawn as usual, but tools cannot select,
    /// move or erase it.
    /// </summary>
    /// <param name="elementIds">The elements; ids not in the document are ignored.</param>
    /// <param name="locked">Whether they end up locked.</param>
    /// <returns>True when anything changed.</returns>
    public bool SetLocked(IEnumerable<Guid> elementIds, bool locked)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        var ops = new List<BinkDrawingOperation>();
        var seen = new HashSet<Guid>();
        foreach (Guid id in elementIds)
        {
            // An element of a type this version does not know is saved exactly as it was read, so it is not changed.
            if (!seen.Add(id) || !TryGetElement(id, out BinkElement? e) || e.IsLocked == locked || e is BinkUnknownElement) continue;
            ops.Add(new BinkReplaceElementOperation(e with { IsLocked = locked }));
        }
        if (ops.Count == 0) return false;
        Apply(new BinkBatchOperation(ops, locked ? "Lock" : "Unlock"));
        return true;
    }

    /// <summary>
    /// Returns the operation with element versions filled in from the document's current state. Only replacements
    /// need this: every other operation already carries whatever version its element should keep.
    /// </summary>
    /// <remarks>
    /// Inside a batch the same element can be replaced several times (redoing a drag replays every frame), and
    /// each replacement is stored one version above the previous one. <paramref name="versions"/> carries the
    /// versions given out so far in the batch, so the versions reported match the versions stored; deciding each
    /// one from the document's state before the batch would report the same version for every frame.
    /// </remarks>
    private BinkDrawingOperation AssignVersions(BinkDrawingOperation operation, bool remote, Dictionary<Guid, int>? versions)
    {
        switch (operation)
        {
            case BinkReplaceElementOperation replace:
            {
                Guid id = replace.Element.Id;
                int current;
                if (versions is not null && versions.TryGetValue(id, out int pending)) current = pending;
                else if (TryGetElement(id, out BinkElement? existing)) current = existing.Version;
                else return replace;
                int version = replace.Element.Version > current || (remote && replace.Element.Version == current) ? replace.Element.Version : NextVersion(current);
                if (versions is not null) versions[id] = version;
                return version == replace.Element.Version ? replace : new BinkReplaceElementOperation(replace.Element with { Version = version });
            }
            case BinkAddElementOperation add when versions is not null:
                versions[add.Element.Id] = add.Element.Version;
                return add;
            case BinkAddLayerOperation { Elements: { } elements } addLayer when versions is not null:
                foreach (BinkElement e in elements) versions[e.Id] = e.Version;
                return addLayer;
            case BinkBatchOperation batch:
            {
                List<BinkDrawingOperation>? rewritten = null;
                for (int i = 0; i < batch.Operations.Count; i++)
                {
                    BinkDrawingOperation assigned = AssignVersions(batch.Operations[i], remote, versions);
                    if (!ReferenceEquals(assigned, batch.Operations[i]) && rewritten is null)
                    {
                        rewritten = new List<BinkDrawingOperation>(batch.Operations.Count);
                        for (int j = 0; j < i; j++) rewritten.Add(batch.Operations[j]);
                    }
                    rewritten?.Add(assigned);
                }
                return rewritten is null ? batch : new BinkBatchOperation(rewritten, batch.Label);
            }
            default:
                return operation;
        }
    }

    /// <summary>
    /// The version after <paramref name="version"/>. It saturates rather than wrapping: a version read from an
    /// untrusted file or collaborator can be <see cref="int.MaxValue"/>, and wrapping to a negative number would
    /// make every later edit of that element lose last-writer-wins on every other replica.
    /// </summary>
    private static int NextVersion(int version) => version == int.MaxValue ? int.MaxValue : version + 1;

    /// <summary>
    /// True while a failed <see cref="BinkBatchOperation"/> undoes the part of itself that was applied. Replacements
    /// then restore the previous element exactly, version included.
    /// </summary>
    internal bool Restoring { get; set; }

    // ---- Core mutations (called by operations) ------------------------------------------------------------------------

    internal void InsertElementCore(Guid layerId, BinkElement element, int? index)
    {
        BinkLayer layer = RequireLayer(layerId);
        if (_elementLayers.ContainsKey(element.Id)) throw new InvalidOperationException($"An element with id {element.Id} already exists in the document.");
        ValidateElement(element);
        if (_elementLayers.Count >= Limits.MaxElements) throw new InvalidOperationException($"The document limit of {Limits.MaxElements} elements was reached.");
        int at = index is null ? layer.Count : Math.Clamp(index.Value, 0, layer.Count);
        layer.InsertAt(at, element);
        _elementLayers[element.Id] = layer;
        if (element is BinkStroke s) _totalPoints += s.Points.Count;
    }

    internal (BinkLayer Layer, int Index, BinkElement Element) RemoveElementCore(Guid elementId)
    {
        if (!_elementLayers.TryGetValue(elementId, out BinkLayer? layer)) throw new InvalidOperationException($"Element {elementId} is not in the document.");
        int index = layer.IndexOf(elementId);
        BinkElement element = layer.Elements[index];
        layer.RemoveAt(index);
        _elementLayers.Remove(elementId);
        if (element is BinkStroke s) _totalPoints -= s.Points.Count;
        return (layer, index, element);
    }

    internal BinkElement ReplaceElementCore(BinkElement element)
    {
        if (!_elementLayers.TryGetValue(element.Id, out BinkLayer? layer)) throw new InvalidOperationException($"Element {element.Id} is not in the document.");
        int index = layer.IndexOf(element.Id);
        BinkElement previous = layer.Elements[index];

        // The outgoing element's points no longer count towards the total, so remove them before validating.
        // Otherwise replacing a stroke with one of the same size could falsely trip the document's point limit.
        if (previous is BinkStroke outgoing) _totalPoints -= outgoing.Points.Count;
        try
        {
            ValidateElement(element);
        }
        catch
        {
            if (previous is BinkStroke restore) _totalPoints += restore.Points.Count;
            throw;
        }

        // Apply assigned the version already; anything reaching here with a stale one is a direct caller, and is
        // still bumped so two versions of an element are never confused. Two cases keep the version they carry: a
        // remote edit that tied and won (see Apply), and the rollback of a failed batch, which must put the
        // element back exactly as it was or this replica's versions drift from everyone else's.
        bool keep = element.Version > previous.Version
            || Restoring
            || (_keepRemoteVersions && element.Version == previous.Version);
        BinkElement next = keep ? element : element with { Version = NextVersion(previous.Version) };
        layer.SetAt(index, next);
        if (next is BinkStroke incoming) _totalPoints += incoming.Points.Count;
        return previous;
    }

    internal (Guid PreviousLayerId, int PreviousIndex) ReorderElementCore(Guid elementId, Guid targetLayerId, int? index)
    {
        BinkLayer target = RequireLayer(targetLayerId);
        if (_elementLayers.TryGetValue(elementId, out BinkLayer? current) && current == target)
        {
            int position = target.IndexOf(elementId);
            if ((index is null ? target.Count - 1 : Math.Clamp(index.Value, 0, target.Count - 1)) == position) return (target.Id, position);
        }
        (BinkLayer source, int previousIndex, BinkElement element) = RemoveElementCore(elementId);
        int at = index is null ? target.Count : Math.Clamp(index.Value, 0, target.Count);
        target.InsertAt(at, element);
        _elementLayers[elementId] = target;
        if (element is BinkStroke s) _totalPoints += s.Points.Count;
        return (source.Id, previousIndex);
    }

    internal void InsertLayerCore(Guid layerId, BinkLayerProperties properties, int? index, IReadOnlyList<BinkElement>? elements)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (_layersById.ContainsKey(layerId)) throw new InvalidOperationException($"A layer with id {layerId} already exists.");
        if (_layers.Count >= Limits.MaxLayers) throw new InvalidOperationException($"The document limit of {Limits.MaxLayers} layers was reached.");
        if (elements is not null)
        {
            // The elements arrive together, so they are checked against each other as well as the document: a
            // repeated id would leave two elements behind one index entry, and validating each stroke against the
            // document's point total alone would let one layer carry any number of points past the limit.
            var ids = new HashSet<Guid>();
            long points = 0;
            foreach (BinkElement e in elements)
            {
                if (_elementLayers.ContainsKey(e.Id) || !ids.Add(e.Id)) throw new InvalidOperationException($"An element with id {e.Id} already exists in the document.");
                ValidateElement(e);
                if (e is BinkStroke s) points += s.Points.Count;
            }
            if (_elementLayers.Count + elements.Count > Limits.MaxElements) throw new InvalidOperationException($"The document limit of {Limits.MaxElements} elements was reached.");
            if (_totalPoints + points > Limits.MaxTotalPoints) throw new InvalidOperationException($"The document limit of {Limits.MaxTotalPoints} stroke points was reached.");
        }
        var layer = new BinkLayer(layerId, Sanitize(properties), elements);
        int at = index is null ? _layers.Count : Math.Clamp(index.Value, 0, _layers.Count);
        _layers.Insert(at, layer);
        _layersById[layerId] = layer;
        foreach (BinkElement e in layer.Elements)
        {
            _elementLayers[e.Id] = layer;
            if (e is BinkStroke s) _totalPoints += s.Points.Count;
        }
    }

    internal (int Index, BinkLayer Layer) RemoveLayerCore(Guid layerId)
    {
        BinkLayer layer = RequireLayer(layerId);
        if (_layers.Count == 1) throw new InvalidOperationException("A document must keep at least one layer.");
        int index = _layers.IndexOf(layer);
        _layers.RemoveAt(index);
        _layersById.Remove(layerId);
        foreach (BinkElement e in layer.Elements)
        {
            _elementLayers.Remove(e.Id);
            if (e is BinkStroke s) _totalPoints -= s.Points.Count;
        }
        return (index, layer);
    }

    internal BinkLayerProperties UpdateLayerCore(Guid layerId, BinkLayerProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        BinkLayer layer = RequireLayer(layerId);
        BinkLayerProperties previous = layer.Properties;
        layer.Properties = Sanitize(properties);
        return previous;
    }

    /// <summary>
    /// Keeps layer properties to values every renderer draws the same way. Erasing is what an element does to its own
    /// layer; a whole layer composited with it would cut its shape out of everything below, which nothing promises.
    /// </summary>
    private static BinkLayerProperties Sanitize(BinkLayerProperties properties)
    {
        float opacity = float.IsFinite(properties.Opacity) ? Math.Clamp(properties.Opacity, 0f, 1f) : 1f;
        BinkBlendMode blend = properties.BlendMode is BinkBlendMode.Multiply or BinkBlendMode.Screen ? properties.BlendMode : BinkBlendMode.Normal;
        if (opacity == properties.Opacity && blend == properties.BlendMode && properties.Name is not null) return properties;
        return properties with { Opacity = opacity, BlendMode = blend, Name = properties.Name ?? string.Empty };
    }

    internal int ReorderLayerCore(Guid layerId, int index)
    {
        BinkLayer layer = RequireLayer(layerId);
        int previous = _layers.IndexOf(layer);
        _layers.RemoveAt(previous);
        _layers.Insert(Math.Clamp(index, 0, _layers.Count), layer);
        return previous;
    }

    internal BinkDocumentSettings UpdateSettingsCore(BinkDocumentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        BinkDocumentSettings previous = Settings;
        Settings = settings with { Width = Math.Clamp(settings.Width, 0, Limits.MaxCoordinate), Height = Math.Clamp(settings.Height, 0, Limits.MaxCoordinate) };
        return previous;
    }

    internal string? SetMetadataCore(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _metadata.TryGetValue(key, out string? previous);
        if (value is null) _metadata.Remove(key);
        else _metadata[key] = value;
        return previous;
    }

    private void ValidateElement(BinkElement element)
    {
        // Both spaces are checked. Local bounds catch corrupt geometry, and document bounds catch a transform
        // that scales modest local coordinates far outside the range: validating only the local box would let an
        // imported document place elements millions of units away and make zoom-to-fit and export meaningless.
        ValidateBounds(element, element.LocalBounds, "local");
        if (!element.Transform.IsIdentity) ValidateBounds(element, element.Bounds, "document");
        if (element is BinkStroke stroke)
        {
            if (stroke.Points.Count > Limits.MaxPointsPerStroke) throw new InvalidOperationException($"Stroke has {stroke.Points.Count} points; the limit is {Limits.MaxPointsPerStroke}.");
            if (_totalPoints + stroke.Points.Count > Limits.MaxTotalPoints) throw new InvalidOperationException($"The document limit of {Limits.MaxTotalPoints} stroke points was reached.");
            if (stroke.Brush.Width > Limits.MaxStrokeWidth) throw new InvalidOperationException($"Brush width {stroke.Brush.Width} exceeds the limit of {Limits.MaxStrokeWidth}.");
            stroke.Brush.Validate();
        }
        // The same bound loading applies, so a document that holds a path can always be read back.
        else if (element is BinkPathElement path && path.Path.VerbCount > Limits.MaxPathSegments)
        {
            throw new InvalidOperationException($"Path has {path.Path.VerbCount} segments; the limit is {Limits.MaxPathSegments}.");
        }
    }

    private void ValidateBounds(BinkElement element, BinkRect b, string space)
    {
        if (!float.IsFinite(b.X) || !float.IsFinite(b.Y) || !float.IsFinite(b.Width) || !float.IsFinite(b.Height))
            throw new InvalidOperationException($"{element.TypeName} has non-finite {space} bounds.");
        float max = Limits.MaxCoordinate;
        if (MathF.Abs(b.Left) > max || MathF.Abs(b.Top) > max || MathF.Abs(b.Right) > max || MathF.Abs(b.Bottom) > max)
            throw new InvalidOperationException($"{element.TypeName} lies outside the allowed {space} coordinate range (+/-{max}).");
    }

    /// <summary>
    /// Creates an independent copy. Elements are immutable and shared; layers, settings and metadata are copied.
    /// </summary>
    public BinkDocument Clone()
    {
        var copy = new BinkDocument(Id, Settings, Limits, withoutLayers: true);
        foreach (BinkLayer layer in _layers) copy.InsertLayerCore(layer.Id, layer.Properties, null, layer.Elements.ToArray());
        foreach ((string k, string v) in _metadata) copy._metadata[k] = v;
        copy.Revision = Revision;
        copy.SavedRevision = SavedRevision;
        return copy;
    }

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"BinkDocument {Id:N}: {_layers.Count} layers, {ElementCount} elements, revision {Revision}");
}

/// <summary>Where <see cref="BinkDocument.Arrange"/> moves elements in their layer's stacking order.</summary>
public enum BinkArrangement : byte
{
    /// <summary>Above everything else in the layer.</summary>
    BringToFront = 0,
    /// <summary>One step up: above the next element that is not being moved.</summary>
    BringForward = 1,
    /// <summary>One step down: below the previous element that is not being moved.</summary>
    SendBackward = 2,
    /// <summary>Below everything else in the layer.</summary>
    SendToBack = 3,
}
