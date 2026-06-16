
namespace Bit.Bmotion;
/// <summary>
/// Semi-implicit Euler spring physics driver for numeric properties.
/// Automatically subdivides each frame to maintain numerical stability for
/// high-stiffness / high-damping configurations.
/// </summary>
internal sealed class BmotionSpringDriver : IBmotionAnimationDriver
{
    private double _target;
    private double _from;
    private readonly double _k;        // stiffness
    private readonly double _d;        // damping
    private readonly double _m;        // mass
    private readonly double _initialVel;
    private readonly double _restSpeed;
    private readonly double _restDelta;
    private readonly double _repeatDelayMs;
    private readonly double _maxSubDt;
    private readonly int _repeat;
    private readonly bool _isInfinite;
    private readonly BmotionRepeatType _repeatType;
    private readonly Action<double> _apply;

    private double _pos;
    private double _vel;
    private double _currentDelayMs;
    private double _lastTs = -1;
    private double _startTs = -1;
    private int _iteration;
    private bool _cancelled;

    public BmotionSpringDriver(double from, double to, BmotionTransitionConfig config, Action<double> apply)
    {
        _pos = _from = from;
        _target = to;

        // Resolve stiffness/damping: if Bounce+VisualDuration (or Bounce+Duration) are set,
        // derive them from those intuitive parameters (Framer Motion-compatible).
        double k = config.Stiffness;
        double d = config.Damping;
        if (config.Bounce.HasValue)
        {
            double vd = config.VisualDuration ?? config.Duration;
            (k, d) = BmotionTransitionConfig.SpringFromBounce(vd, config.Bounce.Value, config.Mass);
        }

        _k = k;
        _d = d;
        // Mass divides the acceleration each sub-step; a value <= 0 would yield NaN/Infinity and
        // trap the spring (the rest test would never pass). Fall back to the default mass of 1.
        _m = config.Mass > 0 ? config.Mass : 1.0;
        _vel = _initialVel = config.Velocity;
        // Rest thresholds are scaled by the animation's magnitude so large-range springs (e.g.
        // x: 0→1000) settle in proportion to their distance instead of chasing an absolute 0.01px/
        // 0.01px-per-sec target for many extra frames. Small ranges keep the absolute thresholds.
        double range = Math.Abs(to - from);
        double restScale = range > 1.0 ? range : 1.0;
        // Clamp to a small positive floor: a non-positive RestSpeed/RestDelta would make the
        // completion gate (Abs(vel) < restSpeed && Abs(pos-target) < restDelta) unsatisfiable,
        // leaving the spring ticking forever.
        const double minRest = 1e-4;
        _restSpeed = Math.Max(config.RestSpeed * restScale, minRest);
        _restDelta = Math.Max(config.RestDelta * restScale, minRest);
        _currentDelayMs = config.Delay * 1000;
        _repeatDelayMs = config.RepeatDelay * 1000;
        _repeat = config.Repeat;
        _isInfinite = config.IsInfiniteRepeat;
        _repeatType = config.RepeatType;
        _apply = apply;

        // Compute a maximum sub-step size that keeps semi-implicit Euler stable
        _maxSubDt = Math.Max(0.001, Math.Min(
            _d > 0 ? 1.8 / _d : 1.0,
            _k > 0 ? 0.9 / Math.Sqrt(_k) : 1.0));
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) return true;

        if (_startTs < 0) _startTs = timestamp;
        if (timestamp - _startTs < _currentDelayMs) { _apply(_pos); return false; }

        if (_lastTs < 0) _lastTs = timestamp;

        double dt = Math.Min((timestamp - _lastTs) / 1000.0, 0.064);
        _lastTs = timestamp;

        int subSteps = Math.Max(1, (int)Math.Ceiling(dt / _maxSubDt));
        double subDt = dt / subSteps;
        for (int i = 0; i < subSteps; i++)
        {
            double springF = -_k * (_pos - _target);
            double dampF = -_d * _vel;
            _vel += (springF + dampF) / _m * subDt;
            _pos += _vel * subDt;
        }

        _apply(_pos);

        if (Math.Abs(_vel) < _restSpeed && Math.Abs(_pos - _target) < _restDelta)
        {
            _apply(_target);

            if (_isInfinite || _iteration < _repeat)
            {
                if (!_isInfinite) _iteration++;
                // Mirror/Reverse ping-pong back to the start; Loop replays from the origin.
                if (_repeatType is BmotionRepeatType.Mirror or BmotionRepeatType.Reverse)
                    (_from, _target) = (_target, _from);
                _pos = _from;
                _vel = _initialVel;
                _lastTs = -1;
                _startTs = timestamp;            // re-arm the delay window for this repeat
                _currentDelayMs = _repeatDelayMs;
                return false;
            }
            return true;
        }
        return false;
    }

    public void Cancel() => _cancelled = true;

    public void Complete() => _apply(_target);
}
