namespace Bit.Blow;

/// <summary>
/// The built-in router: <see cref="BlowEdgeRouting.Orthogonal"/> with these settings. Set as <c>BlowCanvas.Routing</c>
/// to tune it, or set another <see cref="IBlowEdgeRouter"/> there to replace it.
/// </summary>
public sealed class BlowRoutingOptions : IBlowEdgeRouter
{
    /// <summary>The least clearance a route keeps from every obstacle, in flow units.</summary>
    public double Margin { get; set; } = 16;

    /// <summary>How far a route runs straight out of a handle before it may turn, in flow units.</summary>
    public double StubLength { get; set; } = 24;

    /// <summary>
    /// What a turn costs, in flow units of length: the route takes a detour of up to this much rather than one more
    /// bend. Higher gives fewer, longer-way-round bends.
    /// </summary>
    public double BendPenalty { get; set; } = 60;

    /// <summary>The radius of the rounded corners of the drawn path; 0 draws square corners.</summary>
    public double BorderRadius { get; set; } = 6;

    /// <summary>
    /// The most obstacles one route considers. Past it the search space grows with the square of the count, so the
    /// router gives up and the caller falls back to a plain step path.
    /// </summary>
    public int MaxObstacles { get; set; } = 100;

    /// <summary>The edge's orthogonal path round the obstacles, with these settings; see <see cref="BlowEdgeRouting.Route"/>.</summary>
    public BlowPathResult? Route(BlowRouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return BlowEdgeRouting.Route(request.Source, request.SourceSide, request.Target, request.TargetSide, request.Obstacles, this);
    }

    /// <summary>The settings the canvas and the SVG export route with when they are given no router.</summary>
    internal static readonly BlowRoutingOptions Default = new();
}

