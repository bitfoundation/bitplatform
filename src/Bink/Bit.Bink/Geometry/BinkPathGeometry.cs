using System.Numerics;

namespace Bit.Bink;

/// <summary>Path drawing verbs. The number of coordinates consumed by each verb is given by <see cref="BinkPathGeometry.CoordinateCount"/>.</summary>
public enum BinkPathVerb : byte
{
    /// <summary>Starts a new sub-path (2 coordinates).</summary>
    MoveTo = 0,
    /// <summary>Straight line (2 coordinates).</summary>
    LineTo = 1,
    /// <summary>Quadratic Bézier: control point then end point (4 coordinates).</summary>
    QuadTo = 2,
    /// <summary>Cubic Bézier: two control points then end point (6 coordinates).</summary>
    CubicTo = 3,
    /// <summary>Closes the current sub-path (0 coordinates).</summary>
    Close = 4,
}

/// <summary>Fill rule used when a path self-intersects.</summary>
public enum BinkFillRule : byte
{
    /// <summary>Non-zero winding: self-overlapping regions of a freehand outline stay solid.</summary>
    NonZero = 0,
    /// <summary>Even-odd: overlapping regions alternate between filled and empty.</summary>
    EvenOdd = 1,
}

/// <summary>
/// An immutable vector path made of <see cref="BinkPathVerb"/>s and packed float coordinates.
/// Paths are produced by <see cref="BinkPathBuilder"/> and consumed by every renderer (canvas, SVG, raster).
/// </summary>
public sealed class BinkPathGeometry
{
    /// <summary>An empty path.</summary>
    public static readonly BinkPathGeometry Empty = new([], [], BinkRect.Empty);

    private readonly BinkPathVerb[] _verbs;
    private readonly float[] _coords;

    internal BinkPathGeometry(BinkPathVerb[] verbs, float[] coords, BinkRect bounds)
    {
        _verbs = verbs;
        _coords = coords;
        Bounds = bounds;
    }

    /// <summary>The verbs in drawing order.</summary>
    public ReadOnlySpan<BinkPathVerb> Verbs => _verbs;

    /// <summary>Packed coordinates (x0, y0, x1, y1, ...) consumed by the verbs in order.</summary>
    public ReadOnlySpan<float> Coordinates => _coords;

    /// <summary>Tight bounding box of all control points (a conservative bound of the actual curve).</summary>
    public BinkRect Bounds { get; }

    /// <summary>True when the path contains no verbs.</summary>
    public bool IsEmpty => _verbs.Length == 0;

    /// <summary>Number of verbs.</summary>
    public int VerbCount => _verbs.Length;

    /// <summary>Number of coordinates consumed by a verb.</summary>
    public static int CoordinateCount(BinkPathVerb verb) => verb switch
    {
        BinkPathVerb.MoveTo => 2,
        BinkPathVerb.LineTo => 2,
        BinkPathVerb.QuadTo => 4,
        BinkPathVerb.CubicTo => 6,
        _ => 0,
    };

