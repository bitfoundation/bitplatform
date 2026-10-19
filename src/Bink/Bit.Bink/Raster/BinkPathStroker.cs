using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Converts a stroked path into the polygons that cover it, so a rasterizer only needs to fill polygons.
/// Supports butt/round/square caps and miter/round/bevel joins.
/// </summary>
public static class BinkPathStroker
{
    private const float DefaultMiterLimit = 4f;

    /// <summary>
    /// Produces the outline polygons of a stroked path. Each polygon is closed and should be filled with the
    /// non-zero rule; overlapping polygons therefore merge into a single solid stroke.
    /// </summary>
    /// <param name="path">The path to stroke.</param>
    /// <param name="width">Line width.</param>
    /// <param name="cap">Cap style.</param>
    /// <param name="join">Join style.</param>
    /// <param name="tolerance">Flattening tolerance.</param>
    /// <param name="dash">Optional dash pattern.</param>
    /// <param name="output">Receives the polygons. Cleared first.</param>
    public static void Stroke(BinkPathGeometry path, float width, BinkLineCap cap, BinkLineJoin join, float tolerance, float[]? dash, List<Vector2[]> output)
        => Stroke(path, width, cap, join, tolerance, dash, output, bounds: null);

    /// <summary>
    /// Produces the outline polygons of a stroked path, skipping dash work that cannot affect
    /// <paramref name="bounds"/>.
    /// </summary>
    /// <remarks>
    /// A dash pattern costs one polygon per dash, so a tiny dash along a huge path is unbounded work. With
    /// <paramref name="bounds"/> the pattern is only cut where the path can be seen; elsewhere its phase is advanced
    /// arithmetically, so the output depends on the size of the visible area rather than the length of the path.
    /// Whatever the bounds, a path that would still need more than <see cref="MaxDashPieces"/> dashes is stroked
    /// solid, which at that density looks the same.
    /// </remarks>
    /// <param name="path">The path to stroke.</param>
    /// <param name="width">Line width.</param>
    /// <param name="cap">Cap style.</param>
    /// <param name="join">Join style.</param>
    /// <param name="tolerance">Flattening tolerance.</param>
    /// <param name="dash">Optional dash pattern.</param>
    /// <param name="output">Receives the polygons. Cleared first.</param>
    /// <param name="bounds">The area that will be drawn, in the coordinates of the path, or null for all of it.</param>
    public static void Stroke(BinkPathGeometry path, float width, BinkLineCap cap, BinkLineJoin join, float tolerance, float[]? dash, List<Vector2[]> output, BinkRect? bounds)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(output);
        output.Clear();
        if (path.IsEmpty || width <= 0) return;
        float radius = width / 2f;

        // Anything a piece cut at the edge of this area draws - its cap, or the miter spike of a corner just
        // outside it - stays outside the drawn area, so the cut cannot be seen.
        BinkRect? cull = bounds is { IsEmpty: false } b ? b.Inflate(radius * DefaultMiterLimit + 1f) : null;

        var scratch = new List<Vector2>(64);
        List<Vector2[]>? dashed = dash is { Length: > 0 } ? new List<Vector2[]>() : null;
        bool solid = false;
        path.Flatten(tolerance, (memory, closed) =>
        {
            ReadOnlySpan<Vector2> points = memory.Span;
            if (dashed is null)
            {
                StrokePolyline(points, closed, radius, cap, join, output, scratch);
                return;
            }
            // Past the limit the whole path is stroked solid below, so there is nothing more to cut.
            if (!solid) solid = !ApplyDash(points, closed, dash!, cull, dashed);
        });