/// <summary>
/// Routes edges around the nodes in their way: orthogonal paths - horizontal and vertical segments - that leave each
/// handle straight, keep a margin from every obstacle, and prefer few bends. A pure function of points and
/// rectangles, usable without a canvas; the canvas uses it for <see cref="BlowEdgeType.Routed"/> edges.
/// </summary>
/// <remarks>
/// A shortest-path search (A*) over the sparse grid formed by the obstacles' margin lines and the two stubs' lines,
/// with every turn charged <see cref="BlowRoutingOptions.BendPenalty"/>: the classic orthogonal connector router.
/// The grid has about (2k + 4)² points for k obstacles, so pass the obstacles near the edge rather than the whole
/// graph.
/// </remarks>
public static class BlowEdgeRouting
{
    /// <summary>
    /// The corner points of an orthogonal route from <paramref name="source"/> to <paramref name="target"/> that keeps
    /// clear of <paramref name="obstacles"/>, both ends included; or null when there is none (the target walled in,
    /// or more obstacles than <see cref="BlowRoutingOptions.MaxObstacles"/>).
    /// </summary>
    /// <param name="source">Where the route starts: the source handle.</param>
    /// <param name="sourceSide">The side of its node the source handle is on, which the route leaves through.</param>
    /// <param name="target">Where the route ends: the target handle.</param>
    /// <param name="targetSide">The side of its node the target handle is on, which the route arrives through.</param>
    /// <param name="obstacles">
    /// Rectangles to keep clear of - normally the nodes near the edge, its own two nodes included, so the route does
    /// not cut back across them. An obstacle that covers a handle's stub is left out, since the route has to start
    /// there anyway.
    /// </param>
    /// <param name="options">Margins, stub length, bend cost; the defaults when null.</param>
    public static IReadOnlyList<BlowXYPosition>? Orthogonal(BlowXYPosition source, BlowPosition sourceSide,
        BlowXYPosition target, BlowPosition targetSide, IReadOnlyList<BlowRect> obstacles, BlowRoutingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(obstacles);
        var opts = options ?? new BlowRoutingOptions();
        var margin = Math.Max(0, opts.Margin);
        var stub = Math.Max(opts.StubLength, margin + 1);

        var (sdx, sdy) = Direction(sourceSide);
        var (tdx, tdy) = Direction(targetSide);
        var start = new BlowXYPosition(source.X + sdx * stub, source.Y + sdy * stub);
        var goal = new BlowXYPosition(target.X + tdx * stub, target.Y + tdy * stub);

        // Inflated by the margin. An obstacle so close to a handle that the stub ends inside its margin is kept to its
        // own box instead - the route may pass nearer than the margin, but not through it; one the stub ends inside
        // is one no route can avoid, and is left out.
        var rects = new List<BlowRect>(obstacles.Count);
        foreach (var o in obstacles)
        {
            var r = new BlowRect(o.X - margin, o.Y - margin, o.Width + 2 * margin, o.Height + 2 * margin);
            if (StrictlyInside(r, start) || StrictlyInside(r, goal))
            {
                if (StrictlyInside(o, start) || StrictlyInside(o, goal)) continue;
                r = o;
            }
            rects.Add(r);
        }
        if (rects.Count > opts.MaxObstacles) return null;

        // The grid: every line a route could usefully run along.
        var xs = new SortedSet<double> { start.X, goal.X, (start.X + goal.X) / 2 };
        var ys = new SortedSet<double> { start.Y, goal.Y, (start.Y + goal.Y) / 2 };
        foreach (var r in rects)
        {
            xs.Add(r.Left);
            xs.Add(r.Right);
            ys.Add(r.Top);
            ys.Add(r.Bottom);
        }
        var gx = xs.ToArray();
        var gy = ys.ToArray();
        int w = gx.Length, h = gy.Length;

        // Which points are inside an obstacle, and which unit segments of the grid cross one.
        var blocked = new bool[w * h];
        var blockedRight = new bool[w * h]; // the segment from (i, j) to (i + 1, j)
        var blockedDown = new bool[w * h];  // the segment from (i, j) to (i, j + 1)
        foreach (var r in rects)
        {
            int i0 = Array.BinarySearch(gx, r.Left), i1 = Array.BinarySearch(gx, r.Right);
            int j0 = Array.BinarySearch(gy, r.Top), j1 = Array.BinarySearch(gy, r.Bottom);
            for (var i = i0; i <= i1; i++)
            {
                for (var j = j0; j <= j1; j++)
                {
                    var interiorX = i > i0 && i < i1;
                    var interiorY = j > j0 && j < j1;
                    if (interiorX && interiorY) blocked[j * w + i] = true;
                    if (i < i1 && interiorY) blockedRight[j * w + i] = true;
                    if (j < j1 && interiorX) blockedDown[j * w + i] = true;
                }
            }
        }

        var from = Array.BinarySearch(gy, start.Y) * w + Array.BinarySearch(gx, start.X);
        var to = Array.BinarySearch(gy, goal.Y) * w + Array.BinarySearch(gx, goal.X);
        if (blocked[from] || blocked[to]) return null;

        // A* over (point, axis it was reached along). Leaving along the stub's own axis is free; so is arriving along
        // the target stub's.
        var startAxis = sdx != 0 ? 0 : 1;
        var goalAxis = tdx != 0 ? 0 : 1;
        var cost = new double[w * h * 2];
        Array.Fill(cost, double.PositiveInfinity);
        var previous = new int[w * h * 2];
        Array.Fill(previous, -1);
        var queue = new PriorityQueue<int, double>();

        double Heuristic(int p) => Math.Abs(gx[p % w] - goal.X) + Math.Abs(gy[p / w] - goal.Y);

        var first = from * 2 + startAxis;
        cost[first] = 0;
        queue.Enqueue(first, Heuristic(from));
        var reached = -1;
        var best = double.PositiveInfinity;
        while (queue.TryDequeue(out var state, out var priority))
        {
            // Nothing left in the queue can beat the best way in found so far: the heuristic never overestimates.
            if (priority >= best) break;

            var p = state / 2;
            var axis = state % 2;
            if (priority - Heuristic(p) > cost[state] + 1e-9) continue; // a stale entry
            if (p == to)
            {
                // Arriving across the target stub's axis costs one more turn, into it.
                var total = cost[state] + (axis == goalAxis ? 0 : opts.BendPenalty);
                if (total < best)
                {
                    best = total;
                    reached = state;
                }
                continue;
            }

            int i = p % w, j = p / w;
            Step(i + 1, j, 0, i < w - 1 && !blockedRight[p]);
            Step(i - 1, j, 0, i > 0 && !blockedRight[p - 1]);
            Step(i, j + 1, 1, j < h - 1 && !blockedDown[p]);
            Step(i, j - 1, 1, j > 0 && !blockedDown[p - w]);

            void Step(int ni, int nj, int moveAxis, bool open)
            {
                if (!open) return;
                var q = nj * w + ni;
                if (blocked[q]) return;
                var length = Math.Abs(gx[ni] - gx[i]) + Math.Abs(gy[nj] - gy[j]);
                var next = cost[state] + length + (moveAxis == axis ? 0 : opts.BendPenalty);
                var key = q * 2 + moveAxis;
                if (next >= cost[key] - 1e-9) return;
                cost[key] = next;
                previous[key] = state;
                queue.Enqueue(key, next + Heuristic(q));
            }
        }

        if (reached < 0) return null;

        var corners = new List<BlowXYPosition> { target, goal };
        for (var s = previous[reached]; s >= 0; s = previous[s])
        {
            var p = s / 2;
            corners.Add(new BlowXYPosition(gx[p % w], gy[p / w]));
        }
        corners.Add(source);
        corners.Reverse();
        return Simplify(corners);
    }

