using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Bit.Blow.Demo.Server.Dtos;

namespace Bit.Blow.Demo.Server.Services;

/// <summary>
/// Runs a flow - nodes, edges and a viewport, in the JSON shape System.Text.Json gives a
/// <see cref="BlowSnapshot"/> - through the library itself, so "is this graph well formed, what is in
/// it, and what would a layout make of it?" is answered by the code that decides it at runtime rather
/// than by a reading of the model.
/// <para>
/// Three things do the work, and none of them is this class. System.Text.Json reads the JSON into
/// the library's own <see cref="BlowSnapshot"/>, <see cref="BlowNode"/> and <see cref="BlowEdge"/>
/// types - so a flow this tool reads is one an application can deserialize, and a property it rejects
/// is one the model does not have. A real <see cref="BlowCanvas"/> renders it (see
/// <see cref="BlowCanvasHost"/>) and raises what it finds wrong through OnError, resolving every
/// node's absolute position and size on the way, so <see cref="BlowGraphUtils.GetNodesBounds"/> and
/// <see cref="BlowLayout"/> then measure the flow the canvas would draw. What this class adds is only
/// what the canvas does not report - a parent cycle, an edge pointing at a handle that is not there -
/// and the notes that say what to do about each finding.
/// </para>
/// </summary>
public static class BlowFlowLab
{
    /// <summary>
    /// The largest flow this tool reads. The flow arrives as a tool argument, and a graph past this
    /// size is past what a question about its structure needs.
    /// </summary>
    public const int MaxInputBytes = 2 * 1024 * 1024;

    /// <summary>The most nodes and edges together that one call renders through the canvas.</summary>
    public const int MaxElements = 10_000;

    /// <summary>The most items one note lists by id before it says how many more there are.</summary>
    private const int MaxListed = 8;

    /// <summary>The layouts the tool can run: "none", and the three of <see cref="BlowLayout"/> (the layered one in each direction).</summary>
    public static readonly string[] Layouts =
    [
        "none", "layered", "layered-left-to-right", "layered-bottom-to-top", "layered-right-to-left", "grid", "circle"
    ];

    /// <summary>The node types the built-in renderer draws; any other one needs a NodeTemplate.</summary>
    private static readonly HashSet<string> _builtInTypes = new(StringComparer.Ordinal) { "default", "input", "output", "group" };

    /// <summary>
    /// The properties System.Text.Json writes for a node but never reads back: they have no public
    /// setter, because the canvas maintains them. A saved flow carrying them is the sign of a
    /// <c>JsonSerializer.Serialize(canvas.ToObject())</c> round trip.
    /// </summary>
    private static readonly string[] _writeOnlyNodeProperties = ["measured", "handles", "absolutePosition", "effectiveSize", "dragging", "resizing"];

    /// <summary>
    /// How the JSON is read: the casing either way (PascalCase is what JsonSerializer writes by
    /// default, camelCase what ASP.NET writes), enums as names or numbers, and a null where the
    /// model's annotations say there is never one - a node whose "id" is null - refused, as an
    /// application that turned the same check on would.
    /// </summary>
    private static readonly JsonSerializerOptions _read = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 64,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonDocumentOptions _rawOptions = new()
    {
        MaxDepth = 64,
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// How the sample is written: camelCase and enum names, and without the members a reader never
    /// takes back (see <see cref="_writeOnlyNodeProperties"/>), so the sample is what goes in.
    /// </summary>
    private static readonly JsonSerializerOptions _write = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // The same text on every operating system, rather than one with the server's own line endings.
        NewLine = "\n",
        IgnoreReadOnlyProperties = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Lazy<string> _sample = new(BuildSample);

    /// <summary>A small flow built with the library's types and written by System.Text.Json.</summary>
    public static string Sample => _sample.Value;

    public static async Task<BlowFlowReportDto> InspectAsync(string? json, string? layout)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new BlowFlowReportDto
            {
                Valid = false,
                InputBytes = 0,
                Sample = Sample,
                Notes =
                [
                    "No flow was passed, so nothing was inspected. Sample is a small flow - a sub-flow included - built from BlowNode and " +
                    "BlowEdge and written by System.Text.Json with only the properties that differ from a new instance; pass it, or the JSON " +
                    "of your own BlowCanvas.ToObject(), as json to see the report. Both camelCase and PascalCase names, and enums as names " +
                    "or numbers, are read."
                ]
            };
        }

        var inputBytes = Encoding.UTF8.GetByteCount(json);

        if (inputBytes > MaxInputBytes)
        {
            return Invalid(inputBytes, $"The flow is {inputBytes} bytes; this tool reads flows of up to {MaxInputBytes} bytes.");
        }

