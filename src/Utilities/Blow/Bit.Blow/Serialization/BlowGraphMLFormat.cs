using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;

namespace Bit.Blow;

/// <summary>
/// GraphML in and out - the XML graph format of yEd, Gephi, Cytoscape, NetworkX and most graph tools. Export writes
/// nodes with their label, type, position and size, edges with their label and ports, groups as nested graphs, and
/// JSON data. Import reads that, plain GraphML from other tools, and yEd's geometry and labels; nodes without positions
/// are laid out.
/// </summary>
/// <remarks>
/// Import never resolves anything outside the text: DTDs are refused and no external entity or schema is loaded. Input
/// is bounded by <see cref="BlowImportLimits"/>, and attributes with no counterpart in Blow are kept in the node's or
/// edge's data, as a JSON object of the attributes' names.
/// </remarks>
public static class BlowGraphMLFormat
{
    private const string Ns = "http://graphml.graphdrawing.org/xmlns";
    private const string YNs = "http://www.yworks.com/xml/graphml";

    /// <summary>Writes the document as GraphML.</summary>
    public static string Export(BlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var nodes = document.Nodes.GroupBy(n => n.Id).Select(g => g.First()).ToList();
        var present = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);

        // Indented while that stays readable: XmlWriter indents every level in full, so a deep chain would grow with its square.
        var indent = BlowDiagramText.Walk(nodes).All(step => step.Depth <= 16);
        // The declaration says UTF-8, which is how the text is saved; a plain StringWriter would declare UTF-16.
        var text = new Utf8StringWriter();
        using (var xml = XmlWriter.Create(text, new XmlWriterSettings { Indent = indent, OmitXmlDeclaration = false }))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("graphml", Ns);
            Key(xml, "label", "node", "label", "string");
            Key(xml, "type", "node", "type", "string");
            Key(xml, "x", "node", "x", "double");
            Key(xml, "y", "node", "y", "double");
            Key(xml, "width", "node", "width", "double");
            Key(xml, "height", "node", "height", "double");
            Key(xml, "data", "node", "data", "string");
            Key(xml, "elabel", "edge", "label", "string");
            Key(xml, "edata", "edge", "data", "string");

            xml.WriteStartElement("graph", Ns);
            xml.WriteAttributeString("id", "G");
            xml.WriteAttributeString("edgedefault", "directed");

