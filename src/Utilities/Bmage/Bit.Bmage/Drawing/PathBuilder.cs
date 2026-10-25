using System.Numerics;

namespace Bit.Bmage.Drawing;

/// <summary>
/// A vector path made of straight and curved segments. Curves are flattened to line segments at construction time using
/// an adaptive tolerance, so the rasteriser only ever sees polygons.
/// </summary>
public sealed class VectorPath
{
    private readonly List<Vector2[]> _contours;

    internal VectorPath(List<Vector2[]> contours, bool[] closed)
    {
        _contours = contours;
        Closed = closed;
    }

    /// <summary>The flattened contours; each is a polyline of at least two points.</summary>
    public IReadOnlyList<Vector2[]> Contours => _contours;

    /// <summary>Whether each contour is closed.</summary>
    public IReadOnlyList<bool> Closed { get; }

    /// <summary>True when the path has nothing to draw.</summary>
    public bool IsEmpty => _contours.Count == 0;

    /// <summary>The axis-aligned bounding box of all points, or an empty rectangle for an empty path.</summary>
    public System.Drawing.RectangleF Bounds
    {
        get
        {
            if (_contours.Count == 0) return System.Drawing.RectangleF.Empty;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var c in _contours)
                foreach (var p in c)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
            return System.Drawing.RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }
    }

    /// <summary>Returns a copy of the path with every point transformed.</summary>
    public VectorPath Transform(Matrix3x2 matrix)
    {
        var contours = new List<Vector2[]>(_contours.Count);
        foreach (var c in _contours)
        {
            var t = new Vector2[c.Length];
            for (var i = 0; i < c.Length; i++) t[i] = Vector2.Transform(c[i], matrix);
            contours.Add(t);
        }
        return new VectorPath(contours, (bool[])((bool[])Closed).Clone());
    }
}

/// <summary>Builds a <see cref="VectorPath"/> from move/line/curve commands, flattening curves as it goes.</summary>
public sealed class PathBuilder
{
    private readonly List<Vector2[]> _contours = [];
    private readonly List<bool> _closed = [];
    private readonly List<Vector2> _current = [];
    private Vector2 _start;
    private Vector2 _position;
    private bool _hasCurrent;

    /// <summary>Maximum deviation in pixels allowed when flattening curves. Smaller is smoother and slower.</summary>
    public float FlatteningTolerance { get; set; } = 0.2f;

    /// <summary>The current point.</summary>
    public Vector2 Position => _position;

    /// <summary>Starts a new contour at the given point.</summary>
    public PathBuilder MoveTo(Vector2 point)
    {
        EndContour(false);
        _current.Add(point);
        _start = point;
        _position = point;
        _hasCurrent = true;
        return this;
    }

    /// <summary>Starts a new contour at the given point.</summary>
    public PathBuilder MoveTo(float x, float y) => MoveTo(new Vector2(x, y));

    /// <summary>Adds a straight segment to the given point.</summary>
    public PathBuilder LineTo(Vector2 point)
    {
        if (!_hasCurrent) return MoveTo(point);
        if (Vector2.DistanceSquared(_position, point) > 1e-12f) _current.Add(point);
        _position = point;
        return this;
    }

    /// <summary>Adds a straight segment to the given point.</summary>
    public PathBuilder LineTo(float x, float y) => LineTo(new Vector2(x, y));

