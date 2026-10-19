using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A growable structure-of-arrays point buffer used while a stroke is being drawn. Appending never
/// allocates except when capacity doubles. Convert to an immutable <see cref="BinkStrokePoints"/> with
/// <see cref="ToStrokePoints"/> when the stroke completes.
/// </summary>
public sealed class BinkStrokePointBuffer
{
    private float[] _x;
    private float[] _y;
    private float[] _pressure;
    private float[] _time;
    private float[]? _tiltX;
    private float[]? _tiltY;
    private float[]? _twist;
    private float _minX = float.MaxValue, _minY = float.MaxValue, _maxX = float.MinValue, _maxY = float.MinValue;

    /// <summary>Creates a buffer with room for <paramref name="capacity"/> points.</summary>
    public BinkStrokePointBuffer(int capacity = 256)
    {
        capacity = Math.Max(capacity, 8);
        _x = new float[capacity];
        _y = new float[capacity];
        _pressure = new float[capacity];
        _time = new float[capacity];
    }

    /// <summary>Number of points.</summary>
    public int Count { get; private set; }

    /// <summary>Current capacity.</summary>
    public int Capacity => _x.Length;

    /// <summary>X coordinates.</summary>
    public ReadOnlySpan<float> X => _x.AsSpan(0, Count);

    /// <summary>Y coordinates.</summary>
    public ReadOnlySpan<float> Y => _y.AsSpan(0, Count);

    /// <summary>Pressure values.</summary>
    public ReadOnlySpan<float> Pressure => _pressure.AsSpan(0, Count);

    /// <summary>Time values.</summary>
    public ReadOnlySpan<float> Time => _time.AsSpan(0, Count);

    /// <summary>Tilt X values or empty.</summary>
    public ReadOnlySpan<float> TiltX => _tiltX is null ? default : _tiltX.AsSpan(0, Count);

    /// <summary>Tilt Y values or empty.</summary>
    public ReadOnlySpan<float> TiltY => _tiltY is null ? default : _tiltY.AsSpan(0, Count);

    /// <summary>Twist values or empty.</summary>
    public ReadOnlySpan<float> Twist => _twist is null ? default : _twist.AsSpan(0, Count);

    /// <summary>Bounds of the positions.</summary>
    public BinkRect Bounds => Count == 0 ? BinkRect.Empty : BinkRect.FromLTRB(_minX, _minY, _maxX, _maxY);

    /// <summary>Position of a point.</summary>
    public Vector2 PositionAt(int index) => new(_x[index], _y[index]);

    /// <summary>Pressure of a point.</summary>
    public float PressureAt(int index) => _pressure[index];

    /// <summary>Time of a point.</summary>
    public float TimeAt(int index) => _time[index];

    /// <summary>Last point, or default when empty.</summary>
    public BinkPoint Last => Count == 0 ? default : this[Count - 1];

    /// <summary>Gets a point by index.</summary>
    public BinkPoint this[int index] => new()
    {
        X = _x[index],
        Y = _y[index],
        Pressure = _pressure[index],
        Time = _time[index],
        TiltX = _tiltX?[index] ?? 0f,
        TiltY = _tiltY?[index] ?? 0f,
        Twist = _twist?[index] ?? 0f,
    };

    /// <summary>Appends a point.</summary>
    public void Add(in BinkPoint point)
    {
        if (Count == _x.Length) Grow();
        int i = Count;
        _x[i] = point.X;
        _y[i] = point.Y;
        _pressure[i] = point.Pressure;
        _time[i] = point.Time;
        if (point.TiltX != 0 || point.TiltY != 0 || _tiltX is not null)
        {
            EnsureTilt();
            _tiltX![i] = point.TiltX;
            _tiltY![i] = point.TiltY;
        }
        if (point.Twist != 0 || _twist is not null)
        {
            EnsureTwist();
            _twist![i] = point.Twist;
        }
        if (point.X < _minX) _minX = point.X;
        if (point.X > _maxX) _maxX = point.X;
        if (point.Y < _minY) _minY = point.Y;
        if (point.Y > _maxY) _maxY = point.Y;
        Count++;
    }

    /// <summary>Replaces the last point (used by input smoothing to refine the most recent sample).</summary>
    public void ReplaceLast(in BinkPoint point)
    {
        if (Count == 0) { Add(point); return; }
        int i = Count - 1;
        _x[i] = point.X;
        _y[i] = point.Y;
        _pressure[i] = point.Pressure;
        _time[i] = point.Time;
        // Same rule as Add: the first non-zero value allocates the channel, so a refined sample keeps its tilt.
        if (point.TiltX != 0 || point.TiltY != 0 || _tiltX is not null)
        {
            EnsureTilt();
            _tiltX![i] = point.TiltX;
            _tiltY![i] = point.TiltY;
        }
        if (point.Twist != 0 || _twist is not null)
        {
            EnsureTwist();
            _twist![i] = point.Twist;
        }
        // Bounds may now be slightly too large; that is acceptable (conservative) for a live stroke.
        if (point.X < _minX) _minX = point.X;
        if (point.X > _maxX) _maxX = point.X;
        if (point.Y < _minY) _minY = point.Y;
        if (point.Y > _maxY) _maxY = point.Y;
    }

    /// <summary>Removes all points while keeping capacity.</summary>
    public void Clear()
    {
        Count = 0;
        _minX = float.MaxValue; _minY = float.MaxValue; _maxX = float.MinValue; _maxY = float.MinValue;
        _tiltX = null;
        _tiltY = null;
        _twist = null;
    }

    /// <summary>Creates an immutable copy of the points.</summary>
    public BinkStrokePoints ToStrokePoints()
    {
        if (Count == 0) return BinkStrokePoints.Empty;
        return new BinkStrokePoints(
            _x.AsSpan(0, Count).ToArray(),
            _y.AsSpan(0, Count).ToArray(),
            _pressure.AsSpan(0, Count).ToArray(),
            _time.AsSpan(0, Count).ToArray(),
            _tiltX?.AsSpan(0, Count).ToArray(),
            _tiltY?.AsSpan(0, Count).ToArray(),
            _twist?.AsSpan(0, Count).ToArray(),
            Count);
    }

    private void Grow()
    {
        int n = _x.Length * 2;
        Array.Resize(ref _x, n);
        Array.Resize(ref _y, n);
        Array.Resize(ref _pressure, n);
        Array.Resize(ref _time, n);
        if (_tiltX is not null) { Array.Resize(ref _tiltX, n); Array.Resize(ref _tiltY, n); }
        if (_twist is not null) Array.Resize(ref _twist, n);
    }

    private void EnsureTilt()
    {
        if (_tiltX is not null) return;
        _tiltX = new float[_x.Length];
        _tiltY = new float[_x.Length];
    }

    private void EnsureTwist()
    {
        _twist ??= new float[_x.Length];
    }
}
