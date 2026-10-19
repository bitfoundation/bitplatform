using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Geometric editing operations on stroke point data: splitting a stroke where an eraser crossed it and
/// trimming its ends. Used by the segment eraser and by tools that cut strokes.
/// </summary>
public static class BinkStrokeEditor
{
    /// <summary>
    /// Splits a stroke's points into the runs that survive erasing with a circular eraser swept along a path.
    /// Each stored point is classified on its own, so the caller is responsible for supplying points dense enough
    /// that the eraser cannot pass between two of them. <see cref="EraseSegments"/> does that and is what tools
    /// should use; this is the lower level primitive.
    /// </summary>
    /// <param name="points">The stroke's points in local coordinates.</param>
    /// <param name="eraserPath">The eraser positions in the same coordinates (at least one).</param>
    /// <param name="eraserRadius">Eraser radius, already including half the stroke width.</param>
    /// <param name="runs">Receives the surviving index ranges. Cleared first. Empty means the whole stroke is erased.</param>
    /// <returns>True when anything was erased.</returns>
    public static bool SplitByEraser(BinkStrokePoints points, ReadOnlySpan<Vector2> eraserPath, float eraserRadius, List<(int Start, int Length)> runs)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(runs);
        runs.Clear();
        int n = points.Count;
        if (n == 0 || eraserPath.IsEmpty) return false;

        float r2 = eraserRadius * eraserRadius;
        ReadOnlySpan<float> xs = points.X;
        ReadOnlySpan<float> ys = points.Y;

        Span<bool> erased = n <= 1024 ? stackalloc bool[n] : new bool[n];
        bool any = false;
        for (int i = 0; i < n; i++)
        {
            var p = new Vector2(xs[i], ys[i]);
            bool hit = false;
            if (eraserPath.Length == 1)
            {
                hit = (p - eraserPath[0]).LengthSquared() <= r2;
            }
            else
            {
                for (int e = 1; e < eraserPath.Length; e++)
                {
                    if (BinkGeometryMath.DistanceToSegmentSquared(p, eraserPath[e - 1], eraserPath[e]) <= r2) { hit = true; break; }
                }
            }
            erased[i] = hit;
            any |= hit;
        }

        if (!any)
        {
            runs.Add((0, n));
            return false;
        }

