using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A reusable, allocation-conscious builder for <see cref="BinkPathGeometry"/>.
/// Reuse one instance across frames and call <see cref="Reset"/> to avoid garbage.
/// </summary>
public sealed class BinkPathBuilder
{
    private BinkPathVerb[] _verbs;
    private float[] _coords;
    private int _verbCount;
    private int _coordCount;
    private float _minX = float.MaxValue, _minY = float.MaxValue, _maxX = float.MinValue, _maxY = float.MinValue;
    private Vector2 _start;
    private Vector2 _current;

    /// <summary>Creates a builder with a default initial capacity.</summary>
    public BinkPathBuilder() : this(64) { }

    /// <summary>Creates a builder with capacity for roughly <paramref name="pointCapacity"/> points.</summary>
    public BinkPathBuilder(int pointCapacity)
    {
        pointCapacity = Math.Max(pointCapacity, 4);
        _verbs = new BinkPathVerb[pointCapacity];
        _coords = new float[pointCapacity * 2];
    }

    /// <summary>Number of verbs added so far.</summary>
    public int VerbCount => _verbCount;

    /// <summary>The current pen position.</summary>
    public Vector2 CurrentPoint => _current;

    /// <summary>True when nothing has been added.</summary>
    public bool IsEmpty => _verbCount == 0;

    /// <summary>Verbs added so far.</summary>
    public ReadOnlySpan<BinkPathVerb> Verbs => _verbs.AsSpan(0, _verbCount);

    /// <summary>Coordinates added so far.</summary>
    public ReadOnlySpan<float> Coordinates => _coords.AsSpan(0, _coordCount);

    /// <summary>Bounding box of the control points so far.</summary>
    public BinkRect Bounds => _verbCount == 0 ? BinkRect.Empty : BinkRect.FromLTRB(_minX, _minY, _maxX, _maxY);

    /// <summary>Clears the builder while keeping its buffers.</summary>
    public void Reset()
    {
        _verbCount = 0;
        _coordCount = 0;
        _minX = float.MaxValue; _minY = float.MaxValue; _maxX = float.MinValue; _maxY = float.MinValue;
        _start = _current = default;
    }

    /// <summary>Starts a new sub-path.</summary>
    public BinkPathBuilder MoveTo(float x, float y)
    {
        Add(BinkPathVerb.MoveTo);
        Push(x, y);
        _start = _current = new Vector2(x, y);
        return this;
    }

    /// <summary>Starts a new sub-path.</summary>
    public BinkPathBuilder MoveTo(Vector2 p) => MoveTo(p.X, p.Y);

    /// <summary>Adds a straight line.</summary>
    public BinkPathBuilder LineTo(float x, float y)
    {
        EnsureOpen();
        Add(BinkPathVerb.LineTo);
        Push(x, y);
        _current = new Vector2(x, y);
        return this;
    }

    /// <summary>Adds a straight line.</summary>
    public BinkPathBuilder LineTo(Vector2 p) => LineTo(p.X, p.Y);

    /// <summary>Adds a quadratic Bézier curve.</summary>
    public BinkPathBuilder QuadTo(float cx, float cy, float x, float y)
    {
        EnsureOpen();
        Add(BinkPathVerb.QuadTo);
        Push(cx, cy);
        Push(x, y);
        _current = new Vector2(x, y);
        return this;
    }

    /// <summary>Adds a quadratic Bézier curve.</summary>
    public BinkPathBuilder QuadTo(Vector2 control, Vector2 end) => QuadTo(control.X, control.Y, end.X, end.Y);

    /// <summary>Adds a cubic Bézier curve.</summary>
    public BinkPathBuilder CubicTo(float c1x, float c1y, float c2x, float c2y, float x, float y)
    {
        EnsureOpen();
        Add(BinkPathVerb.CubicTo);
        Push(c1x, c1y);
        Push(c2x, c2y);
        Push(x, y);
        _current = new Vector2(x, y);
        return this;
    }

    /// <summary>Adds a cubic Bézier curve.</summary>
    public BinkPathBuilder CubicTo(Vector2 c1, Vector2 c2, Vector2 end) => CubicTo(c1.X, c1.Y, c2.X, c2.Y, end.X, end.Y);

    /// <summary>Closes the current sub-path.</summary>
    public BinkPathBuilder Close()
    {
        if (_verbCount > 0 && _verbs[_verbCount - 1] != BinkPathVerb.Close)
        {
            Add(BinkPathVerb.Close);
            _current = _start;
        }
        return this;
    }

    /// <summary>Adds a closed rectangle.</summary>
    public BinkPathBuilder AddRect(BinkRect rect)
    {
        MoveTo(rect.Left, rect.Top);
        LineTo(rect.Right, rect.Top);
        LineTo(rect.Right, rect.Bottom);
        LineTo(rect.Left, rect.Bottom);
        return Close();
    }

    /// <summary>Adds a closed rounded rectangle.</summary>
    public BinkPathBuilder AddRoundedRect(BinkRect rect, float radius)
    {
        radius = MathF.Min(radius, MathF.Min(rect.Width, rect.Height) / 2f);
        if (radius <= 0) return AddRect(rect);
        const float k = 0.5522847498f; // 4/3 * (sqrt(2) - 1): cubic approximation of a quarter circle
        float kr = k * radius;
        float l = rect.Left, t = rect.Top, r = rect.Right, b = rect.Bottom;
        MoveTo(l + radius, t);
        LineTo(r - radius, t);
        CubicTo(r - radius + kr, t, r, t + radius - kr, r, t + radius);
        LineTo(r, b - radius);
        CubicTo(r, b - radius + kr, r - radius + kr, b, r - radius, b);
        LineTo(l + radius, b);
        CubicTo(l + radius - kr, b, l, b - radius + kr, l, b - radius);
        LineTo(l, t + radius);
        CubicTo(l, t + radius - kr, l + radius - kr, t, l + radius, t);
        return Close();
    }

