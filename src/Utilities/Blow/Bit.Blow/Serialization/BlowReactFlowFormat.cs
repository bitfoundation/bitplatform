using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blow;

/// <summary>
/// Reads and writes the JSON React Flow's <c>toObject()</c> produces - <c>{ nodes, edges, viewport }</c> - so a flow
/// can move between a React Flow front end and a Blazor one, or an existing React Flow application's saved flows can be
/// brought over.
/// </summary>
/// <remarks>
/// <para>
/// The two models are close but not the same, and the translation says where it is lossy rather than guessing:
/// <c>data.label</c> becomes <see cref="BlowNode.Label"/> (the whole <c>data</c> object is kept as the node's data);
/// <c>style</c> and <c>className</c> objects become inline CSS and classes; markers, which React Flow draws only when
/// asked, are read as such, so an edge without <c>markerEnd</c> comes over without an arrowhead. What has no
/// counterpart - a drag-handle selector, React elements as labels - is reported as an issue.
/// </para>
/// <para>
/// Imports are bounded by the same <see cref="BlowImportLimits"/> as Blow documents, and report problems the same
/// way. There is no version to migrate: React Flow's object carries none.
/// </para>
/// </remarks>
public static class BlowReactFlowFormat
{
    /// <summary>Reads a React Flow <c>toObject()</c> JSON into a document. Problems are reported, never thrown.</summary>
    public static BlowImportResult Import(string json, BlowSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        var o = options ?? BlowSerializerOptions.Default;
        var limits = o.Limits;
        if (json.Length > limits.MaxBytes || Encoding.UTF8.GetByteCount(json) > limits.MaxBytes)
            return Single(BlowImportCodes.TooLarge, $"The document is larger than {limits.MaxBytes} bytes.");

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                MaxDepth = limits.MaxDepth,
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
        }
        catch (JsonException ex)
        {
            return Single(BlowImportCodes.InvalidJson,
                $"The document is not valid JSON, or nests deeper than {limits.MaxDepth} levels: {ex.Message}");
        }

