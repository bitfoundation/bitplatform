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
public sealed class BinkDocument
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
            _batchInverses!.Insert(0, inverse);
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
        var ops = new List<BinkDrawingOperation>();
        var seen = new HashSet<Guid>();
        // A repeated id would remove the element and then fail on the second attempt, rolling the batch back.
        foreach (Guid id in elementIds)
        {
            if (ContainsElement(id) && seen.Add(id)) ops.Add(new BinkRemoveElementOperation(id));
        }
        if (ops.Count > 0) Apply(new BinkBatchOperation(ops, "Delete"));
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
        List<BinkElement> list = layer.MutableElements;
        int at = index is null ? list.Count : Math.Clamp(index.Value, 0, list.Count);
        list.Insert(at, element);
        _elementLayers[element.Id] = layer;
        if (element is BinkStroke s) _totalPoints += s.Points.Count;
    }

    internal (BinkLayer Layer, int Index, BinkElement Element) RemoveElementCore(Guid elementId)
    {
        if (!_elementLayers.TryGetValue(elementId, out BinkLayer? layer)) throw new InvalidOperationException($"Element {elementId} is not in the document.");
        int index = layer.IndexOf(elementId);
        BinkElement element = layer.Elements[index];
        layer.MutableElements.RemoveAt(index);
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
        layer.MutableElements[index] = next;
        if (next is BinkStroke incoming) _totalPoints += incoming.Points.Count;
        return previous;
    }

    internal (Guid PreviousLayerId, int PreviousIndex) ReorderElementCore(Guid elementId, Guid targetLayerId, int? index)
    {
        BinkLayer target = RequireLayer(targetLayerId);
        (BinkLayer source, int previousIndex, BinkElement element) = RemoveElementCore(elementId);
        List<BinkElement> list = target.MutableElements;
        int at = index is null ? list.Count : Math.Clamp(index.Value, 0, list.Count);
        list.Insert(at, element);
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
        var layer = new BinkLayer(layerId, properties, elements);
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
        layer.Properties = properties with { Opacity = Math.Clamp(properties.Opacity, 0f, 1f), Name = properties.Name ?? string.Empty };
        return previous;
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
        return copy;
    }

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"BinkDocument {Id:N}: {_layers.Count} layers, {ElementCount} elements, revision {Revision}");
}
