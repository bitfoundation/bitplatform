using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowSvgExport.ToSvg"/>.</summary>
public sealed class BlowSvgExportOptions
{
    /// <summary>Space around the graph, in flow units.</summary>
    public double Padding { get; set; } = 20;

    /// <summary>The image's accessible name, written as its <c>&lt;title&gt;</c> and <c>aria-label</c>.</summary>
    public string Title { get; set; } = "Flow diagram";

    /// <summary>The canvas's <c>NodeOrigin</c>: which point of a node its position names.</summary>
    public BlowXYPosition NodeOrigin { get; set; } = BlowXYPosition.Zero;

    /// <summary>Background colour; null leaves the image transparent.</summary>
    public string? Background { get; set; } = "#ffffff";

    /// <summary>Node fill, outline and text colours.</summary>
    public string NodeFill { get; set; } = "#ffffff";

    /// <inheritdoc cref="NodeFill"/>
    public string NodeStroke { get; set; } = "#1a192b";

    /// <inheritdoc cref="NodeFill"/>
    public string TextColor { get; set; } = "#222222";

    /// <summary>Fill and outline of nodes of type <c>group</c>.</summary>
    public string GroupFill { get; set; } = "#f3f3f8";

    /// <inheritdoc cref="GroupFill"/>
    public string GroupStroke { get; set; } = "#9aa0b5";

    /// <summary>Edge colour, for edges that name none of their own.</summary>
    public string EdgeStroke { get; set; } = "#b1b1b7";

    /// <summary>Font family and size of labels, in flow units.</summary>
    public string FontFamily { get; set; } = "sans-serif";

    /// <inheritdoc cref="FontFamily"/>
    public double FontSize { get; set; } = 12;

    /// <summary>How <see cref="BlowEdgeType.Routed"/> edges are routed; the defaults when null.</summary>
    public BlowRoutingOptions? Routing { get; set; }
}

