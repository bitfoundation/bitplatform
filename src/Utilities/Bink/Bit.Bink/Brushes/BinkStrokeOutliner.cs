using System.Numerics;

namespace Bit.Bink;

/// <summary>Options for <see cref="BinkStrokeOutliner"/>.</summary>
public readonly record struct BinkOutlineOptions
{
    /// <summary>Maximum stroke width (diameter) in document units.</summary>
    public float Width { get; init; }

    /// <summary>Pressure to width-factor mapping.</summary>
    public BinkPressureCurve PressureCurve { get; init; }

    /// <summary>Taper-in length at the start.</summary>
    public float TaperStart { get; init; }

    /// <summary>Taper-out length at the end.</summary>
    public float TaperEnd { get; init; }

    /// <summary>Whether to draw round caps (false gives flat caps).</summary>
    public bool RoundCaps { get; init; }

    /// <summary>Whether to emit a smooth (quadratic) outline instead of straight polygon edges.</summary>
    public bool Smooth { get; init; }

    /// <summary>Creates default options for a width.</summary>
    public BinkOutlineOptions(float width)
    {
        Width = width;
        PressureCurve = BinkPressureCurve.Linear;
        RoundCaps = true;
        Smooth = true;
    }
}

/// <summary>
/// Generates a closed, variable-width outline polygon around a polyline of pressure-annotated points.
/// The outline is filled with the non-zero rule, so tight turns that fold over themselves stay solid.
/// </summary>
/// <remarks>
/// Algorithm overview:
/// <list type="number">
/// <item>The radius at each point is <c>Width/2 * PressureCurve(pressure) * taper</c>.</item>
/// <item>Left and right offset points are placed along the perpendicular of the averaged neighbouring directions,
/// with a bounded miter correction so the visual width stays constant through gentle corners.</item>
/// <item>At sharp corners (direction change beyond 90 degrees) an arc of points is inserted around the corner
/// so the outline does not pinch.</item>
/// <item>Round or flat caps close both ends; the polygon is <c>left + end cap + reversed right + start cap</c>.</item>
/// <item>Optionally the polygon is emitted as a quadratic-midpoint smooth path.</item>
/// </list>
/// </remarks>
public static class BinkStrokeOutliner
{
    private const float SharpCornerDot = 0.0f; // cos(90°): direction changes of 90° or more get an inserted arc
    private const float MaxMiterScale = 1.6f;
    private const int MaxArcSteps = 12;

    /// <summary>Most extra samples one segment contributes to a taper, so a very long segment stays bounded.</summary>
    private const int MaxTaperSamples = 24;

    /// <summary>
    /// Point capacity kept between strokes. The scratch lists are reused so an ordinary stroke allocates nothing,
    /// but one enormous stroke must not pin its buffer for the life of the thread: on a server that is once per
    /// thread-pool thread that ever drew.
    /// </summary>
    private const int RetainedCapacity = 4096;

    [ThreadStatic]
    private static List<Vector2>? t_left;
    [ThreadStatic]
    private static List<Vector2>? t_right;
    [ThreadStatic]
    private static List<Vector2>? t_polygon;

    /// <summary>
    /// Builds the outline into <paramref name="builder"/> (which is reset first) and returns the polygon's bounds.
    /// </summary>
    public static BinkRect Build(BinkStrokePointsView points, in BinkOutlineOptions options, BinkPathBuilder builder)
    {
        builder.Reset();
        int n = points.Count;
        if (n == 0 || options.Width <= 0) return BinkRect.Empty;

        List<Vector2> polygon = t_polygon ??= new List<Vector2>(512);
        polygon.Clear();
        BuildPolygon(points, options, polygon);
        if (polygon.Count == 0) return BinkRect.Empty;

        ReadOnlySpan<Vector2> poly = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(polygon);
        if (options.Smooth) builder.AddSmoothPolygon(poly);
        else builder.AddPolygon(poly);
        BinkRect bounds = BinkGeometryMath.Bounds(poly);
        Trim();
        return bounds;
    }

    /// <summary>Releases scratch buffers that one very large stroke grew, keeping the ordinary-case capacity.</summary>
    private static void Trim()
    {
        if (t_polygon is { Capacity: > RetainedCapacity }) t_polygon = null;
        if (t_left is { Capacity: > RetainedCapacity }) t_left = null;
        if (t_right is { Capacity: > RetainedCapacity }) t_right = null;
    }

