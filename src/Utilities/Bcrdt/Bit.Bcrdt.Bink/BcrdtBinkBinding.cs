using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Bit.Bcrdt.Integrations;
using Bit.Bink;

namespace Bit.Bcrdt.Bink;

/// <summary>
/// Keeps a Bink drawing and a collaborative map in step, in both directions, so several people draw on one
/// <see cref="BinkDocument"/> with bit Bcrdt as the collaboration backend.
/// </summary>
/// <remarks>
/// <para>
/// Every layer and every element is an entry of its own in <see cref="Root"/>, keyed by its identifier: <c>l:{id}</c>
/// for a layer and <c>e:{id}</c> for an element, each a nested <see cref="BcrdtMap"/> holding the object's properties in
/// Bink's own file format, one key per property. Page settings are <c>s:width</c>, <c>s:height</c> and
/// <c>s:background</c>, and document metadata is <c>m:{key}</c>. Nothing is kept in an array: two people adding,
/// changing or deleting different elements never touch the same entry, a deletion is by identifier, and concurrent
/// changes to different properties of one element - one moving it, one recolouring it - both survive. A finished
/// stroke's points never change, so they are one value.
/// </para>
/// <para>
/// Stacking order is a number in each entry (<c>$order</c>), and an element's layer is another (<c>$layer</c>): moving an
/// element up, down or to another layer is one field write, which merges like any other. Concurrent moves of the same
/// element resolve to one of them, the same one everywhere.
/// </para>
/// <para>
/// Local changes - every operation the drawing reports that did not come from here - are written as one transaction
/// per gesture, tagged with <see cref="Origin"/>. Every other change to the map - a collaborator's, an undo's, your own
/// code's - is applied to the drawing as one batch with <see cref="BinkChangeOrigin.Remote"/>, which Bink's history does
/// not record and this binding does not write back. Nothing echoes.
/// </para>
/// <para>
/// Like the document it binds, the binding is not thread-safe: use both from the thread the document's updates are
/// applied on, which a document opened through <c>IBcrdtCollaborationService</c> already guarantees.
/// </para>
/// </remarks>
public sealed class BcrdtBinkBinding : IDisposable
{
    private const string ElementPrefix = "e:";
    private const string LayerPrefix = "l:";
    private const string SettingPrefix = "s:";
    private const string MetadataPrefix = "m:";
    private const string LayerKey = "$layer";

    /// <summary>The presence field the cursor and selection are published under.</summary>
    public const string PresenceField = "bink";

    /// <summary>
    /// The identifier of the layer shown while the shared drawing has none of its own.
    /// </summary>
    /// <remarks>
    /// Bink always has at least one layer, and a new <see cref="BinkDocument"/> creates it with a fresh identifier. Two
    /// people opening a new shared drawing would otherwise each contribute a layer of their own, and both would end up
    /// with two. Every replica shows this one instead, under the same identifier, so what they draw into it lands in
    /// the same layer.
    /// </remarks>
    public static readonly Guid DefaultLayerId = new("2f0c9a6e-5b1d-4c3e-8a7f-1d6b3e9c4a20");

    private static readonly BinkLayerProperties DefaultLayerProperties = new() { Name = "Drawing" };

    private static readonly HashSet<string> ExcludedElementProperties = new(StringComparer.Ordinal) { "id", "v" };

    private static readonly HashSet<string> LayerProperties = new(StringComparer.Ordinal) { "name", "visible", "locked", "opacity", "blend", "metadata" };

    private readonly BcrdtBinkBindingOptions _options;
    private readonly IDisposable _subscription;

    private readonly HashSet<Guid> _dirtyElements = [];
    private readonly HashSet<Guid> _dirtyLayers = [];
    private readonly HashSet<Guid> _dirtyElementOrder = [];
    private readonly HashSet<string> _dirtyMetadata = new(StringComparer.Ordinal);
    private bool _dirtyLayerOrder;
    private bool _dirtySettings;

    private Vector2? _cursor;
    private Guid[] _selection = [];
    private bool _disposed;

