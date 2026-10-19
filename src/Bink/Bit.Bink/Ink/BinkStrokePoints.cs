using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Immutable, structure-of-arrays storage for the points of a stroke. Designed so rendering, hit testing
/// and serialization read raw spans without allocating per point. Optional channels (tilt, twist) are
/// only stored when present.
/// </summary>
public sealed class BinkStrokePoints
{
    /// <summary>An empty point set.</summary>
    public static readonly BinkStrokePoints Empty = new([], [], [], [], null, null, null, 0);

    private readonly float[] _x;
    private readonly float[] _y;
    private readonly float[] _pressure;
    private readonly float[] _time;
    private readonly float[]? _tiltX;
    private readonly float[]? _tiltY;
    private readonly float[]? _twist;

    internal BinkStrokePoints(float[] x, float[] y, float[] pressure, float[] time, float[]? tiltX, float[]? tiltY, float[]? twist, int count)
    {
        _x = x;
        _y = y;
        _pressure = pressure;
        _time = time;
        _tiltX = tiltX;
        _tiltY = tiltY;
        _twist = twist;
        Count = count;
        Bounds = ComputeBounds(x, y, count);
    }

    /// <summary>Number of points.</summary>
    public int Count { get; }

    /// <summary>Bounding box of the point positions (without stroke width).</summary>
    public BinkRect Bounds { get; }

    /// <summary>X coordinates.</summary>
    public ReadOnlySpan<float> X => _x.AsSpan(0, Count);

    /// <summary>Y coordinates.</summary>
    public ReadOnlySpan<float> Y => _y.AsSpan(0, Count);

    /// <summary>Pressure values (0..1).</summary>
    public ReadOnlySpan<float> Pressure => _pressure.AsSpan(0, Count);

    /// <summary>Milliseconds since stroke start.</summary>
    public ReadOnlySpan<float> Time => _time.AsSpan(0, Count);

    /// <summary>Tilt X values, or empty when the stroke has no tilt channel.</summary>
    public ReadOnlySpan<float> TiltX => _tiltX is null ? default : _tiltX.AsSpan(0, Count);

    /// <summary>Tilt Y values, or empty when the stroke has no tilt channel.</summary>
    public ReadOnlySpan<float> TiltY => _tiltY is null ? default : _tiltY.AsSpan(0, Count);

    /// <summary>Twist values, or empty when the stroke has no twist channel.</summary>
    public ReadOnlySpan<float> Twist => _twist is null ? default : _twist.AsSpan(0, Count);

    /// <summary>Which optional channels are present.</summary>
    public BinkPointChannels Channels => (_tiltX is null ? BinkPointChannels.None : BinkPointChannels.Tilt) | (_twist is null ? BinkPointChannels.None : BinkPointChannels.Twist);