        if (dashed is null) return;
        if (solid)
        {
            // Too many dashes to be worth drawing one by one. The whole path is stroked solid, sub-paths that were
            // already dashed included, so one path never mixes the two.
            path.Flatten(tolerance, (memory, closed) => StrokePolyline(memory.Span, closed, radius, cap, join, output, scratch));
            return;
        }
        foreach (Vector2[] piece in dashed) StrokePolyline(piece, false, radius, cap, join, output, scratch);
    }

    /// <summary>
    /// Most dashes one stroke may be cut into. Beyond this the dashes are far below what can be told apart from a
    /// solid line, and drawing them one by one would only cost memory and time.
    /// </summary>
    public const int MaxDashPieces = 100_000;

    private static void StrokePolyline(ReadOnlySpan<Vector2> raw, bool closed, float radius, BinkLineCap cap, BinkLineJoin join, List<Vector2[]> output, List<Vector2> scratch)
    {
        int first = output.Count;
        StrokePolylineCore(raw, closed, radius, cap, join, output, scratch);
        UnifyWinding(output, first);
    }

    /// <summary>
    /// Gives every polygon from <paramref name="first"/> on the same winding as the segment quads.
    /// </summary>
    /// <remarks>
    /// The pieces are merged by the non-zero fill rule, which only adds them up when they all wind the same way.
    /// A join or cap circle wound the opposite way cancels the one quad it overlaps and leaves a hole in the stroke,
    /// which is what a round join on a nearly straight run of points produced.
    /// </remarks>
    private static void UnifyWinding(List<Vector2[]> output, int first)
    {
        for (int i = first; i < output.Count; i++)
        {
            Vector2[] polygon = output[i];
            float doubleArea = 0f;
            for (int j = 0, k = polygon.Length - 1; j < polygon.Length; k = j++) doubleArea += BinkGeometryMath.Cross(polygon[k], polygon[j]);
            if (doubleArea > 0f) Array.Reverse(polygon);
        }
    }

    private static void StrokePolylineCore(ReadOnlySpan<Vector2> raw, bool closed, float radius, BinkLineCap cap, BinkLineJoin join, List<Vector2[]> output, List<Vector2> scratch)
    {
        // Drop repeated points: zero-length segments have no direction and would produce NaN normals.
        scratch.Clear();
        foreach (Vector2 p in raw)
        {
            if (scratch.Count == 0 || Vector2.DistanceSquared(scratch[^1], p) > BinkGeometryMath.Epsilon * BinkGeometryMath.Epsilon) scratch.Add(p);
        }
        if (closed && scratch.Count > 1 && Vector2.DistanceSquared(scratch[0], scratch[^1]) <= BinkGeometryMath.Epsilon * BinkGeometryMath.Epsilon) scratch.RemoveAt(scratch.Count - 1);

        int n = scratch.Count;
        if (n == 0) return;
        if (n == 1)
        {
            // A lone point renders as a dot for round and square caps, and as nothing for butt caps.
            if (cap == BinkLineCap.Round) output.Add(CreateCircle(scratch[0], radius));
            else if (cap == BinkLineCap.Square) output.Add(CreateSquare(scratch[0], radius));
            return;
        }

        // Each segment contributes a quad; each join and cap contributes its own polygon. Filling them all with
        // the non-zero rule merges them into one shape, which avoids the self-intersection problems of a single
        // offset outline on tight curves.
        for (int i = 0; i < n - 1; i++) output.Add(CreateSegmentQuad(scratch[i], scratch[i + 1], radius));
        if (closed) output.Add(CreateSegmentQuad(scratch[n - 1], scratch[0], radius));

        int jointStart = closed ? 0 : 1;
        int jointEnd = closed ? n : n - 1;
        for (int i = jointStart; i < jointEnd; i++)
        {
            Vector2 previous = scratch[(i - 1 + n) % n];
            Vector2 current = scratch[i];
            Vector2 next = scratch[(i + 1) % n];
            AddJoin(output, previous, current, next, radius, join);
        }

        if (!closed)
        {
            AddCap(output, scratch[0], BinkGeometryMath.SafeNormalize(scratch[0] - scratch[1]), radius, cap);
            AddCap(output, scratch[n - 1], BinkGeometryMath.SafeNormalize(scratch[n - 1] - scratch[n - 2]), radius, cap);
        }
    }

    private static Vector2[] CreateSegmentQuad(Vector2 a, Vector2 b, float radius)
    {
        Vector2 normal = BinkGeometryMath.Perpendicular(BinkGeometryMath.SafeNormalize(b - a)) * radius;
        return [a + normal, b + normal, b - normal, a - normal];
    }

    private static void AddJoin(List<Vector2[]> output, Vector2 previous, Vector2 current, Vector2 next, float radius, BinkLineJoin join)
    {
        Vector2 dirIn = BinkGeometryMath.SafeNormalize(current - previous);
        Vector2 dirOut = BinkGeometryMath.SafeNormalize(next - current);
        if (dirIn == Vector2.Zero || dirOut == Vector2.Zero) return;
        float cross = BinkGeometryMath.Cross(dirIn, dirOut);
        // Collinear and continuing: the segment quads already meet flush. A path that doubles back on itself is
        // collinear too, but its turn-around point still needs the round join, as canvas and SVG draw it.
        if (MathF.Abs(cross) < 1e-6f && (join != BinkLineJoin.Round || Vector2.Dot(dirIn, dirOut) > 0f)) return;

        switch (join)
        {
            case BinkLineJoin.Round:
                output.Add(CreateCircle(current, radius));
                break;
            case BinkLineJoin.Bevel:
                AddBevel(output, current, dirIn, dirOut, radius, cross);
                break;
            default:
            {
                // Miter: extend to the intersection unless the corner is so sharp that the spike exceeds the limit.
                float sign = cross > 0 ? -1f : 1f;
                Vector2 nIn = BinkGeometryMath.Perpendicular(dirIn) * (radius * sign);
                Vector2 nOut = BinkGeometryMath.Perpendicular(dirOut) * (radius * sign);
                Vector2 bisector = BinkGeometryMath.SafeNormalize(nIn + nOut);
                float cosHalf = Vector2.Dot(bisector, BinkGeometryMath.SafeNormalize(nIn));
                if (bisector == Vector2.Zero || cosHalf < 1e-4f || 1f / cosHalf > DefaultMiterLimit)
                {
                    AddBevel(output, current, dirIn, dirOut, radius, cross);
                    return;
                }
                Vector2 tip = current + bisector * (radius / cosHalf);
                output.Add([current, current + nIn, tip, current + nOut]);
                break;
            }
        }
    }

    private static void AddBevel(List<Vector2[]> output, Vector2 current, Vector2 dirIn, Vector2 dirOut, float radius, float cross)
    {
        float sign = cross > 0 ? -1f : 1f;
        Vector2 nIn = BinkGeometryMath.Perpendicular(dirIn) * (radius * sign);
        Vector2 nOut = BinkGeometryMath.Perpendicular(dirOut) * (radius * sign);
        output.Add([current, current + nIn, current + nOut]);
    }

    private static void AddCap(List<Vector2[]> output, Vector2 point, Vector2 outward, float radius, BinkLineCap cap)
    {
        switch (cap)
        {
            case BinkLineCap.Round:
                output.Add(CreateCircle(point, radius));
                break;
            case BinkLineCap.Square:
            {
                if (outward == Vector2.Zero) { output.Add(CreateSquare(point, radius)); return; }
                Vector2 normal = BinkGeometryMath.Perpendicular(outward) * radius;
                Vector2 extend = outward * radius;
                output.Add([point + normal, point + normal + extend, point - normal + extend, point - normal]);
                break;
            }
        }
    }

    private static Vector2[] CreateCircle(Vector2 center, float radius)
    {
        int steps = Math.Clamp((int)MathF.Ceiling(MathF.PI / MathF.Acos(MathF.Max(1f - 0.2f / MathF.Max(radius, 0.2f), -1f + 1e-6f))), 8, 64);
        var points = new Vector2[steps];
        for (int i = 0; i < steps; i++)
        {
            float a = i / (float)steps * MathF.Tau;
            points[i] = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
        }
        return points;
    }

    private static Vector2[] CreateSquare(Vector2 center, float radius)
        => [new(center.X - radius, center.Y - radius), new(center.X + radius, center.Y - radius), new(center.X + radius, center.Y + radius), new(center.X - radius, center.Y + radius)];

    /// <summary>
    /// Splits a polyline into the "on" pieces of a dash pattern, appending them to <paramref name="result"/>.
    /// Returns false when the pattern needs more than <see cref="MaxDashPieces"/> pieces.
    /// </summary>
    private static bool ApplyDash(ReadOnlySpan<Vector2> points, bool closed, float[] dash, BinkRect? cull, List<Vector2[]> result)
    {
        float total = 0f;
        foreach (float d in dash) total += MathF.Max(d, 0f);
        if (total <= BinkGeometryMath.Epsilon || points.Length < 2) return true;

        var state = new DashState(dash, result);
        state.Begin(points[0]);

        int count = closed ? points.Length : points.Length - 1;
        for (int i = 0; i < count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Length];
            if (cull is not { } area || (area.Contains(a) && area.Contains(b)))
            {
                state.Dash(a, b);
            }
            else if (ClipSegment(a, b, area, out float t0, out float t1))
            {
                Vector2 enter = Vector2.Lerp(a, b, t0);
                Vector2 exit = Vector2.Lerp(a, b, t1);
                if (t0 > 0f) state.Skip(a, enter);
                state.Dash(enter, exit);
                if (t1 < 1f) state.Skip(exit, b);
            }
            else
            {
                state.Skip(a, b);
            }
            if (result.Count > MaxDashPieces) return false;
        }
        state.End();
        return result.Count <= MaxDashPieces;
    }

    /// <summary>
    /// The part of the segment from <paramref name="a"/> to <paramref name="b"/> inside <paramref name="area"/>, as
    /// parameters along it (Liang-Barsky). False when the segment misses the area.
    /// </summary>
    private static bool ClipSegment(Vector2 a, Vector2 b, BinkRect area, out float t0, out float t1)
    {
        t0 = 0f;
        t1 = 1f;
        Vector2 d = b - a;
        return Clip(-d.X, a.X - area.Left, ref t0, ref t1)
            && Clip(d.X, area.Right - a.X, ref t0, ref t1)
            && Clip(-d.Y, a.Y - area.Top, ref t0, ref t1)
            && Clip(d.Y, area.Bottom - a.Y, ref t0, ref t1)
            && t0 < t1;

        static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (p == 0f) return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }
    }

    /// <summary>Where along the dash pattern the walk is, and the "on" piece being collected.</summary>
    private sealed class DashState(float[] dash, List<Vector2[]> result)
    {
        private readonly List<Vector2> _current = new(16);
        private int _index;
        private float _remaining = dash[0];
        private bool _on = true;

        public void Begin(Vector2 start) => _current.Add(start);

        /// <summary>Cuts a segment into dashes.</summary>
        public void Dash(Vector2 a, Vector2 b)
        {
            float length = Vector2.Distance(a, b);
            float position = 0f;
            while (length - position > _remaining)
            {
                position += _remaining;
                Vector2 split = Vector2.Lerp(a, b, position / length);
                if (_on)
                {
                    _current.Add(split);
                    Emit();
                }
                else
                {
                    _current.Clear();
                    _current.Add(split);
                }
                Next();
                if (result.Count > MaxDashPieces) return;
            }
            _remaining -= length - position;
            if (_on) _current.Add(b);
        }

        /// <summary>
        /// Walks the pattern along a segment that cannot be seen, producing nothing. A dash in progress ends where
        /// the segment starts and resumes where it ends: both points are outside the visible area, so the gap
        /// between them is never drawn either way.
        /// </summary>
        public void Skip(Vector2 a, Vector2 b)
        {
            if (_on) Emit();
            Advance(Vector2.Distance(a, b));
            if (_on) _current.Add(b);
        }

        public void End()
        {
            if (_on) Emit();
        }

        private void Emit()
        {
            if (_current.Count > 1) result.Add(_current.ToArray());
            _current.Clear();
        }

        private void Next()
        {
            _on = !_on;
            _index = (_index + 1) % dash.Length;
            _remaining = MathF.Max(dash[_index], 0.01f);
        }

        /// <summary>Moves the pattern on by a distance in constant time, however long the distance is.</summary>
        private void Advance(double distance)
        {
            if (distance < _remaining)
            {
                _remaining -= (float)distance;
                return;
            }
            distance -= _remaining;
            Next();

            // The pattern repeats after one pass through its entries, or two when it has an odd number of them,
            // because each entry flips between on and off. Whole repetitions change nothing, so only the remainder
            // is walked.
            double period = 0;
            foreach (float d in dash) period += MathF.Max(d, 0.01f);
            if (dash.Length % 2 == 1) period *= 2;
            distance %= period;

            while (distance >= _remaining)
            {
                distance -= _remaining;
                Next();
            }
            _remaining -= (float)distance;
        }
    }
}