    /// <summary>Adds a closed ellipse inscribed in the rectangle.</summary>
    public BinkPathBuilder AddEllipse(BinkRect rect)
    {
        const float k = 0.5522847498f;
        float cx = rect.Center.X, cy = rect.Center.Y;
        float rx = rect.Width / 2f, ry = rect.Height / 2f;
        float kx = rx * k, ky = ry * k;
        MoveTo(cx + rx, cy);
        CubicTo(cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry);
        CubicTo(cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy);
        CubicTo(cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry);
        CubicTo(cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy);
        return Close();
    }

    /// <summary>Adds a circle.</summary>
    public BinkPathBuilder AddCircle(Vector2 center, float radius) => AddEllipse(BinkRect.FromCenter(center, radius * 2, radius * 2));

    /// <summary>Adds a polyline (open).</summary>
    public BinkPathBuilder AddPolyline(ReadOnlySpan<Vector2> points)
    {
        if (points.IsEmpty) return this;
        MoveTo(points[0]);
        for (int i = 1; i < points.Length; i++) LineTo(points[i]);
        return this;
    }

    /// <summary>Adds a closed polygon.</summary>
    public BinkPathBuilder AddPolygon(ReadOnlySpan<Vector2> points)
    {
        if (points.IsEmpty) return this;
        AddPolyline(points);
        return Close();
    }

    /// <summary>
    /// Adds a closed, smooth outline through the given polygon vertices using quadratic curves between segment midpoints.
    /// This is the standard technique for rendering freehand stroke outlines without visible corners.
    /// </summary>
    public BinkPathBuilder AddSmoothPolygon(ReadOnlySpan<Vector2> points)
    {
        int n = points.Length;
        if (n < 3) return AddPolygon(points);
        Vector2 mid0 = (points[0] + points[1]) / 2f;
        MoveTo(mid0);
        for (int i = 1; i < n; i++)
        {
            Vector2 next = points[(i + 1) % n];
            Vector2 mid = (points[i] + next) / 2f;
            QuadTo(points[i], mid);
        }
        QuadTo(points[0], mid0);
        return Close();
    }

    /// <summary>
    /// Adds an open, smooth curve through the given points (Catmull-Rom converted to cubic Béziers).
    /// </summary>
    /// <param name="points">Points to pass through.</param>
    /// <param name="tension">0 = standard Catmull-Rom (smooth), 1 = straight segments.</param>
    public BinkPathBuilder AddCatmullRom(ReadOnlySpan<Vector2> points, float tension = 0f)
    {
        int n = points.Length;
        if (n == 0) return this;
        if (n == 1) { MoveTo(points[0]); return this; }
        if (n == 2) { MoveTo(points[0]); LineTo(points[1]); return this; }
        float s = (1f - Math.Clamp(tension, 0f, 1f)) / 6f;
        MoveTo(points[0]);
        for (int i = 0; i < n - 1; i++)
        {
            Vector2 p0 = points[Math.Max(i - 1, 0)];
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];
            Vector2 p3 = points[Math.Min(i + 2, n - 1)];
            Vector2 c1 = p1 + (p2 - p0) * s;
            Vector2 c2 = p2 - (p3 - p1) * s;
            CubicTo(c1, c2, p2);
        }
        return this;
    }

    /// <summary>Appends another path.</summary>
    public BinkPathBuilder AddPath(BinkPathGeometry path)
    {
        ReadOnlySpan<float> coords = path.Coordinates;
        int c = 0;
        foreach (BinkPathVerb verb in path.Verbs)
        {
            switch (verb)
            {
                case BinkPathVerb.MoveTo: MoveTo(coords[c], coords[c + 1]); c += 2; break;
                case BinkPathVerb.LineTo: LineTo(coords[c], coords[c + 1]); c += 2; break;
                case BinkPathVerb.QuadTo: QuadTo(coords[c], coords[c + 1], coords[c + 2], coords[c + 3]); c += 4; break;
                case BinkPathVerb.CubicTo: CubicTo(coords[c], coords[c + 1], coords[c + 2], coords[c + 3], coords[c + 4], coords[c + 5]); c += 6; break;
                case BinkPathVerb.Close: Close(); break;
            }
        }
        return this;
    }

    /// <summary>Creates an immutable snapshot of the path. The builder can keep being used afterwards.</summary>
    public BinkPathGeometry ToGeometry()
    {
        if (_verbCount == 0) return BinkPathGeometry.Empty;
        return new BinkPathGeometry(_verbs.AsSpan(0, _verbCount).ToArray(), _coords.AsSpan(0, _coordCount).ToArray(), Bounds);
    }

    private void EnsureOpen()
    {
        if (_verbCount == 0 || _verbs[_verbCount - 1] == BinkPathVerb.Close) MoveTo(_current);
    }

    private void Add(BinkPathVerb verb)
    {
        if (_verbCount == _verbs.Length) Array.Resize(ref _verbs, _verbs.Length * 2);
        _verbs[_verbCount++] = verb;
    }

    private void Push(float x, float y)
    {
        if (_coordCount + 2 > _coords.Length) Array.Resize(ref _coords, _coords.Length * 2);
        _coords[_coordCount++] = x;
        _coords[_coordCount++] = y;
        if (x < _minX) _minX = x;
        if (x > _maxX) _maxX = x;
        if (y < _minY) _minY = y;
        if (y > _maxY) _maxY = y;
    }
}
