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
    /// that the eraser cannot pass between two of them. <see cref="EraseSegments"/> computes the cut from the
    /// segments themselves instead and is what tools should use; this is the lower level primitive.
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
    /// <remarks>
    /// The cut is computed where the eraser's circle meets the ink, in document space: each segment is
    /// intersected with the capsule the eraser sweeps, so the cut lands exactly where the ink is however far apart
    /// the stored points are, and a stretched stroke is measured with the thickness it is drawn with. A fragment
    /// holds the original points it spans plus one new point at each cut end.
    /// </remarks>
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
        if (!TryPrepare(stroke, eraserPathDocument, eraserRadiusDocument, out EraseGeometry geometry, out int n)) return false;

        // A single point is ink only as a dot, erased whole or not at all.
        if (n == 1) return geometry.ReachesDot(geometry.Document(0));

        List<Cut>? cuts = FindCuts(geometry, n);
        if (cuts is null) return false;

        // Walk the stroke from its start, closing a fragment at the start of each cut and opening the next at its
        // end. A cut that runs to the end of a segment opens the next fragment at the following point, so cuts
        // that meet across a point leave nothing between them.
        int startSegment = 0;
        double startT = 0;
        foreach (Cut cut in cuts)
        {
            AddFragment(stroke, geometry, startSegment, startT, cut.Segment, cut.Start, fragments);
            (startSegment, startT) = cut.End >= 1 ? (cut.Segment + 1, 0d) : (cut.Segment, cut.End);
        }
        AddFragment(stroke, geometry, startSegment, startT, n - 1, 0, fragments);
        return true;
    }

    /// <summary>The erased part of one segment, as a range of its parameter.</summary>
    private readonly record struct Cut(int Segment, double Start, double End);

    /// <summary>The stroke and the eraser, in the terms the cut is computed in.</summary>
    private readonly ref struct EraseGeometry
    {
        private readonly BinkStrokePoints _points;
        private readonly Matrix3x2 _transform;
        private readonly Matrix3x2 _inverse;
        private readonly bool _identity;
        private readonly float _determinant;

        public EraseGeometry(BinkStrokePoints points, Matrix3x2 transform, bool identity, Matrix3x2 inverse, float determinant, float halfWidth, float radius, ReadOnlySpan<Vector2> eraser)
        {
            _points = points;
            _transform = transform;
            _identity = identity;
            _inverse = inverse;
            _determinant = determinant;
            HalfWidth = halfWidth;
            Radius = radius;
            Eraser = eraser;
            // The widest the ink can be in the document is its half-width times the transform's largest stretch,
            // which the Frobenius norm bounds from above. It is only used to discard what is out of reach.
            float stretch = identity ? 1f : MathF.Sqrt(transform.M11 * transform.M11 + transform.M12 * transform.M12 + transform.M21 * transform.M21 + transform.M22 * transform.M22);
            Reach = BinkGeometryMath.Bounds(eraser).Inflate(radius + halfWidth * stretch);
            // A piece shorter than half the ink is wide renders as a dot: the crumb an eraser is expected to leave
            // nothing of.
            MinimumLength = MathF.Max(halfWidth * MathF.Sqrt(determinant), 1e-3f);
        }

        /// <summary>Half the ink's width in the stroke's local units.</summary>
        public float HalfWidth { get; }

        /// <summary>The eraser radius in document units.</summary>
        public float Radius { get; }

        /// <summary>The eraser path in document coordinates.</summary>
        public ReadOnlySpan<Vector2> Eraser { get; }

        /// <summary>Everything the eraser can touch, in document coordinates.</summary>
        public BinkRect Reach { get; }

        /// <summary>The shortest fragment worth keeping, in document units.</summary>
        public float MinimumLength { get; }

        /// <summary>A stored point in document coordinates.</summary>
        public Vector2 Document(int index) => DocumentOf(_points.PositionAt(index));

        /// <summary>A local position in document coordinates.</summary>
        public Vector2 DocumentOf(Vector2 local) => _identity ? local : Vector2.Transform(local, _transform);

        /// <summary>
        /// How far the ink reaches either side of a segment in the document. A local segment of half-width h is a
        /// band whose sides stay parallel under the transform, at h times the determinant over the segment's
        /// stretch: an even scale multiplies it by the scale, a stretch along the segment leaves it alone, and a
        /// stretch across it widens it by the full factor.
        /// </summary>
        public float SegmentHalfWidth(int segment, float documentLength)
        {
            if (_identity) return HalfWidth;
            float localLength = Vector2.Distance(_points.PositionAt(segment), _points.PositionAt(segment + 1));
            return HalfWidth * _determinant * localLength / documentLength;
        }

        /// <summary>
        /// True when the eraser meets the ink of a single point, which a transform turns into an ellipse. The ink is
        /// measured along the line towards the eraser's nearest position: never further than it really reaches, so a
        /// dot is erased only by an eraser that touches it.
        /// </summary>
        public bool ReachesDot(Vector2 point)
        {
            Vector2 nearest = Eraser[0];
            float best = Vector2.DistanceSquared(point, nearest);
            for (int e = 1; e < Eraser.Length; e++)
            {
                Vector2 from = Eraser[e - 1], to = Eraser[e];
                Vector2 candidate = Vector2.Lerp(from, to, BinkGeometryMath.ProjectOnSegment(point, from, to));
                float d = Vector2.DistanceSquared(point, candidate);
                if (d < best) { best = d; nearest = candidate; }
            }
            float distance = MathF.Sqrt(best);
            float reach = HalfWidth;
            if (!_identity && distance > BinkGeometryMath.Epsilon)
            {
                float stretch = Vector2.TransformNormal((nearest - point) / distance, _inverse).Length();
                reach = stretch > BinkGeometryMath.Epsilon ? HalfWidth / stretch : 0f;
            }
            return distance <= Radius + reach;
        }
    }

    /// <summary>
    /// Whether the eraser's path, swept with its radius, touches the stroke's ink: true exactly when
    /// <see cref="EraseSegments"/> would change the stroke, decided without computing the cut or the fragments it
    /// leaves, so a stroke eraser can test every stroke under a swipe on each move without allocating for it.
    /// </summary>
    /// <param name="stroke">The stroke.</param>
    /// <param name="eraserPathDocument">Eraser path in document coordinates.</param>
    /// <param name="eraserRadiusDocument">Eraser radius in document units.</param>
    public static bool Reaches(BinkStroke stroke, ReadOnlySpan<Vector2> eraserPathDocument, float eraserRadiusDocument)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        if (!TryPrepare(stroke, eraserPathDocument, eraserRadiusDocument, out EraseGeometry geometry, out int n)) return false;
        if (n == 1) return geometry.ReachesDot(geometry.Document(0));

        // The same walk as FindCuts, stopping at the first touch.
        ReadOnlySpan<Vector2> eraser = geometry.Eraser;
        Vector2 b = geometry.Document(0);
        for (int i = 0; i < n - 1; i++)
        {
            Vector2 a = b;
            b = geometry.Document(i + 1);
            BinkRect box = BinkRect.FromPoints(a, b);
            if (!box.Intersects(geometry.Reach)) continue;

            float length = Vector2.Distance(a, b);
            if (length < BinkGeometryMath.Epsilon)
            {
                if (geometry.ReachesDot(a)) return true;
                continue;
            }
            float reach = geometry.Radius + geometry.SegmentHalfWidth(i, length);
            for (int e = Math.Min(1, eraser.Length - 1); e < eraser.Length; e++)
            {
                Vector2 c = eraser[Math.Max(e - 1, 0)], d = eraser[e];
                if (!BinkRect.FromPoints(c, d).Inflate(reach).Intersects(box)) continue;
                if (IntersectCapsule(a, b, c, d, reach, out _, out _)) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sets up the geometry both the cut and the touch test work in. False when there is nothing to test: an empty
    /// stroke or eraser path, or a stroke transform that cannot be inverted.
    /// </summary>
    private static bool TryPrepare(BinkStroke stroke, ReadOnlySpan<Vector2> eraserPathDocument, float eraserRadiusDocument, out EraseGeometry geometry, out int n)
    {
        geometry = default;
        BinkStrokePoints points = stroke.Points;
        n = points.Count;
        if (n == 0 || eraserPathDocument.IsEmpty) return false;

        // Measured in document space, which is where the eraser is. Working in the stroke's local space instead
        // means turning the eraser's circle into an ellipse whenever the stroke is stretched unevenly (which the
        // selection's side handles do); dividing the radius by one average scale treated it as a circle, missing
        // ink along the stretch and erasing short of it across.
        Matrix3x2 transform = stroke.Transform;
        bool identity = transform.IsIdentity;
        Matrix3x2 inverse = Matrix3x2.Identity;
        if (!identity && !Matrix3x2.Invert(transform, out inverse)) return false;
        float determinant = identity ? 1f : MathF.Abs(transform.GetDeterminant());
        if (!float.IsFinite(determinant) || determinant < BinkGeometryMath.Epsilon) return false;

        geometry = new EraseGeometry(points, transform, identity, inverse, determinant, stroke.Brush.HitTestWidth / 2f, MathF.Max(eraserRadiusDocument, 0f), eraserPathDocument);
        return true;
    }

    /// <summary>
    /// Intersects every segment the eraser can reach with the capsules its path sweeps. Returns the cuts in stroke
    /// order with overlapping ones merged, or null when nothing is erased.
    /// </summary>
    private static List<Cut>? FindCuts(in EraseGeometry geometry, int n)
    {
        List<Cut>? cuts = null;
        List<(double Start, double End)>? ranges = null;
        ReadOnlySpan<Vector2> eraser = geometry.Eraser;
        Vector2 b = geometry.Document(0);
        for (int i = 0; i < n - 1; i++)
        {
            Vector2 a = b;
            b = geometry.Document(i + 1);
            BinkRect box = BinkRect.FromPoints(a, b);
            if (!box.Intersects(geometry.Reach)) continue;

            ranges?.Clear();
            float length = Vector2.Distance(a, b);
            if (length < BinkGeometryMath.Epsilon)
            {
                // A repeated point is ink only as a dot.
                if (geometry.ReachesDot(a)) (ranges ??= new()).Add((0, 1));
            }
            else
            {
                float reach = geometry.Radius + geometry.SegmentHalfWidth(i, length);
                // A path of one position is a disc: the capsule of a segment from that position to itself.
                for (int e = Math.Min(1, eraser.Length - 1); e < eraser.Length; e++)
                {
                    Vector2 c = eraser[Math.Max(e - 1, 0)], d = eraser[e];
                    if (!BinkRect.FromPoints(c, d).Inflate(reach).Intersects(box)) continue;
                    if (IntersectCapsule(a, b, c, d, reach, out double start, out double end)) (ranges ??= new()).Add((start, end));
                }
            }
            if (ranges is not { Count: > 0 }) continue;

            ranges.Sort(static (x, y) => x.Start.CompareTo(y.Start));
            cuts ??= new();
            (double from, double to) = ranges[0];
            for (int r = 1; r < ranges.Count; r++)
            {
                if (ranges[r].Start <= to) { to = Math.Max(to, ranges[r].End); continue; }
                cuts.Add(new Cut(i, from, to));
                (from, to) = ranges[r];
            }
            cuts.Add(new Cut(i, from, to));
        }
        return cuts;
    }

    /// <summary>
    /// Finds the part of the segment a→b within <paramref name="radius"/> of the segment c→d, as a parameter range
    /// of a→b. The region is a capsule, which is convex, so the part is one range: the union of what lies in its two
    /// end discs and in the rectangle between them. Computed in double precision, because squared distances on a
    /// large page lose most of a float's digits.
    /// </summary>
    private static bool IntersectCapsule(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float radius, out double start, out double end)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double r = radius;
        start = double.PositiveInfinity;
        end = double.NegativeInfinity;

        IntersectDisc(c, ref start, ref end);
        if (c != d) IntersectDisc(d, ref start, ref end);

        double ex = d.X - c.X, ey = d.Y - c.Y;
        double span = Math.Sqrt(ex * ex + ey * ey);
        if (span > 1e-9)
        {
            double ux = ex / span, uy = ey / span;
            double ax = a.X - c.X, ay = a.Y - c.Y;
            double low = 0, high = 1;
            // Along the eraser segment: between its ends. Across it: within the radius. Liang-Barsky clipping of
            // the parameter against each pair of parallel edges.
            if (Clip(ax * ux + ay * uy, dx * ux + dy * uy, 0, span, ref low, ref high)
                && Clip(ay * ux - ax * uy, dy * ux - dx * uy, -r, r, ref low, ref high))
            {
                start = Math.Min(start, low);
                end = Math.Max(end, high);
            }
        }

        // Touching at a single parameter erases nothing, and must not split the stroke there either.
        start = Math.Max(start, 0);
        end = Math.Min(end, 1);
        return start < end;

        void IntersectDisc(Vector2 centre, ref double from, ref double to)
        {
            double px = a.X - centre.X, py = a.Y - centre.Y;
            double qa = dx * dx + dy * dy;
            double qb = 2 * (dx * px + dy * py);
            double qc = px * px + py * py - r * r;
            double discriminant = qb * qb - 4 * qa * qc;
            if (discriminant < 0 || qa <= 0) return;
            double root = Math.Sqrt(discriminant);
            double t0 = (-qb - root) / (2 * qa), t1 = (-qb + root) / (2 * qa);
            if (t1 < 0 || t0 > 1) return;
            from = Math.Min(from, t0);
            to = Math.Max(to, t1);
        }
    }

    /// <summary>Narrows [low, high] to where <c>offset + slope * t</c> lies between the bounds. False when nothing is left.</summary>
    private static bool Clip(double offset, double slope, double lower, double upper, ref double low, ref double high)
    {
        if (Math.Abs(slope) < 1e-12) return offset >= lower && offset <= upper;
        double t0 = (lower - offset) / slope, t1 = (upper - offset) / slope;
        if (t0 > t1) (t0, t1) = (t1, t0);
        low = Math.Max(low, t0);
        high = Math.Min(high, t1);
        return low <= high;
    }

    /// <summary>
    /// Adds the piece of the stroke between two positions, each a segment and a parameter along it, as a fragment.
    /// The piece keeps the original points between them, so nothing the cut did not touch moves, and gains one
    /// interpolated point at each end. A piece too short to be more than a dot is dropped.
    /// </summary>
    private static void AddFragment(BinkStroke stroke, in EraseGeometry geometry, int startSegment, double startT, int endSegment, double endT, List<BinkStroke> fragments)
    {
        if (endSegment < startSegment || (endSegment == startSegment && endT <= startT)) return;
        BinkStrokePoints points = stroke.Points;

        // Original points strictly inside the piece; an end that falls exactly on a point is that point.
        int firstInside = startSegment + 1;
        int lastInside = endT > 0 ? endSegment : endSegment - 1;
        int count = 2 + Math.Max(lastInside - firstInside + 1, 0);

        BinkPoint startPoint = At(points, startSegment, (float)startT);
        BinkPoint endPoint = At(points, endSegment, (float)endT);
        float length = 0;
        Vector2 previous = DocumentPosition(geometry, startPoint);
        for (int k = firstInside; k <= lastInside; k++)
        {
            Vector2 next = geometry.Document(k);
            length += Vector2.Distance(previous, next);
            previous = next;
        }
        length += Vector2.Distance(previous, DocumentPosition(geometry, endPoint));
        if (!(length >= geometry.MinimumLength)) return;

        bool tilt = !points.TiltX.IsEmpty, twist = !points.Twist.IsEmpty;
        float[] x = new float[count], y = new float[count], pressure = new float[count], time = new float[count];
        float[]? tiltX = tilt ? new float[count] : null, tiltY = tilt ? new float[count] : null, twistValues = twist ? new float[count] : null;
        int index = 0;
        Put(startPoint);
        for (int k = firstInside; k <= lastInside; k++) Put(points[k]);
        Put(endPoint);

        var slice = new BinkStrokePoints(x, y, pressure, time, tiltX, tiltY, twistValues, count);
        fragments.Add(fragments.Count == 0
            ? stroke with { Points = slice }
            : stroke with { Points = slice, Id = Guid.NewGuid(), Version = 0 });

        void Put(in BinkPoint p)
        {
            x[index] = p.X;
            y[index] = p.Y;
            pressure[index] = p.Pressure;
            time[index] = p.Time;
            if (tiltX is not null) { tiltX[index] = p.TiltX; tiltY![index] = p.TiltY; }
            if (twistValues is not null) twistValues[index] = p.Twist;
            index++;
        }
    }

    private static BinkPoint At(BinkStrokePoints points, int segment, float t)
        => t <= 0 ? points[segment] : Lerp(points[segment], points[segment + 1], t);

    private static Vector2 DocumentPosition(in EraseGeometry geometry, in BinkPoint point) => geometry.DocumentOf(point.Position);

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