        using (parsed)
        {
            return new Reader(o).Read(parsed.RootElement);
        }
    }

    private static BlowImportResult Single(string code, string message)
        => new(null, [new BlowImportIssue(code, BlowValidationSeverity.Error, "$", message)], 0);

    /// <summary>
    /// Writes a document as React Flow's <c>toObject()</c> JSON, ready for <c>setNodes</c>, <c>setEdges</c> and
    /// <c>setViewport</c> - or React Flow's own <c>restore</c> pattern.
    /// </summary>
    /// <exception cref="BlowSerializationException">An element carries data no mapping says how to save.</exception>
    public static string Export(BlowDocument document, BlowSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var o = options ?? BlowSerializerOptions.Default;

        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = o.WriteIndented }))
        {
            w.WriteStartObject();
            w.WriteStartArray("nodes");
            foreach (var node in document.Nodes) WriteNode(w, node, o);
            w.WriteEndArray();
            w.WriteStartArray("edges");
            foreach (var edge in document.Edges) WriteEdge(w, edge, o);
            w.WriteEndArray();
            var viewport = document.Viewport ?? BlowViewport.Identity;
            w.WriteStartObject("viewport");
            w.WriteNumber("x", viewport.X);
            w.WriteNumber("y", viewport.Y);
            w.WriteNumber("zoom", viewport.Zoom);
            w.WriteEndObject();
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    // ---------------------------------------------------------------- writing

    private static void WriteNode(Utf8JsonWriter w, BlowNode n, BlowSerializerOptions o)
    {
        w.WriteStartObject();
        w.WriteString("id", n.Id);
        w.WriteString("type", n.Type);
        w.WriteStartObject("position");
        w.WriteNumber("x", n.Position.X);
        w.WriteNumber("y", n.Position.Y);
        w.WriteEndObject();

        // React Flow keeps the label in data, and data is always an object.
        var data = DataObject(n.Data, o, () => $"Node '{n.Id}'");
        if (n.Label is { } label) data["label"] = label;
        w.WritePropertyName("data");
        data.WriteTo(w);

        if (n.Width is { } width) w.WriteNumber("width", width);
        if (n.Height is { } height) w.WriteNumber("height", height);
        if (n.ParentId is { } parent) w.WriteString("parentId", parent);
        if (n.ExtentParent) w.WriteString("extent", "parent");
        else if (n.Extent is { } extent)
        {
            w.WriteStartArray("extent");
            WritePair(w, extent.Left, extent.Top);
            WritePair(w, extent.Right, extent.Bottom);
            w.WriteEndArray();
        }
        if (n.ExpandParent) w.WriteBoolean("expandParent", true);
        if (n.SourcePosition != BlowPosition.Bottom) w.WriteString("sourcePosition", Side(n.SourcePosition));
        if (n.TargetPosition != BlowPosition.Top) w.WriteString("targetPosition", Side(n.TargetPosition));
        if (n.Hidden) w.WriteBoolean("hidden", true);
        if (n.Selected && o.IncludeSelection) w.WriteBoolean("selected", true);
        if (!n.Draggable) w.WriteBoolean("draggable", false);
        if (!n.Selectable) w.WriteBoolean("selectable", false);
        if (!n.Connectable) w.WriteBoolean("connectable", false);
        if (!n.Deletable) w.WriteBoolean("deletable", false);
        if (!n.Focusable) w.WriteBoolean("focusable", false);
        if (n.Class is { } cls) w.WriteString("className", cls);
        if (n.Style is { } style) WriteStyle(w, "style", style);
        if (n.ZIndex is { } z) w.WriteNumber("zIndex", z);
        if (n.Origin is { } origin)
        {
            w.WritePropertyName("origin");
            WritePair(w, origin.X, origin.Y);
        }
        if (n.AriaLabel is { } aria) w.WriteString("ariaLabel", aria);
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
        w.WriteString("type", e.Type switch
        {
            BlowEdgeType.Straight => "straight",
            BlowEdgeType.Step => "step",
            BlowEdgeType.SmoothStep => "smoothstep",
            BlowEdgeType.SimpleBezier => "simplebezier",
            _ => "default",
        });
        if (e.Label is { } label) w.WriteString("label", label);
        if (e.Animated) w.WriteBoolean("animated", true);
        if (e.Hidden) w.WriteBoolean("hidden", true);
        if (e.Selected && o.IncludeSelection) w.WriteBoolean("selected", true);
        if (!e.Selectable) w.WriteBoolean("selectable", false);
        if (!e.Deletable) w.WriteBoolean("deletable", false);
        if (!e.Focusable) w.WriteBoolean("focusable", false);
        if (!e.Reconnectable) w.WriteBoolean("reconnectable", false);
        if (e.MarkerEnd) WriteMarker(w, "markerEnd", e);
        if (e.MarkerStart) WriteMarker(w, "markerStart", e);

        var style = ParseCss(e.Style);
        if (e.Stroke is { } stroke) style["stroke"] = stroke;
        if (e.StrokeWidth != 1.5) style["strokeWidth"] = e.StrokeWidth;
        if (style.Count > 0)
        {
            w.WritePropertyName("style");
            style.WriteTo(w);
        }

        if (e.Class is { } cls) w.WriteString("className", cls);
        if (e.ZIndex is { } z) w.WriteNumber("zIndex", z);
        if (e.InteractionWidth != 14) w.WriteNumber("interactionWidth", e.InteractionWidth);
        if (e.LabelStyle is { } ls) WriteStyle(w, "labelStyle", ls);
        if (!e.LabelShowBg) w.WriteBoolean("labelShowBg", false);
        if (e.LabelBgStyle is { } lbs) WriteStyle(w, "labelBgStyle", lbs);
        if (e.LabelBgPaddingX != 4 || e.LabelBgPaddingY != 2)
        {
            w.WritePropertyName("labelBgPadding");
            WritePair(w, e.LabelBgPaddingX, e.LabelBgPaddingY);
        }
        if (e.LabelBgBorderRadius != 3) w.WriteNumber("labelBgBorderRadius", e.LabelBgBorderRadius);
        if (e.PathOptions is { } po)
        {
            w.WriteStartObject("pathOptions");
            if (po.Curvature is { } c) w.WriteNumber("curvature", c);
            if (po.BorderRadius is { } br) w.WriteNumber("borderRadius", br);
            if (po.Offset is { } off) w.WriteNumber("offset", off);
            if (po.StepPosition is { } sp) w.WriteNumber("stepPosition", sp);
            w.WriteEndObject();
        }
        if (e.Data is not null)
        {
            w.WritePropertyName("data");
            DataObject(e.Data, o, () => $"Edge '{e.Id}'").WriteTo(w);
        }
        w.WriteEndObject();
    }

    private static void WriteMarker(Utf8JsonWriter w, string name, BlowEdge e)
    {
        w.WriteStartObject(name);
        w.WriteString("type", e.MarkerType == BlowMarkerType.ArrowClosed ? "arrowclosed" : "arrow");
        if (e.MarkerColor is { } color) w.WriteString("color", color);
        if (e.MarkerWidth is { } width) w.WriteNumber("width", width);
        if (e.MarkerHeight is { } height) w.WriteNumber("height", height);
        w.WriteEndObject();
    }

    private static void WritePair(Utf8JsonWriter w, double a, double b)
    {
        w.WriteStartArray();
        w.WriteNumberValue(a);
        w.WriteNumberValue(b);
        w.WriteEndArray();
    }

    private static void WriteStyle(Utf8JsonWriter w, string name, string css)
    {
        w.WritePropertyName(name);
        ParseCss(css).WriteTo(w);
    }

    /// <summary>
    /// Data as the JSON object React Flow expects. An object comes over as it is; any other value is wrapped as
    /// <c>{ "value": ... }</c>, since React Flow's data is always an object.
    /// </summary>
    private static JsonObject DataObject(object? data, BlowSerializerOptions o, Func<string> owner)
    {
        JsonNode? node = data switch
        {
            null => null,
            JsonElement element => JsonNode.Parse(element.GetRawText()),
            JsonNode n => n.DeepClone(),
            string s => JsonValue.Create(s),
            bool b => JsonValue.Create(b),
            int i => JsonValue.Create(i),
            long l => JsonValue.Create(l),
            double d => JsonValue.Create(d),
            float f => JsonValue.Create(f),
            decimal m => JsonValue.Create(m),
            _ => o.Writer(data.GetType()) is { } write
                ? JsonNode.Parse(write(data).GetRawText())
                : throw new BlowSerializationException(
                    $"{owner()} carries data of type '{data.GetType().FullName}', which no mapping says how to save. " +
                    "Register its JSON contract with BlowSerializerOptions.MapNodeData, MapEdgeData or MapData."),
        };

        return node switch
        {
            JsonObject obj => obj,
            null => [],
            _ => new JsonObject { ["value"] = node },
        };
    }

    private static string Side(BlowPosition position) => position switch
    {
        BlowPosition.Left => "left",
        BlowPosition.Top => "top",
        BlowPosition.Right => "right",
        _ => "bottom",
    };

    // ---------------------------------------------------------------- CSS

    /// <summary>React's unitless CSS properties: a number there is not a length.</summary>
    private static readonly HashSet<string> Unitless = new(StringComparer.Ordinal)
    {
        "opacity", "zIndex", "fontWeight", "lineHeight", "flex", "flexGrow", "flexShrink", "order", "zoom",
        "fillOpacity", "strokeOpacity", "strokeWidth", "strokeMiterlimit",
    };

    /// <summary>A React style object as inline CSS: <c>{ backgroundColor: "#fff", width: 10 }</c> to <c>background-color:#fff;width:10px;</c>.</summary>
    internal static string? CssFromStyle(JsonElement style, Func<JsonProperty, bool>? skip = null)
    {
        if (style.ValueKind != JsonValueKind.Object) return null;
        var css = new StringBuilder();
        foreach (var p in style.EnumerateObject())
        {
            if (skip?.Invoke(p) == true) continue;
            var value = p.Value.ValueKind switch
            {
                JsonValueKind.String => p.Value.GetString(),
                JsonValueKind.Number when Unitless.Contains(p.Name) => p.Value.GetRawText(),
                JsonValueKind.Number => p.Value.GetRawText() + "px",
                _ => null,
            };
            if (value is null) continue;
            css.Append(Kebab(p.Name)).Append(':').Append(value).Append(';');
        }
        return css.Length == 0 ? null : css.ToString();
    }

    /// <summary>Inline CSS as a React style object, with camel-cased names; numbers in pixels become numbers.</summary>
    internal static JsonObject ParseCss(string? css)
    {
        var style = new JsonObject();
        if (string.IsNullOrWhiteSpace(css)) return style;
        foreach (var declaration in css.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0) continue;
            var name = Camel(declaration[..colon].Trim());
            var value = declaration[(colon + 1)..].Trim();
            var number = value.EndsWith("px", StringComparison.Ordinal) ? value[..^2] : Unitless.Contains(name) ? value : null;
            style[name] = number is not null && double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                ? JsonValue.Create(d)
                : JsonValue.Create(value);
        }
        return style;
    }

    private static string Kebab(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        foreach (var ch in name)
        {
            if (char.IsUpper(ch)) sb.Append('-').Append(char.ToLowerInvariant(ch));
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string Camel(string name)
    {
        var sb = new StringBuilder(name.Length);
        var upper = false;
        foreach (var ch in name)
        {
            if (ch == '-') { upper = sb.Length > 0; continue; }
            sb.Append(upper ? char.ToUpperInvariant(ch) : ch);
            upper = false;
        }
        return sb.ToString();
    }

    // ---------------------------------------------------------------- reading

    private sealed class Reader(BlowSerializerOptions o) : BlowJsonReader(o.Limits)
    {
        public BlowImportResult Read(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array)
            {
                return new(null, [new BlowImportIssue(BlowImportCodes.NotADocument, BlowValidationSeverity.Error, "$",
                    "This is not a React Flow object: its root has to be an object with a \"nodes\" array.")], 0);
            }

            var document = new BlowDocument();
            if (nodes.GetArrayLength() > _limits.MaxNodes)
                return Fatal("$.nodes", $"The document has {nodes.GetArrayLength()} nodes; at most {_limits.MaxNodes} are read.");

            var i = 0;
            foreach (var n in nodes.EnumerateArray())
            {
                if (Node(n, $"$.nodes[{i++}]") is { } node) document.Nodes.Add(node);
            }

            if (root.TryGetProperty("edges", out var edges))
            {
                if (edges.ValueKind != JsonValueKind.Array) Error(BlowImportCodes.InvalidValue, "$.edges", "\"edges\" has to be an array.");
                else if (edges.GetArrayLength() > _limits.MaxEdges)
                    return Fatal("$.edges", $"The document has {edges.GetArrayLength()} edges; at most {_limits.MaxEdges} are read.");
                else
                {
                    i = 0;
                    foreach (var e in edges.EnumerateArray())
                    {
                        if (Edge(e, $"$.edges[{i++}]") is { } edge) document.Edges.Add(edge);
                    }
                }
            }

            if (root.TryGetProperty("viewport", out var viewport)) document.Viewport = Viewport(viewport, "$.viewport");
            return new BlowImportResult(document, _issues.ToArray(), 0);
        }

        private BlowImportResult Fatal(string path, string message)
        {
            Error(BlowImportCodes.TooMany, path, message);
            return new BlowImportResult(null, _issues.ToArray(), 0);
        }

        private BlowNode? Node(JsonElement e, string path)
        {
            if (e.ValueKind != JsonValueKind.Object) { Error(BlowImportCodes.InvalidValue, path, "A node has to be an object."); return null; }
            if (RequiredString(e, "id", path) is not { } id) return null;

            var node = new BlowNode { Id = id };
            foreach (var p in e.EnumerateObject())
            {
                var at = $"{path}.{p.Name}";
                var value = p.Value;
                switch (p.Name)
                {
                    case "id": break;
                    case "type": node.Type = String(value, at) ?? node.Type; break;
                    case "position": node.Position = Point(value, at) ?? node.Position; break;
                    case "data":
                        if (value.ValueKind == JsonValueKind.Object)
                        {
                            if (value.TryGetProperty("label", out var label))
                            {
                                if (label.ValueKind == JsonValueKind.String) node.Label = String(label, at + ".label");
                                else if (label.ValueKind != JsonValueKind.Null)
                                    Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, at + ".label",
                                        "Only a text label can be brought over; the label was left in the node's data.");
                            }
                            node.Data = value.Clone();
                        }
                        else if (value.ValueKind != JsonValueKind.Null) node.Data = value.Clone();
                        break;
                    case "width": node.Width = Size(value, at); break;
                    case "height": node.Height = Size(value, at); break;
                    case "parentId" or "parentNode": node.ParentId = String(value, at); break;
                    case "extent":
                        if (value.ValueKind == JsonValueKind.String && value.GetString() == "parent") node.ExtentParent = true;
                        else node.Extent = Extent(value, at);
                        break;
                    case "expandParent": node.ExpandParent = Bool(value, at) ?? false; break;
                    case "sourcePosition": node.SourcePosition = Position(value, at) ?? node.SourcePosition; break;
                    case "targetPosition": node.TargetPosition = Position(value, at) ?? node.TargetPosition; break;
                    case "hidden": node.Hidden = Bool(value, at) ?? false; break;
                    case "selected": node.Selected = Bool(value, at) ?? false; break;
                    case "draggable": node.Draggable = Bool(value, at) ?? true; break;
                    case "selectable": node.Selectable = Bool(value, at) ?? true; break;
                    case "connectable": node.Connectable = Bool(value, at) ?? true; break;
                    case "deletable": node.Deletable = Bool(value, at) ?? true; break;
                    case "focusable": node.Focusable = Bool(value, at) ?? true; break;
                    case "dragHandle":
                        node.UseCustomDragHandle = true;
                        Issue(BlowImportCodes.UnknownProperty, BlowValidationSeverity.Warning, at,
                            "React Flow names its drag handle with a CSS selector; in Blow the node's template places a BlowDragHandle instead.");
                        break;
                    case "className": node.Class = String(value, at); break;
                    case "style": node.Style = CssFromStyle(value); break;
                    case "zIndex": node.ZIndex = Int(value, at); break;
                    case "origin": node.Origin = Pair(value, at); break;
                    case "ariaLabel": node.AriaLabel = String(value, at); break;
                    case "measured" or "handles" or "dragging" or "resizing" or "internals" or "initialWidth" or "initialHeight": break;
                    default: Unknown(at); break;
                }
            }
            return node;
        }

        private BlowEdge? Edge(JsonElement e, string path)
        {
            if (e.ValueKind != JsonValueKind.Object) { Error(BlowImportCodes.InvalidValue, path, "An edge has to be an object."); return null; }
            var id = RequiredString(e, "id", path);
            var source = RequiredString(e, "source", path);
            var target = RequiredString(e, "target", path);
            if (id is null || source is null || target is null) return null;

            // React Flow draws no arrowhead unless asked for one.
            var edge = new BlowEdge { Id = id, Source = source, Target = target, MarkerEnd = false };
            foreach (var p in e.EnumerateObject())
            {
                var at = $"{path}.{p.Name}";
                var value = p.Value;
                switch (p.Name)
                {
                    case "id" or "source" or "target": break;
                    case "sourceHandle": edge.SourceHandle = String(value, at); break;
                    case "targetHandle": edge.TargetHandle = String(value, at); break;
                    case "type":
                        var type = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
                        edge.Type = type switch
                        {
                            "straight" => BlowEdgeType.Straight,
                            "step" => BlowEdgeType.Step,
                            "smoothstep" => BlowEdgeType.SmoothStep,
                            "simplebezier" => BlowEdgeType.SimpleBezier,
                            "default" or "bezier" or null => BlowEdgeType.Bezier,
                            _ => Custom(at, type),
                        };
                        break;
                    case "label":
                        if (value.ValueKind == JsonValueKind.String) edge.Label = String(value, at);
                        else if (value.ValueKind != JsonValueKind.Null)
                            Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, at, "Only a text label can be brought over.");
                        break;
                    case "animated": edge.Animated = Bool(value, at) ?? false; break;
                    case "hidden": edge.Hidden = Bool(value, at) ?? false; break;
                    case "selected": edge.Selected = Bool(value, at) ?? false; break;
                    case "selectable": edge.Selectable = Bool(value, at) ?? true; break;
                    case "deletable": edge.Deletable = Bool(value, at) ?? true; break;
                    case "focusable": edge.Focusable = Bool(value, at) ?? true; break;
                    case "reconnectable":
                        // React Flow can limit reconnection to one end; Blow's switch is all or nothing.
                        edge.Reconnectable = value.ValueKind != JsonValueKind.False;
                        break;
                    case "markerEnd": edge.MarkerEnd = Marker(value, at, edge); break;
                    case "markerStart": edge.MarkerStart = Marker(value, at, edge); break;
                    case "style": Style(value, edge); break;
                    case "className": edge.Class = String(value, at); break;
                    case "zIndex": edge.ZIndex = Int(value, at); break;
                    case "interactionWidth": edge.InteractionWidth = Size(value, at) ?? edge.InteractionWidth; break;
                    case "labelStyle": edge.LabelStyle = CssFromStyle(value); break;
                    case "labelShowBg": edge.LabelShowBg = Bool(value, at) ?? true; break;
                    case "labelBgStyle": edge.LabelBgStyle = CssFromStyle(value); break;
                    case "labelBgPadding":
                        if (Pair(value, at) is { } padding) (edge.LabelBgPaddingX, edge.LabelBgPaddingY) = (padding.X, padding.Y);
                        break;
                    case "labelBgBorderRadius": edge.LabelBgBorderRadius = Size(value, at) ?? edge.LabelBgBorderRadius; break;
                    case "pathOptions": edge.PathOptions = PathOptions(value, at); break;
                    case "data": if (value.ValueKind != JsonValueKind.Null) edge.Data = value.Clone(); break;
                    default: Unknown(at); break;
                }
            }
            return edge;
        }

        private BlowEdgeType Custom(string path, string? type)
        {
            Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, path,
                $"The custom edge type '{type}' is drawn as a bezier; an EdgeTemplate can draw it as before (the original type is not kept).");
            return BlowEdgeType.Bezier;
        }

        /// <summary>A marker, given as a type name or an object; true when there is one.</summary>
        private bool Marker(JsonElement value, string path, BlowEdge edge)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    edge.MarkerType = value.GetString() == "arrowclosed" ? BlowMarkerType.ArrowClosed : BlowMarkerType.Arrow;
                    return true;
                case JsonValueKind.Object:
                    if (value.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
                        edge.MarkerType = type.GetString() == "arrowclosed" ? BlowMarkerType.ArrowClosed : BlowMarkerType.Arrow;
                    if (value.TryGetProperty("color", out var color)) edge.MarkerColor = String(color, path + ".color");
                    if (value.TryGetProperty("width", out var width)) edge.MarkerWidth = Size(width, path + ".width");
                    if (value.TryGetProperty("height", out var height)) edge.MarkerHeight = Size(height, path + ".height");
                    return true;
                case JsonValueKind.Null:
                    return false;
                default:
                    return Invalid<bool>(path, "Expected a marker type or a marker object.");
            }
        }

        /// <summary>An edge's style object: stroke and stroke width to their own properties, the rest as CSS.</summary>
        private void Style(JsonElement value, BlowEdge edge)
        {
            if (value.ValueKind != JsonValueKind.Object) return;
            foreach (var p in value.EnumerateObject())
            {
                if (Stroke(p)) edge.Stroke = p.Value.GetString();
                else if (StrokeWidth(p)) edge.StrokeWidth = p.Value.GetDouble();
            }
            edge.Style = CssFromStyle(value, p => Stroke(p) || StrokeWidth(p));

            static bool Stroke(JsonProperty p) => p.Name == "stroke" && p.Value.ValueKind == JsonValueKind.String;
            static bool StrokeWidth(JsonProperty p) => p.Name == "strokeWidth" && p.Value.ValueKind == JsonValueKind.Number;
        }

        private BlowXYPosition? Pair(JsonElement value, string path)
        {
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2)
                return Invalid<BlowXYPosition?>(path, "Expected a pair of numbers.");
            return Number(value[0], path + "[0]") is { } a && Number(value[1], path + "[1]") is { } b ? new BlowXYPosition(a, b) : null;
        }

        private BlowRect? Extent(JsonElement value, string path)
        {
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2)
                return Invalid<BlowRect?>(path, "Expected \"parent\" or [[x1, y1], [x2, y2]].");
            if (Pair(value[0], path + "[0]") is not { } from || Pair(value[1], path + "[1]") is not { } to) return null;
            return new BlowRect(Math.Min(from.X, to.X), Math.Min(from.Y, to.Y), Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
        }
    }
}
