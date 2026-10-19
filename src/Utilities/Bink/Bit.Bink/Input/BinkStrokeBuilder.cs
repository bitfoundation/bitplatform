using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Turns raw pointer samples into the processed points of one stroke while it is being drawn.
/// </summary>
/// <remarks>
/// Processing per sample, all allocation-free:
/// <list type="bullet">
/// <item><b>Spacing filter</b>: samples closer than <c>BinkBrush.Spacing * BinkBrush.Width</c> to the previous stored point are merged
/// into it (their pressure is averaged), which removes sensor jitter and keeps documents small.</item>
/// <item><b>Streamline smoothing</b>: each stored position is pulled toward the previous one by <c>BinkBrush.Smoothing</c>,
/// an exponential filter that removes hand tremor without lag on straight sections.</item>
/// <item><b>Pressure</b>: real pen pressure is lightly smoothed; when the device has none and the brush asks for
/// it, pressure is derived from speed so fast movements thin out like a real pen.</item>
/// <item><b>Timestamps</b> are stored relative to the first sample.</item>
/// </list>
/// The last stored point is refined (not appended) when the pointer lifts, so strokes end exactly where the pen left.
/// </remarks>
public sealed class BinkStrokeBuilder
{
    private readonly BinkStrokePointBuffer _points = new(256);
    private BinkBrush _brush = new BinkPenBrush();
    private double _startTime;
    private Vector2 _lastRaw;
    private Vector2 _lastStored;
    private float _lastPressure;
    private double _lastTime;
    private float _speed;
    private bool _hasRealPressure;
    private int _pressureSamples;
    private BinkPointerKind _kind;
    private int _pointerId = -1;
    private Matrix3x2 _viewToDocument = Matrix3x2.Identity;

    /// <summary>True while a stroke is in progress.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The pointer id of the active stroke, or -1.</summary>
    public int PointerId => _pointerId;

    /// <summary>The brush of the active stroke.</summary>
    public BinkBrush Brush => _brush;

    /// <summary>Number of stored points.</summary>
    public int Count => _points.Count;

    /// <summary>The live points (valid until the next call that modifies the builder).</summary>
    public BinkStrokePointBuffer Points => _points;

    /// <summary>Bounds of the stored positions in document units.</summary>
    public BinkRect Bounds => _points.Bounds;

    /// <summary>Speed in document units per millisecond above which simulated pressure reaches its minimum.</summary>
    public float MaxSpeed { get; set; } = 3f;

    /// <summary>Smoothing factor 0..1 applied to real pressure (higher = smoother). Default 0.35.</summary>
    public float PressureSmoothing { get; set; } = 0.35f;

    /// <summary>Whether the active stroke has received real pressure values.</summary>
    public bool HasRealPressure => _hasRealPressure;

    /// <summary>Starts a stroke.</summary>
    /// <param name="sample">The pointer-down sample (view coordinates).</param>
    /// <param name="brush">The brush.</param>
    /// <param name="viewToDocument">Transform from view (CSS pixel) to document coordinates.</param>
    public void Begin(in BinkInputSample sample, BinkBrush brush, Matrix3x2 viewToDocument)
    {
        ArgumentNullException.ThrowIfNull(brush);
        _points.Clear();
        _brush = brush;
        _viewToDocument = viewToDocument;
        _startTime = sample.Timestamp;
        _lastTime = sample.Timestamp;
        _kind = sample.Kind;
        _pointerId = sample.PointerId;
        _hasRealPressure = false;
        _pressureSamples = 0;
        _speed = 0f;
        IsActive = true;

        Vector2 p = Vector2.Transform(sample.Position, viewToDocument);
        _lastRaw = p;
        _lastStored = p;
        float pressure = EffectivePressure(sample, 0f);
        _lastPressure = pressure;
        _points.Add(MakePoint(p, pressure, sample));
    }