    /// <summary>
    /// An edge path around the obstacles: <see cref="Orthogonal"/> drawn with rounded corners, its label at half its
    /// length; or null when there is no route, for the caller to fall back to another path.
    /// </summary>
    public static BlowPathResult? Route(BlowXYPosition source, BlowPosition sourceSide, BlowXYPosition target,
        BlowPosition targetSide, IReadOnlyList<BlowRect> obstacles, BlowRoutingOptions? options = null)
    {
        if (Orthogonal(source, sourceSide, target, targetSide, obstacles, options) is not { } points) return null;
        var (labelX, labelY) = HalfwayAlong(points);
        return new BlowPathResult(BlowEdgePath.RoundedPath(points, options?.BorderRadius ?? 6), labelX, labelY);
    }

    private static (double X, double Y) Direction(BlowPosition side) => side switch
    {
        BlowPosition.Left => (-1, 0),
        BlowPosition.Right => (1, 0),
        BlowPosition.Top => (0, -1),
        _ => (0, 1),
    };

    private static bool StrictlyInside(BlowRect r, BlowXYPosition p)
        => p.X > r.Left && p.X < r.Right && p.Y > r.Top && p.Y < r.Bottom;

    /// <summary>The corners alone: repeated points and points in the middle of a straight run dropped.</summary>
    private static List<BlowXYPosition> Simplify(List<BlowXYPosition> points)
    {
        var result = new List<BlowXYPosition>(points.Count);
        foreach (var p in points)
        {
            if (result.Count > 0 && Near(result[^1].X, p.X) && Near(result[^1].Y, p.Y)) continue;
            if (result.Count >= 2)
            {
                var a = result[^2];
                var b = result[^1];
                if ((Near(a.X, b.X) && Near(b.X, p.X)) || (Near(a.Y, b.Y) && Near(b.Y, p.Y)))
                {
                    result[^1] = p;
                    continue;
                }
            }
            result.Add(p);
        }
        return result;
    }

    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-6;

    private static (double X, double Y) HalfwayAlong(IReadOnlyList<BlowXYPosition> points)
    {
        var total = 0.0;
        for (var i = 1; i < points.Count; i++) total += Length(points[i - 1], points[i]);
        var remaining = total / 2;
        for (var i = 1; i < points.Count; i++)
        {
            var length = Length(points[i - 1], points[i]);
            if (remaining <= length && length > 0)
            {
                var t = remaining / length;
                return (points[i - 1].X + (points[i].X - points[i - 1].X) * t, points[i - 1].Y + (points[i].Y - points[i - 1].Y) * t);
            }
            remaining -= length;
        }
        return (points[^1].X, points[^1].Y);

        static double Length(BlowXYPosition a, BlowXYPosition b) => Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);
    }
}
