using System.Buffers;
using System.Text.Json;
using Bit.Bcrdt.Integrations;
using Bit.Blow;

namespace Bit.Bcrdt.Blow;

/// <summary>
/// Keeps a Blow graph - the node and edge lists a <c>BlowCanvas</c> edits - and a collaborative map in step, in both
/// directions, so several people edit one flow with bit Bcrdt as the collaboration backend.
/// </summary>
/// <remarks>
/// <para>
/// Every node and every edge is an entry of its own in <see cref="Root"/>, keyed by its id: <c>n:{id}</c> for a node
/// and <c>e:{id}</c> for an edge, each a nested <see cref="BcrdtMap"/> holding the element's properties in Blow's own
/// file format, one key per property. Nothing is kept in an array: two people adding, changing or deleting different
/// elements never touch the same entry, a deletion is by id, and concurrent changes to different properties of one
/// node - one dragging it, one renaming it, one editing its data - all survive. Two changes to the same property
/// resolve to one of them, the same one everywhere.
/// </para>
/// <para>
/// List order - what Blow draws by among peers, and what <c>BringToFrontAsync</c> and <c>SendToBackAsync</c> change -
/// is a fractional order key in each entry (<c>$order</c>, a <see cref="BcrdtOrderKey"/>), so reordering is one field
/// write per moved element. Selection, measured
/// sizes, handle anchors and drag state are each participant's own and never shared; selection travels through
/// presence instead.
/// </para>
/// <para>
/// Blow's model is plain lists of mutable objects that the canvas edits in place, and nothing in it reports a change as
/// it happens; the canvas reports each finished edit through <c>OnGraphChanged</c>. So local changes are written by
/// <see cref="Commit"/>, which compares the lists with the shared state and writes only what differs, as one
/// transaction tagged with <see cref="Origin"/>: wire <see cref="OnGraphChangedAsync"/> to the canvas and every
/// finished gesture is one transaction. Every other change to the map - a collaborator's, an undo's, your own code's -
/// is applied to the lists in place, the same objects the canvas holds, through
/// <see cref="BcrdtBlowBindingOptions.RemoteEditScope"/>. Nothing echoes: what was applied matches the shared state, so
/// committing it again writes nothing.
/// </para>
/// <para>
/// An edge is shown only while both nodes it joins are: one whose node a collaborator deleted while somebody else
/// drew it is kept in the shared state, out of every replica's lists, and comes back if the node does.
/// </para>
/// <para>
/// Like the lists it binds, the binding is not thread-safe: use both from the thread the document's updates are
/// applied on, which a document opened through <c>IBcrdtCollaborationService</c> already guarantees.
/// </para>
/// </remarks>
public sealed class BcrdtBlowBinding : IDisposable
{
    private const string NodePrefix = "n:";
    private const string EdgePrefix = "e:";

