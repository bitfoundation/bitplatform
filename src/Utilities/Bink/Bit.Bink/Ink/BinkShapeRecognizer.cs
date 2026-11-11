using System.Numerics;

namespace Bit.Bink;

/// <summary>Settings for <see cref="BinkShapeRecognizer"/>.</summary>
public sealed record BinkShapeRecognizerOptions
{
    /// <summary>The settings <see cref="BinkShapeRecognizer"/> uses when given none.</summary>
    public static readonly BinkShapeRecognizerOptions Default = new();

    /// <summary>
    /// How far, on average, a stroke may stray from the shape it is recognized as, as a fraction of the diagonal of its
    /// bounds. Higher accepts sloppier drawing and risks turning ink that was meant as ink into a shape. Default 0.045.
    /// </summary>
    public float Tolerance { get; init; } = 0.045f;

    /// <summary>The smallest diagonal, in document units, a stroke must span to be recognized. Default 12.</summary>
    public float MinimumSize { get; init; } = 12f;

    /// <summary>
    /// Degrees within which a recognized shape is straightened: a line to the nearest multiple of 45 degrees, a
    /// rectangle or ellipse to upright. Zero keeps the angle as drawn. Default 8.
    /// </summary>
    public float AngleSnap { get; init; } = 8f;

    /// <summary>
    /// How much a recognized ellipse's or rectangle's width and height may differ, as a fraction of the larger, and
    /// still be made a circle or a square. Zero keeps the proportions as drawn. Default 0.1.
    /// </summary>
    public float ProportionSnap { get; init; } = 0.1f;

    /// <summary>
    /// The shapes a stroke may become, or null for every one the recognizer knows: <see cref="BinkShapeKind.Line"/>,
    /// <see cref="BinkShapeKind.Arrow"/>, <see cref="BinkShapeKind.Rectangle"/>, <see cref="BinkShapeKind.Ellipse"/>,
    /// <see cref="BinkShapeKind.Triangle"/>, <see cref="BinkShapeKind.Diamond"/> and <see cref="BinkShapeKind.Polygon"/>.
    /// A triangle, diamond or rectangle that is left out may still be recognized as a polygon when that is allowed.
    /// </summary>
    public IReadOnlySet<BinkShapeKind>? Kinds { get; init; }

    internal bool Allows(BinkShapeKind kind) => Kinds is null ? kind is not (BinkShapeKind.Highlight or BinkShapeKind.Star) : Kinds.Contains(kind);
}

/// <summary>A stroke recognized as a shape by <see cref="BinkShapeRecognizer"/>.</summary>
public sealed record BinkShapeRecognition
{
    internal BinkShapeRecognition(BinkShapeKind kind, IReadOnlyList<Vector2> points, BinkRect frame, float rotation, float error)
    {
        Kind = kind;
        Points = points;
        Frame = frame;
        Rotation = rotation;
        Error = error;
    }

    /// <summary>What the stroke was recognized as.</summary>
    public BinkShapeKind Kind { get; }

    /// <summary>
    /// The fitted points in document units: the start and end of a line, the tail and tip of an arrow, the vertices
    /// of a triangle, diamond or polygon, the four corners of a rectangle, and none for an ellipse.
    /// </summary>
    public IReadOnlyList<Vector2> Points { get; }

    /// <summary>
    /// The upright rectangle a rectangle or ellipse fills before <see cref="Rotation"/> turns it about its centre, and
    /// the bounds of the points of every other shape.
    /// </summary>
    public BinkRect Frame { get; }

    /// <summary>The rotation in radians of a rectangle or ellipse about the centre of <see cref="Frame"/>; zero otherwise.</summary>
    public float Rotation { get; }

    /// <summary>
    /// How far the stroke strays from the shape on average, as a fraction of the diagonal of its bounds: zero for a
    /// perfect match, never above <see cref="BinkShapeRecognizerOptions.Tolerance"/>.
    /// </summary>
    public float Error { get; }