        int start = -1;
        for (int i = 0; i < n; i++)
        {
            if (!erased[i])
            {
                if (start < 0) start = i;
            }
            else if (start >= 0)
            {
                AddRun(runs, start, i - start);
                start = -1;
            }
        }
        if (start >= 0) AddRun(runs, start, n - start);
        return true;
    }

    private static void AddRun(List<(int Start, int Length)> runs, int start, int length)
    {
        // A single surviving point between two erased regions would render as a dot; drop it.
        if (length >= 2) runs.Add((start, length));
    }

    /// <summary>
    /// Erases part of a stroke and returns the strokes that remain (0, 1 or more). The originals' brush,
    /// transform, description and metadata are preserved; each fragment gets a new id except the first, which
    /// keeps the original id so undo and collaboration see an edit rather than a delete plus two adds.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <param name="eraserPathDocument">Eraser path in document coordinates.</param>
    /// <param name="eraserRadiusDocument">Eraser radius in document units.</param>
    /// <param name="fragments">Receives the surviving fragments. Cleared first.</param>
    /// <returns>True when the stroke was modified.</returns>
    public static bool EraseSegments(BinkStroke stroke, ReadOnlySpan<Vector2> eraserPathDocument, float eraserRadiusDocument, List<BinkStroke> fragments)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        ArgumentNullException.ThrowIfNull(fragments);
        fragments.Clear();
        if (stroke.Points.Count == 0) return false;

        // Work in the stroke's local coordinates.
        Vector2[]? rented = null;
        ReadOnlySpan<Vector2> path = eraserPathDocument;
        float radius = eraserRadiusDocument;
        if (!stroke.Transform.IsIdentity)
        {
            if (!Matrix3x2.Invert(stroke.Transform, out Matrix3x2 inverse)) return false;
            rented = new Vector2[eraserPathDocument.Length];
            for (int i = 0; i < eraserPathDocument.Length; i++) rented[i] = Vector2.Transform(eraserPathDocument[i], inverse);
            path = rented;
            float scale = MathF.Sqrt(MathF.Abs(stroke.Transform.GetDeterminant()));
            if (scale > BinkGeometryMath.Epsilon) radius /= scale;
        }

        float effectiveRadius = radius + stroke.Brush.HitTestWidth / 2f;

        // The cut is made against the stroke's shape, not against the points that happen to be stored. Committed
        // strokes are simplified, so a straight section is often just two points hundreds of units apart: testing
        // only those would let the eraser pass straight through the gap, and touching one end would leave a single
        // surviving point that is dropped as a dot, deleting the whole stroke. Subdividing the segments the eraser
        // can reach makes the cut land where the ink actually is.
        BinkStrokePoints subject = Subdivide(stroke.Points, path, effectiveRadius);

        var runs = new List<(int Start, int Length)>();
        bool erased = SplitByEraser(subject, path, effectiveRadius, runs);
        GC.KeepAlive(rented);
        if (!erased) return false;

        for (int i = 0; i < runs.Count; i++)
        {
            (int start, int length) = runs[i];
            // The inserted points lie exactly on the original segments, so simplification removes the ones the cut
            // did not need and the fragment is no larger than the piece of stroke it represents.
            BinkStrokePoints slice = subject.Slice(start, length).Simplify(0.02f, 0.01f);
            BinkStroke fragment = i == 0
                ? stroke with { Points = slice }
                : stroke with { Points = slice, Id = Guid.NewGuid(), Version = 0 };
            fragments.Add(fragment);
        }
        return true;
    }

    /// <summary>Largest number of points one segment is split into, so a very long segment cannot explode the cost.</summary>
    private const int MaxSubdivisionsPerSegment = 128;

    /// <summary>
    /// Returns the points with extra samples inserted along the segments the eraser can reach, spaced closely
    /// enough that a crossing cannot fall between two of them. Segments the eraser is nowhere near are left alone,
    /// so an eraser stroke over a dense drawing costs nothing extra.
    /// </summary>
    private static BinkStrokePoints Subdivide(BinkStrokePoints points, ReadOnlySpan<Vector2> eraserPath, float radius)
    {
        int n = points.Count;
        if (n < 2 || radius <= 0) return points;

        BinkRect reach = BinkGeometryMath.Bounds(eraserPath).Inflate(radius);
        float step = MathF.Max(radius * 0.5f, 0.05f);

        // Nothing to do when every segment is already shorter than the step, which is the common case.
        bool needed = false;
        for (int i = 1; i < n && !needed; i++)
        {
            Vector2 a = points.PositionAt(i - 1), b = points.PositionAt(i);
            if (Vector2.Distance(a, b) > step && BinkRect.FromPoints(a, b).Intersects(reach)) needed = true;
        }
        if (!needed) return points;

        var buffer = new BinkStrokePointBuffer(Math.Min(n * 4, 4096));
        for (int i = 0; i < n; i++)
        {
            BinkPoint current = points[i];
            buffer.Add(current);
            if (i == n - 1) break;

            BinkPoint next = points[i + 1];
            Vector2 a = current.Position, b = next.Position;
            float length = Vector2.Distance(a, b);
            if (length <= step || !BinkRect.FromPoints(a, b).Intersects(reach)) continue;

            int parts = Math.Min((int)MathF.Ceiling(length / step), MaxSubdivisionsPerSegment);
            for (int k = 1; k < parts; k++)
            {
                float t = k / (float)parts;
                buffer.Add(Lerp(current, next, t));
            }
        }
        return buffer.ToStrokePoints();
    }

    private static BinkPoint Lerp(in BinkPoint a, in BinkPoint b, float t) => new()
    {
        X = a.X + (b.X - a.X) * t,
        Y = a.Y + (b.Y - a.Y) * t,
        Pressure = a.Pressure + (b.Pressure - a.Pressure) * t,
        Time = a.Time + (b.Time - a.Time) * t,
        TiltX = a.TiltX + (b.TiltX - a.TiltX) * t,
        TiltY = a.TiltY + (b.TiltY - a.TiltY) * t,
        Twist = a.Twist + (b.Twist - a.Twist) * t,
    };

    /// <summary>Returns the stroke with the first <paramref name="count"/> points removed, or null when nothing remains.</summary>
    public static BinkStroke? TrimStart(BinkStroke stroke, int count)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        int remaining = stroke.Points.Count - count;
        if (remaining < 2) return null;
        return stroke with { Points = stroke.Points.Slice(count, remaining) };
    }

    /// <summary>Returns the stroke with the last <paramref name="count"/> points removed, or null when nothing remains.</summary>
    public static BinkStroke? TrimEnd(BinkStroke stroke, int count)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        int remaining = stroke.Points.Count - count;
        if (remaining < 2) return null;
        return stroke with { Points = stroke.Points.Slice(0, remaining) };
    }
}