    /// <summary>Adds a quadratic Bézier segment.</summary>
    public PathBuilder QuadraticTo(Vector2 control, Vector2 end)
    {
        if (!_hasCurrent) MoveTo(control);
        var p0 = _position;
        var steps = EstimateQuadraticSteps(p0, control, end, FlatteningTolerance);
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (float)steps;
            var mt = 1 - t;
            var p = mt * mt * p0 + 2 * mt * t * control + t * t * end;
            _current.Add(p);
        }
        _position = end;
        return this;
    }

    /// <summary>Adds a cubic Bézier segment.</summary>
    public PathBuilder CubicTo(Vector2 control1, Vector2 control2, Vector2 end)
    {
        if (!_hasCurrent) MoveTo(control1);
        var p0 = _position;
        var steps = EstimateCubicSteps(p0, control1, control2, end, FlatteningTolerance);
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (float)steps;
            var mt = 1 - t;
            var p = mt * mt * mt * p0 + 3 * mt * mt * t * control1 + 3 * mt * t * t * control2 + t * t * t * end;
            _current.Add(p);
        }
        _position = end;
        return this;
    }

    /// <summary>Adds an elliptical arc segment, sweeping from <paramref name="startAngle"/> by <paramref name="sweepAngle"/> (radians).</summary>
    public PathBuilder ArcTo(Vector2 center, float radiusX, float radiusY, float startAngle, float sweepAngle)
    {
        var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweepAngle) / (2 * Math.PI) * EstimateEllipseSteps(radiusX, radiusY, FlatteningTolerance)));
        for (var i = 0; i <= steps; i++)
        {
            var a = startAngle + sweepAngle * i / steps;
            var p = new Vector2(center.X + radiusX * MathF.Cos(a), center.Y + radiusY * MathF.Sin(a));
            if (i == 0 && !_hasCurrent) MoveTo(p);
            else LineTo(p);
        }
        return this;
    }

    /// <summary>Closes the current contour by joining its end back to its start.</summary>
    public PathBuilder Close()
    {
        if (_hasCurrent && _current.Count > 0)
        {
            _position = _start;
            EndContour(true);
        }
        return this;
    }

    /// <summary>Adds a closed rectangle contour.</summary>
    /// <remarks>
    /// A rectangle with a negative width or height describes the region between its two corners, the same as its
    /// normalised form. That is what a pointer drag up and to the left produces, so an editor does not have to
    /// normalise before drawing. The contour is wound in the opposite direction in that case, which is invisible for
    /// a single shape but does cancel against an overlapping contour under the non-zero fill rule.
    /// </remarks>
    public PathBuilder AddRectangle(System.Drawing.RectangleF rect)
    {
        MoveTo(rect.Left, rect.Top);
        LineTo(rect.Right, rect.Top);
        LineTo(rect.Right, rect.Bottom);
        LineTo(rect.Left, rect.Bottom);
        return Close();
    }

    /// <summary>Adds a closed rounded-rectangle contour. The radius is clamped to half the smaller side.</summary>
    public PathBuilder AddRoundedRectangle(System.Drawing.RectangleF rect, float radius)
    {
        radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);
        if (radius <= 0) return AddRectangle(rect);
        var k = radius * 0.5522847f; // circle approximation constant
        MoveTo(rect.Left + radius, rect.Top);
        LineTo(rect.Right - radius, rect.Top);
        CubicTo(new Vector2(rect.Right - radius + k, rect.Top), new Vector2(rect.Right, rect.Top + radius - k), new Vector2(rect.Right, rect.Top + radius));
        LineTo(rect.Right, rect.Bottom - radius);
        CubicTo(new Vector2(rect.Right, rect.Bottom - radius + k), new Vector2(rect.Right - radius + k, rect.Bottom), new Vector2(rect.Right - radius, rect.Bottom));
        LineTo(rect.Left + radius, rect.Bottom);
        CubicTo(new Vector2(rect.Left + radius - k, rect.Bottom), new Vector2(rect.Left, rect.Bottom - radius + k), new Vector2(rect.Left, rect.Bottom - radius));
        LineTo(rect.Left, rect.Top + radius);
        CubicTo(new Vector2(rect.Left, rect.Top + radius - k), new Vector2(rect.Left + radius - k, rect.Top), new Vector2(rect.Left + radius, rect.Top));
        return Close();
    }

    /// <summary>Adds a closed ellipse contour inscribed in the rectangle.</summary>
    public PathBuilder AddEllipse(System.Drawing.RectangleF rect)
    {
        var center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        ArcTo(center, rect.Width / 2, rect.Height / 2, 0, MathF.Tau);
        return Close();
    }

    /// <summary>Adds a polygon contour through the given points.</summary>
    public PathBuilder AddPolygon(IReadOnlyList<Vector2> points, bool close = true)
    {
        if (points is null || points.Count == 0) return this;
        MoveTo(points[0]);
        for (var i = 1; i < points.Count; i++) LineTo(points[i]);
        return close ? Close() : this;
    }

    /// <summary>Adds an open polyline through the given points.</summary>
    public PathBuilder AddPolyline(IReadOnlyList<Vector2> points) => AddPolygon(points, close: false);

    /// <summary>Finishes the path. The builder can be reused after this call.</summary>
    public VectorPath Build()
    {
        EndContour(false);
        var path = new VectorPath([.. _contours], [.. _closed]);
        _contours.Clear();
        _closed.Clear();
        return path;
    }

    private void EndContour(bool closed)
    {
        if (_current.Count >= 2)
        {
            _contours.Add([.. _current]);
            _closed.Add(closed);
        }
        _current.Clear();
        _hasCurrent = false;
    }

    private static int EstimateQuadraticSteps(Vector2 p0, Vector2 p1, Vector2 p2, float tolerance)
    {
        // Maximum second derivative magnitude bounds the flattening error.
        var d = Vector2.Distance(p0, p1) + Vector2.Distance(p1, p2);
        return Math.Clamp((int)Math.Ceiling(MathF.Sqrt(d / MathF.Max(tolerance, 1e-3f)) * 1.5f), 1, 256);
    }

    private static int EstimateCubicSteps(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float tolerance)
    {
        var d = Vector2.Distance(p0, p1) + Vector2.Distance(p1, p2) + Vector2.Distance(p2, p3);
        return Math.Clamp((int)Math.Ceiling(MathF.Sqrt(d / MathF.Max(tolerance, 1e-3f)) * 2f), 1, 512);
    }

    private static int EstimateEllipseSteps(float rx, float ry, float tolerance)
    {
        var r = MathF.Max(MathF.Abs(rx), MathF.Abs(ry));
        if (r <= 0) return 8;
        // Steps so the sagitta of each chord stays under the tolerance.
        var theta = 2 * MathF.Acos(Math.Clamp(1 - MathF.Max(tolerance, 1e-3f) / r, -1f, 1f));
        return Math.Clamp((int)Math.Ceiling(MathF.Tau / MathF.Max(theta, 1e-4f)), 8, 1024);
    }
}
