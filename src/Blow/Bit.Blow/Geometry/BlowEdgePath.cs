using System.Globalization;
namespace Bit.Blow;

/// <summary>
/// The result of computing an edge path: the SVG path data plus the
/// label anchor point (path midpoint).
/// </summary>
public readonly record struct BlowPathResult(string Path, double LabelX, double LabelY);

/// <summary>
/// Pure functions that compute SVG path data for the built-in edge types.
/// These are C# ports of React Flow's edge path algorithms.
/// </summary>
public static class BlowEdgePath
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Computes the path for an edge of the given type, using the algorithm defaults.</summary>
    /// <param name="type">Which path algorithm to use.</param>
    /// <param name="sx">Source anchor X, in flow space.</param>
    /// <param name="sy">Source anchor Y.</param>
    /// <param name="sourcePos">Which side of the source node the edge leaves from.</param>
    /// <param name="tx">Target anchor X.</param>
    /// <param name="ty">Target anchor Y.</param>
    /// <param name="targetPos">Which side of the target node the edge arrives at.</param>
    public static BlowPathResult Compute(
        BlowEdgeType type,
        double sx, double sy, BlowPosition sourcePos,
        double tx, double ty, BlowPosition targetPos)
        => Compute(type, sx, sy, sourcePos, tx, ty, targetPos, null);

    /// <summary>
    /// Computes the path for an edge, applying any per-edge <see cref="BlowEdgePathOptions"/> on
    /// top of the algorithm defaults.
    /// </summary>
    public static BlowPathResult Compute(
        BlowEdgeType type,
        double sx, double sy, BlowPosition sourcePos,
        double tx, double ty, BlowPosition targetPos,
        BlowEdgePathOptions? options) => type switch
    {
        BlowEdgeType.Straight => Straight(sx, sy, tx, ty),
        BlowEdgeType.Step => SmoothStep(sx, sy, sourcePos, tx, ty, targetPos,
            borderRadius: options?.BorderRadius ?? 0,
            offset: options?.Offset ?? 20,
            stepPosition: options?.StepPosition ?? 0.5),
        BlowEdgeType.SmoothStep => SmoothStep(sx, sy, sourcePos, tx, ty, targetPos,
            borderRadius: options?.BorderRadius ?? 5,
            offset: options?.Offset ?? 20,
            stepPosition: options?.StepPosition ?? 0.5),
        BlowEdgeType.SimpleBezier => SimpleBezier(sx, sy, sourcePos, tx, ty, targetPos),
        _ => Bezier(sx, sy, sourcePos, tx, ty, targetPos, options?.Curvature ?? 0.25),
    };

    /// <summary>
    /// Routes an edge that starts and ends on the same node, looping clear of it. Every ordinary
    /// path algorithm degenerates here: the two anchors sit on one node, so a bezier between them
    /// collapses onto a line straight through the node body and a step edge folds flat. Instead
    /// the loop is drawn by pushing the control points out along each handle's own normal, with a
    /// sideways kick added when that alone would leave all four points on one line.
    /// </summary>
    /// <param name="sx">Source anchor X, in flow space.</param>
    /// <param name="sy">Source anchor Y.</param>
    /// <param name="sourcePos">Which side of the node the edge leaves from.</param>
    /// <param name="tx">Target anchor X.</param>
    /// <param name="ty">Target anchor Y.</param>
    /// <param name="targetPos">Which side of the node the edge comes back to.</param>
    /// <param name="radius">How far the loop stands off the node, in flow units.</param>
    /// <param name="node">
    /// The node's bounds, when known. A loop that has to swing round a side of the node (out of
    /// the bottom and back into the top, say) then clears that whole side rather than a fixed
    /// distance from the handles - otherwise on a wide node most of the loop is drawn behind it.
    /// </param>
    public static BlowPathResult Loop(
        double sx, double sy, BlowPosition sourcePos,
        double tx, double ty, BlowPosition targetPos,
        double radius = DefaultLoopRadius, BlowRect? node = null)
    {
        var r = Math.Max(1, radius);
        var (sdx, sdy) = Dir(sourcePos);
        var (tdx, tdy) = Dir(targetPos);

        var c1x = sx + sdx * r;
        var c1y = sy + sdy * r;
        var c2x = tx + tdx * r;
        var c2y = ty + tdy * r;

        // The handles point along one axis and the anchors are strung out along that same axis,
        // so source, target and both control points are collinear - the curve would be a line
        // laid over the node. A perpendicular kick is what turns it back into a loop. The anchor
        // test works in flow units with a sub-pixel tolerance: measured handle centres that differ
        // by a rounding error are still, to the eye, one above the other.
        var handlesCollinear = Math.Abs(sdx * tdy - sdy * tdx) < Epsilon;
        var anchorsOnHandleAxis = Math.Abs((tx - sx) * sdy - (ty - sy) * sdx) < AnchorTolerance;

        if (handlesCollinear && anchorsOnHandleAxis)
        {
            // Perpendicular to the source handle, rotated so the loop opens the same way whatever
            // side it started from.
            var (px, py) = (-sdy, sdx);

            // Handles facing opposite ways (right out, left back in) want the loop swung round one
            // side of the node, so both controls go the same way. Handles facing the same way have
            // nothing to swing round, so the controls split apart into a teardrop instead.
            var opposed = sdx * tdx + sdy * tdy < 0;
            var sign = opposed ? 1 : -1;

            c1x += px * Kick(px, py);
            c1y += py * Kick(px, py);
            c2x += px * sign * Kick(px * sign, py * sign);
            c2y += py * sign * Kick(px * sign, py * sign);
        }

        var labelX = 0.125 * sx + 0.375 * c1x + 0.375 * c2x + 0.125 * tx;
        var labelY = 0.125 * sy + 0.375 * c1y + 0.375 * c2y + 0.125 * ty;

        return new BlowPathResult(F($"M{sx},{sy} C{c1x},{c1y} {c2x},{c2y} {tx},{ty}"), labelX, labelY);

        // How far to push a control point sideways along (dx, dy). Without the node's bounds,
        // just the radius. With them, far enough that the curve - whose midpoint only reaches
        // three quarters of the way to its control points - clears the node's side by the radius.
        double Kick(double dx, double dy)
        {
            if (node is not { } b) return r;

            var toSide =
                dx < 0 ? Math.Max(sx, tx) - b.Left
                : dx > 0 ? b.Right - Math.Min(sx, tx)
                : dy < 0 ? Math.Max(sy, ty) - b.Top
                : b.Bottom - Math.Min(sy, ty);

            return (Math.Max(0, toSide) + r) / 0.75;
        }
    }

    /// <summary>How far a self-loop stands off its node when no radius is given.</summary>
    public const double DefaultLoopRadius = 60;

    private const double Epsilon = 1e-9;

    /// <summary>Flow units within which two anchors count as lying on one line.</summary>
    private const double AnchorTolerance = 0.5;

    /// <summary>A straight line from the source anchor to the target anchor.</summary>
    public static BlowPathResult Straight(double sx, double sy, double tx, double ty)
    {
        var labelX = (sx + tx) / 2;
        var labelY = (sy + ty) / 2;
        return new BlowPathResult(F($"M {sx},{sy} L {tx},{ty}"), labelX, labelY);
    }

    // ---- Bezier ----

    /// <summary>
    /// A cubic bezier whose control points project out along each handle's own axis, reaching
    /// further the further apart the endpoints are. Port of React Flow's bezier edge.
    /// </summary>
    /// <param name="sx">Source anchor X, in flow space.</param>
    /// <param name="sy">Source anchor Y.</param>
    /// <param name="sourcePos">Which side of the source node the edge leaves from.</param>
    /// <param name="tx">Target anchor X.</param>
    /// <param name="ty">Target anchor Y.</param>
    /// <param name="targetPos">Which side of the target node the edge arrives at.</param>
    /// <param name="curvature">How far the control points reach; 0 collapses the curve to a line.</param>
    public static BlowPathResult Bezier(
        double sx, double sy, BlowPosition sourcePos,
        double tx, double ty, BlowPosition targetPos,
        double curvature = 0.25)
    {
        var (c1x, c1y) = ControlWithCurvature(sourcePos, sx, sy, tx, ty, curvature);
        var (c2x, c2y) = ControlWithCurvature(targetPos, tx, ty, sx, sy, curvature);

        // Cubic bezier midpoint (t = 0.5).
        var labelX = 0.125 * sx + 0.375 * c1x + 0.375 * c2x + 0.125 * tx;
        var labelY = 0.125 * sy + 0.375 * c1y + 0.375 * c2y + 0.125 * ty;

        var path = F($"M{sx},{sy} C{c1x},{c1y} {c2x},{c2y} {tx},{ty}");
        return new BlowPathResult(path, labelX, labelY);
    }

    private static double ControlOffset(double distance, double curvature)
        => distance >= 0 ? 0.5 * distance : curvature * 25 * Math.Sqrt(-distance);

    private static (double x, double y) ControlWithCurvature(
        BlowPosition pos, double x1, double y1, double x2, double y2, double c) => pos switch
    {
        BlowPosition.Left => (x1 - ControlOffset(x1 - x2, c), y1),
        BlowPosition.Right => (x1 + ControlOffset(x2 - x1, c), y1),
        BlowPosition.Top => (x1, y1 - ControlOffset(y1 - y2, c)),
        BlowPosition.Bottom => (x1, y1 + ControlOffset(y2 - y1, c)),
        _ => (x1, y1),
    };

    // ---- SimpleBezier ----

    /// <summary>
    /// A bezier whose control points sit halfway between the endpoints along the handle axis.
    /// Unlike <see cref="Bezier"/> the reach doesn't grow when the handles face away from each
    /// other, so the curve stays flat and predictable. Port of React Flow's simple bezier edge.
    /// </summary>
    public static BlowPathResult SimpleBezier(
        double sx, double sy, BlowPosition sourcePos,
        double tx, double ty, BlowPosition targetPos)
    {
        var (c1x, c1y) = SimpleControl(sourcePos, sx, sy, tx, ty);
        var (c2x, c2y) = SimpleControl(targetPos, tx, ty, sx, sy);

        var labelX = 0.125 * sx + 0.375 * c1x + 0.375 * c2x + 0.125 * tx;
        var labelY = 0.125 * sy + 0.375 * c1y + 0.375 * c2y + 0.125 * ty;

        return new BlowPathResult(F($"M{sx},{sy} C{c1x},{c1y} {c2x},{c2y} {tx},{ty}"), labelX, labelY);
    }

    private static (double x, double y) SimpleControl(
        BlowPosition pos, double x1, double y1, double x2, double y2) => pos switch
    {
        BlowPosition.Left or BlowPosition.Right => (x1 + (x2 - x1) / 2, y1),
        BlowPosition.Top or BlowPosition.Bottom => (x1, y1 + (y2 - y1) / 2),
        _ => (x1, y1),
    };

    // ---- SmoothStep / Step (orthogonal routing with rounded corners) ----

    /// <summary>
    /// Orthogonal routing: the edge leaves each handle straight, then turns to meet the other.
    /// Port of React Flow's smooth-step edge; a <paramref name="borderRadius"/> of 0 gives the
    /// square-cornered <see cref="BlowEdgeType.Step"/> shape.
    /// </summary>
    /// <param name="sx">Source anchor X, in flow space.</param>
    /// <param name="sy">Source anchor Y.</param>
    /// <param name="sourcePos">Which side of the source node the edge leaves from.</param>
    /// <param name="tx">Target anchor X.</param>
    /// <param name="ty">Target anchor Y.</param>
    /// <param name="targetPos">Which side of the target node the edge arrives at.</param>
    /// <param name="borderRadius">Corner rounding, in flow units.</param>
    /// <param name="offset">How far the edge runs straight out of each handle before turning.</param>
    /// <param name="stepPosition">Where the bend sits between source (0) and target (1).</param>
    public static BlowPathResult SmoothStep(
        double sx, double sy, BlowPosition sourcePos,
        double tx, double ty, BlowPosition targetPos,
        double borderRadius = 5, double offset = 20, double stepPosition = 0.5)
    {
        var source = new BlowXYPosition(sx, sy);
        var target = new BlowXYPosition(tx, ty);

        var points = GetStepPoints(source, sourcePos, target, targetPos, offset, stepPosition,
            out var labelX, out var labelY);

        var path = BuildRoundedPath(points, borderRadius);
        return new BlowPathResult(path, labelX, labelY);
    }

    private static (double x, double y) Dir(BlowPosition p) => p switch
    {
        BlowPosition.Left => (-1, 0),
        BlowPosition.Right => (1, 0),
        BlowPosition.Top => (0, -1),
        BlowPosition.Bottom => (0, 1),
        _ => (0, 0),
    };

    private static List<BlowXYPosition> GetStepPoints(
        BlowXYPosition source, BlowPosition sourcePos,
        BlowXYPosition target, BlowPosition targetPos,
        double offset, double stepPosition, out double labelX, out double labelY)
    {
        var (sdx, sdy) = Dir(sourcePos);
        var (tdx, tdy) = Dir(targetPos);

        var sourceGap = new BlowXYPosition(source.X + sdx * offset, source.Y + sdy * offset);
        var targetGap = new BlowXYPosition(target.X + tdx * offset, target.Y + tdy * offset);

        // Primary travel axis is determined by the actual direction between the
        // gapped endpoints (React Flow's getDirection). For Left/Right handles the
        // edge travels along X, for Top/Bottom along Y. currDir is the sign of that
        // travel (+1 toward larger coordinate, -1 toward smaller).
        bool dirAccessorX = sourcePos is BlowPosition.Left or BlowPosition.Right;
        double currDir = dirAccessorX
            ? (sourceGap.X < targetGap.X ? 1 : -1)
            : (sourceGap.Y < targetGap.Y ? 1 : -1);

        double sAcc = dirAccessorX ? sdx : sdy;
        double tAcc = dirAccessorX ? tdx : tdy;

        // Extra distance the gapped endpoints are pulled back by (see the same-side case below).
        double sourceGapOffsetX = 0, sourceGapOffsetY = 0, targetGapOffsetX = 0, targetGapOffsetY = 0;

        var inner = new List<BlowXYPosition>();

        if (sAcc * tAcc == -1)
        {
            // Handles face opposite directions on the travel axis. stepPosition slides the bend
            // along that axis between the source (0) and the target (1); across it the edge's
            // middle stays halfway, which is also where its label goes.
            var t = Math.Clamp(stepPosition, 0, 1);
            var centerX = dirAccessorX
                ? sourceGap.X + (targetGap.X - sourceGap.X) * t
                : (sourceGap.X + targetGap.X) / 2;
            var centerY = dirAccessorX
                ? (sourceGap.Y + targetGap.Y) / 2
                : sourceGap.Y + (targetGap.Y - sourceGap.Y) * t;

            var verticalSplit = new[]
            {
                new BlowXYPosition(centerX, sourceGap.Y),
                new BlowXYPosition(centerX, targetGap.Y),
            };
            var horizontalSplit = new[]
            {
                new BlowXYPosition(sourceGap.X, centerY),
                new BlowXYPosition(targetGap.X, centerY),
            };

            // Choose the split orientation based on whether the handle points the
            // same way as the actual travel direction.
            if (sAcc == currDir)
                inner.AddRange(dirAccessorX ? verticalSplit : horizontalSplit);
            else
                inner.AddRange(dirAccessorX ? horizontalSplit : verticalSplit);

            labelX = centerX;
            labelY = centerY;
        }
        else
        {
            // Same / perpendicular directions: single corner. sourceTarget takes x from the
            // source and y from the target; targetSource the other way round.
            var sourceTarget = new BlowXYPosition(sourceGap.X, targetGap.Y);
            var targetSource = new BlowXYPosition(targetGap.X, sourceGap.Y);

            BlowXYPosition corner = dirAccessorX
                ? (sAcc == currDir ? targetSource : sourceTarget)
                : (sAcc == currDir ? sourceTarget : targetSource);

            if (sourcePos == targetPos)
            {
                // Both handles on the same side and closer together along it than the offset:
                // the corner would land on top of one of the gapped endpoints and the path would
                // double back on itself. Pull that endpoint in so the corner has room.
                var diff = Math.Abs(dirAccessorX ? source.X - target.X : source.Y - target.Y);
                if (diff <= offset)
                {
                    var gapOffset = Math.Min(offset - 1, offset - diff);
                    if (sAcc == currDir)
                    {
                        var sign = (dirAccessorX ? sourceGap.X > source.X : sourceGap.Y > source.Y) ? -1 : 1;
                        if (dirAccessorX) sourceGapOffsetX = sign * gapOffset;
                        else sourceGapOffsetY = sign * gapOffset;
                    }
                    else
                    {
                        var sign = (dirAccessorX ? targetGap.X > target.X : targetGap.Y > target.Y) ? -1 : 1;
                        if (dirAccessorX) targetGapOffsetX = sign * gapOffset;
                        else targetGapOffsetY = sign * gapOffset;
                    }
                }
            }
            else
            {
                // Mixed sides (Right -> Top, say): depending on where the target sits across the
                // travel axis, the corner has to be taken from the other point, or the edge runs
                // through the target node and doubles back into its handle.
                var tAccOpposite = dirAccessorX ? tdy : tdx;
                var isSameDir = sAcc == tAccOpposite;
                var sourceOpposite = dirAccessorX ? sourceGap.Y : sourceGap.X;
                var targetOpposite = dirAccessorX ? targetGap.Y : targetGap.X;
                var sourceGtTarget = sourceOpposite > targetOpposite;
                var sourceLtTarget = sourceOpposite < targetOpposite;

                var flipSourceTarget =
                    (sAcc == 1 && ((!isSameDir && sourceGtTarget) || (isSameDir && sourceLtTarget)))
                    || (sAcc != 1 && ((!isSameDir && sourceLtTarget) || (isSameDir && sourceGtTarget)));

                if (flipSourceTarget) corner = dirAccessorX ? sourceTarget : targetSource;
            }

            inner.Add(corner);

            // The label goes on the longest straight run, where there is room for it.
            var sgx = sourceGap.X + sourceGapOffsetX;
            var sgy = sourceGap.Y + sourceGapOffsetY;
            var tgx = targetGap.X + targetGapOffsetX;
            var tgy = targetGap.Y + targetGapOffsetY;
            var maxXDistance = Math.Max(Math.Abs(sgx - corner.X), Math.Abs(tgx - corner.X));
            var maxYDistance = Math.Max(Math.Abs(sgy - corner.Y), Math.Abs(tgy - corner.Y));

            if (maxXDistance >= maxYDistance)
            {
                labelX = (sgx + tgx) / 2;
                labelY = corner.Y;
            }
            else
            {
                labelX = corner.X;
                labelY = (sgy + tgy) / 2;
            }
        }

        var points = new List<BlowXYPosition>
        {
            source,
            new(sourceGap.X + sourceGapOffsetX, sourceGap.Y + sourceGapOffsetY),
        };
        points.AddRange(inner);
        points.Add(new BlowXYPosition(targetGap.X + targetGapOffsetX, targetGap.Y + targetGapOffsetY));
        points.Add(target);
        return points;
    }

    private static string BuildRoundedPath(IReadOnlyList<BlowXYPosition> points, double radius)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (i > 0 && i < points.Count - 1 && radius > 0)
            {
                sb.Append(Bend(points[i - 1], p, points[i + 1], radius));
            }
            else
            {
                sb.Append(i == 0 ? F($"M{p.X} {p.Y}") : F($"L{p.X} {p.Y}"));
            }
        }
        return sb.ToString();
    }

    private static string Bend(BlowXYPosition a, BlowXYPosition b, BlowXYPosition c, double size)
    {
        var bendSize = Math.Min(Math.Min(Distance(a, b) / 2, Distance(b, c) / 2), size);
        double x = b.X, y = b.Y;

        // Collinear: no bend needed.
        if ((Eq(a.X, x) && Eq(x, c.X)) || (Eq(a.Y, y) && Eq(y, c.Y)))
            return F($"L{x} {y}");

        if (Eq(a.Y, y))
        {
            int xDir = a.X < c.X ? -1 : 1;
            int yDir = a.Y < c.Y ? 1 : -1;
            return F($"L {x + bendSize * xDir} {y}Q {x} {y} {x} {y + bendSize * yDir}");
        }

        int xDir2 = a.X < c.X ? 1 : -1;
        int yDir2 = a.Y < c.Y ? -1 : 1;
        return F($"L {x} {y + bendSize * yDir2}Q {x} {y} {x + bendSize * xDir2} {y}");
    }

    private static double Distance(BlowXYPosition a, BlowXYPosition b)
        => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

    private static bool Eq(double a, double b) => Math.Abs(a - b) < 0.0001;

    private static string F(FormattableString s) => s.ToString(Inv);
}