    /// <summary>Computes the raw outline polygon (no smoothing) for a set of points.</summary>
    public static void BuildPolygon(BinkStrokePointsView points, in BinkOutlineOptions options, List<Vector2> polygon)
    {
        polygon.Clear();
        int n = points.Count;
        if (n == 0) return;

        float halfWidth = options.Width / 2f;
        BinkPressureCurve curve = options.PressureCurve ?? BinkPressureCurve.Linear;

        // Distances along the stroke are needed for tapering.
        float totalLength = 0f;
        for (int i = 1; i < n; i++) totalLength += Vector2.Distance(points.PositionAt(i - 1), points.PositionAt(i));

        if (n == 1 || totalLength < BinkGeometryMath.Epsilon)
        {
            // A dot: a circle of the pressure-scaled radius (never smaller than a visible minimum). AddArc skips the
            // start angle, so the end (the same angle, a full turn later) must be included or one vertex is missing.
            float r = MathF.Max(halfWidth * curve.Apply(points.Pressure[0]), halfWidth * 0.35f);
            AddArc(polygon, points.PositionAt(0), r, 0f, MathF.PI * 2f, 16, includeEnd: true);
            return;
        }

        List<Vector2> left = t_left ??= new List<Vector2>(256);
        List<Vector2> right = t_right ??= new List<Vector2>(256);
        left.Clear();
        right.Clear();

        // Taper is a function of distance along the stroke, but the outline can only be evaluated at the points
        // that are stored. A committed stroke is simplified, so a straight run is often just two points hundreds
        // of units apart: evaluating the taper only at those turns a taper meant for the last few units into a
        // wedge down the whole stroke. Segments that cross a taper zone are sampled along their length instead.
        bool tapered = options.TaperStart > 0 || options.TaperEnd > 0;
        float taperStartEnds = options.TaperStart;
        float taperEndBegins = totalLength - options.TaperEnd;

        float distance = 0f;
        Vector2 prev = points.PositionAt(0);
        Vector2 prevDir = Vector2.Zero;
        // Find the first non-degenerate direction for the start point.
        for (int j = 1; j < n; j++)
        {
            prevDir = BinkGeometryMath.SafeNormalize(points.PositionAt(j) - prev);
            if (prevDir != Vector2.Zero) break;
        }
        if (prevDir == Vector2.Zero)
        {
            // Every step is below the epsilon used to derive a direction, yet the points span enough distance to
            // get here. Without a direction the outline would collapse, so draw the stroke as a dot instead.
            float r = MathF.Max(halfWidth * curve.Apply(points.Pressure[0]), halfWidth * 0.35f);
            AddArc(polygon, points.PositionAt(0), r, 0f, MathF.PI * 2f, 16, includeEnd: true);
            return;
        }

        Vector2 startDir = prevDir;
        Vector2 endDir = prevDir;
        float startRadius = 0f, endRadius = 0f;

        for (int i = 0; i < n; i++)
        {
            Vector2 p = points.PositionAt(i);
            float previousDistance = distance;
            if (i > 0) distance += Vector2.Distance(prev, p);

            Vector2 dirIn = prevDir;
            if (i > 0)
            {
                Vector2 d = BinkGeometryMath.SafeNormalize(p - prev);
                if (d != Vector2.Zero) dirIn = d;
            }
            Vector2 dirOut = dirIn;
            for (int j = i + 1; j < n; j++)
            {
                Vector2 d = BinkGeometryMath.SafeNormalize(points.PositionAt(j) - p);
                if (d != Vector2.Zero) { dirOut = d; break; }
            }

            // Fill in the taper along the segment that led here, before this point's own offsets are added.
            if (i > 0 && tapered)
            {
                Vector2 segmentDirection = BinkGeometryMath.SafeNormalize(p - prev);
                float segmentSpan = distance - previousDistance;
                if (segmentDirection != Vector2.Zero && segmentSpan > BinkGeometryMath.Epsilon)
                {
                    Vector2 normal = BinkGeometryMath.Perpendicular(segmentDirection);
                    float fromPressure = points.Pressure[i - 1];
                    float toPressure = points.Pressure[i];

                    // Two zones: the taper in at the start of the stroke and the taper out at its end. Samples go
                    // where the taper actually varies rather than being spread over the segment, because a segment
                    // can be hundreds of units long while the zone it crosses is only a few.
                    for (int zone = 0; zone < 2; zone++)
                    {
                        float zoneFrom = zone == 0 ? 0f : taperEndBegins;
                        float zoneTo = zone == 0 ? taperStartEnds : totalLength;
                        float from = MathF.Max(zoneFrom, previousDistance);
                        float to = MathF.Min(zoneTo, distance);
                        if (to <= from) continue;

                        float resolution = MathF.Max((zoneTo - zoneFrom) / 8f, 0.01f);
                        int steps = Math.Clamp((int)MathF.Ceiling((to - from) / resolution), 1, MaxTaperSamples);
                        // The first sample sits on the zone boundary, so full width holds right up to it.
                        for (int k = 0; k <= steps; k++)
                        {
                            float d = from + (to - from) * (k / (float)steps);
                            if (d >= distance) break;
                            float u = (d - previousDistance) / segmentSpan;
                            if (u <= 0f) continue;
                            Vector2 q = Vector2.Lerp(prev, p, u);
                            float pressureAt = fromPressure + (toPressure - fromPressure) * u;
                            float r = halfWidth * curve.Apply(pressureAt) * Taper(d, totalLength, options.TaperStart, options.TaperEnd);
                            r = MathF.Max(r, 0.05f);
                            left.Add(q + normal * r);
                            right.Add(q - normal * r);
                        }
                    }
                }
            }

            float radius = halfWidth * curve.Apply(points.Pressure[i]);
            radius *= Taper(distance, totalLength, options.TaperStart, options.TaperEnd);
            radius = MathF.Max(radius, 0.05f);
            if (i == 0) { startRadius = radius; }
            if (i == n - 1) { endRadius = radius; endDir = dirIn; }

            float dot = Vector2.Dot(dirIn, dirOut);
            if (i > 0 && i < n - 1 && dot < SharpCornerDot)
            {
                // Sharp corner: sweep the normal from the incoming to the outgoing direction around the point.
                Vector2 nIn = BinkGeometryMath.Perpendicular(dirIn);
                Vector2 nOut = BinkGeometryMath.Perpendicular(dirOut);
                float angle = MathF.Atan2(BinkGeometryMath.Cross(nIn, nOut), Vector2.Dot(nIn, nOut));
                int steps = Math.Clamp((int)MathF.Ceiling(MathF.Abs(angle) * radius / 3f) + 2, 3, MaxArcSteps);
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 nrm = BinkGeometryMath.Rotate(nIn, angle * s / steps);
                    left.Add(p + nrm * radius);
                    right.Add(p - nrm * radius);
                }
            }
            else
            {
                Vector2 avg = BinkGeometryMath.SafeNormalize(dirIn + dirOut);
                if (avg == Vector2.Zero) avg = dirIn;
                Vector2 nrm = BinkGeometryMath.Perpendicular(avg);
                // Miter correction keeps the visual width through the corner; clamped to avoid spikes.
                float cosHalf = Vector2.Dot(avg, dirIn);
                float scale = cosHalf > 0.01f ? MathF.Min(1f / cosHalf, MaxMiterScale) : 1f;
                Vector2 offset = nrm * (radius * scale);
                left.Add(p + offset);
                right.Add(p - offset);
            }

            prev = p;
            if (dirOut != Vector2.Zero) prevDir = dirOut;
        }