/// <summary>
/// Draws a graph as a standalone SVG image - for documentation, a report, a thumbnail, printing - in the built-in
/// look, without a browser: rectangles and labels for the nodes, the same paths and arrowheads the canvas draws for
/// the edges. Hidden nodes and their edges are left out.
/// </summary>
/// <remarks>
/// <para>
/// The image is safe to embed: every label is written as escaped text, never as markup, and a colour is only written
/// when it is a plain colour value (a hex code, a name, <c>rgb()</c>/<c>hsl()</c>); anything else - a <c>url()</c>,
/// an expression - falls back to the default. Node templates and CSS are not reproduced: the image shows the graph's
/// structure and labels, not the HTML of custom nodes.
/// </para>
/// <para>
/// Handles the canvas has measured are where edges attach; without a measurement, an edge attaches at the middle of
/// the node's side, as the canvas does before it has measured. <c>BlowCanvas.ToSvg</c> exports what is on screen.
/// </para>
/// </remarks>
public static class BlowSvgExport
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The graph as an SVG document.</summary>
    public static string ToSvg(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, BlowSvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        var o = options ?? new BlowSvgExportOptions();

        var index = new Dictionary<string, BlowNode>(StringComparer.Ordinal);
        foreach (var n in nodes) index.TryAdd(n.Id, n);
        var rects = new Dictionary<string, BlowRect>(StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            if (!IsShown(n, index)) continue;
            rects[n.Id] = AbsoluteRect(n, index, o.NodeOrigin);
        }

        // Edges first, so they run beneath the nodes as on the canvas; groups beneath their children. An end folded
        // into a collapsed group is drawn to the group, as the canvas draws it.
        var paths = new List<(BlowEdge Edge, BlowPathResult Path)>();
        foreach (var e in edges)
        {
            if (e.Hidden || Shown(e.Source, rects, index) is not { } from || Shown(e.Target, rects, index) is not { } to) continue;
            if ((from.Moved || to.Moved) && from.Id == to.Id) continue; // inside the collapsed group
            var source = index[from.Id];
            var target = index[to.Id];
            var s = rects[from.Id];
            var t = rects[to.Id];
            var (sx, sy, sp) = Anchor(source, s, BlowHandleType.Source, from.Moved ? null : e.SourceHandle, source.SourcePosition);
            var (tx, ty, tp) = Anchor(target, t, BlowHandleType.Target, to.Moved ? null : e.TargetHandle, target.TargetPosition);
            BlowPathResult path;
            if (ReferenceEquals(source, target))
                path = BlowEdgePath.Loop(sx, sy, sp, tx, ty, tp, e.PathOptions?.LoopRadius ?? BlowEdgePath.DefaultLoopRadius, s);
            else if (e.Type == BlowEdgeType.Routed
                     && BlowEdgeRouting.Route(new(sx, sy), sp, new(tx, ty), tp, Obstacles(rects, s, t, source, target, index), o.Routing) is { } routed)
                path = routed;
            else
                path = BlowEdgePath.Compute(e.Type == BlowEdgeType.Routed ? BlowEdgeType.SmoothStep : e.Type, sx, sy, sp, tx, ty, tp, e.PathOptions);
            paths.Add((e, path));
        }

        // The picture's extent: the nodes, and the edges' label points (a loop or a detour can reach past the nodes).
        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        foreach (var r in rects.Values)
        {
            left = Math.Min(left, r.Left);
            top = Math.Min(top, r.Top);
            right = Math.Max(right, r.Right);
            bottom = Math.Max(bottom, r.Bottom);
        }
        // A curve stays inside the box of its control points, so the points a path names bound it - a loop, a detour
        // and an arrowhead's end included.
        foreach (var (_, path) in paths)
        {
            foreach (var (x, y) in PointsOf(path.Path))
            {
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }
        if (rects.Count == 0) left = top = right = bottom = 0;
        left -= o.Padding;
        top -= o.Padding;
        var width = right - left + o.Padding;
        var height = bottom - top + o.Padding;

        var sb = new StringBuilder();
        sb.Append(F($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width:0.##}\" height=\"{height:0.##}\" viewBox=\"{left:0.##} {top:0.##} {width:0.##} {height:0.##}\" role=\"img\" aria-label=\""))
          .Append(Escape(o.Title)).Append("\">\n");
        sb.Append("<title>").Append(Escape(o.Title)).Append("</title>\n");

        var markers = new Dictionary<string, (BlowMarkerType Type, string Paint, double Width, double Height)>(StringComparer.Ordinal);
        string? Marker(BlowEdge e, bool show)
        {
            if (!show) return null;
            var spec = e.ResolveMarker(null);
            var paint = Paint(spec.Color ?? e.Stroke, o.EdgeStroke);
            var id = $"m{markers.Count}";
            foreach (var (key, value) in markers)
            {
                if (value == (spec.Type, paint, spec.Width, spec.Height)) return key;
            }
            markers[id] = (spec.Type, paint, spec.Width, spec.Height);
            return id;
        }

        var edgeMarkup = new StringBuilder();
        foreach (var (e, path) in paths)
        {
            var start = Marker(e, e.MarkerStart);
            var end = Marker(e, e.MarkerEnd);
            edgeMarkup.Append("<path d=\"").Append(Escape(path.Path)).Append("\" fill=\"none\" stroke=\"")
                .Append(Paint(e.Stroke, o.EdgeStroke)).Append(F($"\" stroke-width=\"{e.StrokeWidth:0.##}\""));
            if (start is not null) edgeMarkup.Append(" marker-start=\"url(#").Append(start).Append(")\"");
            if (end is not null) edgeMarkup.Append(" marker-end=\"url(#").Append(end).Append(")\"");
            edgeMarkup.Append("/>\n");
            if (!string.IsNullOrEmpty(e.Label))
            {
                edgeMarkup.Append(F($"<text x=\"{path.LabelX:0.##}\" y=\"{path.LabelY:0.##}\" text-anchor=\"middle\" dominant-baseline=\"central\" fill=\""))
                    .Append(Paint(null, o.TextColor)).Append("\" paint-order=\"stroke\" stroke=\"").Append(Paint(o.Background, "#ffffff"))
                    .Append("\" stroke-width=\"4\">").Append(Escape(e.Label)).Append("</text>\n");
            }
        }

        if (markers.Count > 0)
        {
            sb.Append("<defs>\n");
            foreach (var (id, m) in markers)
            {
                sb.Append("<marker id=\"").Append(id).Append(F($"\" viewBox=\"0 0 12 12\" refX=\"9\" refY=\"6\" markerWidth=\"{m.Width:0.##}\" markerHeight=\"{m.Height:0.##}\" orient=\"auto-start-reverse\">"));
                sb.Append(m.Type == BlowMarkerType.ArrowClosed
                    ? $"<path d=\"M1,1 L9,6 L1,11 z\" fill=\"{m.Paint}\" stroke=\"{m.Paint}\" stroke-width=\"1\"/>"
                    : $"<path d=\"M1,1 L9,6 L1,11\" fill=\"none\" stroke=\"{m.Paint}\" stroke-width=\"1.5\"/>");
                sb.Append("</marker>\n");
            }
            sb.Append("</defs>\n");
        }

        if (o.Background is not null)
            sb.Append(F($"<rect x=\"{left:0.##}\" y=\"{top:0.##}\" width=\"{width:0.##}\" height=\"{height:0.##}\" fill=\"")).Append(Paint(o.Background, "#ffffff")).Append("\"/>\n");

        // Groups first, shallower first, so a child is never drawn beneath its parent.
        var ordered = nodes.Where(n => rects.ContainsKey(n.Id)).OrderBy(n => Depth(n, index)).ToList();
        var groups = new StringBuilder();
        var plain = new StringBuilder();
        foreach (var n in ordered)
        {
            var r = rects[n.Id];
            var isGroup = n.Type == "group";
            var target = isGroup ? groups : plain;
            target.Append(F($"<g><rect x=\"{r.X:0.##}\" y=\"{r.Y:0.##}\" width=\"{r.Width:0.##}\" height=\"{r.Height:0.##}\" rx=\"6\" fill=\""))
                .Append(Paint(null, isGroup ? o.GroupFill : o.NodeFill)).Append("\" stroke=\"").Append(Paint(null, isGroup ? o.GroupStroke : o.NodeStroke))
                .Append("\"/>");
            var label = n.Label ?? n.AriaLabel;
            if (!string.IsNullOrEmpty(label))
            {
                var (x, y, anchor) = isGroup ? (r.X + 8, r.Y + o.FontSize + 4, "start") : (r.X + r.Width / 2, r.Y + r.Height / 2, "middle");
                target.Append(F($"<text x=\"{x:0.##}\" y=\"{y:0.##}\" text-anchor=\"{anchor}\" dominant-baseline=\"central\" fill=\""))
                    .Append(Paint(null, o.TextColor)).Append("\">").Append(Escape(label)).Append("</text>");
            }
            target.Append("</g>\n");
        }

        sb.Append("<g font-family=\"").Append(Escape(o.FontFamily)).Append(F($"\" font-size=\"{o.FontSize:0.##}\">\n"));
        sb.Append(groups).Append(edgeMarkup).Append(plain);
        sb.Append("</g>\n</svg>\n");
        return sb.ToString();
    }

    /// <summary>
    /// The node an edge's end is drawn to: the node itself when it is shown, else the nearest collapsed ancestor that
    /// is shown, with <c>Moved</c> set; null when neither.
    /// </summary>
    private static (string Id, bool Moved)? Shown(string id, Dictionary<string, BlowRect> rects, Dictionary<string, BlowNode> index)
    {
        if (rects.ContainsKey(id)) return (id, false);
        if (!index.TryGetValue(id, out var node)) return null;
        for (var guard = 0; guard < 64 && node.ParentId is { } pid && index.TryGetValue(pid, out var parent); guard++)
        {
            if (parent.Collapsed && rects.ContainsKey(pid)) return (pid, true);
            node = parent;
        }
        return null;
    }

    /// <summary>The coordinate pairs in SVG path data, in order: every point the path names, control points included.</summary>
    private static IEnumerable<(double X, double Y)> PointsOf(string path)
    {
        var numbers = Number.Matches(path);
        for (var i = 0; i + 1 < numbers.Count; i += 2)
            yield return (double.Parse(numbers[i].Value, Inv), double.Parse(numbers[i + 1].Value, Inv));
    }

    private static readonly Regex Number = new(@"-?\d+(\.\d+)?([eE][-+]?\d+)?", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));

    private static bool IsShown(BlowNode node, Dictionary<string, BlowNode> index)
    {
        var current = node;
        for (var guard = 0; guard < 64; guard++)
        {
            if (current.Hidden) return false;
            if (current.ParentId is not { } pid || !index.TryGetValue(pid, out var parent)) return true;
            current = parent;
        }
        return true;
    }

    private static int Depth(BlowNode node, Dictionary<string, BlowNode> index)
    {
        var depth = 0;
        for (var current = node; depth < 64 && current.ParentId is { } pid && index.TryGetValue(pid, out var parent); current = parent) depth++;
        return depth;
    }

    /// <summary>A node's box in flow space: its position point summed up its parents, less its origin.</summary>
    private static BlowRect AbsoluteRect(BlowNode node, Dictionary<string, BlowNode> index, BlowXYPosition nodeOrigin)
    {
        double x = node.Position.X, y = node.Position.Y;
        var seen = new HashSet<string>(StringComparer.Ordinal) { node.Id };
        for (var current = node; current.ParentId is { } pid && index.TryGetValue(pid, out var parent) && seen.Add(pid); current = parent)
        {
            x += parent.Position.X;
            y += parent.Position.Y;
        }
        var size = node.EffectiveSize;
        var origin = node.Origin ?? nodeOrigin;
        return new BlowRect(x - origin.X * size.Width, y - origin.Y * size.Height, size.Width, size.Height);
    }

    private static (double X, double Y, BlowPosition Side) Anchor(BlowNode node, BlowRect rect, BlowHandleType type, string? handleId, BlowPosition fallback)
    {
        if (node.Handles.TryGetValue(BlowHandleBounds.Key(type, handleId), out var h))
            return (rect.X + h.OffsetX, rect.Y + h.OffsetY, h.Position);

        return fallback switch
        {
            BlowPosition.Top => (rect.X + rect.Width / 2, rect.Y, fallback),
            BlowPosition.Bottom => (rect.X + rect.Width / 2, rect.Bottom, fallback),
            BlowPosition.Left => (rect.X, rect.Y + rect.Height / 2, fallback),
            _ => (rect.Right, rect.Y + rect.Height / 2, fallback),
        };
    }

    /// <summary>
    /// What a routed edge goes around, chosen as the canvas chooses it: the shown nodes near its two ends - its own two
    /// included - less the groups either end is inside.
    /// </summary>
    private static List<BlowRect> Obstacles(Dictionary<string, BlowRect> rects, BlowRect source, BlowRect target, BlowNode from,
        BlowNode to, Dictionary<string, BlowNode> index)
    {
        var containers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var end in new[] { from, to })
        {
            for (var current = end; current.ParentId is { } pid && index.TryGetValue(pid, out var parent) && containers.Add(pid); current = parent) { }
        }

        const double Reach = 150;
        var left = Math.Min(source.Left, target.Left) - Reach;
        var top = Math.Min(source.Top, target.Top) - Reach;
        var right = Math.Max(source.Right, target.Right) + Reach;
        var bottom = Math.Max(source.Bottom, target.Bottom) + Reach;

        var result = new List<BlowRect>();
        foreach (var (id, r) in rects)
        {
            if (containers.Contains(id)) continue;
            if (r.Left < right && r.Right > left && r.Top < bottom && r.Bottom > top) result.Add(r);
        }
        return result;
    }

    private static readonly Regex SafeColor = new(
        @"^(#[0-9a-fA-F]{3,8}|[a-zA-Z]{1,30}|(rgb|rgba|hsl|hsla)\(\s*[0-9.,%\s/+-]{1,60}\))$",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));

    /// <summary>A colour written only when it is a plain colour value; anything else gives the fallback.</summary>
    private static string Paint(string? value, string fallback)
    {
        if (value is not null && SafeColor.IsMatch(value.Trim())) return value.Trim();
        return SafeColor.IsMatch(fallback) ? fallback : "#000000";
    }

    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&#39;"); break;
                // What XML 1.0 does not allow at all is dropped rather than breaking the document: control characters,
                // U+FFFE and U+FFFF, and a surrogate without its pair (which no encoder can write either).
                case < ' ' when c is not ('\t' or '\n' or '\r'): break;
                case '\uFFFE' or '\uFFFF': break;
                default:
                    if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        sb.Append(c).Append(text[++i]);
                    }
                    else if (!char.IsSurrogate(c))
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private static string F(FormattableString s) => s.ToString(Inv);
}
