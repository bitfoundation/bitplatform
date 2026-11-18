using System.Text;

namespace Bit.Blow;

/// <summary>Helpers shared by the text diagram formats.</summary>
internal static class BlowDiagramText
{
    /// <summary>A step of a walk through the nodes as a tree of groups.</summary>
    public enum Step
    {
        /// <summary>A node with no children.</summary>
        Leaf,

        /// <summary>A group's start: its children follow.</summary>
        Enter,

        /// <summary>A group's end.</summary>
        Exit,
    }

    /// <summary>
    /// The nodes as a tree, groups opening before their children and closing after them, without recursing: a chain of
    /// parents of any depth is walked with a stack. Each node comes once. A node whose parent is missing, or whose
    /// parents form a cycle, is treated as top-level, so no node is dropped.
    /// </summary>
    public static IEnumerable<(BlowNode Node, int Depth, Step Step)> Walk(IReadOnlyList<BlowNode> nodes)
    {
        var present = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        var children = nodes.Where(n => n.ParentId is not null && present.Contains(n.ParentId) && n.ParentId != n.Id)
            .ToLookup(n => n.ParentId!, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var roots = nodes.Where(n => n.ParentId is null || !present.Contains(n.ParentId) || n.ParentId == n.Id).ToList();

        // Nodes no root leads to are on a parent cycle; each such node not yet reached starts a tree of its own.
        for (var round = 0; round < 2; round++)
        {
            foreach (var root in round == 0 ? roots : nodes)
            {
                if (!seen.Add(root.Id)) continue;
                var stack = new Stack<(BlowNode Node, int Depth, bool Exit)>();
                stack.Push((root, 0, false));
                while (stack.Count > 0)
                {
                    var (node, depth, exit) = stack.Pop();
                    if (exit)
                    {
                        yield return (node, depth, Step.Exit);
                        continue;
                    }
                    var kids = children[node.Id].Where(c => !seen.Contains(c.Id)).ToList();
                    if (kids.Count == 0)
                    {
                        yield return (node, depth, Step.Leaf);
                        continue;
                    }
                    yield return (node, depth, Step.Enter);
                    stack.Push((node, depth, true));
                    for (var i = kids.Count - 1; i >= 0; i--)
                    {
                        seen.Add(kids[i].Id);
                        stack.Push((kids[i], depth + 1, false));
                    }
                }
            }
        }
    }

    /// <summary>Indentation for a nesting depth, capped so deep nesting does not make the text grow with its square.</summary>
    public static string Indent(int depth) => new(' ', 4 * (Math.Min(depth, 16) + 1));

    /// <summary>
    /// Lays out a document that came without positions: groups from the deepest up, each one's children laid out and
    /// the group sized round them, then the nodes at the top level.
    /// </summary>
    public static void Arrange(BlowDocument document, IBlowLayout? layout, double padding)
    {
        if (layout is null || document.Nodes.Count == 0) return;

        var byId = document.Nodes.GroupBy(n => n.Id).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var children = document.Nodes.Where(n => n.ParentId is not null && byId.ContainsKey(n.ParentId))
            .GroupBy(n => n.ParentId!).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        // Each edge goes to the groups holding both its ends, sorted once rather than every edge scanned per group.
        // An id can repeat, so an id maps to every parent a node with it has.
        var parentsOf = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (parent, members) in children)
        {
            foreach (var m in members)
            {
                if (!parentsOf.TryGetValue(m.Id, out var parents)) parentsOf[m.Id] = parents = [];
                if (!parents.Contains(parent, StringComparer.Ordinal)) parents.Add(parent);
            }
        }
        var inner = new Dictionary<string, List<BlowEdge>>(StringComparer.Ordinal);
        foreach (var edge in document.Edges)
        {
            if (!parentsOf.TryGetValue(edge.Source, out var sourceParents) || !parentsOf.TryGetValue(edge.Target, out var targetParents)) continue;
            foreach (var parent in sourceParents)
            {
                if (!targetParents.Contains(parent, StringComparer.Ordinal)) continue;
                if (!inner.TryGetValue(parent, out var list)) inner[parent] = list = [];
                list.Add(edge);
            }
        }

        // Deepest groups first: the walk lists each group's exit after every group inside it.
        var groups = Walk(document.Nodes).Where(s => s.Step == Step.Exit).Select(s => s.Node).ToList();
        foreach (var group in groups)
        {
            var members = children[group.Id];
            layout.Arrange(members, inner.TryGetValue(group.Id, out var edges) ? edges : []);

            var minX = members.Min(m => m.Position.X);
            var minY = members.Min(m => m.Position.Y);
            foreach (var m in members) m.Position = new BlowXYPosition(m.Position.X - minX + padding, m.Position.Y - minY + padding * 1.5);
            group.Width ??= members.Max(m => m.Position.X + m.EffectiveSize.Width) + padding;
            group.Height ??= members.Max(m => m.Position.Y + m.EffectiveSize.Height) + padding;
        }

        var topOf = new Dictionary<string, string>(StringComparer.Ordinal);
        var top = new List<BlowNode>();
        var open = new Stack<string>();
        foreach (var (node, _, step) in Walk(document.Nodes))
        {
            if (step == Step.Exit)
            {
                open.Pop();
                continue;
            }
            var root = open.Count == 0 ? node.Id : open.Last();
            if (open.Count == 0) top.Add(node);
            topOf[node.Id] = root;
            if (step == Step.Enter) open.Push(node.Id);
        }

        // Edges into groups pull the groups into place, as the edges among their children would.
        var lifted = document.Edges
            .Where(e => topOf.ContainsKey(e.Source) && topOf.ContainsKey(e.Target))
            .Select(e => (Source: topOf[e.Source], Target: topOf[e.Target], e.Id))
            .Where(e => e.Source != e.Target)
            .Select(e => new BlowEdge { Id = e.Id, Source = e.Source, Target = e.Target })
            .ToList();
        layout.Arrange(top, lifted);
    }

    public static BlowImportResult Fail(string code, string message)
        => new(null, [new BlowImportIssue(code, BlowValidationSeverity.Error, "$", message)], 0);

    /// <summary>
    /// The text with only what XML 1.0 can carry: control characters other than tab and line breaks, lone surrogates and
    /// the non-characters U+FFFE and U+FFFF are dropped. Also what keeps those out of text formats.
    /// </summary>
    public static string XmlSafe(string value)
    {
        var clean = true;
        for (var i = 0; i < value.Length && clean; i++)
        {
            var c = value[i];
            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) i++;
            else if (!System.Xml.XmlConvert.IsXmlChar(c)) clean = false;
        }
        if (clean) return value;

        var b = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                b.Append(c).Append(value[++i]);
            }
            else if (System.Xml.XmlConvert.IsXmlChar(c))
            {
                b.Append(c);
            }
        }
        return b.ToString();
    }
}