    /// <summary>Gets a point by index.</summary>
    public BinkPoint this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return new BinkPoint
            {
                X = _x[index],
                Y = _y[index],
                Pressure = _pressure[index],
                Time = _time[index],
                TiltX = _tiltX?[index] ?? 0f,
                TiltY = _tiltY?[index] ?? 0f,
                Twist = _twist?[index] ?? 0f,
            };
        }
    }

    /// <summary>Position of a point.</summary>
    public Vector2 PositionAt(int index) => new(_x[index], _y[index]);

    /// <summary>Total polyline length.</summary>
    public float Length
    {
        get
        {
            float len = 0;
            for (int i = 1; i < Count; i++) len += Vector2.Distance(PositionAt(i - 1), PositionAt(i));
            return len;
        }
    }

    /// <summary>Duration in milliseconds between the first and last point.</summary>
    public float Duration => Count == 0 ? 0 : _time[Count - 1] - _time[0];

    /// <summary>
    /// Velocity at a point in document units per millisecond, estimated from its neighbours. Returns 0 for a single point.
    /// </summary>
    public float VelocityAt(int index)
    {
        if (Count < 2) return 0;
        int a = Math.Max(index - 1, 0);
        int b = Math.Min(index + 1, Count - 1);
        float dt = _time[b] - _time[a];
        if (dt <= 0) return 0;
        return Vector2.Distance(PositionAt(a), PositionAt(b)) / dt;
    }

    /// <summary>Creates points from an array of <see cref="BinkPoint"/>. Tilt/twist channels are kept only when any value is non-zero.</summary>
    public static BinkStrokePoints FromPoints(ReadOnlySpan<BinkPoint> points)
    {
        var buffer = new BinkStrokePointBuffer(points.Length);
        foreach (BinkPoint p in points) buffer.Add(p);
        return buffer.ToStrokePoints();
    }

    /// <summary>Creates points from plain positions with constant pressure.</summary>
    public static BinkStrokePoints FromPositions(ReadOnlySpan<Vector2> positions, float pressure = 0.5f)
    {
        var buffer = new BinkStrokePointBuffer(positions.Length);
        for (int i = 0; i < positions.Length; i++) buffer.Add(new BinkPoint(positions[i].X, positions[i].Y, pressure, i));
        return buffer.ToStrokePoints();
    }

    /// <summary>
    /// Creates points from arrays this instance takes ownership of. Callers must not modify them afterwards.
    /// Used by the decoder, which has already built exactly the arrays needed, so nothing is copied twice.
    /// </summary>
    internal static BinkStrokePoints FromOwnedArrays(float[] x, float[] y, float[] pressure, float[] time, float[]? tiltX, float[]? tiltY, float[]? twist)
        => new(x, y, pressure, time, tiltX, tiltY, twist, x.Length);

    /// <summary>Creates points from parallel arrays. Arrays are copied.</summary>
    public static BinkStrokePoints FromArrays(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> pressure = default, ReadOnlySpan<float> time = default, ReadOnlySpan<float> tiltX = default, ReadOnlySpan<float> tiltY = default, ReadOnlySpan<float> twist = default)
    {
        int n = x.Length;
        if (y.Length != n) throw new ArgumentException("X and Y must have the same length.", nameof(y));
        if (!pressure.IsEmpty && pressure.Length != n) throw new ArgumentException("Pressure length must match X.", nameof(pressure));
        if (!time.IsEmpty && time.Length != n) throw new ArgumentException("Time length must match X.", nameof(time));
        // Tilt is one channel: supplying only one axis would store an orphan array that Channels does not report.
        if ((!tiltX.IsEmpty || !tiltY.IsEmpty) && (tiltX.Length != n || tiltY.Length != n)) throw new ArgumentException("Tilt lengths must match X.", nameof(tiltX));
        if (!twist.IsEmpty && twist.Length != n) throw new ArgumentException("Twist length must match X.", nameof(twist));

        float[] p = pressure.IsEmpty ? Filled(n, 0.5f) : pressure.ToArray();
        float[] t;
        if (time.IsEmpty)
        {
            t = new float[n];
            for (int i = 0; i < n; i++) t[i] = i;
        }
        else
        {
            t = time.ToArray();
        }
        return new BinkStrokePoints(x.ToArray(), y.ToArray(), p, t, tiltX.IsEmpty ? null : tiltX.ToArray(), tiltY.IsEmpty ? null : tiltY.ToArray(), twist.IsEmpty ? null : twist.ToArray(), n);
    }

    private static float[] Filled(int n, float value)
    {
        float[] a = new float[n];
        Array.Fill(a, value);
        return a;
    }

    /// <summary>Returns a new point set with every position transformed.</summary>
    public BinkStrokePoints Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity || Count == 0) return this;
        float[] nx = new float[Count];
        float[] ny = new float[Count];
        for (int i = 0; i < Count; i++)
        {
            Vector2 p = Vector2.Transform(new Vector2(_x[i], _y[i]), matrix);
            nx[i] = p.X;
            ny[i] = p.Y;
        }
        return new BinkStrokePoints(nx, ny, _pressure, _time, _tiltX, _tiltY, _twist, Count);
    }

    /// <summary>Returns a contiguous sub-range of the points (arrays are copied, channels preserved).</summary>
    public BinkStrokePoints Slice(int start, int length)
    {
        if (start < 0 || length < 0 || start + length > Count) throw new ArgumentOutOfRangeException(nameof(start));
        return new BinkStrokePoints(
            _x.AsSpan(start, length).ToArray(),
            _y.AsSpan(start, length).ToArray(),
            _pressure.AsSpan(start, length).ToArray(),
            _time.AsSpan(start, length).ToArray(),
            _tiltX?.AsSpan(start, length).ToArray(),
            _tiltY?.AsSpan(start, length).ToArray(),
            _twist?.AsSpan(start, length).ToArray(),
            length);
    }

    /// <summary>Returns the points selected by <paramref name="indices"/> (must be increasing).</summary>
    public BinkStrokePoints Select(ReadOnlySpan<int> indices)
    {
        int n = indices.Length;
        float[] nx = new float[n], ny = new float[n], np = new float[n], nt = new float[n];
        float[]? tx = _tiltX is null ? null : new float[n];
        float[]? ty = _tiltY is null ? null : new float[n];
        float[]? tw = _twist is null ? null : new float[n];
        for (int i = 0; i < n; i++)
        {
            int j = indices[i];
            nx[i] = _x[j]; ny[i] = _y[j]; np[i] = _pressure[j]; nt[i] = _time[j];
            if (tx is not null) tx[i] = _tiltX![j];
            if (ty is not null) ty[i] = _tiltY![j];
            if (tw is not null) tw[i] = _twist![j];
        }
        return new BinkStrokePoints(nx, ny, np, nt, tx, ty, tw, n);
    }

    /// <summary>
    /// Simplifies the stroke, removing points that change neither its shape nor its width.
    /// </summary>
    /// <param name="tolerance">Maximum positional deviation in document units. Zero or negative returns the same instance.</param>
    /// <param name="pressureTolerance">
    /// Maximum deviation of pressure (0..1) from the straight line between retained points. Position-only
    /// simplification would collapse a straight stroke drawn with changing pressure into two points and throw
    /// its width variation away, so pressure is simplified as its own channel.
    /// </param>
    public BinkStrokePoints Simplify(float tolerance, float pressureTolerance = 0.04f)
    {
        if (tolerance <= 0 || Count <= 2) return this;
        var keep = new List<int>(Count);
        BinkGeometryMath.Simplify(X, Y, tolerance, keep);
        if (pressureTolerance > 0 && keep.Count < Count) RefineByPressure(keep, pressureTolerance);
        if (keep.Count == Count) return this;
        return Select(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(keep));
    }

    /// <summary>
    /// Adds back the points where pressure departs from the interpolation between the points already kept.
    /// This is the Ramer-Douglas-Peucker idea applied to the pressure channel.
    /// </summary>
    private void RefineByPressure(List<int> keep, float tolerance)
    {
        ReadOnlySpan<float> pressure = Pressure;
        var extra = new List<int>();
        var stack = new Stack<(int Start, int End)>();

        for (int k = 0; k < keep.Count - 1; k++) stack.Push((keep[k], keep[k + 1]));

        while (stack.Count > 0)
        {
            (int start, int end) = stack.Pop();
            if (end - start < 2) continue;
            float a = pressure[start];
            float b = pressure[end];
            float span = end - start;
            float worst = 0f;
            int index = -1;
            for (int i = start + 1; i < end; i++)
            {
                float expected = a + (b - a) * ((i - start) / span);
                float deviation = MathF.Abs(pressure[i] - expected);
                if (deviation > worst)
                {
                    worst = deviation;
                    index = i;
                }
            }
            if (index >= 0 && worst > tolerance)
            {
                extra.Add(index);
                stack.Push((start, index));
                stack.Push((index, end));
            }
        }

        if (extra.Count == 0) return;
        keep.AddRange(extra);
        keep.Sort();
    }

    private static BinkRect ComputeBounds(float[] x, float[] y, int count)
    {
        if (count == 0) return BinkRect.Empty;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < count; i++)
        {
            float px = x[i], py = y[i];
            if (px < minX) minX = px;
            if (px > maxX) maxX = px;
            if (py < minY) minY = py;
            if (py > maxY) maxY = py;
        }
        return BinkRect.FromLTRB(minX, minY, maxX, maxY);
    }
}