            // An iterative walk: a chain of parents of any depth is written without recursing.
            foreach (var (node, _, step) in BlowDiagramText.Walk(nodes))
            {
                if (step == BlowDiagramText.Step.Exit)
                {
                    xml.WriteEndElement(); // graph
                    xml.WriteEndElement(); // node
                    continue;
                }
                xml.WriteStartElement("node", Ns);
                xml.WriteAttributeString("id", Clean(node.Id));
                if (node.Label is { } label) Data(xml, "label", label);
                if (node.Type != "default") Data(xml, "type", node.Type);
                Data(xml, "x", Number(node.Position.X));
                Data(xml, "y", Number(node.Position.Y));
                if (node.Width is { } w) Data(xml, "width", Number(w));
                if (node.Height is { } h) Data(xml, "height", Number(h));
                JsonData(xml, "data", node.Data);
                // A Blow node has a source and a target port of one name as often as not; GraphML names each port once.
                var portNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var port in node.Ports ?? [])
                {
                    if (string.IsNullOrEmpty(port.Id) || !portNames.Add(port.Id)) continue;
                    xml.WriteStartElement("port", Ns);
                    xml.WriteAttributeString("name", Clean(port.Id));
                    xml.WriteEndElement();
                }
                if (step == BlowDiagramText.Step.Enter)
                {
                    xml.WriteStartElement("graph", Ns);
                    xml.WriteAttributeString("id", Clean(node.Id) + ":");
                    xml.WriteAttributeString("edgedefault", "directed");
                }
                else
                {
                    xml.WriteEndElement();
                }
            }

            foreach (var edge in document.Edges)
            {
                if (!present.Contains(edge.Source) || !present.Contains(edge.Target)) continue;
                xml.WriteStartElement("edge", Ns);
                xml.WriteAttributeString("id", Clean(edge.Id));
                xml.WriteAttributeString("source", Clean(edge.Source));
                xml.WriteAttributeString("target", Clean(edge.Target));
                if (!string.IsNullOrEmpty(edge.SourceHandle)) xml.WriteAttributeString("sourceport", Clean(edge.SourceHandle));
                if (!string.IsNullOrEmpty(edge.TargetHandle)) xml.WriteAttributeString("targetport", Clean(edge.TargetHandle));
                // The graph's default is directed; an edge with no arrowhead says it is not, so it comes back without one.
                if (!edge.MarkerEnd) xml.WriteAttributeString("directed", "false");
                if (edge.Label is { } label) Data(xml, "elabel", label);
                JsonData(xml, "edata", edge.Data);
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        return text.ToString();
    }

    private sealed class Utf8StringWriter() : StringWriter(CultureInfo.InvariantCulture)
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    /// <summary>A payload as JSON data, or a comment saying it was left out when it is not something JSON writes without reflection.</summary>
    private static void JsonData(XmlWriter xml, string key, object? data)
    {
        if (data is null) return;
        if (Json(data) is { } json)
        {
            Data(xml, key, json);
            return;
        }
        // "--" may not appear in a comment.
        var type = Clean(data.GetType().Name).Replace("-", "", StringComparison.Ordinal);
        xml.WriteComment($" {key} not written: a {type} is not JSON, a string, a number or a boolean ");
    }

    private static void Key(XmlWriter xml, string id, string target, string name, string type)
    {
        xml.WriteStartElement("key", Ns);
        xml.WriteAttributeString("id", id);
        xml.WriteAttributeString("for", target);
        xml.WriteAttributeString("attr.name", name);
        xml.WriteAttributeString("attr.type", type);
        xml.WriteEndElement();
    }

    private static void Data(XmlWriter xml, string key, string value)
    {
        xml.WriteStartElement("data", Ns);
        xml.WriteAttributeString("key", key);
        xml.WriteString(Clean(value));
        xml.WriteEndElement();
    }

    // XML 1.0 cannot carry most control characters, lone surrogates or U+FFFE/U+FFFF at all, escaped or not.
    private static string Clean(string value) => BlowDiagramText.XmlSafe(value);

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string? Json(object? data) => data switch
    {
        null => null,
        JsonNode node => node.ToJsonString(),
        JsonElement element => element.GetRawText(),
        string s => JsonValue.Create(s).ToJsonString(),
        bool b => b ? "true" : "false",
        int or long or short or byte or sbyte or ushort or uint or ulong => ((IFormattable)data).ToString(null, CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        double d when double.IsFinite(d) => Number(d),
        float f when float.IsFinite(f) => f.ToString("R", CultureInfo.InvariantCulture),
        _ => null,
    };

    // ---- import ----

    /// <summary>Reads GraphML into a document. Problems are reported, never thrown.</summary>
    public static BlowImportResult Import(string xml, BlowDiagramImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var o = options ?? new BlowDiagramImportOptions();
        var limits = o.Limits;
        if (xml.Length > limits.MaxBytes || Encoding.UTF8.GetByteCount(xml) > limits.MaxBytes)
            return BlowDiagramText.Fail(BlowImportCodes.TooLarge, $"The document is larger than {limits.MaxBytes} bytes.");

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = limits.MaxBytes,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
        };
        var doc = new XmlDocument { XmlResolver = null };
        try
        {
            // Elements nested deeper than any GraphML needs are refused before a tree is built: reading the text of a
            // deep tree recurses once per level.
            var maxDepth = 2 * limits.MaxDepth + 16;
            using (var probe = XmlReader.Create(new StringReader(xml), settings))
            {
                while (probe.Read())
                {
                    if (probe.Depth > maxDepth)
                        return BlowDiagramText.Fail(BlowImportCodes.TooDeep, $"The document nests elements deeper than {maxDepth}.");
                }
            }
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            doc.Load(reader);
        }
        catch (XmlException ex)
        {
            return BlowDiagramText.Fail(BlowImportCodes.InvalidXml, $"The document is not well-formed XML: {ex.Message}");
        }

        if (doc.DocumentElement is not { LocalName: "graphml" } root)
            return BlowDiagramText.Fail(BlowImportCodes.NotADocument, "The document is not GraphML: its root is not <graphml>.");

        return new Reader(limits, new BlowIssueList(o.MaxIssues)).Read(root, o);
    }

    private sealed class Reader(BlowImportLimits limits, BlowIssueList issues)
    {
        private readonly BlowIssueList _issues = issues;
        private readonly List<BlowNode> _nodes = [];
        private readonly List<BlowEdge> _edges = [];
        private readonly HashSet<string> _ids = new(StringComparer.Ordinal);
        private readonly HashSet<string> _edgeIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _keyNames = new(StringComparer.Ordinal);
        private readonly HashSet<string> _placed = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BlowXYPosition> _absolute = new(StringComparer.Ordinal);

        public BlowImportResult Read(XmlElement root, BlowDiagramImportOptions options)
        {
            foreach (var key in root.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "key"))
            {
                var id = key.GetAttribute("id");
                var name = key.GetAttribute("attr.name");
                if (id.Length > 0 && id.Length <= limits.MaxStringLength) _keyNames[id] = Limit(name.Length > 0 ? name : id);
            }

            var graph = root.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName == "graph");
            if (graph is null) return BlowDiagramText.Fail(BlowImportCodes.NotADocument, "The document has no <graph>.");
            if (graph.GetAttribute("edgedefault") == "undirected")
                _issues.Add(new BlowImportIssue(BlowImportCodes.UnknownProperty, BlowValidationSeverity.Info, "graph",
                    "The graph is undirected; its edges are imported pointing from source to target, without arrowheads."));

            ReadGraph(graph, parentId: null, depth: 0, directed: graph.GetAttribute("edgedefault") != "undirected");
            foreach (var edge in _edges)
            {
                if (!_ids.Contains(edge.Source) || !_ids.Contains(edge.Target))
                {
                    _issues.Add(new BlowImportIssue(BlowValidationCodes.MissingEdgeEndpoint, BlowValidationSeverity.Warning, $"edge[@id='{edge.Id}']",
                        $"The edge '{edge.Id}' runs to a node that is not in the document; it is kept, and drawn once the node exists."));
                }
            }
            var document = new BlowDocument { Nodes = _nodes, Edges = _edges };
            ArrangeUnplaced(document, options);
            return new BlowImportResult(document, _issues.Issues, 0);
        }

        private void ReadGraph(XmlElement graph, string? parentId, int depth, bool directed)
        {
            if (depth > limits.MaxDepth)
            {
                _issues.Add(new BlowImportIssue(BlowImportCodes.TooDeep, BlowValidationSeverity.Error, Path(graph), $"Graphs nest deeper than {limits.MaxDepth}."));
                return;
            }
            foreach (var element in graph.ChildNodes.OfType<XmlElement>())
            {
                if (element.LocalName == "node") ReadNode(element, parentId, depth, directed);
                else if (element.LocalName == "edge") ReadEdge(element, directed);
            }
        }

        private void ReadNode(XmlElement element, string? parentId, int depth, bool directed)
        {
            var id = element.GetAttribute("id");
            if (id.Length == 0 || id.Length > limits.MaxStringLength)
            {
                _issues.Add(new BlowImportIssue(BlowImportCodes.MissingProperty, BlowValidationSeverity.Error, Path(element), "A node has no usable id; it is left out."));
                return;
            }
            if (!_ids.Add(id))
            {
                _issues.Add(new BlowImportIssue(BlowErrorCode.DuplicateNodeId, BlowValidationSeverity.Warning, Path(element), $"The node id '{id}' is used again; this node is left out."));
                return;
            }
            if (_nodes.Count >= limits.MaxNodes)
            {
                _issues.Once("nodes", new BlowImportIssue(BlowImportCodes.TooMany, BlowValidationSeverity.Error, Path(element), $"The graph has more than {limits.MaxNodes} nodes; the rest are left out."));
                return;
            }

            var node = new BlowNode { Id = id, ParentId = parentId };
            // Blow's own x and y are relative to the parent; yEd's geometry is in diagram coordinates. Blow's win when both are there.
            double? x = null, y = null, gx = null, gy = null;
            var extra = new JsonObject();
            foreach (var data in element.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "data"))
            {
                var name = _keyNames.GetValueOrDefault(data.GetAttribute("key"), data.GetAttribute("key"));
                var geometry = data.GetElementsByTagName("Geometry", YNs).OfType<XmlElement>().FirstOrDefault();
                if (geometry is not null)
                {
                    gx = Double(geometry.GetAttribute("x")) ?? gx;
                    gy = Double(geometry.GetAttribute("y")) ?? gy;
                    node.Width = Double(geometry.GetAttribute("width")) ?? node.Width;
                    node.Height = Double(geometry.GetAttribute("height")) ?? node.Height;
                    var yLabel = data.GetElementsByTagName("NodeLabel", YNs).OfType<XmlElement>().FirstOrDefault();
                    if (yLabel is not null && node.Label is null) node.Label = Limit(yLabel.InnerText.Trim());
                    continue;
                }
                // Only the model's strings are cut to MaxStringLength; data is kept whole (MaxBytes bounds it) so it still parses.
                var raw = data.InnerText;
                var value = Limit(raw);
                switch (name.ToLowerInvariant())
                {
                    case "label" or "name": node.Label = value; break;
                    case "type": node.Type = value.Length > 0 ? value : "default"; break;
                    case "x": x = Double(value) ?? x; break;
                    case "y": y = Double(value) ?? y; break;
                    case "width": node.Width = Double(value); break;
                    case "height": node.Height = Double(value); break;
                    case "data": node.Data = ParseJson(raw, data) ?? node.Data; break;
                    default: extra[name] = raw; break;
                }
            }
            if (extra.Count > 0) node.Data ??= extra;

            // Where the parent is in diagram coordinates, when that is known.
            BlowXYPosition? origin = parentId is null ? new BlowXYPosition(0, 0) : _absolute.TryGetValue(parentId, out var o) ? o : null;
            if (x is not null || y is not null)
            {
                node.Position = new BlowXYPosition(x ?? 0, y ?? 0);
                _placed.Add(id);
                if (origin is { } at) _absolute[id] = new BlowXYPosition(at.X + node.Position.X, at.Y + node.Position.Y);
            }
            else if (gx is not null || gy is not null)
            {
                // yEd's geometry is in diagram coordinates, while a Blow child is placed relative to its parent.
                var world = new BlowXYPosition(gx ?? 0, gy ?? 0);
                _absolute[id] = world;
                node.Position = origin is { } at ? new BlowXYPosition(world.X - at.X, world.Y - at.Y) : world;
                _placed.Add(id);
            }

            // Each name becomes two ports, a source and a target, so half the limit of names.
            var ports = element.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "port").Select(p => p.GetAttribute("name"))
                .Where(n => n.Length > 0 && n.Length <= limits.MaxStringLength).Distinct(StringComparer.Ordinal)
                .Take(Math.Max(0, limits.MaxPortsPerNode / 2)).ToList();
            if (ports.Count > 0)
            {
                _issues.Once("ports", new BlowImportIssue(BlowImportCodes.UnknownProperty, BlowValidationSeverity.Info, Path(element),
                    "GraphML ports have no direction; each is imported as a source and a target port of the same name."));
                node.Ports = [.. ports.SelectMany(p => new[]
                {
                    new BlowPort { Id = p, Type = BlowHandleType.Target, Label = p },
                    new BlowPort { Id = p, Type = BlowHandleType.Source, Label = p },
                })];
            }
            _nodes.Add(node);

            var nested = element.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName == "graph");
            if (nested is not null)
            {
                if (node.Type == "default") node.Type = "group";
                // A nested graph says its own default direction; one that does not takes the enclosing graph's.
                var nestedDirected = nested.HasAttribute("edgedefault") ? nested.GetAttribute("edgedefault") != "undirected" : directed;
                ReadGraph(nested, id, depth + 1, nestedDirected);
            }
        }

        /// <summary>
        /// Lays out the nodes the file gave no position: all of them when none had one; in a file that places only some,
        /// the rest among themselves, per parent, set below the placed nodes beside them.
        /// </summary>
        private void ArrangeUnplaced(BlowDocument document, BlowDiagramImportOptions options)
        {
            if (_placed.Count == _nodes.Count) return;
            if (_placed.Count == 0)
            {
                BlowDiagramText.Arrange(document, options.Layout, options.GroupPadding);
                return;
            }
            if (options.Layout is null) return;

            var byId = _nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
            bool Unplaced(string? id) => id is not null && byId.ContainsKey(id) && !_placed.Contains(id);

            // Each unplaced node joins the tree of its topmost unplaced ancestor; trees are laid out per placed parent.
            var trees = new Dictionary<string, List<BlowNode>>(StringComparer.Ordinal);
            var roots = new Dictionary<string, List<BlowNode>>(StringComparer.Ordinal);
            foreach (var node in _nodes.Where(n => !_placed.Contains(n.Id)))
            {
                var root = node;
                while (Unplaced(root.ParentId) && root.ParentId != node.Id) root = byId[root.ParentId!];
                var anchor = root.ParentId ?? "";
                if (!trees.TryGetValue(anchor, out var members)) trees[anchor] = members = [];
                members.Add(node);
                if (root == node)
                {
                    if (!roots.TryGetValue(anchor, out var tops)) roots[anchor] = tops = [];
                    tops.Add(node);
                }
            }

            foreach (var (anchor, members) in trees)
            {
                var tops = roots[anchor];
                var parents = tops.ToDictionary(n => n, n => n.ParentId);
                foreach (var top in tops) top.ParentId = null;
                var ids = members.Select(m => m.Id).ToHashSet(StringComparer.Ordinal);
                BlowDiagramText.Arrange(new BlowDocument
                {
                    Nodes = members,
                    Edges = [.. _edges.Where(e => ids.Contains(e.Source) && ids.Contains(e.Target))],
                }, options.Layout, options.GroupPadding);
                foreach (var (top, parent) in parents) top.ParentId = parent;

                var placed = _nodes.Where(n => _placed.Contains(n.Id) && (n.ParentId ?? "") == anchor).ToList();
                var minX = tops.Min(n => n.Position.X);
                var minY = tops.Min(n => n.Position.Y);
                double dx, dy;
                if (placed.Count > 0)
                {
                    dx = placed.Min(n => n.Position.X) - minX;
                    dy = placed.Max(n => n.Position.Y + n.EffectiveSize.Height) + options.GroupPadding * 2 - minY;
                }
                else if (anchor.Length > 0)
                {
                    dx = options.GroupPadding - minX;
                    dy = options.GroupPadding * 1.5 - minY;
                }
                else continue;
                foreach (var top in tops) top.Position = new BlowXYPosition(top.Position.X + dx, top.Position.Y + dy);
            }
        }

        private void ReadEdge(XmlElement element, bool directed)
        {
            var source = element.GetAttribute("source");
            var target = element.GetAttribute("target");
            if (source.Length == 0 || target.Length == 0 || source.Length > limits.MaxStringLength || target.Length > limits.MaxStringLength)
            {
                _issues.Add(new BlowImportIssue(BlowImportCodes.MissingProperty, BlowValidationSeverity.Error, Path(element), "An edge has no source or target; it is left out."));
                return;
            }
            if (_edges.Count >= limits.MaxEdges)
            {
                _issues.Once("edges", new BlowImportIssue(BlowImportCodes.TooMany, BlowValidationSeverity.Error, Path(element), $"The graph has more than {limits.MaxEdges} edges; the rest are left out."));
                return;
            }
            var id = element.GetAttribute("id");
            if (id.Length == 0 || id.Length > limits.MaxStringLength || !_edgeIds.Add(id))
            {
                // A generated id that an explicit one already took is passed over.
                var n = _edges.Count;
                do id = $"e{n++}-{source}-{target}"; while (!_edgeIds.Add(id));
            }
            var isDirected = element.HasAttribute("directed") ? element.GetAttribute("directed") != "false" : directed;
            var edge = new BlowEdge
            {
                Id = id,
                Source = source,
                Target = target,
                SourceHandle = NullIfEmpty(element.GetAttribute("sourceport")),
                TargetHandle = NullIfEmpty(element.GetAttribute("targetport")),
                MarkerEnd = isDirected,
            };
            var extra = new JsonObject();
            foreach (var data in element.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "data"))
            {
                var name = _keyNames.GetValueOrDefault(data.GetAttribute("key"), data.GetAttribute("key"));
                var yLabel = data.GetElementsByTagName("EdgeLabel", YNs).OfType<XmlElement>().FirstOrDefault();
                if (yLabel is not null)
                {
                    edge.Label ??= Limit(yLabel.InnerText.Trim());
                    continue;
                }
                if (data.ChildNodes.OfType<XmlElement>().Any()) continue; // another tool's graphics, not data
                var raw = data.InnerText;
                switch (name.ToLowerInvariant())
                {
                    case "label" or "name": edge.Label = Limit(raw); break;
                    case "data": edge.Data = ParseJson(raw, data) ?? edge.Data; break;
                    default: extra[name] = raw; break;
                }
            }
            if (extra.Count > 0) edge.Data ??= extra;
            _edges.Add(edge);
        }

        private JsonNode? ParseJson(string value, XmlElement where)
        {
            try
            {
                return JsonNode.Parse(value, documentOptions: new JsonDocumentOptions { MaxDepth = limits.MaxDepth });
            }
            catch (JsonException)
            {
                _issues.Add(new BlowImportIssue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, Path(where), "The data is not valid JSON; it is kept as text."));
                return JsonValue.Create(value);
            }
        }

        private string Limit(string value) => value.Length > limits.MaxStringLength ? value[..limits.MaxStringLength] : value;

        private static double? Double(string value)
            => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d) ? d : null;

        private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

        private static string Path(XmlElement element)
        {
            var id = element.GetAttribute("id");
            return id.Length > 0 ? $"{element.LocalName}[@id='{id}']" : element.LocalName;
        }
    }
}