    /// <summary>The presence field the cursor and selection are published under.</summary>
    public const string PresenceField = "blow";

    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.Ordinal) { "id", "selected" };

    // Each persistent property, by the name Blow's serializer gives it, and how to take it from a node read from the
    // shared state. A change to one property is applied to that property alone, so a node being dragged here keeps
    // following the pointer when somebody else renames it.
    private static readonly Dictionary<string, Action<BlowNode, BlowNode>> NodeProperties = new(StringComparer.Ordinal)
    {
        ["type"] = static (node, shared) => node.Type = shared.Type,
        ["label"] = static (node, shared) => node.Label = shared.Label,
        ["position"] = static (node, shared) => node.Position = shared.Position,
        ["width"] = static (node, shared) => node.Width = shared.Width,
        ["height"] = static (node, shared) => node.Height = shared.Height,
        ["parentId"] = static (node, shared) => node.ParentId = shared.ParentId,
        ["origin"] = static (node, shared) => node.Origin = shared.Origin,
        ["extentParent"] = static (node, shared) => node.ExtentParent = shared.ExtentParent,
        ["extent"] = static (node, shared) => node.Extent = shared.Extent,
        ["expandParent"] = static (node, shared) => node.ExpandParent = shared.ExpandParent,
        ["sourcePosition"] = static (node, shared) => node.SourcePosition = shared.SourcePosition,
        ["targetPosition"] = static (node, shared) => node.TargetPosition = shared.TargetPosition,
        ["hidden"] = static (node, shared) => node.Hidden = shared.Hidden,
        ["collapsed"] = static (node, shared) => node.Collapsed = shared.Collapsed,
        ["draggable"] = static (node, shared) => node.Draggable = shared.Draggable,
        ["selectable"] = static (node, shared) => node.Selectable = shared.Selectable,
        ["connectable"] = static (node, shared) => node.Connectable = shared.Connectable,
        ["deletable"] = static (node, shared) => node.Deletable = shared.Deletable,
        ["focusable"] = static (node, shared) => node.Focusable = shared.Focusable,
        ["useCustomDragHandle"] = static (node, shared) => node.UseCustomDragHandle = shared.UseCustomDragHandle,
        ["ariaLabel"] = static (node, shared) => node.AriaLabel = shared.AriaLabel,
        ["class"] = static (node, shared) => node.Class = shared.Class,
        ["style"] = static (node, shared) => node.Style = shared.Style,
        ["zIndex"] = static (node, shared) => node.ZIndex = shared.ZIndex,
        ["ports"] = static (node, shared) => node.Ports = shared.Ports,
        ["data"] = static (node, shared) => node.Data = shared.Data,
    };

    private static readonly Dictionary<string, Action<BlowEdge, BlowEdge>> EdgeProperties = new(StringComparer.Ordinal)
    {
        ["source"] = static (edge, shared) => edge.Source = shared.Source,
        ["target"] = static (edge, shared) => edge.Target = shared.Target,
        ["sourceHandle"] = static (edge, shared) => edge.SourceHandle = shared.SourceHandle,
        ["targetHandle"] = static (edge, shared) => edge.TargetHandle = shared.TargetHandle,
        ["type"] = static (edge, shared) => edge.Type = shared.Type,
        ["pathOptions"] = static (edge, shared) => edge.PathOptions = shared.PathOptions,
        ["label"] = static (edge, shared) => edge.Label = shared.Label,
        ["labelStyle"] = static (edge, shared) => edge.LabelStyle = shared.LabelStyle,
        ["labelShowBg"] = static (edge, shared) => edge.LabelShowBg = shared.LabelShowBg,
        ["labelBgStyle"] = static (edge, shared) => edge.LabelBgStyle = shared.LabelBgStyle,
        ["labelBgPaddingX"] = static (edge, shared) => edge.LabelBgPaddingX = shared.LabelBgPaddingX,
        ["labelBgPaddingY"] = static (edge, shared) => edge.LabelBgPaddingY = shared.LabelBgPaddingY,
        ["labelBgBorderRadius"] = static (edge, shared) => edge.LabelBgBorderRadius = shared.LabelBgBorderRadius,
        ["animated"] = static (edge, shared) => edge.Animated = shared.Animated,
        ["hidden"] = static (edge, shared) => edge.Hidden = shared.Hidden,
        ["selectable"] = static (edge, shared) => edge.Selectable = shared.Selectable,
        ["focusable"] = static (edge, shared) => edge.Focusable = shared.Focusable,
        ["deletable"] = static (edge, shared) => edge.Deletable = shared.Deletable,
        ["reconnectable"] = static (edge, shared) => edge.Reconnectable = shared.Reconnectable,
        ["strokeWidth"] = static (edge, shared) => edge.StrokeWidth = shared.StrokeWidth,
        ["stroke"] = static (edge, shared) => edge.Stroke = shared.Stroke,
        ["markerEnd"] = static (edge, shared) => edge.MarkerEnd = shared.MarkerEnd,
        ["markerStart"] = static (edge, shared) => edge.MarkerStart = shared.MarkerStart,
        ["markerType"] = static (edge, shared) => edge.MarkerType = shared.MarkerType,
        ["markerColor"] = static (edge, shared) => edge.MarkerColor = shared.MarkerColor,
        ["markerWidth"] = static (edge, shared) => edge.MarkerWidth = shared.MarkerWidth,
        ["markerHeight"] = static (edge, shared) => edge.MarkerHeight = shared.MarkerHeight,
        ["class"] = static (edge, shared) => edge.Class = shared.Class,
        ["style"] = static (edge, shared) => edge.Style = shared.Style,
        ["interactionWidth"] = static (edge, shared) => edge.InteractionWidth = shared.InteractionWidth,
        ["zIndex"] = static (edge, shared) => edge.ZIndex = shared.ZIndex,
        ["data"] = static (edge, shared) => edge.Data = shared.Data,
    };

    private readonly IList<BlowNode> _nodes;
    private readonly IList<BlowEdge> _edges;
    private readonly BcrdtBlowBindingOptions _options;
    private readonly BlowSerializerOptions _serializerOptions;
    private readonly IDisposable _subscription;

    private BlowXYPosition? _cursor;
    private bool _disposed;

    /// <summary>Binds a graph to a collaborative map.</summary>
    /// <param name="root">
    /// The map to keep the graph in - a root map of its own, such as <c>document.Crdt.GetMap("flow")</c>, or a nested
    /// one that already exists on every replica. It must belong to a document.
    /// </param>
    /// <param name="nodes">The node list the canvas is given (<c>Nodes</c>). Changed in place, never replaced.</param>
    /// <param name="edges">The edge list the canvas is given (<c>Edges</c>). Changed in place, never replaced.</param>
    /// <param name="options">Settings, or null for the defaults.</param>
    /// <remarks>
    /// <para>
    /// When the map already holds a graph, that graph replaces whatever the lists held: the shared state is the truth,
    /// and a participant joins it. When the map is empty and the lists are not, the lists are written into the map, in
    /// one transaction, to start the shared graph from. Bind before the canvas first renders - in
    /// <c>OnInitialized</c> - so it starts with the shared graph; the binding is applied to the lists directly here,
    /// not through <see cref="BcrdtBlowBindingOptions.RemoteEditScope"/>.
    /// </para>
    /// <para>
    /// Starting a shared graph from existing content is a job for one participant. Two that each write their own graph
    /// into an empty map end up with both, merged.
    /// </para>
    /// </remarks>
    public BcrdtBlowBinding(BcrdtMap root, IList<BlowNode> nodes, IList<BlowEdge> edges, BcrdtBlowBindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        if (!root.IsAttached) throw new ArgumentException("The map must belong to a document before a graph can be bound to it.", nameof(root));

        Root = root;
        _nodes = nodes;
        _edges = edges;
        _options = options ?? new BcrdtBlowBindingOptions();
        _serializerOptions = _options.SerializerOptions ?? new BlowSerializerOptions();
        Origin = _options.Origin ?? this;

        if (HasSharedState()) ApplyToLists(Everything());
        else if (nodes.Count > 0 || edges.Count > 0) Commit();

        _subscription = root.ObserveDeep(OnSharedChanged);
    }

    /// <summary>The map the graph is kept in.</summary>
    public BcrdtMap Root { get; }

    /// <summary>The origin the binding's transactions carry.</summary>
    public object Origin { get; }

    /// <summary>
    /// Raised after a change from the shared state has been applied to the lists - for refreshing whatever shows them
    /// when no <see cref="BcrdtBlowBindingOptions.RemoteEditScope"/> does (<c>canvas.Refresh(remeasure: true)</c>).
    /// </summary>
    public event Action<BcrdtBlowRemoteChange>? RemoteChanged;

    /// <summary>
    /// Raised when a change from the shared state could not be read - it breaks Blow's import limits or is not a graph
    /// this version understands - with the reason. The elements it holds are left out of this replica's lists.
    /// </summary>
    public event Action<string>? RemoteChangeRejected;

    /// <summary>
    /// Writes what differs between the lists and the shared state, as one transaction.
    /// </summary>
    /// <returns>Whether anything was written.</returns>
    /// <remarks>
    /// <para>
    /// Only the properties that changed are written, and only for the elements that changed, so committing never
    /// reverts what a collaborator changed elsewhere meanwhile, and committing an unchanged graph writes nothing. Call
    /// it once an edit is finished - which is what <see cref="OnGraphChangedAsync"/> does for the canvas's edits - and
    /// after changing the lists or their elements from your own code.
    /// </para>
    /// <para>
    /// It reads the whole graph through Blow's serializer, so its cost is in proportion to the graph, not to the
    /// change: call it at the end of an edit, not on every pointer move.
    /// </para>
    /// </remarks>
    /// <exception cref="BlowSerializationException">An element carries data no mapping says how to save.</exception>
    public bool Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        (List<(string Id, Dictionary<string, object?> Fields)> nodes, List<(string Id, Dictionary<string, object?> Fields)> edges) = Snapshot();
        bool wrote = false;
        Root.Doc.Transact(_ => wrote = Write(nodes, edges), Origin);
        return wrote;
    }

    /// <summary>
    /// Commits a finished edit of the canvas. Pass it to the canvas's <c>OnGraphChanged</c>.
    /// </summary>
    /// <remarks>
    /// A change with <see cref="BlowChangeOrigin.Remote"/> is one this binding applied, and is not written back.
    /// Everything else - a gesture, an API call, an undo or redo of the canvas's own history - is committed, as one
    /// transaction.
    /// </remarks>
    public Task OnGraphChangedAsync(BlowGraphChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (change.Origin != BlowChangeOrigin.Remote) Commit();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a selective undo manager for this graph: it undoes this participant's commits and nobody else's.
    /// </summary>
    /// <param name="options">
    /// Settings for the manager; <see cref="Origin"/> is added to its tracked origins. Null uses the defaults.
    /// </param>
    /// <remarks>
    /// <para>
    /// The manager is scoped to <see cref="Root"/> and tracks the binding's transactions, so a step is one commit
    /// (merged with the ones that follow within <see cref="BcrdtUndoManagerOptions.CaptureTimeout"/>). Undoing writes
    /// new changes to the shared state, which the binding applies to the lists like a collaborator's, through
    /// <see cref="BcrdtBlowBindingOptions.RemoteEditScope"/>: they reach everybody, and the canvas's own history writes
    /// them into its steps rather than recording them.
    /// </para>
    /// <para>
    /// The canvas's history (<c>HistoryEnabled</c>) keeps working beside it, and its undo is committed like any other
    /// edit. The difference is in what an undo puts back. The canvas's history restores whole elements as they were,
    /// with collaborators' edits it has been told about written into its steps. This manager restores only the
    /// properties the step changed, and only if nobody has written them since. Use one or the other for a given graph,
    /// not both.
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

    /// <summary>Publishes where the local pointer is, in flow coordinates, or null when it left the canvas.</summary>
    /// <exception cref="InvalidOperationException">No <see cref="BcrdtBlowBindingOptions.Presence"/> was given.</exception>
    public void PublishCursor(BlowXYPosition? position)
    {
        _cursor = position;
        Publish();
    }

    /// <summary>
    /// Publishes which nodes and edges are selected - the ones whose <c>Selected</c> is set. Call it from the canvas's
    /// <c>OnSelectionChange</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <see cref="BcrdtBlowBindingOptions.Presence"/> was given.</exception>
    public void PublishSelection() => Publish();

    /// <summary>
    /// Everybody else in the graph, with the cursor and selection each has published. A selection lists only the
    /// elements this replica has.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <see cref="BcrdtBlowBindingOptions.Presence"/> was given.</exception>
    public IReadOnlyList<BcrdtBlowParticipant> Participants
    {
        get
        {
            BcrdtPresenceService presence = RequirePresence();
            var nodeIds = _nodes.Select(static node => node.Id).ToHashSet(StringComparer.Ordinal);
            var edgeIds = _edges.Select(static edge => edge.Id).ToHashSet(StringComparer.Ordinal);

            var result = new List<BcrdtBlowParticipant>();
            foreach (BcrdtCollaborator other in presence.Others)
            {
                BlowXYPosition? cursor = null;
                List<string> nodes = [];
                List<string> edges = [];
                if (other.Custom.GetValueOrDefault(PresenceField) is IEnumerable<KeyValuePair<string, object?>> state)
                {
                    foreach ((string key, object? value) in state)
                    {
                        // Peer-written: anything malformed is ignored rather than trusted.
                        if (key == "cursor") cursor = ReadPoint(value);
                        else if (key == "nodes") nodes = ReadIds(value, nodeIds);
                        else if (key == "edges") edges = ReadIds(value, edgeIds);
                    }
                }
                result.Add(new BcrdtBlowParticipant(other, cursor, nodes, edges));
            }
            return result;
        }
    }

    // ---- Local changes -------------------------------------------------------------------------------------------

    private (List<(string Id, Dictionary<string, object?> Fields)> Nodes, List<(string Id, Dictionary<string, object?> Fields)> Edges) Snapshot()
    {
        // Blow tolerates a repeated id; the shared state has one entry per id, the first element's.
        var document = new BlowDocument
        {
            Nodes = [.. _nodes.DistinctBy(static node => node.Id, StringComparer.Ordinal)],
            Edges = [.. _edges.DistinctBy(static edge => edge.Id, StringComparer.Ordinal)],
        };

        byte[] json = BlowSerializer.SerializeToUtf8Bytes(document, _serializerOptions);
        using JsonDocument parsed = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 256 });
        return (Split(parsed.RootElement, "nodes"), Split(parsed.RootElement, "edges"));
    }

    private static List<(string Id, Dictionary<string, object?> Fields)> Split(JsonElement document, string property)
    {
        var result = new List<(string Id, Dictionary<string, object?> Fields)>();
        if (!document.TryGetProperty(property, out JsonElement items)) return result;
        foreach (JsonElement item in items.EnumerateArray())
        {
            if (item.TryGetProperty("id", out JsonElement id) && id.GetString() is { } text)
            {
                result.Add((text, BcrdtIntegrationFields.Split(item, ExcludedProperties)));
            }
        }
        return result;
    }

    private bool Write(List<(string Id, Dictionary<string, object?> Fields)> nodes, List<(string Id, Dictionary<string, object?> Fields)> edges)
    {
        bool wrote = false;

        var localNodes = nodes.Select(static node => node.Id).ToHashSet(StringComparer.Ordinal);
        var deletedNodes = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string id, Dictionary<string, object?> fields) in nodes) wrote |= WriteEntry(NodePrefix + id, fields, NodeProperties);
        foreach (string id in SharedIds(NodePrefix))
        {
            if (localNodes.Contains(id)) continue;
            Root.Delete(NodePrefix + id);
            deletedNodes.Add(id);
            wrote = true;
        }

        var localEdges = edges.Select(static edge => edge.Id).ToHashSet(StringComparer.Ordinal);
        foreach ((string id, Dictionary<string, object?> fields) in edges) wrote |= WriteEntry(EdgePrefix + id, fields, EdgeProperties);
        foreach (string id in SharedIds(EdgePrefix))
        {
            if (localEdges.Contains(id)) continue;

            // Missing from the lists because it was deleted here, or because it is hidden: a node it joins is
            // missing everywhere. Only the first is a deletion - including an edge that went with a node deleted now.
            BcrdtMap? entry = Root.GetMap(EdgePrefix + id);
            string? source = entry?.Get("source") as string;
            string? target = entry?.Get("target") as string;
            bool joined = source is not null && target is not null && localNodes.Contains(source) && localNodes.Contains(target);
            bool orphaned = (source is not null && deletedNodes.Contains(source)) || (target is not null && deletedNodes.Contains(target));
            if (entry is not null && !joined && !orphaned) continue;

            Root.Delete(EdgePrefix + id);
            wrote = true;
        }

        wrote |= WriteOrder(nodes, NodePrefix);
        wrote |= WriteOrder(edges, EdgePrefix);
        return wrote;
    }

    private bool WriteEntry<T>(string key, Dictionary<string, object?> fields, Dictionary<string, Action<T, T>> known)
    {
        if (Root.GetMap(key) is { } entry) return BcrdtIntegrationFields.Write(entry, fields, known.ContainsKey);

        Root.Set(key, BcrdtIntegrationFields.Create(fields));
        return true;
    }

    private bool WriteOrder(List<(string Id, Dictionary<string, object?> Fields)> items, string prefix)
    {
        var entries = new List<BcrdtMap>(items.Count);
        var ids = new List<string>(items.Count);
        var orders = new List<string?>(items.Count);
        foreach ((string id, _) in items)
        {
            if (Root.GetMap(prefix + id) is not { } entry) continue;
            entries.Add(entry);
            ids.Add(id);
            orders.Add(BcrdtIntegrationOrder.Read(entry));
        }

        List<(int Index, string Order)> moves = BcrdtIntegrationOrder.Assign(ids, orders);
        foreach ((int index, string order) in moves) entries[index].Set(BcrdtIntegrationOrder.Key, order);
        return moves.Count > 0;
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
        if (change.Nodes.Count == 0 && change.Edges.Count == 0) return;

        BcrdtBlowRemoteChange? applied = null;
        if (_options.RemoteEditScope is { } scope)
        {
            bool ran = false;
            void Edit()
            {
                if (ran) return;
                ran = true;
                applied = ApplyToLists(change);
            }

            // The task is the canvas's: it finishes once the canvas has rendered and raised its own events. The edit
            // runs here whatever the scope did - even if it threw - or the lists would fall behind the shared state.
            try
            {
                _ = scope(Edit);
            }
            finally
            {
                Edit();
            }
        }
        else
        {
            applied = ApplyToLists(change);
        }

        if (applied is not null) RemoteChanged?.Invoke(applied);
    }

    private SharedChange Everything()
    {
        var change = new SharedChange();
        foreach (string key in Root.Keys) change.Touch(key, null);
        foreach (BlowNode node in _nodes) change.Nodes[node.Id] = null;
        foreach (BlowEdge edge in _edges) change.Edges[edge.Id] = null;
        return change;
    }

    private BcrdtBlowRemoteChange ApplyToLists(SharedChange change)
    {
        var touchedNodes = new HashSet<string>(StringComparer.Ordinal);
        var touchedEdges = new HashSet<string>(StringComparer.Ordinal);
        bool arrangeNodes = false, arrangeEdges = false;

        Dictionary<string, BlowNode> sharedNodes = ReadNodes(change.Nodes.Keys);
        var addedNodes = new HashSet<string>(StringComparer.Ordinal);
        var removedNodes = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string id, HashSet<string>? keys) in change.Nodes)
        {
            int index = IndexOf(_nodes, id, static node => node.Id);
            if (!sharedNodes.TryGetValue(id, out BlowNode? shared))
            {
                if (index < 0) continue;
                _nodes.RemoveAt(index);
                removedNodes.Add(id);
                touchedNodes.Add(id);
                continue;
            }

            if (keys is null || keys.Contains(BcrdtIntegrationOrder.Key)) arrangeNodes = true;
            if (index < 0)
            {
                _nodes.Add(shared);
                addedNodes.Add(id);
                touchedNodes.Add(id);
            }
            else if (Copy(_nodes[index], shared, keys, NodeProperties))
            {
                touchedNodes.Add(id);
            }
        }

        // An edge is shown while both of its nodes are: those that lost one go, those waiting for one come.
        if (removedNodes.Count > 0)
        {
            for (int index = _edges.Count - 1; index >= 0; index--)
            {
                BlowEdge edge = _edges[index];
                if (!removedNodes.Contains(edge.Source) && !removedNodes.Contains(edge.Target)) continue;
                _edges.RemoveAt(index);
                touchedEdges.Add(edge.Id);
            }
        }
        if (addedNodes.Count > 0)
        {
            foreach (string id in SharedIds(EdgePrefix))
            {
                BcrdtMap? entry = Root.GetMap(EdgePrefix + id);
                if ((entry?.Get("source") is string source && addedNodes.Contains(source)) ||
                    (entry?.Get("target") is string target && addedNodes.Contains(target)))
                {
                    change.Edges[id] = null;
                }
            }
        }

        var nodeIds = _nodes.Select(static node => node.Id).ToHashSet(StringComparer.Ordinal);
        Dictionary<string, BlowEdge> sharedEdges = ReadEdges(change.Edges.Keys);
        foreach ((string id, HashSet<string>? keys) in change.Edges)
        {
            int index = IndexOf(_edges, id, static edge => edge.Id);
            if (!sharedEdges.TryGetValue(id, out BlowEdge? shared) || !nodeIds.Contains(shared.Source) || !nodeIds.Contains(shared.Target))
            {
                if (index < 0) continue;
                _edges.RemoveAt(index);
                touchedEdges.Add(id);
                continue;
            }

            if (keys is null || keys.Contains(BcrdtIntegrationOrder.Key)) arrangeEdges = true;
            if (index < 0)
            {
                _edges.Add(shared);
                touchedEdges.Add(id);
            }
            else if (Copy(_edges[index], shared, keys, EdgeProperties))
            {
                touchedEdges.Add(id);
            }
        }

        bool reordered = false;
        if (arrangeNodes) reordered |= Arrange(_nodes, NodePrefix, static node => node.Id);
        if (arrangeEdges) reordered |= Arrange(_edges, EdgePrefix, static edge => edge.Id);
        return new BcrdtBlowRemoteChange(touchedNodes, touchedEdges, reordered);
    }

    private static bool Copy<T>(T element, T shared, HashSet<string>? keys, Dictionary<string, Action<T, T>> properties)
    {
        bool copied = false;
        foreach ((string name, Action<T, T> copy) in properties)
        {
            if (keys is not null && !keys.Contains(name)) continue;
            copy(element, shared);
            copied = true;
        }
        return copied;
    }

    private bool Arrange<T>(IList<T> items, string prefix, Func<T, string> id) where T : class
    {
        List<T> arranged = BcrdtIntegrationOrder.Arrange([.. items], item =>
            Root.GetMap(prefix + id(item)) is { } entry ? (BcrdtIntegrationOrder.Read(entry), id(item)) : null);

        bool moved = false;
        for (int index = 0; index < arranged.Count; index++)
        {
            if (ReferenceEquals(items[index], arranged[index])) continue;
            items[index] = arranged[index];
            moved = true;
        }
        return moved;
    }

    private Dictionary<string, BlowNode> ReadNodes(IEnumerable<string> ids)
    {
        var result = new Dictionary<string, BlowNode>(StringComparer.Ordinal);
        if (Read(ids, NodePrefix, "nodes") is { } document)
        {
            foreach (BlowNode node in document.Nodes) result.TryAdd(node.Id, node);
        }
        return result;
    }

    private Dictionary<string, BlowEdge> ReadEdges(IEnumerable<string> ids)
    {
        var result = new Dictionary<string, BlowEdge>(StringComparer.Ordinal);
        if (Read(ids, EdgePrefix, "edges") is { } document)
        {
            foreach (BlowEdge edge in document.Edges) result.TryAdd(edge.Id, edge);
        }
        return result;
    }

    /// <summary>
    /// Reads entries back through Blow's own reader, as one document, so they get exactly the validation, limits and
    /// data mappings a file would.
    /// </summary>
    private BlowDocument? Read(IEnumerable<string> ids, string prefix, string property)
    {
        var buffer = new ArrayBufferWriter<byte>(1024);
        int count = 0;
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("format", BlowSerializer.Format);
            writer.WriteNumber("schemaVersion", BlowSerializer.SchemaVersion);
            // Written by this version, so no migration applies to it.
            writer.WriteNumber("version", _serializerOptions.CurrentVersion);
            writer.WriteStartArray(property);
            foreach (string id in ids)
            {
                if (Root.GetMap(prefix + id) is not { } entry) continue;
                BcrdtIntegrationFields.WriteObject(writer, "id", id, BcrdtIntegrationFields.Read(entry));
                count++;
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        if (count == 0) return null;

        BlowImportResult result = BlowSerializer.Deserialize(buffer.WrittenSpan, _serializerOptions);
        foreach (BlowImportIssue issue in result.Issues)
        {
            if (issue.Severity == BlowValidationSeverity.Error) RemoteChangeRejected?.Invoke($"{issue.Path}: {issue.Message}");
        }
        return result.Document;
    }

    // ---- Helpers -------------------------------------------------------------------------------------------------

    private List<string> SharedIds(string prefix)
    {
        var result = new List<string>();
        foreach (string key in Root.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal) && key.Length > prefix.Length) result.Add(key[prefix.Length..]);
        }
        return result;
    }

    private bool HasSharedState() => Root.Keys.Any(static key =>
        key.StartsWith(NodePrefix, StringComparison.Ordinal) || key.StartsWith(EdgePrefix, StringComparison.Ordinal));

    private static int IndexOf<T>(IList<T> items, string id, Func<T, string> key)
    {
        for (int index = 0; index < items.Count; index++)
        {
            if (key(items[index]) == id) return index;
        }
        return -1;
    }

    private static BlowXYPosition? ReadPoint(object? value)
    {
        if (value is not System.Collections.IEnumerable items || value is string) return null;
        var coordinates = new List<double>(2);
        foreach (object? item in items)
        {
            double? coordinate = item switch
            {
                double number when double.IsFinite(number) => number,
                long number => number,
                _ => null,
            };
            if (coordinate is null) return null;
            coordinates.Add(coordinate.Value);
        }
        return coordinates.Count == 2 ? new BlowXYPosition(coordinates[0], coordinates[1]) : null;
    }

    private static List<string> ReadIds(object? value, HashSet<string> present)
    {
        var result = new List<string>();
        if (value is not System.Collections.IEnumerable items || value is string) return result;
        foreach (object? item in items)
        {
            if (item is string id && present.Contains(id)) result.Add(id);
        }
        return result;
    }

    private BcrdtPresenceService RequirePresence() =>
        _options.Presence ?? throw new InvalidOperationException("The binding was created without a presence; set BcrdtBlowBindingOptions.Presence.");

    private void Publish()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequirePresence().SetCustomField(PresenceField, new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["cursor"] = _cursor is { } cursor ? new List<object?> { cursor.X, cursor.Y } : null,
            ["nodes"] = _nodes.Where(static node => node.Selected).Select(static node => (object?)node.Id).ToList(),
            ["edges"] = _edges.Where(static edge => edge.Selected).Select(static edge => (object?)edge.Id).ToList(),
        });
    }

    /// <summary>Stops following both sides. The lists and the shared state are left as they are.</summary>
    /// <remarks>Nothing is committed: commit first if the lists hold changes the shared state should have.</remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscription.Dispose();
        RemoteChanged = null;
        RemoteChangeRejected = null;
    }

    /// <summary>What one shared transaction touched, by entry.</summary>
    private sealed class SharedChange
    {
        /// <summary>Each node touched, with the properties that changed, or null when the whole entry did.</summary>
        public readonly Dictionary<string, HashSet<string>?> Nodes = new(StringComparer.Ordinal);

        /// <summary>Each edge touched, with the properties that changed, or null when the whole entry did.</summary>
        public readonly Dictionary<string, HashSet<string>?> Edges = new(StringComparer.Ordinal);

        public void Touch(string key, string[]? properties)
        {
            if (key.StartsWith(NodePrefix, StringComparison.Ordinal) && key.Length > NodePrefix.Length) Touch(Nodes, key[NodePrefix.Length..], properties);
            else if (key.StartsWith(EdgePrefix, StringComparison.Ordinal) && key.Length > EdgePrefix.Length) Touch(Edges, key[EdgePrefix.Length..], properties);
        }

        private static void Touch(Dictionary<string, HashSet<string>?> entries, string id, string[]? properties)
        {
            if (properties is null) entries[id] = null;
            else if (!entries.TryGetValue(id, out HashSet<string>? known)) entries[id] = new HashSet<string>(properties, StringComparer.Ordinal);
            else known?.UnionWith(properties);
        }
    }
}
