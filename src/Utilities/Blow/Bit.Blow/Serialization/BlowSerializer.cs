using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blow;

/// <summary>
/// Saves a graph as JSON and reads it back - with a versioned, documented schema, limits that keep a hostile or
/// broken file from exhausting the page, migrations for the application's own document versions, and import problems
/// reported with their location rather than thrown.
/// </summary>
/// <remarks>
/// <para>
/// The format is stable and written deterministically: properties in a fixed order, defaults left out, numbers in
/// invariant round-trip form, so saving the same graph twice produces the same text, and a saved graph diffs well.
/// The schema is described in the README ("Saving and loading").
/// </para>
/// <para>
/// Data is data. Nothing in a document names a .NET type, and nothing read from one is instantiated except through a
/// contract the application registered (<see cref="BlowSerializerOptions.MapNodeData"/>). Data with no contract is kept
/// as a <see cref="JsonElement"/> and written back unchanged, so a document survives a round trip through an
/// application that does not understand all of it.
/// </para>
/// </remarks>
public static class BlowSerializer
{
    /// <summary>The value of the <c>format</c> property every Blow document carries.</summary>
    public const string Format = "bit-blow";

    /// <summary>The version of the document schema this library writes, and the newest it reads.</summary>
    public const int SchemaVersion = 1;

    // ---------------------------------------------------------------- writing

    /// <summary>Writes a document as a JSON string.</summary>
    /// <exception cref="BlowSerializationException">An element carries data no mapping says how to save.</exception>
    public static string Serialize(BlowDocument document, BlowSerializerOptions? options = null)
        => Encoding.UTF8.GetString(SerializeToUtf8Bytes(document, options));

    /// <summary>Writes a snapshot - what <c>BlowCanvas.ToObject()</c> returns - as a JSON string.</summary>
    public static string Serialize(BlowSnapshot snapshot, BlowSerializerOptions? options = null)
        => Serialize(BlowDocument.FromSnapshot(snapshot), options);