    /// <summary>Creates the shape element, in document coordinates, drawn with <paramref name="style"/>.</summary>
    public BinkShapeElement CreateElement(BinkShapeStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        Matrix3x2 transform = Rotation == 0f ? Matrix3x2.Identity : Matrix3x2.CreateRotation(Rotation, Frame.Center);
        return Kind switch
        {
            BinkShapeKind.Line => new BinkLineShape(Points[0], Points[1]) { Style = style },
            BinkShapeKind.Arrow => new BinkLineShape(Points[0], Points[1]) { EndArrow = true, Style = style },
            BinkShapeKind.Rectangle => new BinkRectangleShape(Frame) { Transform = transform, Style = style },
            BinkShapeKind.Ellipse => new BinkEllipseShape(Frame) { Transform = transform, Style = style },
            _ => new BinkPolygonShape(Points) { Style = style },
        };
    }
}

/// <summary>
/// Recognizes a hand-drawn stroke as a clean shape: a line, an arrow, a rectangle, an ellipse, a triangle, a diamond
/// or a polygon. It is what turns a sketched box into a box on a whiteboard; <see cref="BinkDrawTool.ShapeRecognition"/>
/// applies it as strokes are drawn.
/// </summary>
/// <remarks>
/// <para>
/// The stroke is resampled to evenly spaced points, so neither drawing speed nor point density changes the outcome.
/// An open stroke can be a line, or an arrow whose head is drawn in the same stroke after the shaft. A closed one,
/// where the end comes back near the start or crosses past it, is fitted both as an ellipse (least squares in its
/// principal axes) and as a polygon (corners found by simplification); the better fit wins, and a polygon of four
/// right angles is a rectangle, one with its corners at the middles of its sides a diamond.
/// </para>
/// <para>
/// A fit is only accepted when the stroke stays close to it everywhere on average (<see cref="BinkShapeRecognizerOptions.Tolerance"/>),
/// so writing, scribbles and curves are left alone. The recognizer is a pure function: it allocates a few small
/// buffers and touches no document.
/// </para>
/// </remarks>
public static class BinkShapeRecognizer
{
    private const int Samples = 96;
    private const int MaxPolygonVertices = 8;

    /// <summary>Recognizes stroke points as a shape, or returns null when they are not close enough to one.</summary>
    public static BinkShapeRecognition? Recognize(BinkStrokePoints points, BinkShapeRecognizerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(points);
        var positions = new Vector2[points.Count];
        for (int i = 0; i < positions.Length; i++) positions[i] = points.PositionAt(i);
        return Recognize(positions, options);
    }

    /// <summary>Recognizes a polyline, in drawing order, as a shape, or returns null when it is not close enough to one.</summary>
    public static BinkShapeRecognition? Recognize(ReadOnlySpan<Vector2> points, BinkShapeRecognizerOptions? options = null)
    {
        options ??= BinkShapeRecognizerOptions.Default;
        if (points.Length < 2) return null;
        foreach (Vector2 p in points) if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) return null;

        BinkRect bounds = BinkGeometryMath.Bounds(points);
        float diagonal = bounds.Size.Length();
        if (!(diagonal >= MathF.Max(options.MinimumSize, BinkGeometryMath.Epsilon))) return null;
        float tolerance = float.IsFinite(options.Tolerance) ? Math.Clamp(options.Tolerance, 0f, 0.5f) : 0.045f;

        Vector2[]? resampled = Resample(points, Samples);
        if (resampled is null) return null;