    /// <summary>Returns a new path with every coordinate transformed.</summary>
    public BinkPathGeometry Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity || IsEmpty) return this;
        float[] coords = new float[_coords.Length];
        for (int i = 0; i < coords.Length; i += 2)
        {
            Vector2 p = Vector2.Transform(new Vector2(_coords[i], _coords[i + 1]), matrix);
            coords[i] = p.X;
            coords[i + 1] = p.Y;
        }
        return new BinkPathGeometry(_verbs, coords, Bounds.Transform(matrix));
    }

    /// <summary>
    /// Flattens the path into polylines (one per sub-path). Curves are subdivided until the deviation is below <paramref name="tolerance"/>.
    /// </summary>
    /// <param name="tolerance">Maximum distance between the curve and its polyline approximation.</param>
    /// <param name="onSubPath">Called with the points of each sub-path and whether it was explicitly closed.</param>
    public void Flatten(float tolerance, Action<ReadOnlyMemory<Vector2>, bool> onSubPath)
    {
        var points = new List<Vector2>();
        int c = 0;
        Vector2 current = default;
        bool closed = false;
        // True while the only point held is the implicit start a Close leaves behind for a following LineTo.
        bool implicitStart = false;

        void Emit()
        {
            if (points.Count > 0) onSubPath(points.ToArray(), closed);
            points.Clear();
            closed = false;
        }

        foreach (BinkPathVerb verb in _verbs)
        {
            switch (verb)
            {
                case BinkPathVerb.MoveTo:
                    // The point a Close left behind is not a sub-path of its own: emitting it would make a stroker
                    // draw a cap dot at the start of every closed sub-path that is followed by another one.
                    if (implicitStart) points.Clear();
                    implicitStart = false;
                    Emit();
                    current = new Vector2(_coords[c], _coords[c + 1]);
                    c += 2;
                    points.Add(current);
                    break;
                case BinkPathVerb.LineTo:
                    implicitStart = false;
                    current = new Vector2(_coords[c], _coords[c + 1]);
                    c += 2;
                    points.Add(current);
                    break;
                case BinkPathVerb.QuadTo:
                {
                    implicitStart = false;
                    var p1 = new Vector2(_coords[c], _coords[c + 1]);
                    var p2 = new Vector2(_coords[c + 2], _coords[c + 3]);
                    c += 4;
                    FlattenQuad(current, p1, p2, tolerance, points);
                    current = p2;
                    break;
                }
                case BinkPathVerb.CubicTo:
                {
                    implicitStart = false;
                    var p1 = new Vector2(_coords[c], _coords[c + 1]);
                    var p2 = new Vector2(_coords[c + 2], _coords[c + 3]);
                    var p3 = new Vector2(_coords[c + 4], _coords[c + 5]);
                    c += 6;
                    FlattenCubic(current, p1, p2, p3, tolerance, points);
                    current = p3;
                    break;
                }
                case BinkPathVerb.Close:
                    closed = true;
                    if (points.Count > 0)
                    {
                        Vector2 start = points[0];
                        Emit();
                        current = start;
                        // A subsequent LineTo without MoveTo continues from the sub-path start.
                        points.Add(start);
                        implicitStart = true;
                    }
                    break;
            }
        }

        // Trailing implicit MoveTo after a Close leaves a single point; drop it.
        if (points.Count > 1) Emit();
    }

    /// <summary>Adds a flattened quadratic Bézier (excluding the start point) to <paramref name="output"/>.</summary>
    public static void FlattenQuad(Vector2 p0, Vector2 p1, Vector2 p2, float tolerance, List<Vector2> output)
    {
        int segments = SegmentCountForQuad(p0, p1, p2, tolerance);
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float mt = 1 - t;
            output.Add(p0 * (mt * mt) + p1 * (2 * mt * t) + p2 * (t * t));
        }
    }

    /// <summary>Adds a flattened cubic Bézier (excluding the start point) to <paramref name="output"/>.</summary>
    public static void FlattenCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float tolerance, List<Vector2> output)
    {
        int segments = SegmentCountForCubic(p0, p1, p2, p3, tolerance);
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float mt = 1 - t;
            output.Add(p0 * (mt * mt * mt) + p1 * (3 * mt * mt * t) + p2 * (3 * mt * t * t) + p3 * (t * t * t));
        }
    }

    /// <summary>Segment count so that a quadratic's flattening error stays below <paramref name="tolerance"/> (Wang's formula).</summary>
    public static int SegmentCountForQuad(Vector2 p0, Vector2 p1, Vector2 p2, float tolerance)
    {
        tolerance = MathF.Max(tolerance, 1e-3f);
        float dd = (p0 - 2 * p1 + p2).Length();
        int n = (int)MathF.Ceiling(MathF.Sqrt(dd / (8 * tolerance) * 4));
        return Math.Clamp(n, 1, 64);
    }

    /// <summary>Segment count so that a cubic's flattening error stays below <paramref name="tolerance"/> (Wang's formula).</summary>
    public static int SegmentCountForCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float tolerance)
    {
        tolerance = MathF.Max(tolerance, 1e-3f);
        float dd = MathF.Max((p0 - 2 * p1 + p2).Length(), (p1 - 2 * p2 + p3).Length());
        int n = (int)MathF.Ceiling(MathF.Sqrt(dd * 6 / (8 * tolerance)));
        return Math.Clamp(n, 1, 96);
    }

    /// <summary>Writes the path in SVG path-data syntax using invariant culture and a fixed number of decimals.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="decimals">Number of decimals for each coordinate.</param>
    public void WriteSvgPathData(TextWriter writer, int decimals = 2)
    {
        int c = 0;
        decimals = Math.Clamp(decimals, 0, 6);
        Span<char> buffer = stackalloc char[BinkSvgNumber.MaxLength];
        bool first = true;
        foreach (BinkPathVerb verb in _verbs)
        {
            if (!first) writer.Write(' ');
            first = false;
            switch (verb)
            {
                case BinkPathVerb.MoveTo: writer.Write('M'); WriteCoords(writer, buffer, decimals, ref c, 2); break;
                case BinkPathVerb.LineTo: writer.Write('L'); WriteCoords(writer, buffer, decimals, ref c, 2); break;
                case BinkPathVerb.QuadTo: writer.Write('Q'); WriteCoords(writer, buffer, decimals, ref c, 4); break;
                case BinkPathVerb.CubicTo: writer.Write('C'); WriteCoords(writer, buffer, decimals, ref c, 6); break;
                case BinkPathVerb.Close: writer.Write('Z'); break;
            }
        }
    }

    private void WriteCoords(TextWriter writer, Span<char> buffer, int decimals, ref int c, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (i > 0) writer.Write(i % 2 == 0 ? ' ' : ',');
            writer.Write(buffer[..BinkSvgNumber.Format(_coords[c++], decimals, buffer)]);
        }
    }

    /// <summary>Returns the path in SVG path-data syntax.</summary>
    public string ToSvgPathData(int decimals = 2)
    {
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        WriteSvgPathData(writer, decimals);
        return writer.ToString();
    }
}
