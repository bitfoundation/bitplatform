using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Blow;

/// <summary>Settings for writing a Mermaid flowchart.</summary>
public sealed class BlowMermaidExportOptions
{
    /// <summary>The flowchart direction: <c>TB</c>, <c>LR</c>, <c>BT</c> or <c>RL</c>. Null takes it from the nodes' handle sides.</summary>
    public string? Direction { get; set; }

    /// <summary>
    /// The shape each node is drawn with, as Mermaid's brackets: <c>"[]"</c> (a box, the default), <c>"()"</c>, <c>"([])"</c>,
    /// <c>"[[]]"</c>, <c>"[()]"</c>, <c>"(())"</c>, <c>"{}"</c>, <c>"{{}}"</c>. Null draws input nodes as stadiums and
    /// the rest as boxes.
    /// </summary>
    public Func<BlowNode, string?>? Shape { get; set; }

    /// <summary>Hidden nodes and edges are left out unless this is true.</summary>
    public bool IncludeHidden { get; set; }
}

/// <summary>
/// Mermaid flowcharts in and out - for documentation, pull requests and chat, which render Mermaid as a picture.
/// Export writes nodes, edges, labels and groups (as subgraphs); import reads the common flowchart syntax and lays out
/// the result, since Mermaid has no positions.
/// </summary>
/// <remarks>
/// <para>
/// Exported text is safe to embed: ids Mermaid cannot take are replaced by generated ones, and labels are quoted with
/// every character Mermaid would read as markup written as an entity.
/// </para>
/// <para>
/// Import is a reader, not an interpreter: <c>click</c>, <c>style</c>, <c>classDef</c> and the like are reported and
/// skipped - nothing in the text can run code or load anything. The input is bounded by
/// <see cref="BlowImportLimits"/> (its size, nodes, edges, statement length and how deep subgraphs nest), its reading
/// takes time linear in its length, and the issues by <see cref="BlowDiagramImportOptions.MaxIssues"/>. What has no
/// counterpart in Blow (shapes, link styles) is reported, not guessed.
/// </para>
/// </remarks>
public static partial class BlowMermaidFormat
{
    private const int RegexTimeoutMs = 1000;

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "end", "graph", "flowchart", "subgraph", "style", "class", "classDef", "click", "linkStyle", "direction", "default",
    };

    /// <summary>Writes the document as a Mermaid flowchart.</summary>
    public static string Export(BlowDocument document, BlowMermaidExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var o = options ?? new BlowMermaidExportOptions();
        var nodes = document.Nodes.Where(n => o.IncludeHidden || !n.Hidden).GroupBy(n => n.Id).Select(g => g.First()).ToList();
        var ids = MermaidIds(nodes);
        var direction = o.Direction is "TB" or "TD" or "LR" or "BT" or "RL" ? o.Direction
            : nodes.Count(n => n.SourcePosition == BlowPosition.Right) * 2 > nodes.Count ? "LR" : "TB";

        var text = new StringBuilder().Append("flowchart ").Append(direction).Append('\n');
        foreach (var (node, depth, step) in BlowDiagramText.Walk(nodes))
        {
            var indent = BlowDiagramText.Indent(depth);
            switch (step)
            {
                case BlowDiagramText.Step.Enter:
                    text.Append(indent).Append("subgraph ").Append(ids[node.Id]).Append(" [\"").Append(Escape(Name(node))).Append("\"]\n");
                    break;
                case BlowDiagramText.Step.Exit:
                    text.Append(indent).Append("end\n");
                    break;
                default:
                    var (open, close) = (o.Shape?.Invoke(node) ?? (node.Type == "input" ? "([])" : "[]")) switch
                    {
                        "()" => ("(", ")"),
                        "([])" => ("([", "])"),
                        "[[]]" => ("[[", "]]"),
                        "[()]" => ("[(", ")]"),
                        "(())" => ("((", "))"),
                        "{}" => ("{", "}"),
                        "{{}}" => ("{{", "}}"),
                        _ => ("[", "]"),
                    };
                    text.Append(indent).Append(ids[node.Id]).Append(open).Append('"').Append(Escape(Name(node))).Append('"').Append(close).Append('\n');
                    break;
            }
        }

        foreach (var edge in document.Edges)
        {
            if ((!o.IncludeHidden && edge.Hidden) || !ids.TryGetValue(edge.Source, out var s) || !ids.TryGetValue(edge.Target, out var t)) continue;
            // Mermaid has no link with a head at its start only: that one is written the other way round, head at the end.
            if (edge.MarkerStart && !edge.MarkerEnd) (s, t) = (t, s);
            var arrow = (edge.Animated, edge.MarkerStart && edge.MarkerEnd, edge.MarkerStart || edge.MarkerEnd) switch
            {
                (true, true, _) => "<-.->",
                (true, false, true) => "-.->",
                (true, false, false) => "-.-",
                (false, true, _) => "<-->",
                (false, false, true) => "-->",
                _ => "---",
            };
            text.Append("    ").Append(s).Append(' ').Append(arrow);
            if (!string.IsNullOrWhiteSpace(edge.Label)) text.Append("|\"").Append(Escape(edge.Label!)).Append("\"|");
            text.Append(' ').Append(t).Append('\n');
        }
        return text.ToString();
    }

    private static Dictionary<string, string> MermaidIds(List<BlowNode> nodes)
    {
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (SafeId().IsMatch(node.Id) && !Reserved.Contains(node.Id) && used.Add(node.Id)) ids[node.Id] = node.Id;
        }
        var next = 0;
        foreach (var node in nodes)
        {
            if (ids.ContainsKey(node.Id)) continue;
            string id;
            do id = $"n{next++}"; while (!used.Add(id));
            ids[node.Id] = id;
        }
        return ids;
    }

    private static string Name(BlowNode node) => string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label!;

    /// <summary>Text inside a quoted Mermaid label: entities for everything Mermaid or HTML would read as markup, no line breaks.</summary>
    private static string Escape(string text)
    {
        var b = new StringBuilder(text.Length);
        foreach (var c in BlowDiagramText.XmlSafe(text))
        {
            b.Append(c switch
            {
                '"' => "#quot;",
                '#' => "#35;",
                '<' => "#lt;",
                '>' => "#gt;",
                '&' => "#amp;",
                '|' => "#124;",
                '`' => "#96;",
                // "%%" starts a comment.
                '%' => "#37;",
                '\r' or '\n' or '\t' => " ",
                _ => c.ToString(),
            });
        }
        return b.ToString();
    }

    private static string Unescape(string text) => BlowDiagramText.XmlSafe(EntityPattern().Replace(text, m => m.Groups[1].Value switch
    {
        "quot" => "\"",
        "amp" => "&",
        "lt" => "<",
        "gt" => ">",
        // Characters only: no surrogate halves, no non-characters, nothing XML or a label could not carry.
        var code when int.TryParse(code, out var n) && n is >= 0x20 and < 0x110000 && (n < 0xD800 || n > 0xDFFF)
                                                     && (n & 0xFFFE) != 0xFFFE && n is < 0xFDD0 or > 0xFDEF => char.ConvertFromUtf32(n),
        _ => m.Value,
    }));

    // ---- import ----

    /// <summary>
    /// Reads a Mermaid flowchart (<c>flowchart</c> or <c>graph</c>): nodes with any bracket shape and quoted or plain
    /// text, chains and <c>&amp;</c> lists of links of every arrow kind with <c>|label|</c> or <c>-- label --&gt;</c>
    /// text, and subgraphs, which become group nodes. Problems are reported, never thrown.
    /// </summary>
    public static BlowImportResult Import(string text, BlowDiagramImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var o = options ?? new BlowDiagramImportOptions();
        var limits = o.Limits;
        if (text.Length > limits.MaxBytes || Encoding.UTF8.GetByteCount(text) > limits.MaxBytes)
            return BlowDiagramText.Fail(BlowImportCodes.TooLarge, $"The text is larger than {limits.MaxBytes} bytes.");

        var reader = new MermaidReader(limits, new BlowIssueList(o.MaxIssues));
        if (!reader.Read(text, out var direction)) return new BlowImportResult(null, reader.Issues.Issues, 0);

        var document = new BlowDocument { Nodes = reader.Nodes, Edges = reader.Edges };
        foreach (var node in document.Nodes)
        {
            node.SourcePosition = direction switch { "LR" => BlowPosition.Right, "RL" => BlowPosition.Left, "BT" => BlowPosition.Top, _ => BlowPosition.Bottom };
            node.TargetPosition = direction switch { "LR" => BlowPosition.Left, "RL" => BlowPosition.Right, "BT" => BlowPosition.Bottom, _ => BlowPosition.Top };
        }
        var layout = o.Layout is BlowLayeredLayoutOptions layered
            ? new BlowLayeredLayoutOptions
            {
                Direction = direction switch
                {
                    "LR" => BlowLayoutDirection.LeftToRight,
                    "RL" => BlowLayoutDirection.RightToLeft,
                    "BT" => BlowLayoutDirection.BottomToTop,
                    _ => BlowLayoutDirection.TopToBottom,
                },
                LayerGap = layered.LayerGap,
                NodeGap = layered.NodeGap,
                CrossingReductionPasses = layered.CrossingReductionPasses,
                AlignHandlesToDirection = false,
                FallbackSize = layered.FallbackSize,
            }
            : o.Layout;
        BlowDiagramText.Arrange(document, layout, o.GroupPadding);
        return new BlowImportResult(document, reader.Issues.Issues, 0);
    }

    private sealed class MermaidReader(BlowImportLimits limits, BlowIssueList issues)
    {
        public readonly List<BlowNode> Nodes = [];
        public readonly List<BlowEdge> Edges = [];
        public BlowIssueList Issues => issues;
        private readonly Dictionary<string, BlowNode> _byId = new(StringComparer.Ordinal);
        private readonly Stack<string> _groups = new();
        private int _line;
        private int _skippedGroups;
        private bool _stopped;

        public bool Read(string text, out string direction)
        {
            direction = "TB";
            var started = false;
            _line = 0;
            foreach (var rawLine in text.Split('\n'))
            {
                _line++;
                if (_stopped) break;
                var raw = rawLine.TrimEnd('\r');
                var comment = CommentStart(raw);
                if (comment >= 0) raw = raw[..comment];
                foreach (var part in SplitStatements(raw))
                {
                    if (_stopped) break;
                    var statement = part.Trim();
                    if (statement.Length == 0) continue;
                    if (statement.Length > limits.MaxStringLength)
                    {
                        Issue(BlowImportCodes.StringTooLong, BlowValidationSeverity.Error, $"The statement is longer than {limits.MaxStringLength} characters; it is left out.");
                        continue;
                    }
                    if (!started)
                    {
                        if (!Header(statement, out direction))
                        {
                            issues.Add(new BlowImportIssue(BlowImportCodes.NotADocument, BlowValidationSeverity.Error, $"line {_line}",
                                "The text is not a Mermaid flowchart: it should start with 'flowchart' or 'graph'."));
                            return false;
                        }
                        started = true;
                        continue;
                    }
                    try
                    {
                        Statement(statement);
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, "The statement took too long to read; it is left out.");
                    }
                }
            }
            if (!started)
            {
                issues.Add(new BlowImportIssue(BlowImportCodes.NotADocument, BlowValidationSeverity.Error, "$", "The text is empty."));
                return false;
            }
            if (_groups.Count > 0) Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, $"{_groups.Count} subgraph(s) were not closed with 'end'.");
            return true;
        }

        private static bool Header(string statement, out string direction)
        {
            direction = "TB";
            var words = statement.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length is 0 or > 2 || !(words[0].Equals("flowchart", StringComparison.OrdinalIgnoreCase) || words[0].Equals("graph", StringComparison.OrdinalIgnoreCase)))
                return false;
            if (words.Length == 1) return true;
            var d = words[1].ToUpperInvariant();
            if (d is not ("TB" or "TD" or "BT" or "LR" or "RL")) return false;
            direction = d == "TD" ? "TB" : d;
            return true;
        }

        private void Statement(string statement)
        {
            var space = statement.IndexOfAny([' ', '\t']);
            var keyword = space < 0 ? statement : statement[..space];
            if (keyword == "end")
            {
                if (_skippedGroups > 0) _skippedGroups--;
                else if (_groups.Count == 0) Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, "'end' closes no subgraph.");
                else _groups.Pop();
                return;
            }
            if (keyword == "subgraph")
            {
                Subgraph(space < 0 ? "" : statement[(space + 1)..].Trim());
                return;
            }
            if (keyword == "direction") return;
            if (keyword is "style" or "classDef" or "class" or "click" or "linkStyle")
            {
                issues.Once(keyword, new BlowImportIssue(BlowImportCodes.UnknownProperty, BlowValidationSeverity.Info, $"line {_line}",
                    keyword == "click"
                        ? "'click' is not imported: nothing in a diagram may run code or open links."
                        : $"'{keyword}' is not imported: Blow styles nodes with classes and its theme."));
                return;
            }

            // A chain: node-list (link node-list)*, read by position so a long one costs time in proportion to its length.
            var at = 0;
            var previous = NodeList(statement, ref at);
            if (previous is null)
            {
                if (!_stopped) Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, $"Not understood: {Truncate(statement)}");
                return;
            }
            while (at < statement.Length && !_stopped)
            {
                var link = LinkPattern().Match(statement, at);
                if (!link.Success)
                {
                    Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, $"Not understood: {Truncate(statement[at..].Trim())}");
                    return;
                }
                at += link.Length;
                var label = link.Groups["pipe"].Success ? Text(link.Groups["pipe"].Value)
                    : link.Groups["mid"].Success ? Text(link.Groups["mid"].Value) : null;
                var arrow = link.Groups["arrow"].Value;
                var next = NodeList(statement, ref at);
                if (next is null)
                {
                    if (!_stopped) Issue(BlowImportCodes.InvalidValue, BlowValidationSeverity.Warning, $"A link leads nowhere: {Truncate(statement)}");
                    return;
                }
                foreach (var s in previous)
                {
                    foreach (var t in next)
                    {
                        if (_stopped) return;
                        Edge(s, t, arrow, label);
                    }
                }
                previous = next;
            }
        }

        /// <summary><c>id</c>, <c>id [title]</c>, <c>id ["title"]</c> or <c>title words</c>, read by hand.</summary>
        private void Subgraph(string rest)
        {
            if (_groups.Count + _skippedGroups >= limits.MaxDepth)
            {
                issues.Once("depth", new BlowImportIssue(BlowImportCodes.TooDeep, BlowValidationSeverity.Error, $"line {_line}",
                    $"Subgraphs nest deeper than {limits.MaxDepth}; the deeper ones are read as part of the one around them."));
                _skippedGroups++;
                return;
            }

            string id, title;
            var bracket = rest.IndexOf('[');
            if (bracket > 0 && rest.EndsWith(']'))
            {
                id = rest[..bracket].Trim();
                title = Text(rest[(bracket + 1)..^1]);
            }
            else if (rest.IndexOfAny([' ', '\t']) >= 0)
            {
                // Title words: the whole text is the title, and the id is made up, as Mermaid does.
                id = "";
                title = Text(rest);
            }
            else
            {
                id = rest;
                title = rest;
            }
            if (id.Length == 0)
            {
                var number = Nodes.Count;
                do id = $"subgraph{number++}"; while (_byId.ContainsKey(id));
            }

            var group = Node(id, title);
            if (group is null) return;
            group.Type = "group";
            _groups.Push(group.Id);
        }

        private List<BlowNode>? NodeList(string statement, ref int at)
        {
            var list = new List<BlowNode>();
            while (true)
            {
                var m = NodePattern().Match(statement, at);
                if (!m.Success) return list.Count == 0 ? null : list;
                at += m.Length;
                var label = m.Groups["shape"].Success ? Text(StripShape(m.Groups["shape"].Value)) : null;
                var node = Node(m.Groups["id"].Value, label);
                if (node is null) return null;
                list.Add(node);

                var amp = AmpersandPattern().Match(statement, at);
                if (!amp.Success) return list;
                at += amp.Length;
            }
        }

        private static string StripShape(string shape)
        {
            foreach (var (open, close) in Shapes)
            {
                if (shape.StartsWith(open, StringComparison.Ordinal) && shape.EndsWith(close, StringComparison.Ordinal) && shape.Length >= open.Length + close.Length)
                    return shape[open.Length..^close.Length];
            }
            return shape;
        }

        private static readonly (string, string)[] Shapes =
        [
            ("([", "])"), ("[[", "]]"), ("[(", ")]"), ("((", "))"), ("{{", "}}"), ("[/", "/]"), ("[\\", "\\]"), ("[/", "\\]"), ("[\\", "/]"),
            ("(", ")"), ("[", "]"), ("{", "}"), (">", "]"),
        ];

        private BlowNode? Node(string id, string? label)
        {
            if (_byId.TryGetValue(id, out var node))
            {
                if (label is not null) node.Label = label;
                // A node named before the subgraph it is listed in joins it, as in Mermaid: one outside every subgraph,
                // or in one around this one, moves in; one already in another subgraph stays in the first. A subgraph
                // named inside itself or a subgraph within it is not moved into it.
                if (_groups.Count > 0 && (node.ParentId is null || _groups.Contains(node.ParentId)) && !Encloses(node.Id, _groups.Peek()))
                    node.ParentId = _groups.Peek();
                return node;
            }
            if (Nodes.Count >= limits.MaxNodes)
            {
                Issue(BlowImportCodes.TooMany, BlowValidationSeverity.Error, $"The diagram has more than {limits.MaxNodes} nodes; the rest are left out.");
                _stopped = true;
                return null;
            }
            node = new BlowNode { Id = id, Label = label ?? id, ParentId = _groups.Count > 0 ? _groups.Peek() : null };
            _byId[id] = node;
            Nodes.Add(node);
            return node;
        }

        /// <summary>
        /// True when the group is the node or inside it, so moving the node into the group would make a loop of parents.
        /// A chain longer than nesting allows counts as one, so a hostile text cannot make each reference walk far.
        /// </summary>
        private bool Encloses(string nodeId, string groupId)
        {
            string? at = groupId;
            for (var steps = 0; at is not null; steps++)
            {
                if (at == nodeId || steps > limits.MaxDepth) return true;
                at = _byId.GetValueOrDefault(at)?.ParentId;
            }
            return false;
        }

        private void Edge(BlowNode source, BlowNode target, string arrow, string? label)
        {
            if (Edges.Count >= limits.MaxEdges)
            {
                Issue(BlowImportCodes.TooMany, BlowValidationSeverity.Error, $"The diagram has more than {limits.MaxEdges} links; the rest are left out.");
                _stopped = true;
                return;
            }
            Edges.Add(new BlowEdge
            {
                Id = $"e{Edges.Count}-{source.Id}-{target.Id}",
                Source = source.Id,
                Target = target.Id,
                Label = label,
                Animated = arrow.Contains('.'),
                MarkerEnd = arrow.EndsWith('>'),
                MarkerStart = arrow.StartsWith('<'),
                StrokeWidth = arrow.Contains('=') ? 3 : 1.5,
            });
            if (arrow.EndsWith('x') || arrow.EndsWith('o'))
                issues.Once("marker", new BlowImportIssue(BlowImportCodes.UnknownProperty, BlowValidationSeverity.Info, $"line {_line}",
                    "Circle and cross arrowheads are imported as plain links."));
        }

        private static string Text(string raw)
        {
            raw = raw.Trim();
            if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"') raw = raw[1..^1];
            raw = BreakPattern().Replace(raw, " ");
            return Unescape(raw).Trim();
        }

        private void Issue(string code, BlowValidationSeverity severity, string message)
            => issues.Add(new BlowImportIssue(code, severity, $"line {_line}", message));

        private static string Truncate(string s) => s.Length > 80 ? s[..80] + "..." : s;

        /// <summary>Where a <c>%%</c> comment starts on the line, outside quotes; -1 when it has none.</summary>
        private static int CommentStart(string line)
        {
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '"') quoted = !quoted;
                else if (!quoted && line[i] == '%' && i + 1 < line.Length && line[i + 1] == '%') return i;
            }
            return -1;
        }

        /// <summary>Splits a line at semicolons outside quotes and brackets.</summary>
        private static IEnumerable<string> SplitStatements(string line)
        {
            var depth = 0;
            var quoted = false;
            var start = 0;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"') quoted = !quoted;
                else if (!quoted && c is '[' or '(' or '{') depth++;
                else if (!quoted && c is ']' or ')' or '}') depth = Math.Max(0, depth - 1);
                else if (!quoted && depth == 0 && c == ';')
                {
                    yield return line[start..i];
                    start = i + 1;
                }
            }
            yield return line[start..];
        }
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.None, RegexTimeoutMs)]
    private static partial Regex SafeId();

    [GeneratedRegex(@"#(quot|amp|lt|gt|\d{1,7});", RegexOptions.None, RegexTimeoutMs)]
    private static partial Regex EntityPattern();

    // An id, then optionally a shape: brackets of any kind holding quoted or plain text without the closing bracket. Every
    // repetition is over a class its neighbours do not share, so a match cannot backtrack more than once per character.
    [GeneratedRegex(@"\G\s*(?<id>[A-Za-z0-9_À-퟿-�]+)(?<shape>\(\[(?:""[^""]*""|[^\]]*)\]\)|\[\[(?:""[^""]*""|[^\]]*)\]\]|\[\((?:""[^""]*""|[^\)]*)\)\]|\(\((?:""[^""]*""|[^\)]*)\)\)|\{\{(?:""[^""]*""|[^\}]*)\}\}|\[[/\\](?:""[^""]*""|[^\]]*)[/\\]\]|\((?:""[^""]*""|[^\)]*)\)|\[(?:""[^""]*""|[^\]]*)\]|\{(?:""[^""]*""|[^\}]*)\}|>(?:""[^""]*""|[^\]]*)\])?(?=\s|$|&|-|=|<|~|\.)", RegexOptions.ExplicitCapture, RegexTimeoutMs)]
    private static partial Regex NodePattern();

    [GeneratedRegex(@"\G\s*&\s*", RegexOptions.None, RegexTimeoutMs)]
    private static partial Regex AmpersandPattern();

    // A link: an arrow with its text between its two halves ("-- text -->"), tried first so the text is not read as a
    // node, or an arrow with its text between pipes after it ("-->|text|"). The text is words separated by runs of
    // white space each followed by a word, so white space before the closing half is matched in one way only.
    [GeneratedRegex(@"\G\s*(?:(?:--|==|-\.)[ \t]*(?<mid>[^-=.>|\s](?:[^>|\s]|[ \t]+(?=[^>|\s]))*)[ \t]*(?<arrow>-{2,}>|={2,}>|\.-+>|-{2,}|={2,}|\.-+)|(?<arrow><?(?:-{2,}|={2,}|-\.+-|~{3})[->xo]?)(?:[ \t]*\|(?<pipe>[^|]*)\|)?)\s*", RegexOptions.ExplicitCapture, RegexTimeoutMs)]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase, RegexTimeoutMs)]
    private static partial Regex BreakPattern();
}