        int closeAt = FindClosure(resampled, diagonal);
        if (closeAt < 0) return RecognizeOpen(resampled, diagonal, tolerance, options);
        return RecognizeClosed(resampled.AsSpan(0, closeAt + 1), diagonal, tolerance, options);
    }

    private static BinkShapeRecognition? RecognizeOpen(Vector2[] r, float diagonal, float tolerance, BinkShapeRecognizerOptions options)
    {
        if (options.Allows(BinkShapeKind.Line) && StraightError(r, 0, r.Length - 1, diagonal) is { } lineError && lineError <= tolerance)
        {
            (Vector2 start, Vector2 end, float error) = Straighten(r, r[0], r[^1], diagonal, tolerance, options);
            return new BinkShapeRecognition(BinkShapeKind.Line, [start, end], BinkRect.FromPoints(start, end), 0f, error);
        }
        return options.Allows(BinkShapeKind.Arrow) ? RecognizeArrow(r, diagonal, tolerance, options) : null;
    }

    /// <summary>
    /// An arrow drawn in one stroke: a straight shaft, then a head that turns back from the tip in one or two barbs
    /// (tip, barb; or tip, barb, tip, barb). The tip is the point farthest from the start.
    /// </summary>
    private static BinkShapeRecognition? RecognizeArrow(Vector2[] r, float diagonal, float tolerance, BinkShapeRecognizerOptions options)
    {
        float farthest = 0f;
        for (int i = 1; i < r.Length; i++) farthest = MathF.Max(farthest, Vector2.Distance(r[0], r[i]));
        // A two-barbed head comes back to the tip, so the tip is where the stroke first gets (nearly) that far.
        int tip = 1;
        while (tip < r.Length - 1 && Vector2.Distance(r[0], r[tip]) < farthest * 0.95f) tip++;
        while (tip < r.Length - 1 && Vector2.Distance(r[0], r[tip + 1]) > Vector2.Distance(r[0], r[tip])) tip++;
        // The head has to be a real part of the stroke, and the shaft most of it.
        if (tip < r.Length / 2 || tip > r.Length - 4) return null;
        float shaft = Vector2.Distance(r[0], r[tip]);
        if (StraightError(r, 0, tip, shaft) is not { } shaftError || shaftError > tolerance) return null;

        Vector2 back = BinkGeometryMath.SafeNormalize(r[0] - r[tip]);
        int barbs = 0;
        float reach = 0f;
        for (int i = tip + 1; i < r.Length; i++)
        {
            Vector2 offset = r[i] - r[tip];
            float distance = offset.Length();
            // The head stays near the tip; anything wandering off is not an arrow.
            if (distance > shaft * 0.5f) return null;
            reach = MathF.Max(reach, distance);
        }
        if (reach < shaft * 0.08f) return null;

        // A barb is a stretch of the head that ends well away from the tip, pointing back along the shaft at an angle.
        var keep = new List<int>();
        int headCount = r.Length - tip;
        var xs = new float[headCount];
        var ys = new float[headCount];
        for (int i = 0; i < headCount; i++) { xs[i] = r[tip + i].X; ys[i] = r[tip + i].Y; }
        BinkGeometryMath.Simplify(xs, ys, reach * 0.2f, keep);
        foreach (int k in keep)
        {
            Vector2 offset = r[tip + k] - r[tip];
            float distance = offset.Length();
            if (distance < reach * 0.6f) continue;
            float angle = MathF.Acos(Math.Clamp(Vector2.Dot(offset / distance, back), -1f, 1f)) * 180f / MathF.PI;
            if (angle < 10f || angle > 75f) return null;
            barbs++;
        }
        if (barbs == 0 || barbs > 2) return null;

        // The shaft is judged against its own length, as it was accepted; the result reports it against the diagonal.
        (Vector2 start, Vector2 end, float error) = Straighten(r.AsSpan(0, tip + 1), r[0], r[tip], shaft, tolerance, options);
        return new BinkShapeRecognition(BinkShapeKind.Arrow, [start, end], BinkRect.FromPoints(start, end), 0f, error * shaft / diagonal);
    }

    /// <summary>
    /// A segment snapped to the nearest multiple of 45 degrees, unless snapping takes it further from the points than
    /// the tolerance allows, and its error, as drawn, as a fraction of <paramref name="scale"/>.
    /// </summary>
    private static (Vector2 Start, Vector2 End, float Error) Straighten(ReadOnlySpan<Vector2> points, Vector2 start, Vector2 end, float scale, float tolerance, BinkShapeRecognizerOptions options)
    {
        (Vector2 snappedStart, Vector2 snappedEnd) = SnapLine(start, end, options.AngleSnap);
        float snapped = SegmentError(points, snappedStart, snappedEnd) / scale;
        return snapped <= tolerance ? (snappedStart, snappedEnd, snapped) : (start, end, SegmentError(points, start, end) / scale);
    }

    private static float SegmentError(ReadOnlySpan<Vector2> points, Vector2 a, Vector2 b)
    {
        float error = 0f;
        foreach (Vector2 p in points) error += BinkGeometryMath.DistanceToSegment(p, a, b);
        return error / points.Length;
    }

    /// <summary>The average distance from the points to a closed outline, as a fraction of <paramref name="diagonal"/>.</summary>
    private static float PolygonError(ReadOnlySpan<Vector2> ring, ReadOnlySpan<Vector2> vertices, float diagonal)
    {
        float error = 0f;
        foreach (Vector2 p in ring) error += MathF.Sqrt(DistanceToPolygonSquared(p, vertices));
        return error / (ring.Length * diagonal);
    }

    /// <summary>The average distance from the points to an ellipse, measured along the ray from its centre, as a fraction of <paramref name="diagonal"/>.</summary>
    private static float EllipseError(ReadOnlySpan<Vector2> ring, Vector2 centre, float rx, float ry, float angle, float diagonal)
    {
        float cos = MathF.Cos(angle), sin = MathF.Sin(angle);
        float error = 0f;
        foreach (Vector2 p in ring)
        {
            Vector2 d = p - centre;
            float u = d.X * cos + d.Y * sin;
            float v = -d.X * sin + d.Y * cos;
            float k = MathF.Sqrt(u * u / (rx * rx) + v * v / (ry * ry));
            if (k > BinkGeometryMath.Epsilon) error += MathF.Abs(1f - 1f / k) * d.Length();
        }
        return error / (ring.Length * diagonal);
    }

    private static BinkShapeRecognition? RecognizeClosed(ReadOnlySpan<Vector2> ring, float diagonal, float tolerance, BinkShapeRecognizerOptions options)
    {
        // A stroke that goes out and comes back along itself encloses nothing: a line drawn twice, not a shape.
        if (MathF.Abs(SignedArea(ring)) < diagonal * diagonal * 0.04f) return null;

        Vector2 centroid = Vector2.Zero;
        foreach (Vector2 p in ring) centroid += p;
        centroid /= ring.Length;

        BinkShapeRecognition? ellipse = options.Allows(BinkShapeKind.Ellipse) ? FitEllipse(ring, centroid, diagonal, tolerance, options) : null;
        BinkShapeRecognition? polygon = FitPolygon(ring, centroid, diagonal, tolerance, options);

        BinkShapeRecognition? best = ellipse;
        if (polygon is not null && (best is null || polygon.Error < best.Error)) best = polygon;
        return best is not null && best.Error <= tolerance ? best : null;
    }

    private static BinkShapeRecognition? FitEllipse(ReadOnlySpan<Vector2> ring, Vector2 centroid, float diagonal, float tolerance, BinkShapeRecognizerOptions options)
    {
        // The principal axes of the points, then a least-squares fit of A u² + B v² = 1 in them, which is linear in A and B.
        double sxx = 0, syy = 0, sxy = 0;
        foreach (Vector2 p in ring)
        {
            Vector2 d = p - centroid;
            sxx += d.X * d.X;
            syy += d.Y * d.Y;
            sxy += d.X * d.Y;
        }
        float angle = 0.5f * MathF.Atan2(2f * (float)sxy, (float)(sxx - syy));
        float cos = MathF.Cos(angle), sin = MathF.Sin(angle);

        double u4 = 0, v4 = 0, u2v2 = 0, u2 = 0, v2 = 0;
        foreach (Vector2 p in ring)
        {
            Vector2 d = p - centroid;
            double u = d.X * cos + d.Y * sin;
            double v = -d.X * sin + d.Y * cos;
            u4 += u * u * u * u;
            v4 += v * v * v * v;
            u2v2 += u * u * v * v;
            u2 += u * u;
            v2 += v * v;
        }
        double det = u4 * v4 - u2v2 * u2v2;
        if (Math.Abs(det) < 1e-12) return null;
        double a = (u2 * v4 - v2 * u2v2) / det;
        double b = (v2 * u4 - u2 * u2v2) / det;
        if (!(a > 0) || !(b > 0)) return null;
        float rx = (float)(1 / Math.Sqrt(a));
        float ry = (float)(1 / Math.Sqrt(b));

        // With the turn kept within a quarter, so that width and height mean what they say.
        if (angle > MathF.PI / 4f) { angle -= MathF.PI / 2f; (rx, ry) = (ry, rx); }
        else if (angle <= -MathF.PI / 4f) { angle += MathF.PI / 2f; (rx, ry) = (ry, rx); }

        // Snapped upright and round when that stays within the tolerance; measured as it will be drawn either way.
        // Each snap is kept when it stays within the tolerance on its own: both, then the circle, then upright, then
        // neither. A circle has no angle, so making it round makes it upright too.
        (float srx, float sry) = SnapProportion(rx, ry, options.ProportionSnap, out bool even);
        float sangle = SnapAngle(angle, MathF.PI / 2f, options.AngleSnap);
        ReadOnlySpan<(float Rx, float Ry, float Angle)> candidates = [(srx, sry, even ? 0f : sangle), (srx, sry, even ? 0f : angle), (rx, ry, sangle), (rx, ry, angle)];
        (float Rx, float Ry, float Angle) chosen = candidates[^1];
        float error = 0f;
        foreach ((float Rx, float Ry, float Angle) candidate in candidates)
        {
            chosen = candidate;
            error = EllipseError(ring, centroid, candidate.Rx, candidate.Ry, candidate.Angle, diagonal);
            if (error <= tolerance) break;
        }
        return new BinkShapeRecognition(BinkShapeKind.Ellipse, [], BinkRect.FromCenter(centroid, chosen.Rx * 2f, chosen.Ry * 2f), chosen.Angle, error);
    }

    private static BinkShapeRecognition? FitPolygon(ReadOnlySpan<Vector2> ring, Vector2 centroid, float diagonal, float tolerance, BinkShapeRecognizerOptions options)
    {
        // Start at the point farthest from the centre, which is a corner of any convex shape, so simplifying the
        // closed outline as an open one does not cut a corner off where it begins.
        int start = 0;
        float farthest = 0f;
        for (int i = 0; i < ring.Length; i++)
        {
            float d = Vector2.DistanceSquared(ring[i], centroid);
            if (d > farthest) { farthest = d; start = i; }
        }
        int n = ring.Length;
        var xs = new float[n + 1];
        var ys = new float[n + 1];
        for (int i = 0; i <= n; i++)
        {
            Vector2 p = ring[(start + i) % n];
            xs[i] = p.X;
            ys[i] = p.Y;
        }
        var keep = new List<int>();
        BinkGeometryMath.Simplify(xs, ys, diagonal * 0.07f, keep);
        var vertices = new List<Vector2>(keep.Count);
        for (int i = 0; i < keep.Count - 1; i++) vertices.Add(new Vector2(xs[keep[i]], ys[keep[i]]));
        RemoveWeakCorners(vertices, diagonal);
        if (vertices.Count < 3 || vertices.Count > MaxPolygonVertices) return null;

        float error = PolygonError(ring, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(vertices), diagonal);

        BinkRect bounds = BinkGeometryMath.Bounds(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(vertices));
        if (vertices.Count == 3 && options.Allows(BinkShapeKind.Triangle))
        {
            return new BinkShapeRecognition(BinkShapeKind.Triangle, vertices, bounds, 0f, error);
        }
        if (vertices.Count == 4)
        {
            // A diamond or rectangle is the corners made regular, so it is only what was drawn when it fits nearly as
            // well as the corners themselves; otherwise the next reading is tried, down to the polygon.
            bool Fits(BinkShapeRecognition candidate) => candidate.Error <= tolerance && candidate.Error <= MathF.Max(error * 2f, error + 0.01f);
            if (options.Allows(BinkShapeKind.Diamond) && IsDiamond(vertices, bounds, diagonal))
            {
                Vector2[] corners = [new(bounds.Center.X, bounds.Top), new(bounds.Right, bounds.Center.Y), new(bounds.Center.X, bounds.Bottom), new(bounds.Left, bounds.Center.Y)];
                var diamond = new BinkShapeRecognition(BinkShapeKind.Diamond, corners, bounds, 0f, PolygonError(ring, corners, diagonal));
                if (Fits(diamond)) return diamond;
            }
            if (options.Allows(BinkShapeKind.Rectangle) && FitRectangle(vertices, ring, diagonal, tolerance, options) is { } rectangle && Fits(rectangle)) return rectangle;
        }
        return options.Allows(BinkShapeKind.Polygon) ? new BinkShapeRecognition(BinkShapeKind.Polygon, vertices, bounds, 0f, error) : null;
    }

    /// <summary>Four corners of near right angles become a rectangle, turned by the average direction of its sides.</summary>
    private static BinkShapeRecognition? FitRectangle(List<Vector2> v, ReadOnlySpan<Vector2> ring, float diagonal, float tolerance, BinkShapeRecognizerOptions options)
    {
        Vector2 direction = Vector2.Zero;
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = BinkGeometryMath.SafeNormalize(v[(i + 1) % 4] - v[i]);
            Vector2 b = BinkGeometryMath.SafeNormalize(v[(i + 2) % 4] - v[(i + 1) % 4]);
            if (MathF.Abs(Vector2.Dot(a, b)) > 0.31f) return null; // more than about 18 degrees from square
            // Quadrupling the angle makes the four sides' directions agree, so they can be averaged.
            float angle = BinkGeometryMath.Angle(a) * 4f;
            direction += new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }
        float rotation = MathF.Atan2(direction.Y, direction.X) / 4f;
        float cos = MathF.Cos(rotation), sin = MathF.Sin(rotation);

        // The corners in the rectangle's own axes; each side sits at the average of the two corners on it.
        Span<float> us = stackalloc float[4];
        Span<float> vs = stackalloc float[4];
        Vector2 centre = (v[0] + v[1] + v[2] + v[3]) / 4f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 d = v[i] - centre;
            us[i] = d.X * cos + d.Y * sin;
            vs[i] = -d.X * sin + d.Y * cos;
        }
        us.Sort();
        vs.Sort();
        float left = (us[0] + us[1]) / 2f, right = (us[2] + us[3]) / 2f;
        float top = (vs[0] + vs[1]) / 2f, bottom = (vs[2] + vs[3]) / 2f;
        Vector2 middle = centre + BinkGeometryMath.Rotate(new Vector2((left + right) / 2f, (top + bottom) / 2f), rotation);
        (float width, float height) = SnapProportion(right - left, bottom - top, options.ProportionSnap, out _);
        float snapped = SnapAngle(rotation, MathF.PI / 2f, options.AngleSnap);

        // Each snap is kept when it stays within the tolerance on its own: both, then square, then upright, then
        // neither; measured as it will be drawn either way.
        BinkRect square = BinkRect.FromCenter(middle, width, height), drawn = BinkRect.FromCenter(middle, right - left, bottom - top);
        BinkShapeRecognition result = Build(square, snapped, ring, diagonal);
        if (result.Error > tolerance) result = Build(square, rotation, ring, diagonal);
        if (result.Error > tolerance) result = Build(drawn, snapped, ring, diagonal);
        if (result.Error > tolerance) result = Build(drawn, rotation, ring, diagonal);
        return result;

        static BinkShapeRecognition Build(BinkRect frame, float rotation, ReadOnlySpan<Vector2> ring, float diagonal)
        {
            Matrix3x2 turn = Matrix3x2.CreateRotation(rotation, frame.Center);
            Vector2[] corners =
            [
                Vector2.Transform(frame.TopLeft, turn),
                Vector2.Transform(new Vector2(frame.Right, frame.Top), turn),
                Vector2.Transform(new Vector2(frame.Right, frame.Bottom), turn),
                Vector2.Transform(new Vector2(frame.Left, frame.Bottom), turn),
            ];
            return new BinkShapeRecognition(BinkShapeKind.Rectangle, corners, frame, rotation, PolygonError(ring, corners, diagonal));
        }
    }

    /// <summary>An upright diamond: each corner near the middle of a side of the corners' bounds.</summary>
    private static bool IsDiamond(List<Vector2> v, BinkRect bounds, float diagonal)
    {
        ReadOnlySpan<Vector2> middles = [new(bounds.Center.X, bounds.Top), new(bounds.Right, bounds.Center.Y), new(bounds.Center.X, bounds.Bottom), new(bounds.Left, bounds.Center.Y)];
        float limit = diagonal * 0.12f;
        foreach (Vector2 m in middles)
        {
            bool near = false;
            foreach (Vector2 p in v) near |= Vector2.Distance(m, p) <= limit;
            if (!near) return false;
        }
        return true;
    }

    /// <summary>Drops corners that barely turn or that sit on top of a neighbour, until none is left to drop.</summary>
    private static void RemoveWeakCorners(List<Vector2> v, float diagonal)
    {
        float closeness = diagonal * 0.08f;
        bool removed = true;
        while (removed && v.Count >= 3)
        {
            removed = false;
            int weakest = -1;
            float weakestTurn = float.MaxValue;
            for (int i = 0; i < v.Count; i++)
            {
                Vector2 previous = v[(i + v.Count - 1) % v.Count];
                Vector2 next = v[(i + 1) % v.Count];
                Vector2 a = BinkGeometryMath.SafeNormalize(v[i] - previous);
                Vector2 b = BinkGeometryMath.SafeNormalize(next - v[i]);
                float turn = MathF.Acos(Math.Clamp(Vector2.Dot(a, b), -1f, 1f));
                if (Vector2.Distance(v[i], next) < closeness) turn = 0f;
                if (turn < weakestTurn) { weakestTurn = turn; weakest = i; }
            }
            if (weakest >= 0 && weakestTurn < 25f * MathF.PI / 180f)
            {
                v.RemoveAt(weakest);
                removed = true;
            }
        }
    }

    /// <summary>
    /// The index at which the stroke closes: the point of its last part nearest the start, when that is near enough.
    /// It covers both a gap left between the ends and a stroke that runs on past where it began. -1 when it is open.
    /// </summary>
    private static int FindClosure(Vector2[] r, float diagonal)
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = (int)(r.Length * 0.6f); i < r.Length; i++)
        {
            float d = Vector2.Distance(r[i], r[0]);
            if (d < bestDistance) { bestDistance = d; best = i; }
        }
        return bestDistance <= diagonal * 0.15f ? best : -1;
    }

    /// <summary>The average distance of points <paramref name="from"/>..<paramref name="to"/> from the chord joining them, over <paramref name="scale"/>; null if they are not one straight run.</summary>
    private static float? StraightError(Vector2[] r, int from, int to, float scale)
    {
        Vector2 a = r[from], b = r[to];
        float chord = Vector2.Distance(a, b);
        if (chord < BinkGeometryMath.Epsilon) return null;
        float length = 0f, error = 0f;
        for (int i = from + 1; i <= to; i++) length += Vector2.Distance(r[i - 1], r[i]);
        // A run that doubles back along itself is not one straight line, however close to the chord it stays.
        if (length > chord * 1.15f) return null;
        for (int i = from; i <= to; i++) error += BinkGeometryMath.DistanceToSegment(r[i], a, b);
        return error / (to - from + 1) / scale;
    }

    private static (Vector2 Start, Vector2 End) SnapLine(Vector2 start, Vector2 end, float snapDegrees)
    {
        Vector2 d = end - start;
        float angle = BinkGeometryMath.Angle(d);
        float snapped = SnapAngle(angle, MathF.PI / 4f, snapDegrees);
        if (snapped == angle) return (start, end);
        // Turned about the middle, so the line stays where it was drawn.
        Vector2 middle = (start + end) / 2f;
        Vector2 half = new Vector2(MathF.Cos(snapped), MathF.Sin(snapped)) * (d.Length() / 2f);
        return (middle - half, middle + half);
    }

    /// <summary>Rounds an angle to the nearest multiple of <paramref name="step"/> when within <paramref name="snapDegrees"/> of it.</summary>
    private static float SnapAngle(float angle, float step, float snapDegrees)
    {
        if (!(snapDegrees > 0)) return angle;
        float nearest = MathF.Round(angle / step) * step;
        return MathF.Abs(angle - nearest) > snapDegrees * MathF.PI / 180f ? angle : nearest;
    }

    private static (float, float) SnapProportion(float width, float height, float snap, out bool even)
    {
        even = false;
        float larger = MathF.Max(width, height);
        if (!(snap > 0) || larger <= 0 || MathF.Abs(width - height) / larger > snap) return (width, height);
        even = true;
        float size = (width + height) / 2f;
        return (size, size);
    }

    /// <summary>Points evenly spaced along a polyline, the first and last at its ends; null when it has no length.</summary>
    private static Vector2[]? Resample(ReadOnlySpan<Vector2> points, int count)
    {
        float length = 0f;
        for (int i = 1; i < points.Length; i++) length += Vector2.Distance(points[i - 1], points[i]);
        if (length < BinkGeometryMath.Epsilon) return null;

        var result = new Vector2[count];
        float step = length / (count - 1);
        result[0] = points[0];
        int written = 1;
        float carried = 0f;
        for (int i = 1; i < points.Length && written < count - 1; i++)
        {
            Vector2 a = points[i - 1], b = points[i];
            float segment = Vector2.Distance(a, b);
            if (segment <= 0f) continue;
            float along = step - carried;
            while (along <= segment && written < count - 1)
            {
                result[written++] = Vector2.Lerp(a, b, along / segment);
                along += step;
            }
            carried = segment - (along - step);
        }
        while (written < count) result[written++] = points[^1];
        return result;
    }

    private static float SignedArea(ReadOnlySpan<Vector2> ring)
    {
        float area = 0f;
        for (int i = 0; i < ring.Length; i++) area += BinkGeometryMath.Cross(ring[i], ring[(i + 1) % ring.Length]);
        return area / 2f;
    }

    private static float DistanceToPolygonSquared(Vector2 p, ReadOnlySpan<Vector2> vertices)
    {
        float best = float.MaxValue;
        for (int i = 0; i < vertices.Length; i++) best = MathF.Min(best, BinkGeometryMath.DistanceToSegmentSquared(p, vertices[i], vertices[(i + 1) % vertices.Length]));
        return best;
    }
}