    /// <summary>Adds a move sample. Returns true when a point was stored or refined (i.e. something changed visually).</summary>
    public bool Add(in BinkInputSample sample)
    {
        if (!IsActive) return false;
        Vector2 raw = Vector2.Transform(sample.Position, _viewToDocument);
        double dt = sample.Timestamp - _lastTime;
        float dist = Vector2.Distance(raw, _lastRaw);
        if (dt > 0)
        {
            float instant = dist / (float)dt;
            // Exponential average of speed keeps simulated pressure from flickering on noisy timestamps.
            _speed = _speed + (instant - _speed) * 0.35f;
        }
        _lastRaw = raw;
        if (dt > 0) _lastTime = sample.Timestamp;

        float pressure = EffectivePressure(sample, _speed);

        // Streamline: pull the new point toward the previous stored point.
        float t = 1f - Math.Clamp(_brush.Smoothing, 0f, 0.95f);
        Vector2 target = _lastStored + (raw - _lastStored) * t;

        float minSpacing = MathF.Max(_brush.Spacing * _brush.Width, 0.35f);
        float spacing2 = minSpacing * minSpacing;
        if ((target - _lastStored).LengthSquared() < spacing2)
        {
            // Too close: blend pressure into the last point instead of adding a new one.
            if (_points.Count > 0)
            {
                BinkPoint last = _points.Last;
                float blended = last.Pressure + (pressure - last.Pressure) * 0.5f;
                _points.ReplaceLast(last with { Pressure = blended, TiltX = sample.TiltX, TiltY = sample.TiltY, Twist = sample.Twist });
                _lastPressure = blended;
            }
            return false;
        }

        _lastStored = target;
        _lastPressure = pressure;
        _points.Add(MakePoint(target, pressure, sample));
        return true;
    }

    /// <summary>
    /// Ends the stroke with the final sample and returns the immutable points. The last stored point is moved
    /// to the exact lift position so the stroke does not fall short.
    /// </summary>
    public BinkStrokePoints End(in BinkInputSample sample)
    {
        if (!IsActive) return BinkStrokePoints.Empty;
        Vector2 raw = Vector2.Transform(sample.Position, _viewToDocument);
        float pressure = _points.Count > 0 ? _points.Last.Pressure : EffectivePressure(sample, _speed);
        if (_points.Count > 1)
        {
            BinkPoint last = _points.Last;
            // Only snap when the lift position is reasonably close; a wild final sample would create a spike.
            float snapLimit = MathF.Max(_brush.Width * 2f, 8f);
            if (Vector2.Distance(raw, last.Position) <= snapLimit)
                _points.ReplaceLast(last with { X = raw.X, Y = raw.Y, Time = (float)(sample.Timestamp - _startTime) });
            else
                _points.Add(MakePoint(raw, pressure, sample));
        }
        IsActive = false;
        _pointerId = -1;
        return _points.ToStrokePoints();
    }

    /// <summary>Ends the stroke without a final sample (used for cancellation or pointer loss).</summary>
    public BinkStrokePoints End()
    {
        IsActive = false;
        _pointerId = -1;
        return _points.ToStrokePoints();
    }

    /// <summary>Abandons the stroke.</summary>
    public void Cancel()
    {
        IsActive = false;
        _pointerId = -1;
        _points.Clear();
    }

    /// <summary>Describes the input device of the active/last stroke.</summary>
    public BinkStrokeInputInfo InputInfo => new(_kind, _hasRealPressure);

    private float EffectivePressure(in BinkInputSample sample, float speed)
    {
        float p = sample.Pressure;
        if (sample.Kind == BinkPointerKind.Pen && p > 0)
        {
            // Pens report varying pressure; a constant 0.5 means the digitizer has none.
            if (MathF.Abs(p - 0.5f) > 0.001f) _hasRealPressure = true;
            _pressureSamples++;
        }

        if (_hasRealPressure)
        {
            float smoothed = _pressureSamples <= 1 ? p : _lastPressure + (p - _lastPressure) * (1f - Math.Clamp(PressureSmoothing, 0f, 0.95f));
            return Math.Clamp(smoothed, 0.01f, 1f);
        }

        if (!_brush.SimulatePressure || _brush.PressureCurve.Kind == BinkPressureCurveKind.Constant) return 0.5f;

        // No hardware pressure: slower = heavier, capped so a resting pen does not bloat.
        float s = Math.Clamp(speed / MathF.Max(MaxSpeed, 0.01f), 0f, 1f);
        float simulated = 0.85f - 0.6f * s;
        float result = _pressureSamples == 0 && _points.Count == 0 ? simulated : _lastPressure + (simulated - _lastPressure) * 0.25f;
        return Math.Clamp(result, 0.05f, 1f);
    }

    private BinkPoint MakePoint(Vector2 p, float pressure, in BinkInputSample sample) => new()
    {
        X = p.X,
        Y = p.Y,
        Pressure = pressure,
        Time = (float)(sample.Timestamp - _startTime),
        TiltX = sample.TiltX,
        TiltY = sample.TiltY,
        Twist = sample.Twist,
    };
}