    /// <summary>Binds a drawing to a collaborative map.</summary>
    /// <param name="root">
    /// The map to keep the drawing in - a root map of its own, such as <c>document.Crdt.GetMap("drawing")</c>, or a
    /// nested one that already exists on every replica. It must belong to a document.
    /// </param>
    /// <param name="document">The drawing.</param>
    /// <param name="options">Settings, or null for the defaults.</param>
    /// <remarks>
    /// <para>
    /// When the map already holds a drawing, that drawing replaces whatever <paramref name="document"/> held: the
    /// shared state is the truth, and a participant joins it. When the map is empty and the document has content -
    /// elements, more than one layer, or metadata - the document is written into the map, in one transaction, to start
    /// the shared drawing from. A new, empty document writes nothing; it is shown the shared drawing's default layer
    /// (see <see cref="DefaultLayerId"/>).
    /// </para>
    /// <para>
    /// Starting a shared drawing from existing content is a job for one participant. Two that each write their own
    /// content into an empty map end up with both drawings, merged.
    /// </para>
    /// </remarks>
    public BcrdtBinkBinding(BcrdtMap root, BinkDocument document, BcrdtBinkBindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(document);
        if (!root.IsAttached) throw new ArgumentException("The map must belong to a document before a drawing can be bound to it.", nameof(root));

        _options = options ?? new BcrdtBinkBindingOptions();
        if (_options.Selection is not null && _options.Presence is null)
        {
            throw new ArgumentException("Publishing a selection needs a presence to publish it through.", nameof(options));
        }

        Root = root;
        Document = document;
        Origin = _options.Origin ?? this;

        if (HasSharedState() || IsEmpty(document)) ApplyShared(Everything());
        else Seed();

        document.Changed += OnDocumentChanged;
        if (_options.History is { } history) history.Changed += OnHistoryChanged;
        if (_options.Selection is { } selection) selection.Changed += OnSelectionChanged;
        _subscription = root.ObserveDeep(OnSharedChanged);
    }

    /// <summary>The map the drawing is kept in.</summary>
    public BcrdtMap Root { get; }

    /// <summary>The drawing.</summary>
    public BinkDocument Document { get; }

    /// <summary>The origin the binding's transactions carry.</summary>
    public object Origin { get; }

    /// <summary>
    /// Whether local changes are waiting to be written: a gesture is still under way, or a change was made inside a
    /// history group that has not closed.
    /// </summary>
    public bool HasPendingChanges =>
        _dirtyElements.Count > 0 || _dirtyLayers.Count > 0 || _dirtyElementOrder.Count > 0 || _dirtyMetadata.Count > 0 ||
        _dirtyLayerOrder || _dirtySettings;

    /// <summary>
    /// Raised when a change from the shared state could not be applied to the drawing - it breaks one of the drawing's
    /// <see cref="BinkDocumentLimits"/>, or holds an element this version of Bink cannot read - with the reason.
    /// </summary>
    /// <remarks>
    /// Such an element is left out of this replica's drawing rather than shown wrong, and stays in the shared state for
    /// the replicas that can show it.
    /// </remarks>
    public event Action<string>? RemoteChangeRejected;

