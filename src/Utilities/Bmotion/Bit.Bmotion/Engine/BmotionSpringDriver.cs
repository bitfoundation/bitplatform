
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
    private readonly double _origFrom;
    private double _origTarget;        // moved by TryRetarget
    private readonly double _k;        // stiffness
    private readonly double _d;        // damping
    private readonly double _m;        // mass
    private readonly double _initialVel;
    private readonly double _baseRestSpeed;
    private readonly double _baseRestDelta;
    private double _restSpeed;
    private double _restDelta;
    private readonly double _repeatDelayMs;
    private readonly double _maxSubDt;
    private readonly int _repeat;
    private readonly bool _isInfinite;
    private readonly BmRepeatType _repeatType;
    private readonly Action<double> _apply;

    private double _pos;
    private double _vel;
    private double _currentDelayMs;
    private double _fastForwardMs;
    private double _lastTs = double.NaN; // NaN = not started: a timestamp can be negative (a fast-forward from a clock near 0)
    private double _startTs = double.NaN;
    private int _iteration;
    private bool _cancelled;
    // Reverse plays forward once then replays reversed; latches after the first swap so later
    // passes keep the reversed direction instead of ping-ponging the way Mirror does.
    private bool _reversed;

    public BmotionSpringDriver(double from, double to, BmotionTransitionConfig config, Action<double> apply)
    {
        // Non-finite endpoints poison the physics just like non-finite config: a NaN/Infinity from
        // or to makes _pos/_vel and the magnitude-scaled rest thresholds non-finite, so the rest
        // gate can never be satisfied and the driver ticks forever. Reject them up front.
        if (!double.IsFinite(from) || !double.IsFinite(to))
            throw new ArgumentException("Spring endpoints (from, to) must be finite values.", nameof(from));
        // Non-finite spring inputs poison the physics: a NaN/Infinity stiffness, damping, velocity
        // or rest threshold makes _vel/_pos non-finite (or makes the rest gate unsatisfiable), so
        // the driver would tick forever. Reject them up front like the tween/keyframe drivers do.
        if (!double.IsFinite(config.Stiffness) || !double.IsFinite(config.Damping) ||
            !double.IsFinite(config.Velocity) || !double.IsFinite(config.RestSpeed) ||
            !double.IsFinite(config.RestDelta) || !double.IsFinite(config.Delay) ||
            !double.IsFinite(config.RepeatDelay))
            throw new ArgumentException(
                "Spring configuration values (Stiffness, Damping, Velocity, RestSpeed, RestDelta, " +
                "Delay, RepeatDelay) must be finite.", nameof(config));
        if (config.Bounce.HasValue)
        {
            // When Bounce is set, stiffness/damping are derived from these values - a non-finite
            // bounce or visual/duration would propagate NaN into k/d via SpringFromBounce.
            double vdCheck = config.VisualDuration ?? config.Duration;
            if (!double.IsFinite(config.Bounce.Value) || !double.IsFinite(vdCheck))
                throw new ArgumentException(
                    "Bounce and the VisualDuration/Duration used to derive the spring must be finite.",
                    nameof(config));
        }

        _pos = _from = _origFrom = from;
        _target = _origTarget = to;

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
        _baseRestSpeed = config.RestSpeed;
        _baseRestDelta = config.RestDelta;
        ScaleRestThresholds(to - from);
        // A negative delay starts the spring that far into its motion (WAAPI semantics, and what seeking a playing
        // animation relies on): the time is simulated on the first frame.
        _currentDelayMs = Math.Max(0, config.Delay * 1000);
        _fastForwardMs = Math.Max(0, -config.Delay * 1000);
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

    /// <summary>
    /// Rest thresholds are scaled by the animation's magnitude so large-range springs (e.g.
    /// x: 0→1000) settle in proportion to their distance instead of chasing an absolute 0.01px/
    /// 0.01px-per-sec target for many extra frames. Small ranges keep the absolute thresholds.
    /// </summary>
    private void ScaleRestThresholds(double range)
    {
        range = Math.Abs(range);
        double restScale = range > 1.0 ? range : 1.0;
        // Clamp to a small positive floor: a non-positive RestSpeed/RestDelta would make the
        // completion gate (Abs(vel) < restSpeed && Abs(pos-target) < restDelta) unsatisfiable,
        // leaving the spring ticking forever.
        const double minRest = 1e-4;
        _restSpeed = Math.Max(_baseRestSpeed * restScale, minRest);
        _restDelta = Math.Max(_baseRestDelta * restScale, minRest);
    }

    /// <summary>
    /// Moves the target of the spring in flight, keeping where it is, how fast it is going and its clock - a follower
    /// chasing a source that moves (<see cref="BmotionAnimateService.Spring"/>). Replaced by a new spring instead, it
    /// would start from rest on its next frame, and a source that changes more often than frames are drawn would
    /// replace it every time before it took a step. False for a spring that repeats or is cancelled, which a new one
    /// replaces as before.
    /// </summary>
    internal bool TryRetarget(double to)
    {
        if (_cancelled || _isInfinite || _repeat > 0 || !double.IsFinite(to)) return false;
        _target = _origTarget = to;
        // The rest is judged against the distance left to go, as a new spring from here would judge it.
        ScaleRestThresholds(to - _pos);
        return true;
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) return true;

        if (_fastForwardMs > 0)
        {
            // Started part-way in: the skipped time is played frame by frame, so the spring is exactly where it would
            // have been had it played (one big step would integrate it far more coarsely than frames do).
            double ahead = _fastForwardMs;
            _fastForwardMs = 0;
            for (double t = timestamp - ahead; t < timestamp; t += 1000.0 / 60)
                if (Tick(t)) return true;
        }

        if (double.IsNaN(_startTs)) _startTs = timestamp;
        if (timestamp - _startTs < _currentDelayMs) { _apply(_pos); return false; }

        if (double.IsNaN(_lastTs)) _lastTs = timestamp;

        double dt = Math.Min((timestamp - _lastTs) / 1000.0, 0.064);
        _lastTs = timestamp;

        int subSteps = Math.Max(1, (int)Math.Ceiling(dt / _maxSubDt));
        double subDt = dt / subSteps;
        // Explicit damping is only stable while it removes less than all of the velocity in a step (d·dt/m < 2); the
        // sub-step floor lets a very heavy damping (a hard wall, d in the thousands) past that, where it would flip
        // and amplify the velocity every step until it is non-finite. There the damping is applied implicitly
        // instead, which is stable at any strength; every ordinary spring keeps its exact trajectory.
        double dampingPerStep = _d / _m * subDt;
        bool implicitDamping = dampingPerStep >= 1.8;
        for (int i = 0; i < subSteps; i++)
        {
            double springF = -_k * (_pos - _target);
            if (implicitDamping)
                _vel = (_vel + (springF / _m * subDt)) / (1 + dampingPerStep);
            else
                _vel += (springF + (-_d * _vel)) / _m * subDt;
            _pos += _vel * subDt;
        }

        _apply(_pos);

        if (Math.Abs(_vel) < _restSpeed && Math.Abs(_pos - _target) < _restDelta)
        {
            double target = _target;
            _apply(target);
            // A subscriber of the value can retarget the spring from inside that apply (a source it feeds changing):
            // it is not done then, but heads for the new target from where it is.
            if (_target != target) return false;

            if (_isInfinite || _iteration < _repeat)
            {
                if (!_isInfinite) _iteration++;
                // Mirror ping-pongs every pass (0→1, 1→0, …); Reverse swaps once on the first
                // repeat then keeps the reversed direction so each later pass replays backwards
                // (matches BmotionColorTweenDriver / BmotionNumericKeyframesDriver). Loop replays
                // from the origin without swapping.
                if (_repeatType == BmRepeatType.Mirror)
                    (_from, _target) = (_target, _from);
                else if (_repeatType == BmRepeatType.Reverse && !_reversed)
                {
                    (_from, _target) = (_target, _from);
                    _reversed = true;
                }
                _pos = _from;
                _vel = _initialVel;
                _lastTs = double.NaN;
                _startTs = timestamp;            // re-arm the delay window for this repeat
                _currentDelayMs = _repeatDelayMs;
                return false;
            }
            return true;
        }
        return false;
    }

    public void Cancel() => _cancelled = true;

    public void Complete()
    {
        // _from/_target may have been swapped by repeats, so compute the natural terminal value
        // from the original endpoints (mirrors BmotionColorTweenDriver):
        //  • Mirror ping-pongs each pass (total passes = _repeat + 1): an even count ends back on
        //    the start, an odd count on the target.
        //  • Reverse plays forward once (ending on the target) then replays reversed for every
        //    later pass (ending on the start), so it ends on the target only with no repeats.
        // Infinite repeats have no natural end, so fall through to the target.
        if (!_isInfinite && _repeatType == BmRepeatType.Mirror)
        {
            _apply((_repeat + 1) % 2 == 0 ? _origFrom : _origTarget);
            return;
        }
        if (!_isInfinite && _repeatType == BmRepeatType.Reverse)
        {
            _apply(_repeat == 0 ? _origTarget : _origFrom);
            return;
        }
        _apply(_origTarget);
    }
}