        // ---- Read it the way an application would ----------------------------

        JsonDocument raw;

        try
        {
            raw = JsonDocument.Parse(json, _rawOptions);
        }
        catch (JsonException exception)
        {
            return Invalid(inputBytes, exception.Message);
        }

        using (raw)
        {
            if (raw.RootElement.ValueKind != JsonValueKind.Object || TryGet(raw.RootElement, "nodes", out var rawNodes) is false || rawNodes.ValueKind != JsonValueKind.Array)
            {
                return Invalid(inputBytes, "A flow is a JSON object with a \"nodes\" array, and usually an \"edges\" array and a \"viewport\" - the shape " +
                                           "System.Text.Json gives a BlowSnapshot (BlowCanvas.ToObject()). Call InspectBlowFlow with no json for a sample.");
            }

            BlowSnapshot? snapshot;

            try
            {
                snapshot = JsonSerializer.Deserialize<BlowSnapshot>(json, _read);
            }
            catch (JsonException exception)
            {
                return Invalid(inputBytes, exception.Message, "This is System.Text.Json's own message for reading the input as a BlowSnapshot - " +
                                                              "what JsonSerializer.Deserialize<BlowSnapshot> tells an application for the same text.");
            }

            if (snapshot is null) return Invalid(inputBytes, "The JSON is null, not a flow.");

            var notes = new List<string>();

            // A null in either array is not a node or an edge, and the canvas would throw on it; it is
            // dropped here and said so, rather than allowed to end the inspection.
            var nodes = snapshot.Nodes?.Where(node => node is not null).ToList() ?? [];
            var edges = snapshot.Edges?.Where(edge => edge is not null).ToList() ?? [];

            var droppedNodes = (snapshot.Nodes?.Count ?? 0) - nodes.Count;
            var droppedEdges = (snapshot.Edges?.Count ?? 0) - edges.Count;

            if (droppedNodes + droppedEdges > 0)
            {
                notes.Add($"{droppedNodes} null node(s) and {droppedEdges} null edge(s) were left out: BlowCanvas expects every entry of Nodes and Edges to be an object.");
            }

            if (nodes.Count + edges.Count > MaxElements)
            {
                return Invalid(inputBytes, $"The flow has {nodes.Count} nodes and {edges.Count} edges; this tool renders up to {MaxElements} of them together.");
            }

            // Said after the findings rather than before them: how the input was read matters less than what is wrong with it.
            var readingNotes = new List<string>();
            var measurement = ReadMeasurement(rawNodes, nodes, readingNotes);

            // ---- Let the canvas check it ----------------------------------------

            var rendered = await BlowCanvasHost.RenderAsync(nodes, edges, measurement);

            foreach (var exception in rendered.Exceptions.Take(MaxListed))
            {
                notes.Add($"BlowCanvas threw while rendering this flow - {exception.GetType().Name}: {exception.Message} That is a bug in the library, not in the flow.");
            }

            // The canvas's own rule for a repeated id: the first node with it is the one that counts.
            var index = new Dictionary<string, BlowNode>(StringComparer.Ordinal);
            foreach (var node in nodes) index.TryAdd(node.Id, node);

            var hasViewport = TryGet(raw.RootElement, "viewport", out _);

            var report = new BlowFlowReportDto
            {
                Valid = true,
                InputBytes = inputBytes,
                Nodes = nodes.Count,
                Edges = edges.Count,
                HiddenNodes = nodes.Count(node => node.Hidden),
                HiddenEdges = edges.Count(edge => IsDrawn(edge, index) is false),
                NodeTypes = CountBy(nodes.Select(node => node.Type)),
                EdgeTypes = CountBy(edges.Select(edge => edge.Type.ToString())),
                Bounds = Bounds(nodes, index),
                Viewport = hasViewport ? new BlowViewportDto { X = Round(snapshot.Viewport.X), Y = Round(snapshot.Viewport.Y), Zoom = Round(snapshot.Viewport.Zoom) } : null,
                Problems = [.. rendered.Errors.Select(error => new BlowFlowProblemDto { Code = error.Code, Message = error.Message })],
                Notes = []
            };

            // What the canvas raised comes first, then what it does not report itself.
            Explain(rendered.Errors, notes);

            var (subFlows, maxDepth) = SubFlows(nodes, index, notes);

            report = report with { SubFlows = subFlows, MaxDepth = maxDepth };

            CheckNodes(nodes, edges, notes);
            CheckEdges(edges, index, measurement, notes);

            if (hasViewport) CheckViewport(snapshot.Viewport, notes);

            notes.AddRange(readingNotes);

            // ---- Lay it out, when asked ---------------------------------------------

            var algorithm = (layout ?? "none").Trim().ToLowerInvariant();

            if (Layouts.Contains(algorithm, StringComparer.Ordinal) is false)
            {
                notes.Add($"'{layout}' is not a layout this tool runs, so none ran. Use one of: {string.Join(", ", Layouts)}.");
            }
            else if (algorithm != "none")
            {
                report = report with { Layout = await LayOutAsync(nodes, edges, index, algorithm, measurement, notes) };
            }

            return report with { Notes = [.. notes] };
        }
    }

    private static BlowFlowReportDto Invalid(int inputBytes, string error, string? note = null) => new()
    {
        Valid = false,
        Error = error,
        InputBytes = inputBytes,
        Notes = note is null ? [] : [note]
    };

    // ---- The measurement ------------------------------------------------------------

    /// <summary>
    /// What the browser would have measured, as far as the flow says: the "measured" size and the
    /// "handles" a saved node carries. Neither survives JsonSerializer - the canvas maintains both,
    /// so neither has a public setter - which is why they are read out of the raw JSON and handed to
    /// the canvas the way its DOM measurement would hand them over.
    /// <para>
    /// Handles are read in either shape: the object JsonSerializer writes for
    /// <see cref="BlowNode.Handles"/> (keyed "Source:id", holding a <see cref="BlowHandleBounds"/>), or
    /// an array of entries such as <c>{ "id": "a", "type": "source", "position": "right" }</c> - which,
    /// unlike the object, can name the same handle twice, exactly as two <c>&lt;BlowHandle&gt;</c>
    /// elements of one node can. An entry without offsets is anchored at the middle of its side.
    /// </para>
    /// </summary>
    private static BlowMeasuredNode[] ReadMeasurement(JsonElement rawNodes, List<BlowNode> nodes, List<string> notes)
    {
        var measured = new List<BlowMeasuredNode>();
        var writeOnly = new HashSet<string>(StringComparer.Ordinal);
        var skipped = 0;

        var position = 0;

        foreach (var rawNode in rawNodes.EnumerateArray())
        {
            if (rawNode.ValueKind != JsonValueKind.Object) continue;

            // The raw array and the deserialized list are in the same order, nulls aside.
            var node = nodes[position++];

            foreach (var name in _writeOnlyNodeProperties)
            {
                if (TryGet(rawNode, name, out _)) writeOnly.Add(name);
            }

            var width = 0d;
            var height = 0d;

            if (TryGet(rawNode, "measured", out var size) && size.ValueKind == JsonValueKind.Object)
            {
                width = Number(size, "width") ?? 0;
                height = Number(size, "height") ?? 0;
            }

            var handles = new List<BlowMeasuredHandle>();

            if (TryGet(rawNode, "handles", out var rawHandles))
            {
                var sizeForAnchors = new BlowDimensions(
                    node.Width ?? (width > 0 ? width : BlowNode.DefaultWidth),
                    node.Height ?? (height > 0 ? height : BlowNode.DefaultHeight));

                IEnumerable<(JsonElement Value, string? Key)> entries = rawHandles.ValueKind switch
                {
                    JsonValueKind.Object => rawHandles.EnumerateObject().Select(property => (property.Value, (string?)property.Name)),
                    JsonValueKind.Array => rawHandles.EnumerateArray().Select(item => (item, (string?)null)),
                    _ => []
                };

                foreach (var (value, key) in entries)
                {
                    if (ReadHandle(value, key, node, sizeForAnchors) is { } handle) handles.Add(handle);
                    else skipped++;
                }
            }

            if (width > 0 || height > 0 || handles.Count > 0) measured.Add(new BlowMeasuredNode(node.Id, width, height, [.. handles]));
        }

        if (writeOnly.Count > 0)
        {
            notes.Add($"The JSON carries {string.Join(", ", writeOnly.Order(StringComparer.Ordinal))} - members JsonSerializer writes for a BlowNode and never reads " +
                      "back, since the canvas maintains them and they have no public setter. A flow restored with JsonSerializer.Deserialize therefore " +
                      "starts unmeasured, and the canvas measures it again on its first render. This tool handed measured and handles to the canvas " +
                      "as its DOM measurement, the way a browser would.");
        }

        if (skipped > 0)
        {
            notes.Add($"{skipped} handle entr(ies) were skipped: a handle needs a type, \"source\" or \"target\" (or the \"Source:id\" key JsonSerializer writes for BlowNode.Handles).");
        }

        return [.. measured];
    }

    private static BlowMeasuredHandle? ReadHandle(JsonElement value, string? key, BlowNode node, BlowDimensions size)
    {
        if (value.ValueKind != JsonValueKind.Object) return null;

        // The dictionary key JsonSerializer writes is BlowHandleBounds.Key: "Source:" or "Target:out-1".
        string? keyType = null;
        string? keyId = null;

        if (key is not null && key.IndexOf(':', StringComparison.Ordinal) is var colon and > 0)
        {
            keyType = key[..colon];
            keyId = key[(colon + 1)..];
        }

        var type = ReadEnum(value, "type", keyType, out BlowHandleType parsedType) ? parsedType : (BlowHandleType?)null;
        if (type is null) return null;

        var id = Text(value, "handleId") ?? Text(value, "id") ?? keyId ?? string.Empty;

        var side = ReadEnum(value, "position", null, out BlowPosition parsedSide)
            ? parsedSide
            : type == BlowHandleType.Source ? node.SourcePosition : node.TargetPosition;

        var (x, y) = side switch
        {
            BlowPosition.Top => (size.Width / 2, 0d),
            BlowPosition.Bottom => (size.Width / 2, size.Height),
            BlowPosition.Left => (0d, size.Height / 2),
            _ => (size.Width, size.Height / 2)
        };

        var connectableEnd = TryGet(value, "connectableEnd", out var flag) && flag.ValueKind == JsonValueKind.False ? false : (bool?)null;

        // Spelled the way the browser layer reports a handle, since that is the report this stands in for.
        return new BlowMeasuredHandle(id, type == BlowHandleType.Target ? "target" : "source", side.ToString().ToLowerInvariant(),
            Number(value, "offsetX") ?? x, Number(value, "offsetY") ?? y, connectableEnd);
    }

    // ---- What the numbers say ---------------------------------------------------------

    /// <summary>One note per kind of problem the canvas raised, saying what it does about it and what to change.</summary>
    private static void Explain(IReadOnlyList<BlowError> errors, List<string> notes)
    {
        foreach (var group in errors.GroupBy(error => error.Code, StringComparer.Ordinal))
        {
            var count = group.Count();

            notes.Add(group.Key switch
            {
                BlowErrorCode.DuplicateNodeId =>
                    $"{count} node id(s) are used more than once. The canvas keeps the first node with each id: only that one is drawn, and every " +
                    "edge naming the id attaches to it. Give every node a unique Id.",
                BlowErrorCode.DuplicateEdgeId =>
                    $"{count} edge id(s) are used more than once. Only the first edge with each id is drawn. Give every edge a unique Id - " +
                    "BlowGraphUtils.BuildEdgeId gives the deterministic one the canvas uses for an edge the user draws.",
                BlowErrorCode.MissingEdgeEndpoint =>
                    $"{count} edge(s) name a source or target that is not in the node list, and are drawn as nothing. Remove them, or add the node - " +
                    "deleting a node through the canvas (DeleteElementsAsync, the Delete key) removes its edges with it; removing it from the list yourself does not.",
                BlowErrorCode.MissingParentNode =>
                    $"{count} node(s) name a ParentId that is not in the node list, so the canvas lays each of them out as a root: its Position, " +
                    "meant relative to the parent, is read as absolute. Add the parent, or clear ParentId.",
                BlowErrorCode.DuplicateHandleId =>
                    $"{count} handle id(s) are repeated within a node - two handles of the same type with the same id, or both without one - so an edge " +
                    "naming that handle cannot say which it means and every such edge lands on one of them. Give each BlowHandle of a node its own Id, " +
                    "and set SourceHandle/TargetHandle on the edges.",
                _ => $"The canvas raised '{group.Key}' {count} time(s): {group.First().Message}"
            });
        }
    }

    private static void CheckNodes(List<BlowNode> nodes, List<BlowEdge> edges, List<string> notes)
    {
        if (nodes.Count == 0)
        {
            notes.Add(edges.Count == 0 ? "The flow has no nodes and no edges." : "The flow has no nodes, so none of its edges has anything to run between.");
            return;
        }

        if (nodes.All(node => node.Hidden)) notes.Add("Every node is hidden, so the canvas draws nothing, and FitView has nothing to fit.");

        var custom = nodes.Select(node => node.Type).Where(type => _builtInTypes.Contains(type) is false).Distinct(StringComparer.Ordinal).ToArray();

        if (custom.Length > 0)
        {
            notes.Add($"Node type(s) {List(custom)} are not built in (the built-in ones are default, input, output and group). A canvas draws them " +
                      "through its NodeTemplate, which then draws EVERY node, built-in types included - switch on node.Type inside it and hand " +
                      "the rest to <BlowDefaultNode Node=\"node\" />. Without a NodeTemplate they are drawn as a default node.");
        }

        var unanchored = nodes.Where(node => node.ParentId is null && (node.ExtentParent || node.ExpandParent)).Select(node => node.Id).ToArray();

        if (unanchored.Length > 0)
        {
            notes.Add($"Node(s) {List(unanchored)} set ExtentParent or ExpandParent without a ParentId, so neither does anything.");
        }

        if (nodes.Any(node => node.Data is not null) || edges.Any(edge => edge.Data is not null))
        {
            notes.Add("Data comes back from JSON as a JsonElement, not as the type that was saved: a NodeTemplate that tests `node.Data is MyData` " +
                      "silently falls through to its other branch after a reload. Convert it once after loading (JsonElement.Deserialize<MyData>()).");
        }
    }

    private static void CheckEdges(List<BlowEdge> edges, Dictionary<string, BlowNode> index, BlowMeasuredNode[] measurement, List<string> notes)
    {
        var known = measurement.Where(node => node.Handles.Length > 0)
                               .GroupBy(node => node.Id, StringComparer.Ordinal)
                               .ToDictionary(group => group.Key, group => group.First().Handles, StringComparer.Ordinal);

        var missingHandles = new List<string>();
        var builtInMismatch = new List<string>();
        var namedOnBuiltIn = new List<string>();

        foreach (var edge in edges.Where(edge => edge.Hidden is false))
        {
            if (index.TryGetValue(edge.Source, out var source)) Check(edge, source, BlowHandleType.Source, edge.SourceHandle);
            if (index.TryGetValue(edge.Target, out var target)) Check(edge, target, BlowHandleType.Target, edge.TargetHandle);
        }

        void Check(BlowEdge edge, BlowNode node, BlowHandleType type, string? handleId)
        {
            var side = type == BlowHandleType.Source ? "source" : "target";

            if (known.TryGetValue(node.Id, out var handles))
            {
                // The canvas looks a handle up by type and id, an empty id and a missing one being the same handle.
                var id = handleId ?? string.Empty;

                if (handles.Any(handle => handle.Type == side && handle.HandleId == id) is false)
                {
                    missingHandles.Add($"'{edge.Id}' ({side} handle {(id.Length == 0 ? "with no id" : $"'{id}'")} on '{node.Id}')");
                }

                return;
            }

            if (_builtInTypes.Contains(node.Type) is false) return;

            if ((type == BlowHandleType.Target && node.Type == "input") || (type == BlowHandleType.Source && node.Type == "output") || node.Type == "group")
            {
                builtInMismatch.Add($"'{edge.Id}' ({side} '{node.Id}', {(node.Type == "input" ? "an" : "a")} {node.Type} node)");
            }
            else if (string.IsNullOrEmpty(handleId) is false)
            {
                namedOnBuiltIn.Add($"'{edge.Id}' ({side} handle '{handleId}' on '{node.Id}')");
            }
        }

        if (missingHandles.Count > 0)
        {
            notes.Add($"Edge(s) {List(missingHandles)} name a handle the node does not have, among the handles the flow carries for it. The canvas does " +
                      "not report this: it anchors the edge at the middle of the node's SourcePosition/TargetPosition side instead. Match the edge's " +
                      "SourceHandle/TargetHandle to a BlowHandle's Id - or leave it null for the node's one handle without an id.");
        }

        if (builtInMismatch.Count > 0)
        {
            notes.Add($"Edge(s) {List(builtInMismatch)} end on a side the built-in renderer gives no handle: an input node has no target handle, an " +
                      "output node no source handle, and a group node none at all. Such an edge is anchored at the middle of the node's side, and a " +
                      "user could never have drawn it. With a NodeTemplate that renders those handles, this does not apply.");
        }

        if (namedOnBuiltIn.Count > 0)
        {
            notes.Add($"Edge(s) {List(namedOnBuiltIn)} name a handle id on a built-in node, whose handles have no id, so the canvas anchors them at " +
                      "the middle of the node's side. Clear SourceHandle/TargetHandle, or draw the node with a NodeTemplate whose BlowHandle has that Id.");
        }

        var duplicates = edges.GroupBy(edge => (edge.Source, edge.Target, Handle(edge.SourceHandle), Handle(edge.TargetHandle)))
                              .Where(group => group.Count() > 1)
                              .Select(group => $"{group.Key.Source} -> {group.Key.Target}")
                              .ToArray();

        if (duplicates.Length > 0)
        {
            notes.Add($"Connection(s) {List(duplicates)} are made by more than one edge, which the canvas draws on top of each other. " +
                      "PreventDuplicateEdges (on by default) only stops the user drawing another; it does not remove the ones in the list.");
        }

        static string Handle(string? id) => id ?? string.Empty;
    }

    private static void CheckViewport(BlowViewport viewport, List<string> notes)
    {
        // The limits of a canvas left at its defaults, read off one rather than written down here.
        var canvas = new BlowCanvas();

        if (viewport.Zoom < canvas.MinZoom || viewport.Zoom > canvas.MaxZoom)
        {
            notes.Add(FormattableString.Invariant($"The viewport's zoom {viewport.Zoom} is outside the canvas's default MinZoom..MaxZoom ({canvas.MinZoom}..{canvas.MaxZoom}); ") +
                      "RestoreAsync and SetViewport clamp it into that range, so the flow comes back at a different zoom unless the canvas's limits are widened.");
        }
    }

    /// <summary>
    /// The sub-flow structure: every node that is somebody's parent, with its children, and how deep
    /// the nesting goes. Walking the chains here is also where a parent cycle shows up - which the
    /// canvas does not report: it walks a chain at most fifty parents deep and positions the node at
    /// wherever that leaves it.
    /// </summary>
    private static (BlowSubFlowDto[] SubFlows, int MaxDepth) SubFlows(List<BlowNode> nodes, Dictionary<string, BlowNode> index, List<string> notes)
    {
        var depth = new Dictionary<string, int>(StringComparer.Ordinal);
        var cycles = new List<string>();
        var ownParent = new List<string>();

        foreach (var node in index.Values)
        {
            if (node.ParentId == node.Id)
            {
                ownParent.Add(node.Id);
                depth[node.Id] = 0;
                continue;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal) { node.Id };
            var levels = 0;
            var current = node;

            while (current.ParentId is { } parentId && parentId != current.Id && index.TryGetValue(parentId, out var parent))
            {
                if (seen.Add(parentId) is false)
                {
                    cycles.Add(node.Id);
                    break;
                }

                levels++;
                current = parent;
            }

            depth[node.Id] = levels;
        }

        if (cycles.Count > 0)
        {
            notes.Add($"Node(s) {List(cycles)} sit in a parent cycle - following ParentId leads back to where it started. The canvas does not report " +
                      "this: it adds up the relative positions round the loop until it gives up, fifty parents later, and the nodes land far from where " +
                      "they were meant to be. Break the cycle by clearing one ParentId.");
        }

        if (ownParent.Count > 0)
        {
            notes.Add($"Node(s) {List(ownParent)} name themselves as their own parent. The canvas does not report this and treats each as a root node. Clear ParentId.");
        }

        var subFlows = index.Values
            .Where(node => node.ParentId is { } parentId && parentId != node.Id && index.ContainsKey(parentId))
            .GroupBy(node => node.ParentId!, StringComparer.Ordinal)
            .Select(group => new BlowSubFlowDto
            {
                ParentId = group.Key,
                ParentType = index[group.Key].Type,
                Depth = depth.GetValueOrDefault(group.Key),
                Children = [.. group.Select(child => child.Id)]
            })
            .OrderBy(subFlow => subFlow.Depth)
            .ThenBy(subFlow => subFlow.ParentId, StringComparer.Ordinal)
            .ToArray();

        return (subFlows, depth.Count == 0 ? 0 : depth.Values.Max());
    }

    // ---- Layout ------------------------------------------------------------------------

    /// <summary>
    /// Runs one of the library's layouts the way its documentation says to: positions are
    /// parent-relative, so the root nodes are one set of siblings and each parent's children
    /// another, and each set is laid out on its own. Hidden nodes are left where they are - the
    /// layouts place whatever they are given, and a hidden node is not part of the picture.
    /// </summary>
    private static async Task<BlowLayoutReportDto> LayOutAsync(
        List<BlowNode> nodes, List<BlowEdge> edges, Dictionary<string, BlowNode> index, string algorithm, BlowMeasuredNode[] measurement, List<string> notes)
    {
        var direction = algorithm switch
        {
            "layered-left-to-right" => BlowLayoutDirection.LeftToRight,
            "layered-bottom-to-top" => BlowLayoutDirection.BottomToTop,
            "layered-right-to-left" => BlowLayoutDirection.RightToLeft,
            _ => BlowLayoutDirection.TopToBottom
        };

        var layered = algorithm.StartsWith("layered", StringComparison.Ordinal);

        // Only the node an id resolves to takes part, as it is the only one the canvas draws.
        var siblings = nodes.Where(node => node.Hidden is false && ReferenceEquals(index[node.Id], node))
                            .GroupBy(node => ParentOf(node, index))
                            .ToArray();

        foreach (var set in siblings)
        {
            var group = set.ToList();

            if (layered) BlowLayout.Layered(group, edges, new BlowLayeredLayoutOptions { Direction = direction });
            else if (algorithm == "grid") BlowLayout.Grid(group, new BlowGridLayoutOptions());
            else BlowLayout.Circle(group, new BlowCircleLayoutOptions());
        }

        // The layouts write Position and nothing else; the canvas resolves what that means in absolute
        // terms, so it renders the laid-out flow once more before the bounds are read.
        await BlowCanvasHost.RenderAsync(nodes, edges, measurement);

        // A parent does not grow to fit what a layout did to its children, and a layout does not know the
        // parent is there: children spread wider than the box are drawn hanging out of it.
        var overflowing = siblings.Where(set => set.Key is not null)
                                  .Where(set => Encloses(index[set.Key!].GetRect(), set.Select(child => child.GetRect())) is false)
                                  .Select(set => set.Key!)
                                  .ToArray();

        if (overflowing.Length > 0)
        {
            notes.Add($"The layout spread the children of {List(overflowing)} past the parent's box, which does not grow on its own. Give the parent " +
                      "a Width and Height that fit them, or lay its children out with a smaller gap or another algorithm.");
        }

        var positions = siblings.SelectMany(set => set.Select(node => new BlowNodePositionDto
        {
            Id = node.Id,
            ParentId = set.Key,
            X = Round(node.Position.X),
            Y = Round(node.Position.Y),
            SourcePosition = layered ? node.SourcePosition.ToString() : null,
            TargetPosition = layered ? node.TargetPosition.ToString() : null
        })).ToArray();

        var call = layered
            ? direction == BlowLayoutDirection.TopToBottom
                ? "BlowLayout.Layered(siblings, edges);"
                : $"BlowLayout.Layered(siblings, edges, new BlowLayeredLayoutOptions {{ Direction = BlowLayoutDirection.{direction} }});"
            : algorithm == "grid" ? "BlowLayout.Grid(siblings);" : "BlowLayout.Circle(siblings);";

        var code = $"""
            // Once for the root nodes, and once for each parent's children - positions are parent-relative.
            {call}
            {(layered ? "canvas.UpdateNodeInternals(); // the layout turned the handles to face the flow\n" : string.Empty)}canvas.FitView();
            """;

        notes.Add($"The {algorithm} layout ran with the library's default options over {siblings.Length} set(s) of siblings - the root nodes, then each " +
                  "parent's children, in the parent's own space from its top-left corner. " +
                  (layered ? "It also turned every node's SourcePosition and TargetPosition to face the flow (AlignHandlesToDirection), which only the " +
                             "built-in node renderer reads. " : string.Empty) +
                  "Sizes came from each node's Width/Height, else its measured size, else the 150 x 40 fallback - lay a live flow out from " +
                  "OnNodesInitialized, once every node has been measured. Hidden nodes were left where they are.");

        return new BlowLayoutReportDto
        {
            Algorithm = algorithm,
            Code = code,
            Groups = siblings.Length,
            Positions = positions,
            Bounds = Bounds(nodes, index)
        };
    }

    private static bool Encloses(BlowRect outer, IEnumerable<BlowRect> inner)
        => inner.All(rect => rect.Left >= outer.Left && rect.Top >= outer.Top && rect.Right <= outer.Right && rect.Bottom <= outer.Bottom);

    /// <summary>The parent a node is laid out inside: one that exists and is not itself, else none (a root).</summary>
    private static string? ParentOf(BlowNode node, Dictionary<string, BlowNode> index)
        => node.ParentId is { } parentId && parentId != node.Id && index.ContainsKey(parentId) ? parentId : null;

    // ---- Helpers -----------------------------------------------------------------------

    /// <summary>
    /// Whether the canvas draws an edge: not when it is hidden, and not when a node it runs to is -
    /// an edge with nothing at one end would hang in mid-air.
    /// </summary>
    private static bool IsDrawn(BlowEdge edge, Dictionary<string, BlowNode> index)
    {
        if (edge.Hidden) return false;
        if (index.TryGetValue(edge.Source, out var source) && source.Hidden) return false;
        if (index.TryGetValue(edge.Target, out var target) && target.Hidden) return false;

        return true;
    }

    /// <summary>The bounds of what the canvas draws, by the library's own measure of them.</summary>
    private static BlowRectDto? Bounds(List<BlowNode> nodes, Dictionary<string, BlowNode> index)
    {
        var visible = nodes.Where(node => node.Hidden is false && ReferenceEquals(index[node.Id], node)).ToArray();
        if (visible.Length == 0) return null;

        var bounds = BlowGraphUtils.GetNodesBounds(visible);

        return new BlowRectDto { X = Round(bounds.X), Y = Round(bounds.Y), Width = Round(bounds.Width), Height = Round(bounds.Height) };
    }

    private static BlowCountDto[] CountBy(IEnumerable<string> names)
    {
        return [.. names.GroupBy(name => name, StringComparer.Ordinal)
                        .Select(group => new BlowCountDto { Name = group.Key, Count = group.Count() })
                        .OrderByDescending(count => count.Count)
                        .ThenBy(count => count.Name, StringComparer.Ordinal)];
    }

    private static string List(IReadOnlyCollection<string> items)
    {
        var shown = string.Join(", ", items.Take(MaxListed).Select(item => item.StartsWith('\'') ? item : $"'{item}'"));

        return items.Count > MaxListed ? $"{shown} and {items.Count - MaxListed} more" : shown;
    }

    private static double Round(double value) => Math.Round(value, 2);

    /// <summary>A property by name, ignoring case - the input may come from either naming convention.</summary>
    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static double? Number(JsonElement element, string name)
        => TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) ? number : null;

    private static string? Text(JsonElement element, string name)
        => TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>An enum written as its name (any case) or its number, as JsonStringEnumConverter reads it; or the fallback text.</summary>
    private static bool ReadEnum<TEnum>(JsonElement element, string name, string? fallback, out TEnum result) where TEnum : struct, Enum
    {
        if (TryGet(element, name, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && Enum.IsDefined(typeof(TEnum), number))
            {
                result = (TEnum)Enum.ToObject(typeof(TEnum), number);
                return true;
            }

            if (value.ValueKind == JsonValueKind.String) fallback = value.GetString();
        }

        result = default;

        return fallback is not null && Enum.TryParse(fallback, ignoreCase: true, out result) && Enum.IsDefined(result);
    }

    // ---- The sample --------------------------------------------------------------------

    /// <summary>
    /// The sample: an input, a step, a group with two children that cannot leave it, and an output -
    /// one of each thing a typical flow holds, sub-flow included - written with only the properties
    /// that differ from a new BlowNode or BlowEdge, so the format can be read rather than skimmed. It is
    /// built once per process, so every call answers with the same text.
    /// </summary>
    private static string BuildSample()
    {
        var snapshot = new BlowSnapshot(
            [
                new BlowNode { Id = "start", Type = "input", Label = "Start", Position = new(0, 0) },
                new BlowNode { Id = "review", Label = "Review", Position = new(0, 110) },
                new BlowNode { Id = "checks", Type = "group", Label = "Checks", Position = new(220, 80), Width = 200, Height = 150 },
                new BlowNode { Id = "lint", Label = "Lint", ParentId = "checks", ExtentParent = true, Position = new(25, 35) },
                new BlowNode { Id = "test", Label = "Test", ParentId = "checks", ExtentParent = true, Position = new(25, 95) },
                new BlowNode { Id = "ship", Type = "output", Label = "Ship", Position = new(0, 300) },
            ],
            [
                new BlowEdge { Id = "start-review", Source = "start", Target = "review", Animated = true },
                new BlowEdge { Id = "review-lint", Source = "review", Target = "lint", Type = BlowEdgeType.SmoothStep, Label = "on push" },
                new BlowEdge { Id = "lint-test", Source = "lint", Target = "test" },
                new BlowEdge { Id = "test-ship", Source = "test", Target = "ship", MarkerType = BlowMarkerType.ArrowClosed },
            ],
            BlowViewport.Identity);

        var root = JsonSerializer.SerializeToNode(snapshot, _write)!.AsObject();

        var newNode = JsonSerializer.SerializeToNode(new BlowNode { Id = string.Empty }, _write)!.AsObject();
        var newEdge = JsonSerializer.SerializeToNode(new BlowEdge { Id = string.Empty, Source = string.Empty, Target = string.Empty }, _write)!.AsObject();

        foreach (var node in root["nodes"]!.AsArray()) KeepChanged(node!.AsObject(), newNode, "id");
        foreach (var edge in root["edges"]!.AsArray()) KeepChanged(edge!.AsObject(), newEdge, "id", "source", "target");

        return root.ToJsonString(_write);
    }

    /// <summary>
    /// Drops every property that holds what a new instance holds anyway. Reading the result back
    /// gives the same object: System.Text.Json starts from a new instance and sets only what it finds.
    /// </summary>
    private static void KeepChanged(JsonObject item, JsonObject fresh, params string[] keep)
    {
        foreach (var (name, value) in item.ToArray())
        {
            if (keep.Contains(name, StringComparer.Ordinal)) continue;

            if (fresh.TryGetPropertyValue(name, out var initial) && JsonNode.DeepEquals(value, initial)) item.Remove(name);
        }
    }
}
