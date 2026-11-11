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
        var text = new StringBuilder();
        using (var xml = XmlWriter.Create(text, new XmlWriterSettings { Indent = indent, OmitXmlDeclaration = false, Encoding = Encoding.UTF8 }))
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
                if (Json(node.Data) is { } data) Data(xml, "data", data);
                foreach (var port in node.Ports ?? [])
                {
                    if (string.IsNullOrEmpty(port.Id)) continue;
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
                if (edge.Label is { } label) Data(xml, "elabel", label);
                if (Json(edge.Data) is { } data) Data(xml, "edata", data);
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        // StringBuilder output declares UTF-16; the text is handed on as a string and saved as UTF-8.
        return text.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"", StringComparison.Ordinal);
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
        private bool _anyPosition;

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
            if (!_anyPosition) BlowDiagramText.Arrange(document, options.Layout, options.GroupPadding);
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
            double? x = null, y = null;
            var extra = new JsonObject();
            foreach (var data in element.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "data"))
            {
                var name = _keyNames.GetValueOrDefault(data.GetAttribute("key"), data.GetAttribute("key"));
                var geometry = data.GetElementsByTagName("Geometry", YNs).OfType<XmlElement>().FirstOrDefault();
                if (geometry is not null)
                {
                    x = Double(geometry.GetAttribute("x")) ?? x;
                    y = Double(geometry.GetAttribute("y")) ?? y;
                    node.Width = Double(geometry.GetAttribute("width")) ?? node.Width;
                    node.Height = Double(geometry.GetAttribute("height")) ?? node.Height;
                    var yLabel = data.GetElementsByTagName("NodeLabel", YNs).OfType<XmlElement>().FirstOrDefault();
                    if (yLabel is not null && node.Label is null) node.Label = Limit(yLabel.InnerText.Trim());
                    continue;
                }
                var value = Limit(data.InnerText);
                switch (name.ToLowerInvariant())
                {
                    case "label" or "name": node.Label = value; break;
                    case "type": node.Type = value.Length > 0 ? value : "default"; break;
                    case "x": x = Double(value) ?? x; break;
                    case "y": y = Double(value) ?? y; break;
                    case "width": node.Width = Double(value); break;
                    case "height": node.Height = Double(value); break;
                    case "data": node.Data = ParseJson(value, data) ?? node.Data; break;
                    default: extra[name] = value; break;
                }
            }
            if (extra.Count > 0) node.Data ??= extra;
            if (x is not null || y is not null)
            {
                node.Position = new BlowXYPosition(x ?? 0, y ?? 0);
                _anyPosition = true;
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
                ReadGraph(nested, id, depth + 1, directed);
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
                var value = Limit(data.InnerText);
                switch (name.ToLowerInvariant())
                {
                    case "label" or "name": edge.Label = value; break;
                    case "data": edge.Data = ParseJson(value, data) ?? edge.Data; break;
                    default: extra[name] = value; break;
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
