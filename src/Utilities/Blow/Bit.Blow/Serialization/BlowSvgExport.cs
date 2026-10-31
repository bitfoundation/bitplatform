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

    /// <summary>
    /// What routes <see cref="BlowEdgeType.Routed"/> edges: a <see cref="BlowRoutingOptions"/> or a router of the app's
    /// own; the built-in router with its defaults when null.
    /// </summary>
    public IBlowEdgeRouter? Routing { get; set; }
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
        var nesting = Nesting(nodes, index);
        var rects = new Dictionary<string, BlowRect>(StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            if (!IsShown(n, index)) continue;
            rects[n.Id] = AbsoluteRect(n, nesting[n.Id].Position, o.NodeOrigin);
        }

        // Edges first, so they run beneath the nodes as on the canvas; groups beneath their children. An end folded
        // into a collapsed group is drawn to the group, as the canvas draws it.
        var paths = new List<(BlowEdge Edge, BlowPathResult Path)>();
        Func<BlowNode, bool> shown = n => rects.ContainsKey(n.Id);
        BlowSpatialGrid<(string Id, BlowRect Rect)>? grid = null; // built when the first routed edge asks for it
        foreach (var e in edges)
        {
            if (e.Hidden
                || BlowEdgeLayout.DisplayedEnd(index, e.Source, shown, MaxDepth, out var sourceMoved) is not { } source || !shown(source)
                || BlowEdgeLayout.DisplayedEnd(index, e.Target, shown, MaxDepth, out var targetMoved) is not { } target || !shown(target))
            {
                continue;
            }
            if ((sourceMoved || targetMoved) && ReferenceEquals(source, target)) continue; // inside the collapsed group
            var s = rects[source.Id];
            var t = rects[target.Id];
            var (sx, sy, sp) = Anchor(source, s, BlowHandleType.Source, sourceMoved ? null : e.SourceHandle, source.SourcePosition);
            var (tx, ty, tp) = Anchor(target, t, BlowHandleType.Target, targetMoved ? null : e.TargetHandle, target.TargetPosition);
            BlowPathResult path;
            if (ReferenceEquals(source, target))
                path = BlowEdgePath.Loop(sx, sy, sp, tx, ty, tp, e.PathOptions?.LoopRadius ?? BlowEdgePath.DefaultLoopRadius, s);
            else if (e.Type == BlowEdgeType.Routed
                     && (o.Routing ?? BlowRoutingOptions.Default).Route(
                         new BlowRouteRequest(e, new(sx, sy), sp, new(tx, ty), tp, Obstacles(grid ??= Grid(rects), s, t, source, target, index))) is { Path: not null } routed)
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
        // A curve stays inside the box of its control points, so the points a path names bound it - a loop, a detour,
        // an arrowhead's end, and whatever commands a router of the app's own draws with.
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

        var markers = new Dictionary<(BlowMarkerType Type, string Paint, double Width, double Height), string>();
        string? Marker(BlowEdge e, bool show)
        {
            if (!show) return null;
            var spec = e.ResolveMarker(null);
            var paint = Paint(spec.Color ?? e.Stroke, o.EdgeStroke);
            var key = (spec.Type, paint, spec.Width, spec.Height);
            if (!markers.TryGetValue(key, out var id)) markers[key] = id = $"m{markers.Count}";
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
            foreach (var (m, id) in markers)
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
        var ordered = nodes.Where(n => rects.ContainsKey(n.Id)).OrderBy(n => nesting[n.Id].Depth).ToList();
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

    /// <summary>How deep a chain of parents is followed before it is taken to be a cycle: as deep as the canvas follows it.</summary>
    private const int MaxDepth = BlowCanvas.MaxParentDepth;

    /// <summary>
    /// Points whose box holds an SVG path: every end and control point, in absolute coordinates - relative commands,
    /// <c>H</c> and <c>V</c> resolved - and for an arc the farthest its ellipse can reach from its ends. Reading stops
    /// at anything malformed, keeping what was read.
    /// </summary>
    internal static List<(double X, double Y)> PointsOf(string path)
    {
        var points = new List<(double X, double Y)>();
        var i = 0;
        var command = '\0';
        double x = 0, y = 0, startX = 0, startY = 0;

        // The control point a smooth curve (S, T) reflects: the last cubic's second, or the last quadratic's.
        var previous = '\0';
        double controlX = 0, controlY = 0;

        void Skip()
        {
            while (i < path.Length && (char.IsWhiteSpace(path[i]) || path[i] == ',')) i++;
        }

        bool Number(out double value)
        {
            Skip();
            var start = i;
            if (i < path.Length && path[i] is '+' or '-') i++;
            var digits = 0;
            while (i < path.Length && char.IsAsciiDigit(path[i])) { i++; digits++; }
            if (i < path.Length && path[i] == '.')
            {
                i++;
                while (i < path.Length && char.IsAsciiDigit(path[i])) { i++; digits++; }
            }
            if (digits == 0)
            {
                i = start;
                value = 0;
                return false;
            }
            if (i < path.Length && path[i] is 'e' or 'E')
            {
                var mark = i++;
                if (i < path.Length && path[i] is '+' or '-') i++;
                var exponent = 0;
                while (i < path.Length && char.IsAsciiDigit(path[i])) { i++; exponent++; }
                if (exponent == 0) i = mark;
            }
            return double.TryParse(path.AsSpan(start, i - start), NumberStyles.Float, Inv, out value) && double.IsFinite(value);
        }

        // An arc's flags are single digits that need nothing between them and what follows.
        bool Flag()
        {
            Skip();
            if (i >= path.Length || path[i] is not ('0' or '1')) return false;
            i++;
            return true;
        }

        bool Pair(bool relative, out double px, out double py)
        {
            px = py = 0;
            if (!Number(out var a) || !Number(out var b)) return false;
            (px, py) = relative ? (x + a, y + b) : (a, b);
            return true;
        }

        while (true)
        {
            Skip();
            if (i >= path.Length) break;
            if (char.IsAsciiLetter(path[i]))
            {
                command = path[i++];
                if (command is 'Z' or 'z')
                {
                    (x, y) = (startX, startY);
                    continue;
                }
            }
            else if (command is '\0' or 'Z' or 'z')
            {
                break;
            }

            var relative = char.IsAsciiLetterLower(command);
            var kind = char.ToUpperInvariant(command);
            switch (kind)
            {
                case 'T':
                    // Its control point is the last quadratic's reflected through the current point, or the point.
                    (controlX, controlY) = previous is 'Q' or 'T' ? (2 * x - controlX, 2 * y - controlY) : (x, y);
                    if (!Pair(relative, out var tx, out var ty)) return points;
                    points.Add((controlX, controlY));
                    (x, y) = (tx, ty);
                    points.Add((x, y));
                    break;
                case 'M' or 'L':
                    if (!Pair(relative, out var mx, out var my)) return points;
                    (x, y) = (mx, my);
                    points.Add((x, y));
                    if (char.ToUpperInvariant(command) == 'M')
                    {
                        (startX, startY) = (x, y);
                        command = relative ? 'l' : 'L'; // further pairs after a move are lines
                    }
                    break;
                case 'H':
                    if (!Number(out var h)) return points;
                    x = relative ? x + h : h;
                    points.Add((x, y));
                    break;
                case 'V':
                    if (!Number(out var v)) return points;
                    y = relative ? y + v : v;
                    points.Add((x, y));
                    break;
                case 'C':
                    if (!Pair(relative, out var c1x, out var c1y) || !Pair(relative, out var c2x, out var c2y) || !Pair(relative, out var cx, out var cy))
                        return points;
                    points.Add((c1x, c1y));
                    points.Add((c2x, c2y));
                    (controlX, controlY) = (c2x, c2y);
                    (x, y) = (cx, cy);
                    points.Add((x, y));
                    break;
                case 'S':
                    // Its first control point is the last cubic's second reflected through the current point, or the point.
                    var (s1x, s1y) = previous is 'C' or 'S' ? (2 * x - controlX, 2 * y - controlY) : (x, y);
                    if (!Pair(relative, out var s2x, out var s2y) || !Pair(relative, out var sx, out var sy)) return points;
                    points.Add((s1x, s1y));
                    points.Add((s2x, s2y));
                    (controlX, controlY) = (s2x, s2y);
                    (x, y) = (sx, sy);
                    points.Add((x, y));
                    break;
                case 'Q':
                    if (!Pair(relative, out var qx, out var qy) || !Pair(relative, out var ex, out var ey)) return points;
                    points.Add((qx, qy));
                    (controlX, controlY) = (qx, qy);
                    (x, y) = (ex, ey);
                    points.Add((x, y));
                    break;
                case 'A':
                    if (!Number(out var rx) || !Number(out var ry) || !Number(out _) || !Flag() || !Flag()
                        || !Pair(relative, out var ax, out var ay))
                    {
                        return points;
                    }

                    // The ellipse passes through both ends with radii no smaller than given (scaled up when they
                    // cannot span the ends), so every point of the arc is within its diameter of the start.
                    var reach = 2 * Math.Max(Math.Max(Math.Abs(rx), Math.Abs(ry)), Math.Sqrt((ax - x) * (ax - x) + (ay - y) * (ay - y)) / 2);
                    points.Add((x - reach, y - reach));
                    points.Add((x + reach, y + reach));
                    (x, y) = (ax, ay);
                    points.Add((x, y));
                    break;
                default:
                    return points;
            }
            previous = kind;
        }
        return points;
    }

    private static bool IsShown(BlowNode node, Dictionary<string, BlowNode> index)
    {
        var current = node;
        for (var guard = 0; guard < MaxDepth; guard++)
        {
            if (current.Hidden) return false;
            if (current.ParentId is not { } pid || !index.TryGetValue(pid, out var parent)) return true;
            current = parent;
        }
        return true;
    }

    /// <summary>
    /// Every node's position point summed up its parents, and how deep it is nested, resolved as the canvas resolves
    /// them: in list order, each parent once and remembered, a cycle cut where it is found and the node there taken as
    /// a root, and a chain followed at most <see cref="MaxDepth"/> levels from where its walk began. A node is placed
    /// and stacked where the canvas shows it, and a long chain costs no more than a short one per node.
    /// </summary>
    private static Dictionary<string, (BlowXYPosition Position, int Depth)> Nesting(IReadOnlyList<BlowNode> nodes,
        Dictionary<string, BlowNode> index)
    {
        var resolved = new Dictionary<string, (BlowXYPosition Position, int Depth)>(StringComparer.Ordinal);
        var resolving = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in nodes) Resolve(n, 0);
        return resolved;

        (BlowXYPosition Position, int Depth) Resolve(BlowNode n, int guard)
        {
            if (resolved.TryGetValue(n.Id, out var known)) return known;

            (BlowXYPosition Position, int Depth) result = (n.Position, 0);
            if (guard < MaxDepth && n.ParentId is { } pid && index.TryGetValue(pid, out var parent)
                && !ReferenceEquals(parent, n) && !resolving.Contains(parent.Id))
            {
                resolving.Add(n.Id);
                var (position, depth) = Resolve(parent, guard + 1);
                resolving.Remove(n.Id);
                result = (new BlowXYPosition(position.X + n.Position.X, position.Y + n.Position.Y), depth + 1);
            }
            return resolved[n.Id] = result;
        }
    }

    /// <summary>A node's box in flow space: its position point summed up its parents, less its origin.</summary>
    private static BlowRect AbsoluteRect(BlowNode node, BlowXYPosition position, BlowXYPosition nodeOrigin)
    {
        var size = node.EffectiveSize;
        var origin = node.Origin ?? nodeOrigin;
        return new BlowRect(position.X - origin.X * size.Width, position.Y - origin.Y * size.Height, size.Width, size.Height);
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

    /// <summary>What a routed edge goes around, chosen as the canvas chooses it; see <see cref="BlowEdgeLayout.Obstacles"/>.</summary>
    private static List<BlowRect> Obstacles(BlowSpatialGrid<(string Id, BlowRect Rect)> grid, BlowRect source, BlowRect target,
        BlowNode from, BlowNode to, Dictionary<string, BlowNode> index)
    {
        var area = BlowEdgeLayout.ObstacleArea(source, target);
        return BlowEdgeLayout.Obstacles(grid.Near(area), area, from, to, index, MaxDepth);
    }

    /// <summary>
    /// The shown nodes' boxes bucketed by where they lie, so each routed edge looks only at the nodes near it, as on the
    /// canvas, rather than at every node - in the order of <paramref name="rects"/>, which the obstacles keep.
    /// </summary>
    private static BlowSpatialGrid<(string Id, BlowRect Rect)> Grid(Dictionary<string, BlowRect> rects)
    {
        var grid = new BlowSpatialGrid<(string Id, BlowRect Rect)>();
        foreach (var (id, rect) in rects) grid.Add((id, rect), rect);
        return grid;
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