    /// <summary>Writes the local changes still waiting, as one transaction.</summary>
    /// <returns>Whether there was anything to write.</returns>
    /// <remarks>
    /// Changes are written by themselves, at the end of each gesture; this is for the cases the binding cannot see the
    /// end of - a gesture the history discarded, or a change made with no history while one was configured.
    /// </remarks>
    public bool Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!HasPendingChanges) return false;

        // Taken before writing, so a change a handler makes while the transaction settles starts a fresh set.
        Guid[] elements = [.. _dirtyElements];
        Guid[] layers = [.. _dirtyLayers];
        Guid[] elementOrder = [.. _dirtyElementOrder];
        string[] metadata = [.. _dirtyMetadata];
        bool layerOrder = _dirtyLayerOrder;
        bool settings = _dirtySettings;
        _dirtyElements.Clear();
        _dirtyLayers.Clear();
        _dirtyElementOrder.Clear();
        _dirtyMetadata.Clear();
        _dirtyLayerOrder = false;
        _dirtySettings = false;

        Root.Doc.Transact(_ => Write(elements, layers, elementOrder, metadata, layerOrder, settings), Origin);
        return true;
    }

    /// <summary>
    /// Creates a selective undo manager for this drawing: it undoes this participant's changes and nobody else's.
    /// </summary>
    /// <param name="options">
    /// Settings for the manager; <see cref="Origin"/> is added to its tracked origins. Null uses the defaults.
    /// </param>
    /// <remarks>
    /// <para>
    /// The manager is scoped to <see cref="Root"/> and tracks the binding's transactions, so a step is one gesture
    /// (merged with the ones that follow within <see cref="BcrdtUndoManagerOptions.CaptureTimeout"/>). Undoing writes
    /// new changes to the shared state, which the binding applies to the drawing like a collaborator's: they reach
    /// everybody, and Bink's own history does not record them.
    /// </para>
    /// <para>
    /// Bink's history (<c>BinkCanvas.History</c>, Ctrl+Z on a canvas) keeps working beside it, and its undo is written
    /// like any other local change. The difference is in what an undo puts back. Bink's history restores whole
    /// elements, so undoing a move of an element a collaborator has since recoloured restores the old colour too. This
    /// manager restores only the properties the step changed, and only if nobody has written them since. Use one or the
    /// other for a given drawing, not both.
    /// </para>
    /// </remarks>
    public BcrdtUndoManager CreateUndoManager(BcrdtUndoManagerOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        options ??= new BcrdtUndoManagerOptions();
        return new BcrdtUndoManager(Root, new BcrdtUndoManagerOptions
        {
            CaptureTimeout = options.CaptureTimeout,
            TrackedOrigins = [.. options.TrackedOrigins, Origin],
            DeleteFilter = options.DeleteFilter,
            MaxStackItems = options.MaxStackItems,
            IgnoreRemoteMapChanges = options.IgnoreRemoteMapChanges,
        });
    }

    /// <summary>Publishes where the local pointer is, in document coordinates, or null when it left the drawing.</summary>
    /// <exception cref="InvalidOperationException">No <see cref="BcrdtBinkBindingOptions.Presence"/> was given.</exception>
    public void PublishCursor(Vector2? position)
    {
        _cursor = position;
        Publish();
    }

    /// <summary>Publishes which elements the local participant has selected.</summary>
    /// <remarks>
    /// <see cref="BcrdtBinkBindingOptions.Selection"/> calls this whenever the selection changes; call it yourself for
    /// a selection the binding was not given.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No <see cref="BcrdtBinkBindingOptions.Presence"/> was given.</exception>
    public void PublishSelection(IEnumerable<Guid> elementIds)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        _selection = [.. elementIds];
        Publish();
    }

    /// <summary>
    /// Everybody else in the drawing, with the cursor and selection each has published. A selection lists only the
    /// elements this replica has.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <see cref="BcrdtBinkBindingOptions.Presence"/> was given.</exception>
    public IReadOnlyList<BcrdtBinkParticipant> Participants
    {
        get
        {
            BcrdtPresenceService presence = RequirePresence();
            var result = new List<BcrdtBinkParticipant>();
            foreach (BcrdtCollaborator other in presence.Others)
            {
                Vector2? cursor = null;
                var selection = new List<Guid>();
                if (other.Custom.GetValueOrDefault(PresenceField) is IEnumerable<KeyValuePair<string, object?>> state)
                {
                    foreach ((string key, object? value) in state)
                    {
                        // Peer-written: anything malformed is ignored rather than trusted.
                        if (key == "cursor" && ReadPoint(value) is { } point) cursor = point;
                        else if (key == "selection" && value is System.Collections.IEnumerable ids and not string)
                        {
                            foreach (object? id in ids)
                            {
                                if (id is string text && Guid.TryParse(text, out Guid element) && Document.ContainsElement(element)) selection.Add(element);
                            }
                        }
                    }
                }
                result.Add(new BcrdtBinkParticipant(other, cursor, selection));
            }
            return result;
        }
    }

    // ---- Local changes -------------------------------------------------------------------------------------------

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        // Remote is what this binding applies, and what a Bink collaboration session applies; Load is a scratch
        // document or a reset - neither is this participant's edit, which is also how Bink's own history and
        // collaboration session tell.
        if (e.Origin is BinkChangeOrigin.Remote or BinkChangeOrigin.Load) return;

        Collect(e.Operation);
        if (IsGestureComplete(e)) Flush();
    }

    /// <summary>Whether a change completes a step, so it is written now rather than held for the rest of its gesture.</summary>
    private bool IsGestureComplete(BinkDocumentChangedEventArgs e)
    {
        BinkHistoryManager? history = _options.History;
        if (history is null || history.Limit <= 0) return true;

        // Undo and redo are recorded once applied, and a change made while another is reported is recorded once the
        // whole notification is over: either way the history announces the step after this, and that writes it.
        if (e.Origin != BinkChangeOrigin.Local || e.Depth > 0) return false;

        // Outside a group the history records the step as the change is reported. It may have done so already,
        // before this handler ran - and then its announcement has been and gone, so the step is written here. Inside
        // a group it records the step when the group closes, which is the end of the gesture.
        return history.UndoStack.Count > 0 && ReferenceEquals(history.UndoStack[^1].Forward, e.Operation);
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        if (HasPendingChanges) Flush();
    }

    private void Collect(BinkDrawingOperation operation)
    {
        switch (operation)
        {
            case BinkBatchOperation batch:
                foreach (BinkDrawingOperation inner in batch.Operations) Collect(inner);
                break;
            case BinkAddElementOperation add:
                _dirtyElements.Add(add.Element.Id);
                _dirtyElementOrder.Add(add.LayerId);
                break;
            case BinkRemoveElementOperation remove:
                _dirtyElements.Add(remove.ElementId);
                break;
            case BinkReplaceElementOperation replace:
                _dirtyElements.Add(replace.Element.Id);
                break;
            case BinkReorderElementOperation reorder:
                _dirtyElements.Add(reorder.ElementId);
                _dirtyElementOrder.Add(reorder.TargetLayerId);
                break;
            case BinkAddLayerOperation addLayer:
                _dirtyLayers.Add(addLayer.LayerId);
                _dirtyLayerOrder = true;
                if (addLayer.Elements is { } elements)
                {
                    foreach (BinkElement element in elements) _dirtyElements.Add(element.Id);
                    _dirtyElementOrder.Add(addLayer.LayerId);
                }
                break;
            case BinkRemoveLayerOperation removeLayer:
                _dirtyLayers.Add(removeLayer.LayerId);
                break;
            case BinkUpdateLayerOperation updateLayer:
                _dirtyLayers.Add(updateLayer.LayerId);
                break;
            case BinkReorderLayerOperation:
                _dirtyLayerOrder = true;
                break;
            case BinkUpdateSettingsOperation:
                _dirtySettings = true;
                break;
            case BinkSetMetadataOperation metadata:
                _dirtyMetadata.Add(metadata.Key);
                break;
        }
    }

    private void Seed()
    {
        foreach (BinkLayer layer in Document.Layers)
        {
            _dirtyLayers.Add(layer.Id);
            _dirtyElementOrder.Add(layer.Id);
            foreach (BinkElement element in layer.Elements) _dirtyElements.Add(element.Id);
        }
        foreach (string key in Document.Metadata.Keys) _dirtyMetadata.Add(key);
        _dirtyLayerOrder = true;
        _dirtySettings = true;
        Flush();
    }

    private void Write(Guid[] elements, Guid[] layers, Guid[] elementOrder, string[] metadata, bool layerOrder, bool settings)
    {
        var orderedLayers = new HashSet<Guid>(elementOrder);

        foreach (Guid id in layers)
        {
            if (Document.TryGetLayer(id, out BinkLayer? layer))
            {
                if (WriteLayer(layer)) layerOrder = true;
                continue;
            }

            Root.Delete(LayerPrefix + Key(id));
            // Removing a layer removes what is in it. Bink does that in one operation, which names the layer only.
            string layerId = Key(id);
            foreach ((Guid element, BcrdtMap entry) in Elements())
            {
                if (entry.Get(LayerKey) as string == layerId && !Document.ContainsElement(element)) Root.Delete(ElementPrefix + Key(element));
            }
        }

        foreach (Guid id in elements)
        {
            string key = ElementPrefix + Key(id);
            if (!Document.TryGetElement(id, out BinkElement? element, out BinkLayer? layer))
            {
                if (Root.ContainsKey(key)) Root.Delete(key);
                continue;
            }

            // A layer only this replica has - the default one, before anybody wrote to it - is written with the
            // first element drawn into it, or the element would be in a layer nobody else can show.
            if (!Root.ContainsKey(LayerPrefix + Key(layer.Id)) && WriteLayer(layer)) layerOrder = true;

            Dictionary<string, object?> fields = Fields(element);
            string layerId = Key(layer.Id);
            if (Root.GetMap(key) is { } entry)
            {
                BcrdtIntegrationFields.Write(entry, fields, static _ => true);
                if (entry.Get(LayerKey) as string != layerId)
                {
                    entry.Set(LayerKey, layerId);
                    orderedLayers.Add(layer.Id);
                }
            }
            else
            {
                fields[LayerKey] = layerId;
                Root.Set(key, BcrdtIntegrationFields.Create(fields));
                orderedLayers.Add(layer.Id);
            }
        }

        foreach (Guid id in orderedLayers)
        {
            if (Document.TryGetLayer(id, out BinkLayer? layer)) WriteOrder(layer.Elements, static element => element.Id, ElementPrefix);
        }

        if (layerOrder)
        {
            // Every layer is ordered against every other, so all of them have to be in the shared state: the
            // default layer included, if nobody has drawn into it yet.
            foreach (BinkLayer layer in Document.Layers)
            {
                if (!Root.ContainsKey(LayerPrefix + Key(layer.Id))) WriteLayer(layer);
            }
            WriteOrder(Document.Layers, static layer => layer.Id, LayerPrefix);
        }

        if (settings) WriteSettings();
        foreach (string key in metadata) WriteMetadata(key);
    }

    /// <summary>Writes a layer's properties. Returns whether the layer is new to the shared state.</summary>
    private bool WriteLayer(BinkLayer layer)
    {
        string key = LayerPrefix + Key(layer.Id);
        Dictionary<string, object?> fields = LayerFields(layer.Properties);
        if (Root.GetMap(key) is { } entry)
        {
            BcrdtIntegrationFields.Write(entry, fields, LayerProperties.Contains);
            return false;
        }
        Root.Set(key, BcrdtIntegrationFields.Create(fields));
        return true;
    }

    private void WriteOrder<T>(IReadOnlyList<T> items, Func<T, Guid> id, string prefix)
    {
        var entries = new List<BcrdtMap>(items.Count);
        var ids = new List<string>(items.Count);
        var orders = new List<double?>(items.Count);
        foreach (T item in items)
        {
            string key = Key(id(item));
            if (Root.GetMap(prefix + key) is not { } entry) continue;
            entries.Add(entry);
            ids.Add(key);
            orders.Add(BcrdtIntegrationOrder.Read(entry));
        }

        foreach ((int index, double order) in BcrdtIntegrationOrder.Assign(ids, orders)) entries[index].Set(BcrdtIntegrationOrder.Key, order);
    }

    private void WriteSettings()
    {
        BinkDocumentSettings settings = Document.Settings;
        WriteValue(SettingPrefix + "width", (double)settings.Width);
        WriteValue(SettingPrefix + "height", (double)settings.Height);
        WriteValue(SettingPrefix + "background", settings.Background.ToCss());
    }

    private void WriteMetadata(string key)
    {
        if (Document.Metadata.TryGetValue(key, out string? value)) WriteValue(MetadataPrefix + key, value);
        else if (Root.ContainsKey(MetadataPrefix + key)) Root.Delete(MetadataPrefix + key);
    }

    private void WriteValue(string key, object value)
    {
        if (Root.TryGetValue(key, out object? current) && BcrdtAnyCodec.DeepEquals(current, value)) return;
        Root.Set(key, value);
    }

    private Dictionary<string, object?> Fields(BinkElement element)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            BinkSerializer.WriteElementTo(writer, element, _options.SerializerOptions);
        }
        using JsonDocument json = JsonDocument.Parse(buffer.WrittenMemory);
        return BcrdtIntegrationFields.Split(json.RootElement, ExcludedElementProperties);
    }

    private static Dictionary<string, object?> LayerFields(BinkLayerProperties properties)
    {
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = properties.Name,
            ["visible"] = properties.IsVisible,
            ["locked"] = properties.IsLocked,
            ["opacity"] = (double)properties.Opacity,
            ["blend"] = properties.BlendMode.ToString(),
        };
        if (properties.Metadata is { Count: > 0 } metadata)
        {
            fields["metadata"] = BcrdtIntegrationFields.ToDictionary(metadata);
        }
        return fields;
    }

    // ---- Shared changes ------------------------------------------------------------------------------------------

    private void OnSharedChanged(IReadOnlyList<BcrdtEvent> events, BcrdtTransaction transaction)
    {
        if (ReferenceEquals(transaction.Origin, Origin)) return;

        var change = new SharedChange();
        foreach (BcrdtEvent crdtEvent in events)
        {
            IReadOnlyList<object> path = crdtEvent.Path;
            if (path.Count == 0)
            {
                foreach (string key in crdtEvent.Keys.Keys) change.Touch(key, null);
            }
            else if (path[0] is string key)
            {
                // A change inside an entry. Its properties are plain values, so the entry's own map is as deep as a
                // change goes; anything deeper was written by somebody else's code and is read as a whole entry.
                change.Touch(key, path.Count == 1 ? [.. crdtEvent.Keys.Keys] : null);
            }
        }

        ApplyShared(change);
    }

    private SharedChange Everything()
    {
        var change = new SharedChange { Layers = true, Settings = true };
        foreach (string key in Root.Keys) change.Touch(key, null);
        foreach (BinkElement element in Document.AllElements) change.Elements[element.Id] = null;
        foreach (string key in Document.Metadata.Keys) change.Metadata.Add(key);
        return change;
    }

    private void ApplyShared(SharedChange change)
    {
        using (Document.BeginBatch(null, BinkChangeOrigin.Remote))
        {
            var orderedLayers = new HashSet<Guid>();
            if (change.Layers) ApplyLayers(change);
            foreach ((Guid id, HashSet<string>? keys) in change.Elements) ApplyElement(id, keys, orderedLayers);
            foreach (Guid id in orderedLayers) ArrangeElements(id);
            if (change.Settings) ApplySettings();
            foreach (string key in change.Metadata) ApplyMetadata(key);
        }
    }

    private void ApplyLayers(SharedChange change)
    {
        var shared = new List<(Guid Id, BinkLayerProperties Properties)>();
        foreach (string key in Root.Keys)
        {
            if (!key.StartsWith(LayerPrefix, StringComparison.Ordinal) || !Guid.TryParse(key.AsSpan(LayerPrefix.Length), out Guid id)) continue;
            if (Root.GetMap(key) is { } entry) shared.Add((id, ReadLayer(entry)));
        }
        if (shared.Count == 0) shared.Add((DefaultLayerId, DefaultLayerProperties));

        foreach ((Guid id, BinkLayerProperties properties) in shared)
        {
            if (Document.TryGetLayer(id, out BinkLayer? layer))
            {
                if (!BcrdtAnyCodec.DeepEquals(LayerFields(layer.Properties), LayerFields(properties))) TryApply(new BinkUpdateLayerOperation(id, properties));
            }
            else if (TryApply(new BinkAddLayerOperation(id, properties)))
            {
                // Elements already drawn into a layer this replica did not have become visible with it.
                string layerId = Key(id);
                foreach ((Guid element, BcrdtMap entry) in Elements())
                {
                    if (entry.Get(LayerKey) as string == layerId) change.Elements[element] = null;
                }
            }
        }

        // Added first, so the drawing never runs out of layers on the way. A layer with local changes still to write
        // stays: it is this participant's, and the shared state has not heard of it yet.
        var sharedIds = shared.Select(static layer => layer.Id).ToHashSet();
        foreach (BinkLayer layer in Document.Layers.ToArray())
        {
            if (!sharedIds.Contains(layer.Id) && !_dirtyLayers.Contains(layer.Id)) TryApply(new BinkRemoveLayerOperation(layer.Id));
        }

        List<BinkLayer> arranged = BcrdtIntegrationOrder.Arrange(Document.Layers, layer =>
            !_dirtyLayers.Contains(layer.Id) && Root.GetMap(LayerPrefix + Key(layer.Id)) is { } entry
                ? (BcrdtIntegrationOrder.Read(entry), Key(layer.Id))
                : null);
        for (int index = 0; index < arranged.Count; index++)
        {
            if (Document.Layers[index].Id != arranged[index].Id) TryApply(new BinkReorderLayerOperation(arranged[index].Id, index));
        }
    }

    private void ApplyElement(Guid id, HashSet<string>? keys, HashSet<Guid> orderedLayers)
    {
        BcrdtMap? entry = Root.GetMap(ElementPrefix + Key(id));
        bool exists = Document.TryGetElement(id, out BinkElement? local, out BinkLayer? localLayer);

        // An element whose layer is gone - deleted while somebody drew into it - is not shown, here or anywhere.
        if (entry is null || !Guid.TryParse(entry.Get(LayerKey) as string, out Guid layerId) || !Document.TryGetLayer(layerId, out _))
        {
            if (exists) TryApply(new BinkRemoveElementOperation(id));
            return;
        }

        if (keys is null || keys.Contains(LayerKey) || keys.Contains(BcrdtIntegrationOrder.Key)) orderedLayers.Add(layerId);

        BinkElement? element = null;
        if (!exists || keys is null || keys.Any(static key => key.Length > 0 && key[0] != BcrdtIntegrationFields.ReservedPrefix))
        {
            // An element this participant is in the middle of changing keeps its own unwritten properties; only those
            // the shared change touched are taken from it.
            IReadOnlyDictionary<string, object?> fields = exists && keys is not null && _dirtyElements.Contains(id)
                ? Overlay(Fields(local!), entry, keys)
                : BcrdtIntegrationFields.Read(entry);

            element = Read(id, fields);
            if (element is null)
            {
                if (exists) TryApply(new BinkRemoveElementOperation(id));
                return;
            }
        }

        if (!exists)
        {
            TryApply(new BinkAddElementOperation(layerId, element!));
            return;
        }

        if (localLayer!.Id != layerId) TryApply(new BinkReorderElementOperation(id, layerId, null));
        if (element is not null) TryApply(new BinkReplaceElementOperation(element));
    }

    private static Dictionary<string, object?> Overlay(Dictionary<string, object?> fields, BcrdtMap entry, HashSet<string> keys)
    {
        foreach (string key in keys)
        {
            if (key.Length == 0 || key[0] == BcrdtIntegrationFields.ReservedPrefix) continue;
            if (entry.TryGetValue(key, out object? value)) fields[key] = value;
            else fields.Remove(key);
        }
        return fields;
    }

    private void ArrangeElements(Guid layerId)
    {
        if (!Document.TryGetLayer(layerId, out BinkLayer? layer)) return;

        // Elements with changes still to write keep their place: it is part of what will be written.
        List<BinkElement> arranged = BcrdtIntegrationOrder.Arrange(layer.Elements, element =>
            !_dirtyElements.Contains(element.Id) && Root.GetMap(ElementPrefix + Key(element.Id)) is { } entry
                ? (BcrdtIntegrationOrder.Read(entry), Key(element.Id))
                : null);

        // Moving each element into place from above it leaves everything below where it belongs.
        for (int index = 0; index < arranged.Count; index++)
        {
            if (layer.Elements[index].Id != arranged[index].Id) TryApply(new BinkReorderElementOperation(arranged[index].Id, layerId, index));
        }
    }

    private void ApplySettings()
    {
        BinkDocumentSettings current = Document.Settings;
        BinkDocumentSettings shared = current with
        {
            Width = ReadNumber(Root.Get(SettingPrefix + "width")) ?? current.Width,
            Height = ReadNumber(Root.Get(SettingPrefix + "height")) ?? current.Height,
            Background = Root.Get(SettingPrefix + "background") is string css && BinkColor.TryParse(css, out BinkColor color) ? color : current.Background,
        };
        if (shared != current) TryApply(new BinkUpdateSettingsOperation(shared));
    }

    private void ApplyMetadata(string key)
    {
        if (key.Length == 0) return;
        string? shared = Root.Get(MetadataPrefix + key) as string;
        Document.Metadata.TryGetValue(key, out string? current);
        if (shared != current) TryApply(new BinkSetMetadataOperation(key, shared));
    }

    private BinkElement? Read(Guid id, IEnumerable<KeyValuePair<string, object?>> fields)
    {
        byte[] json = BcrdtIntegrationFields.Compose("id", Key(id), fields);
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            BinkElement? element = BinkSerializer.ReadElementFrom(document.RootElement, _options.DeserializerOptions);
            if (element is null) RemoteChangeRejected?.Invoke($"Element {id} is of a kind this version of Bink cannot show.");
            return element?.Id == id ? element : null;
        }
        catch (Exception ex) when (ex is BinkFormatException or JsonException)
        {
            RemoteChangeRejected?.Invoke($"Element {id} could not be read: {ex.Message}");
            return null;
        }
    }

    private bool TryApply(BinkDrawingOperation operation)
    {
        try
        {
            Document.Apply(operation, BinkChangeOrigin.Remote);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            RemoteChangeRejected?.Invoke(ex.Message);
            return false;
        }
    }

    private static BinkLayerProperties ReadLayer(BcrdtMap entry)
    {
        Dictionary<string, string>? metadata = null;
        if (entry.Get("metadata") is IEnumerable<KeyValuePair<string, object?>> stored)
        {
            foreach ((string key, object? value) in stored)
            {
                if (value is string text) (metadata ??= new Dictionary<string, string>(StringComparer.Ordinal))[key] = text;
            }
        }

        return new BinkLayerProperties
        {
            Name = entry.Get("name") as string ?? DefaultLayerProperties.Name,
            IsVisible = entry.Get("visible") is not false,
            IsLocked = entry.Get("locked") is true,
            Opacity = ReadNumber(entry.Get("opacity")) is { } opacity ? Math.Clamp(opacity, 0f, 1f) : 1f,
            BlendMode = Enum.TryParse(entry.Get("blend") as string, ignoreCase: false, out BinkBlendMode blend) && Enum.IsDefined(blend) ? blend : BinkBlendMode.Normal,
            Metadata = metadata,
        };
    }

    // ---- Helpers -------------------------------------------------------------------------------------------------

    private IEnumerable<(Guid Id, BcrdtMap Entry)> Elements()
    {
        foreach (string key in Root.Keys.ToArray())
        {
            if (key.StartsWith(ElementPrefix, StringComparison.Ordinal) &&
                Guid.TryParse(key.AsSpan(ElementPrefix.Length), out Guid id) &&
                Root.GetMap(key) is { } entry)
            {
                yield return (id, entry);
            }
        }
    }

    private bool HasSharedState() => Root.Keys.Any(static key =>
        key.StartsWith(ElementPrefix, StringComparison.Ordinal) || key.StartsWith(LayerPrefix, StringComparison.Ordinal) ||
        key.StartsWith(SettingPrefix, StringComparison.Ordinal) || key.StartsWith(MetadataPrefix, StringComparison.Ordinal));

    private static bool IsEmpty(BinkDocument document) => document.ElementCount == 0 && document.Layers.Count == 1 && document.Metadata.Count == 0;

    private static string Key(Guid id) => id.ToString("D", CultureInfo.InvariantCulture);

    private static float? ReadNumber(object? value) => value switch
    {
        double number when double.IsFinite(number) => (float)number,
        float number when float.IsFinite(number) => number,
        long number => number,
        _ => null,
    };

    private static Vector2? ReadPoint(object? value)
    {
        if (value is not System.Collections.IEnumerable items || value is string) return null;
        var coordinates = new List<float>(2);
        foreach (object? item in items)
        {
            if (ReadNumber(item) is not { } coordinate) return null;
            coordinates.Add(coordinate);
        }
        return coordinates.Count == 2 ? new Vector2(coordinates[0], coordinates[1]) : null;
    }

    private BcrdtPresenceService RequirePresence() =>
        _options.Presence ?? throw new InvalidOperationException("The binding was created without a presence; set BcrdtBinkBindingOptions.Presence.");

    private void Publish()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequirePresence().SetCustomField(PresenceField, new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["cursor"] = _cursor is { } cursor ? new List<object?> { (double)cursor.X, (double)cursor.Y } : null,
            ["selection"] = _selection.Select(static id => (object?)Key(id)).ToList(),
        });
    }

    private void OnSelectionChanged(object? sender, EventArgs e) => PublishSelection(_options.Selection!.Ids);

    /// <summary>
    /// Writes what is still waiting, then stops following both sides. The drawing and the shared state are left as
    /// they are.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            Flush();
        }
        catch (ObjectDisposedException)
        {
            // The shared document went first; there is nowhere left to write to.
        }

        _disposed = true;
        _subscription.Dispose();
        Document.Changed -= OnDocumentChanged;
        if (_options.History is { } history) history.Changed -= OnHistoryChanged;
        if (_options.Selection is { } selection) selection.Changed -= OnSelectionChanged;
        RemoteChangeRejected = null;
    }

    /// <summary>What one shared transaction touched, by entry.</summary>
    private sealed class SharedChange
    {
        public bool Layers;
        public bool Settings;
        public readonly HashSet<string> Metadata = new(StringComparer.Ordinal);

        /// <summary>Each element touched, with the properties that changed, or null when the whole entry did.</summary>
        public readonly Dictionary<Guid, HashSet<string>?> Elements = [];

        public void Touch(string key, string[]? properties)
        {
            if (key.StartsWith(ElementPrefix, StringComparison.Ordinal))
            {
                if (!Guid.TryParse(key.AsSpan(ElementPrefix.Length), out Guid id)) return;
                if (properties is null) Elements[id] = null;
                else if (!Elements.TryGetValue(id, out HashSet<string>? known)) Elements[id] = new HashSet<string>(properties, StringComparer.Ordinal);
                else known?.UnionWith(properties);
            }
            else if (key.StartsWith(LayerPrefix, StringComparison.Ordinal)) Layers = true;
            else if (key.StartsWith(SettingPrefix, StringComparison.Ordinal)) Settings = true;
            else if (key.StartsWith(MetadataPrefix, StringComparison.Ordinal)) Metadata.Add(key[MetadataPrefix.Length..]);
        }
    }
}