        // Assemble: left side forward, end cap, right side backward, start cap.
        polygon.AddRange(left);

        Vector2 endPoint = points.PositionAt(n - 1);
        if (options.RoundCaps)
        {
            Vector2 nrm = BinkGeometryMath.Perpendicular(endDir);
            float from = BinkGeometryMath.Angle(nrm);
            int steps = Math.Clamp((int)MathF.Ceiling(endRadius) + 3, 3, MaxArcSteps);
            AddArc(polygon, endPoint, endRadius, from, from - MathF.PI, steps, includeEnd: false);
        }
        else
        {
            // Flat cap: extend by a hair so the last segment does not collapse.
            polygon.Add(endPoint + endDir * 0.01f);
        }

        for (int i = right.Count - 1; i >= 0; i--) polygon.Add(right[i]);

        Vector2 startPoint = points.PositionAt(0);
        if (options.RoundCaps)
        {
            Vector2 nrm = BinkGeometryMath.Perpendicular(startDir);
            float from = BinkGeometryMath.Angle(-nrm);
            int steps = Math.Clamp((int)MathF.Ceiling(startRadius) + 3, 3, MaxArcSteps);
            AddArc(polygon, startPoint, startRadius, from, from - MathF.PI, steps, includeEnd: false);
        }
        else
        {
            polygon.Add(startPoint - startDir * 0.01f);
        }
    }

    private static float Taper(float distance, float totalLength, float taperStart, float taperEnd)
    {
        float t = 1f;
        if (taperStart > 0)
        {
            float s = Math.Clamp(distance / taperStart, 0f, 1f);
            t *= EaseOutSine(s);
        }
        if (taperEnd > 0)
        {
            float s = Math.Clamp((totalLength - distance) / taperEnd, 0f, 1f);
            t *= EaseOutSine(s);
        }
        return MathF.Max(t, 0.02f);
    }

    private static float EaseOutSine(float t) => MathF.Sin(t * MathF.PI / 2f);

    private static void AddArc(List<Vector2> polygon, Vector2 center, float radius, float fromAngle, float toAngle, int steps, bool includeEnd)
    {
        int last = includeEnd ? steps : steps - 1;
        for (int s = 1; s <= last; s++)
        {
            float a = fromAngle + (toAngle - fromAngle) * s / steps;
            polygon.Add(center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius);
        }
    }
}