    /// <summary>Writes a document as UTF-8 JSON.</summary>
    public static byte[] SerializeToUtf8Bytes(BlowDocument document, BlowSerializerOptions? options = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = options?.WriteIndented ?? false }))
        {
            Write(writer, document, options ?? BlowSerializerOptions.Default);
        }
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Writes a document as UTF-8 JSON to a stream.</summary>
    public static async Task SerializeAsync(Stream stream, BlowDocument document, BlowSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = SerializeToUtf8Bytes(document, options);
        await stream.WriteAsync(bytes, cancellationToken);
    }

    private static void Write(Utf8JsonWriter w, BlowDocument document, BlowSerializerOptions o)
    {
        ArgumentNullException.ThrowIfNull(document);

        w.WriteStartObject();
        w.WriteString("format", Format);
        w.WriteNumber("schemaVersion", SchemaVersion);
        w.WriteNumber("version", o.CurrentVersion);
        if (document.Id is { } id) w.WriteString("id", id);
        if (document.Metadata is { } metadata)
        {
            w.WritePropertyName("metadata");
            metadata.WriteTo(w);
        }
        if (document.Viewport is { } viewport)
        {
            w.WriteStartObject("viewport");
            w.WriteNumber("x", viewport.X);
            w.WriteNumber("y", viewport.Y);
            w.WriteNumber("zoom", viewport.Zoom);
            w.WriteEndObject();
        }

        w.WriteStartArray("nodes");
        foreach (var node in document.Nodes) WriteNode(w, node, o);
        w.WriteEndArray();

        w.WriteStartArray("edges");
        foreach (var edge in document.Edges) WriteEdge(w, edge, o);
        w.WriteEndArray();

        w.WriteEndObject();
    }

    private static void WriteNode(Utf8JsonWriter w, BlowNode n, BlowSerializerOptions o)
    {
        w.WriteStartObject();
        w.WriteString("id", n.Id);
        if (n.Type != "default") w.WriteString("type", n.Type);
        if (n.Label is { } label) w.WriteString("label", label);
        WritePoint(w, "position", n.Position);
        if (n.Width is { } width) w.WriteNumber("width", width);
        if (n.Height is { } height) w.WriteNumber("height", height);
        if (n.ParentId is { } parent) w.WriteString("parentId", parent);
        if (n.Origin is { } origin) WritePoint(w, "origin", origin);
        if (n.ExtentParent) w.WriteBoolean("extentParent", true);
        if (n.Extent is { } extent)
        {
            w.WriteStartObject("extent");
            w.WriteNumber("x", extent.X);
            w.WriteNumber("y", extent.Y);
            w.WriteNumber("width", extent.Width);
            w.WriteNumber("height", extent.Height);
            w.WriteEndObject();
        }
        if (n.ExpandParent) w.WriteBoolean("expandParent", true);
        if (n.SourcePosition != BlowPosition.Bottom) w.WriteString("sourcePosition", Name(n.SourcePosition));
        if (n.TargetPosition != BlowPosition.Top) w.WriteString("targetPosition", Name(n.TargetPosition));
        if (n.Hidden) w.WriteBoolean("hidden", true);
        if (n.Collapsed) w.WriteBoolean("collapsed", true);
        if (!n.Draggable) w.WriteBoolean("draggable", false);
        if (!n.Selectable) w.WriteBoolean("selectable", false);
        if (!n.Connectable) w.WriteBoolean("connectable", false);
        if (!n.Deletable) w.WriteBoolean("deletable", false);
        if (!n.Focusable) w.WriteBoolean("focusable", false);
        if (n.UseCustomDragHandle) w.WriteBoolean("useCustomDragHandle", true);
        if (o.IncludeSelection && n.Selected) w.WriteBoolean("selected", true);
        if (n.AriaLabel is { } aria) w.WriteString("ariaLabel", aria);
        if (n.Class is { } cls) w.WriteString("class", cls);
        if (n.Style is { } style) w.WriteString("style", style);
        if (n.ZIndex is { } z) w.WriteNumber("zIndex", z);
        if (n.Ports is { } ports)
        {
            w.WriteStartArray("ports");
            foreach (var port in ports) WritePort(w, port, n, o);
            w.WriteEndArray();
        }
        WriteData(w, n.Data, o, () => $"Node '{n.Id}'");
        w.WriteEndObject();
    }

    private static void WritePort(Utf8JsonWriter w, BlowPort p, BlowNode node, BlowSerializerOptions o)
    {
        w.WriteStartObject();
        if (p.Id is { } id) w.WriteString("id", id);
        w.WriteString("type", p.Type == BlowHandleType.Source ? "source" : "target");
        if (p.Position is { } position) w.WriteString("position", Name(position));
        if (p.Label is { } label) w.WriteString("label", label);
        if (p.DataType is { } dataType) w.WriteString("dataType", dataType);
        if (p.MaxConnections is { } max) w.WriteNumber("maxConnections", max);
        if (p.MinConnections != 0) w.WriteNumber("minConnections", p.MinConnections);
        WriteData(w, p.Data, o, () => $"Port '{p.Id}' of node '{node.Id}'");
        w.WriteEndObject();
    }

    private static void WriteEdge(Utf8JsonWriter w, BlowEdge e, BlowSerializerOptions o)
    {
        w.WriteStartObject();
        w.WriteString("id", e.Id);
        w.WriteString("source", e.Source);
        w.WriteString("target", e.Target);
        if (e.SourceHandle is { } sh) w.WriteString("sourceHandle", sh);
        if (e.TargetHandle is { } th) w.WriteString("targetHandle", th);
        if (e.Type != BlowEdgeType.Bezier) w.WriteString("type", Name(e.Type));
        if (e.PathOptions is { } po)
        {
            w.WriteStartObject("pathOptions");
            if (po.Curvature is { } c) w.WriteNumber("curvature", c);
            if (po.BorderRadius is { } br) w.WriteNumber("borderRadius", br);
            if (po.Offset is { } off) w.WriteNumber("offset", off);
            if (po.StepPosition is { } sp) w.WriteNumber("stepPosition", sp);
            if (po.LoopRadius is { } lr) w.WriteNumber("loopRadius", lr);
            w.WriteEndObject();
        }
        if (e.Label is { } label) w.WriteString("label", label);
        if (e.LabelStyle is { } ls) w.WriteString("labelStyle", ls);
        if (!e.LabelShowBg) w.WriteBoolean("labelShowBg", false);
        if (e.LabelBgStyle is { } lbs) w.WriteString("labelBgStyle", lbs);
        if (e.LabelBgPaddingX != 4) w.WriteNumber("labelBgPaddingX", e.LabelBgPaddingX);
        if (e.LabelBgPaddingY != 2) w.WriteNumber("labelBgPaddingY", e.LabelBgPaddingY);
        if (e.LabelBgBorderRadius != 3) w.WriteNumber("labelBgBorderRadius", e.LabelBgBorderRadius);
        if (e.Animated) w.WriteBoolean("animated", true);
        if (e.Hidden) w.WriteBoolean("hidden", true);
        if (!e.Selectable) w.WriteBoolean("selectable", false);
        if (!e.Focusable) w.WriteBoolean("focusable", false);
        if (!e.Deletable) w.WriteBoolean("deletable", false);
        if (!e.Reconnectable) w.WriteBoolean("reconnectable", false);
        if (o.IncludeSelection && e.Selected) w.WriteBoolean("selected", true);
        if (e.StrokeWidth != 1.5) w.WriteNumber("strokeWidth", e.StrokeWidth);
        if (e.Stroke is { } stroke) w.WriteString("stroke", stroke);
        if (!e.MarkerEnd) w.WriteBoolean("markerEnd", false);
        if (e.MarkerStart) w.WriteBoolean("markerStart", true);
        if (e.MarkerType != BlowMarkerType.Arrow) w.WriteString("markerType", "arrowclosed");
        if (e.MarkerColor is { } mc) w.WriteString("markerColor", mc);
        if (e.MarkerWidth is { } mw) w.WriteNumber("markerWidth", mw);
        if (e.MarkerHeight is { } mh) w.WriteNumber("markerHeight", mh);
        if (e.Class is { } cls) w.WriteString("class", cls);
        if (e.Style is { } style) w.WriteString("style", style);
        if (e.InteractionWidth != 14) w.WriteNumber("interactionWidth", e.InteractionWidth);
        if (e.ZIndex is { } z) w.WriteNumber("zIndex", z);
        WriteData(w, e.Data, o, () => $"Edge '{e.Id}'");
        w.WriteEndObject();
    }

    private static void WritePoint(Utf8JsonWriter w, string name, BlowXYPosition p)
    {
        w.WriteStartObject(name);
        w.WriteNumber("x", p.X);
        w.WriteNumber("y", p.Y);
        w.WriteEndObject();
    }

    private static void WriteData(Utf8JsonWriter w, object? data, BlowSerializerOptions o, Func<string> owner)
    {
        if (data is null) return;

        // Checked before anything is written, so a failure cannot leave half a property behind in the output.
        var mapped = DataMapping(data, o, owner);
        w.WritePropertyName("data");
        WriteDataValue(w, data, mapped);
    }

    /// <summary>
    /// How an element's data is saved: null for data that is JSON or a primitive already, else the mapping registered
    /// for its type.
    /// </summary>
    /// <exception cref="BlowSerializationException">No mapping says how to save the data's type.</exception>
    private static Func<object, JsonElement>? DataMapping(object data, BlowSerializerOptions o, Func<string> owner) => data switch
    {
        JsonElement or JsonNode or string or bool or int or long or double or float or decimal => null,
        _ => o.Writer(data.GetType()) ?? throw new BlowSerializationException(
            $"{owner()} carries data of type '{data.GetType().FullName}', which no mapping says how to save. " +
            "Register its JSON contract with BlowSerializerOptions.MapNodeData, MapEdgeData or MapData."),
    };

    private static void WriteDataValue(Utf8JsonWriter w, object data, Func<object, JsonElement>? mapped)
    {
        switch (data)
        {
            case JsonElement element: element.WriteTo(w); break;
            case JsonNode node: node.WriteTo(w); break;
            case string text: w.WriteStringValue(text); break;
            case bool flag: w.WriteBooleanValue(flag); break;
            case int i: w.WriteNumberValue(i); break;
            case long l: w.WriteNumberValue(l); break;
            case double d: w.WriteNumberValue(d); break;
            case float f: w.WriteNumberValue(f); break;
            case decimal m: w.WriteNumberValue(m); break;
            default: mapped!(data).WriteTo(w); break;
        }
    }

    /// <summary>
    /// An element's data as a JSON node of its own, written as <see cref="Serialize(BlowDocument, BlowSerializerOptions?)"/>
    /// writes it.
    /// </summary>
    /// <exception cref="BlowSerializationException">No mapping says how to save the data's type.</exception>
    internal static JsonNode? DataToNode(object data, BlowSerializerOptions o, Func<string> owner)
    {
        var mapped = DataMapping(data, o, owner);
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            WriteDataValue(w, data, mapped);
        }
        return JsonNode.Parse(buffer.WrittenSpan);
    }

    private static string Name(BlowPosition position) => position switch
    {
        BlowPosition.Left => "left",
        BlowPosition.Top => "top",
        BlowPosition.Right => "right",
        _ => "bottom",
    };

    private static string Name(BlowEdgeType type) => type switch
    {
        BlowEdgeType.Straight => "straight",
        BlowEdgeType.Step => "step",
        BlowEdgeType.SmoothStep => "smoothstep",
        BlowEdgeType.SimpleBezier => "simplebezier",
        BlowEdgeType.Routed => "routed",
        _ => "bezier",
    };

    // ---------------------------------------------------------------- reading

    /// <summary>Reads a document from a JSON string. Problems are reported, never thrown.</summary>
    public static BlowImportResult Deserialize(string json, BlowSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        var limits = (options ?? BlowSerializerOptions.Default).Limits;

        // A string of more characters than the limit has bytes is certainly over it; checking first avoids encoding
        // a hostile string only to throw the result away.
        if (json.Length > limits.MaxBytes) return Fatal(BlowImportCodes.TooLarge, "$", $"The document is larger than {limits.MaxBytes} bytes.");
        return Deserialize(Encoding.UTF8.GetBytes(json), options);
    }

    /// <summary>Reads a document from UTF-8 JSON. Problems are reported, never thrown.</summary>
    public static BlowImportResult Deserialize(ReadOnlySpan<byte> utf8Json, BlowSerializerOptions? options = null)
    {
        var o = options ?? BlowSerializerOptions.Default;
        var limits = o.Limits;

        if (utf8Json.Length > limits.MaxBytes)
            return Fatal(BlowImportCodes.TooLarge, "$", $"The document is {utf8Json.Length} bytes, more than the {limits.MaxBytes} allowed.");

        if (Prescan(utf8Json, limits) is { } refused) return refused;

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(utf8Json.ToArray(), DocumentOptions(limits));
        }
        catch (JsonException ex)
        {
            return Fatal(BlowImportCodes.InvalidJson, "$", $"The document is not valid JSON: {ex.Message}");
        }

        using (parsed)
        {
            return new Reader(o).Read(parsed.RootElement);
        }
    }

    /// <summary>
    /// Reads a document from a stream, reading no more than <see cref="BlowImportLimits.MaxBytes"/> of it - an upload
    /// of any size cannot make the reader buffer more than that.
    /// </summary>
    public static async Task<BlowImportResult> DeserializeAsync(Stream stream, BlowSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var limits = (options ?? BlowSerializerOptions.Default).Limits;

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > limits.MaxBytes)
                return Fatal(BlowImportCodes.TooLarge, "$", $"The document is larger than {limits.MaxBytes} bytes.");
            buffer.Write(chunk, 0, read);
        }
        return Deserialize(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), options);
    }

    private static JsonDocumentOptions DocumentOptions(BlowImportLimits limits) => new()
    {
        // The prescan has already refused anything deeper than the limit; one more level leaves room for nothing.
        MaxDepth = limits.MaxDepth + 1,
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// Walks the JSON once, without building anything, to refuse a document nested deeper than the limit with its own
    /// reason (rather than the parser's generic one) and to report a syntax error with its line and position.
    /// </summary>
    private static BlowImportResult? Prescan(ReadOnlySpan<byte> utf8Json, BlowImportLimits limits)
    {
        var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
        {
            MaxDepth = limits.MaxDepth + 2,
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });
        try
        {
            while (reader.Read())
            {
                if (reader.CurrentDepth > limits.MaxDepth)
                {
                    return Fatal(BlowImportCodes.TooDeep, "$",
                        $"The document nests deeper than {limits.MaxDepth} levels (at byte {reader.TokenStartIndex}).");
                }
            }
            return null;
        }
        catch (JsonException ex)
        {
            return Fatal(BlowImportCodes.InvalidJson, "$",
                $"The document is not valid JSON (line {ex.LineNumber + 1}, position {ex.BytePositionInLine + 1}): {ex.Message}");
        }
    }

    private static BlowImportResult Fatal(string code, string path, string message)
        => new(null, [new BlowImportIssue(code, BlowValidationSeverity.Error, path, message)], 0);

    /// <summary>Reads one parsed document, collecting issues as it goes.</summary>
    private sealed class Reader(BlowSerializerOptions o) : BlowJsonReader(o.Limits)
    {
        public BlowImportResult Read(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String
                || format.GetString() != Format)
            {
                return Fatal(BlowImportCodes.NotADocument, "$",
                    $"This is not a Blow document: its root has to be an object whose \"format\" is \"{Format}\".");
            }

            if (!root.TryGetProperty("schemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number
                || !schema.TryGetInt32(out var schemaVersion) || schemaVersion < 1)
                return Fatal(BlowImportCodes.UnsupportedSchema, "$.schemaVersion", "The document has no valid \"schemaVersion\".");
            if (schemaVersion > SchemaVersion)
            {
                return Fatal(BlowImportCodes.UnsupportedSchema, "$.schemaVersion",
                    $"The document was written in schema version {schemaVersion}; this version of Blow reads up to {SchemaVersion}.");
            }

            var version = 0;
            if (root.TryGetProperty("version", out var v) && (v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out version)))
                return Fatal(BlowImportCodes.InvalidValue, "$.version", "\"version\" has to be a whole number.");
            var sourceVersion = version;

            if (version > o.CurrentVersion)
            {
                return new BlowImportResult(null, [new BlowImportIssue(BlowImportCodes.NewerVersion, BlowValidationSeverity.Error, "$.version",
                    $"The document was saved by a newer version of the application (version {version}; this one reads up to {o.CurrentVersion}).")],
                    sourceVersion);
            }

            if (version < o.CurrentVersion)
            {
                if (Migrate(root, version) is not { } migrated)
                    return new BlowImportResult(null, _issues.ToArray(), sourceVersion);

                using (migrated)
                {
                    return ReadDocument(migrated.RootElement, sourceVersion);
                }
            }

            return ReadDocument(root, sourceVersion);
        }

        /// <summary>Runs the application's migrations from <paramref name="version"/> up to the current version.</summary>
        private JsonDocument? Migrate(JsonElement root, int version)
        {
            if (JsonNode.Parse(root.GetRawText(), null, new JsonDocumentOptions { MaxDepth = _limits.MaxDepth + 1 }) is not JsonObject document)
                return null;

            while (version < o.CurrentVersion)
            {
                var step = o.Migrations.FirstOrDefault(m => m.FromVersion == version);
                if (step is null)
                {
                    Error(BlowImportCodes.MissingMigration, "$.version",
                        $"No migration upgrades a document from version {version} (the application is at version {o.CurrentVersion}).");
                    return null;
                }

                try
                {
                    document = step.Migrate(document) ?? throw new InvalidOperationException("The migration returned null.");
                }
                catch (Exception ex)
                {
                    Error(BlowImportCodes.MigrationFailed, "$", $"The migration from version {version} failed: {ex.Message}");
                    return null;
                }

                version++;
                document["version"] = version;
            }

            // A migration can build JSON nested deeper than the limit the document itself was held to.
            try
            {
                return JsonDocument.Parse(document.ToJsonString(), new JsonDocumentOptions { MaxDepth = _limits.MaxDepth + 1 });
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                Error(BlowImportCodes.TooDeep, "$", $"The migrated document nests deeper than {_limits.MaxDepth} levels: {ex.Message}");
                return null;
            }
        }

        private BlowImportResult ReadDocument(JsonElement root, int sourceVersion)
        {
            var document = new BlowDocument { Version = o.CurrentVersion };

            foreach (var property in root.EnumerateObject())
            {
                var path = "$." + property.Name;
                switch (property.Name)
                {
                    case "format" or "schemaVersion" or "version": break;
                    case "id": document.Id = String(property.Value, path); break;
                    case "metadata":
                        if (property.Value.ValueKind == JsonValueKind.Object) document.Metadata = Detach(property.Value) as JsonObject;
                        else Error(BlowImportCodes.InvalidValue, path, "\"metadata\" has to be an object.");
                        break;
                    case "viewport": document.Viewport = Viewport(property.Value, path); break;
                    case "nodes": Nodes(property.Value, path, document.Nodes); break;
                    case "edges": Edges(property.Value, path, document.Edges); break;
                    default: Unknown(path); break;
                }
            }

            if (_issues.Any(i => i.Code == BlowImportCodes.TooMany && i.Path is "$.nodes" or "$.edges"))
                return new BlowImportResult(null, _issues.ToArray(), sourceVersion);

            CheckGraph(document);
            return new BlowImportResult(document, _issues.ToArray(), sourceVersion);
        }

        /// <summary>
        /// The graph-level problems a canvas would report but can draw around - duplicate ids, edges to missing nodes,
        /// parent cycles - reported here as warnings, each at the element it concerns.
        /// </summary>
        private void CheckGraph(BlowDocument document)
        {
            var nodeIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = document.Nodes.Count - 1; i >= 0; i--) nodeIndex[document.Nodes[i].Id] = i;
            var edgeIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = document.Edges.Count - 1; i >= 0; i--) edgeIndex[document.Edges[i].Id] = i;

            var result = new BlowGraphValidator(BlowValidationRules.Structure(BlowConnectionMode.Loose))
            {
                MaxIssues = 1000,
            }.Validate(document.Nodes, document.Edges);

            // A duplicate is reported at the element that repeats the id - one issue per repeat, in order - rather than at
            // the first one to use it, which is the one that is fine.
            var nodeRepeats = Repeats(document.Nodes.Select(n => n.Id));
            var edgeRepeats = Repeats(document.Edges.Select(e => e.Id));

            foreach (var issue in result.Issues)
            {
                var path = issue.Code == BlowValidationCodes.DuplicateEdgeId && issue.EdgeId is { } de && edgeRepeats.TryGetValue(de, out var dei) && dei.Count > 0
                        ? $"$.edges[{dei.Dequeue()}]"
                    : issue.Code == BlowValidationCodes.DuplicateNodeId && issue.NodeId is { } dn && nodeRepeats.TryGetValue(dn, out var dni) && dni.Count > 0
                        ? $"$.nodes[{dni.Dequeue()}]"
                    : issue.EdgeId is { } e && edgeIndex.TryGetValue(e, out var ei) ? $"$.edges[{ei}]"
                    : issue.NodeId is { } n && nodeIndex.TryGetValue(n, out var ni) ? $"$.nodes[{ni}]"
                    : "$";
                Issue(issue.Code, BlowValidationSeverity.Warning, path, issue.Message);
            }

            static Dictionary<string, Queue<int>> Repeats(IEnumerable<string> ids)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var repeats = new Dictionary<string, Queue<int>>(StringComparer.Ordinal);
                var i = 0;
                foreach (var id in ids)
                {
                    if (!seen.Add(id))
                    {
                        if (!repeats.TryGetValue(id, out var at)) repeats[id] = at = new Queue<int>();
                        at.Enqueue(i);
                    }
                    i++;
                }
                return repeats;
            }
        }

        // ---- elements ----

        private void Nodes(JsonElement array, string path, List<BlowNode> nodes)
        {
            if (array.ValueKind != JsonValueKind.Array) { Error(BlowImportCodes.InvalidValue, path, "\"nodes\" has to be an array."); return; }
            if (array.GetArrayLength() > _limits.MaxNodes)
            {
                Error(BlowImportCodes.TooMany, path, $"The document has {array.GetArrayLength()} nodes; at most {_limits.MaxNodes} are read.");
                return;
            }

            var i = 0;
            foreach (var element in array.EnumerateArray())
            {
                if (Node(element, $"{path}[{i++}]") is { } node) nodes.Add(node);
            }
        }

        private BlowNode? Node(JsonElement e, string path)
        {
            if (e.ValueKind != JsonValueKind.Object) { Error(BlowImportCodes.InvalidValue, path, "A node has to be an object."); return null; }
            if (RequiredString(e, "id", path) is not { } id) return null;

            var node = new BlowNode { Id = id };
            JsonElement? data = null;
            foreach (var p in e.EnumerateObject())
            {
                var at = $"{path}.{p.Name}";
                var value = p.Value;
                switch (p.Name)
                {
                    case "id": break;
                    case "type": node.Type = String(value, at) ?? node.Type; break;
                    case "label": node.Label = String(value, at); break;
                    case "position": node.Position = Point(value, at) ?? node.Position; break;
                    case "width": node.Width = Size(value, at); break;
                    case "height": node.Height = Size(value, at); break;
                    case "parentId": node.ParentId = String(value, at); break;
                    case "origin": node.Origin = Point(value, at); break;
                    case "extentParent": node.ExtentParent = Bool(value, at) ?? false; break;
                    case "extent": node.Extent = Rect(value, at); break;
                    case "expandParent": node.ExpandParent = Bool(value, at) ?? false; break;
                    case "sourcePosition": node.SourcePosition = Position(value, at) ?? node.SourcePosition; break;
                    case "targetPosition": node.TargetPosition = Position(value, at) ?? node.TargetPosition; break;
                    case "hidden": node.Hidden = Bool(value, at) ?? false; break;
                    case "collapsed": node.Collapsed = Bool(value, at) ?? false; break;
                    case "draggable": node.Draggable = Bool(value, at) ?? true; break;
                    case "selectable": node.Selectable = Bool(value, at) ?? true; break;
                    case "connectable": node.Connectable = Bool(value, at) ?? true; break;
                    case "deletable": node.Deletable = Bool(value, at) ?? true; break;
                    case "focusable": node.Focusable = Bool(value, at) ?? true; break;
                    case "useCustomDragHandle": node.UseCustomDragHandle = Bool(value, at) ?? false; break;
                    case "selected": node.Selected = Bool(value, at) ?? false; break;
                    case "ariaLabel": node.AriaLabel = String(value, at); break;
                    case "class": node.Class = String(value, at); break;
                    case "style": node.Style = String(value, at); break;
                    case "zIndex": node.ZIndex = Int(value, at); break;
                    case "ports": node.Ports = Ports(value, at); break;
                    case "data": data = value; break;
                    default: Unknown(at); break;
                }
            }

            // Last, so a mapping keyed by node type sees the type whatever order the properties came in.
            if (data is { } d) node.Data = Data(d, $"{path}.data", o.NodeReader(node.Type));
            return node;
        }

        private List<BlowPort>? Ports(JsonElement array, string path)
        {
            if (array.ValueKind != JsonValueKind.Array) { Error(BlowImportCodes.InvalidValue, path, "\"ports\" has to be an array."); return null; }
            if (array.GetArrayLength() > _limits.MaxPortsPerNode)
            {
                Error(BlowImportCodes.TooMany, path, $"The node declares {array.GetArrayLength()} ports; at most {_limits.MaxPortsPerNode} are read.");
                return null;
            }

            var ports = new List<BlowPort>();
            var i = 0;
            foreach (var e in array.EnumerateArray())
            {
                var at = $"{path}[{i++}]";
                if (e.ValueKind != JsonValueKind.Object) { Error(BlowImportCodes.InvalidValue, at, "A port has to be an object."); continue; }

                var port = new BlowPort();
                var typed = false;
                foreach (var p in e.EnumerateObject())
                {
                    var pat = $"{at}.{p.Name}";
                    switch (p.Name)
                    {
                        case "id": port.Id = String(p.Value, pat); break;
                        case "type":
                            if (HandleType(p.Value, pat) is { } type) { port.Type = type; typed = true; }
                            break;
                        case "position": port.Position = Position(p.Value, pat); break;
                        case "label": port.Label = String(p.Value, pat); break;
                        case "dataType": port.DataType = String(p.Value, pat); break;
                        case "maxConnections":
                            var max = Int(p.Value, pat);
                            port.MaxConnections = max < 0 ? Invalid<int?>(pat, "maxConnections cannot be negative.") : max;
                            break;
                        case "minConnections":
                            var min = Int(p.Value, pat) ?? 0;
                            port.MinConnections = min < 0 ? Invalid<int>(pat, "minConnections cannot be negative.") : min;
                            break;
                        case "data": port.Data = Data(p.Value, pat, null); break;
                        default: Unknown(pat); break;
                    }
                }

                if (!typed) { Error(BlowImportCodes.MissingProperty, at, "A port needs a \"type\": \"source\" or \"target\"."); continue; }
                ports.Add(port);
            }
            return ports;
        }

        private void Edges(JsonElement array, string path, List<BlowEdge> edges)
        {
            if (array.ValueKind != JsonValueKind.Array) { Error(BlowImportCodes.InvalidValue, path, "\"edges\" has to be an array."); return; }
            if (array.GetArrayLength() > _limits.MaxEdges)
            {
                Error(BlowImportCodes.TooMany, path, $"The document has {array.GetArrayLength()} edges; at most {_limits.MaxEdges} are read.");
                return;
            }

            var i = 0;
            foreach (var element in array.EnumerateArray())
            {
                if (Edge(element, $"{path}[{i++}]") is { } edge) edges.Add(edge);
            }
        }

        private BlowEdge? Edge(JsonElement e, string path)
        {
            if (e.ValueKind != JsonValueKind.Object) { Error(BlowImportCodes.InvalidValue, path, "An edge has to be an object."); return null; }
            var id = RequiredString(e, "id", path);
            var source = RequiredString(e, "source", path);
            var target = RequiredString(e, "target", path);
            if (id is null || source is null || target is null) return null;

            var edge = new BlowEdge { Id = id, Source = source, Target = target };
            foreach (var p in e.EnumerateObject())
            {
                var at = $"{path}.{p.Name}";
                var value = p.Value;
                switch (p.Name)
                {
                    case "id" or "source" or "target": break;
                    case "sourceHandle": edge.SourceHandle = String(value, at); break;
                    case "targetHandle": edge.TargetHandle = String(value, at); break;
                    case "type": edge.Type = EdgeType(value, at) ?? edge.Type; break;
                    case "pathOptions": edge.PathOptions = PathOptions(value, at); break;
                    case "label": edge.Label = String(value, at); break;
                    case "labelStyle": edge.LabelStyle = String(value, at); break;
                    case "labelShowBg": edge.LabelShowBg = Bool(value, at) ?? true; break;
                    case "labelBgStyle": edge.LabelBgStyle = String(value, at); break;
                    case "labelBgPaddingX": edge.LabelBgPaddingX = Number(value, at) ?? edge.LabelBgPaddingX; break;
                    case "labelBgPaddingY": edge.LabelBgPaddingY = Number(value, at) ?? edge.LabelBgPaddingY; break;
                    case "labelBgBorderRadius": edge.LabelBgBorderRadius = Number(value, at) ?? edge.LabelBgBorderRadius; break;
                    case "animated": edge.Animated = Bool(value, at) ?? false; break;
                    case "hidden": edge.Hidden = Bool(value, at) ?? false; break;
                    case "selectable": edge.Selectable = Bool(value, at) ?? true; break;
                    case "focusable": edge.Focusable = Bool(value, at) ?? true; break;
                    case "deletable": edge.Deletable = Bool(value, at) ?? true; break;
                    case "reconnectable": edge.Reconnectable = Bool(value, at) ?? true; break;
                    case "selected": edge.Selected = Bool(value, at) ?? false; break;
                    case "strokeWidth": edge.StrokeWidth = Size(value, at) ?? edge.StrokeWidth; break;
                    case "stroke": edge.Stroke = String(value, at); break;
                    case "markerEnd": edge.MarkerEnd = Bool(value, at) ?? true; break;
                    case "markerStart": edge.MarkerStart = Bool(value, at) ?? false; break;
                    case "markerType": edge.MarkerType = MarkerType(value, at) ?? edge.MarkerType; break;
                    case "markerColor": edge.MarkerColor = String(value, at); break;
                    case "markerWidth": edge.MarkerWidth = Size(value, at); break;
                    case "markerHeight": edge.MarkerHeight = Size(value, at); break;
                    case "class": edge.Class = String(value, at); break;
                    case "style": edge.Style = String(value, at); break;
                    case "interactionWidth": edge.InteractionWidth = Size(value, at) ?? edge.InteractionWidth; break;
                    case "zIndex": edge.ZIndex = Int(value, at); break;
                    case "data": edge.Data = Data(value, at, o.EdgeReader); break;
                    default: Unknown(at); break;
                }
            }
            return edge;
        }
    }
}
