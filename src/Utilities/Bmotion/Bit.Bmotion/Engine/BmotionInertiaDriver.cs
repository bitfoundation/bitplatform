
namespace Bit.Bmotion;
/// <summary>
/// Exponential-decay inertia driver (motion.dev's <c>inertia</c>). Decelerates from an initial velocity toward its
/// projected resting place; where that lies past <see cref="BmotionTransitionConfig.InertiaMin"/> /
/// <see cref="BmotionTransitionConfig.InertiaMax"/>, it coasts at its own speed until it crosses the bound and then a
/// spring (<see cref="BmotionTransitionConfig.BounceStiffness"/> / <see cref="BmotionTransitionConfig.BounceDamping"/>)
/// takes over with the velocity it had, so it bounces off the bound instead of braking early to stop on it. Released
/// outside the bounds (a drag pulled past its constraints), the spring brings it back from where it is.
/// </summary>
internal sealed class BmotionInertiaDriver : IBmotionAnimationDriver
{
    // How slow the bounce spring must get to count as at rest, in units/s (motion.dev's default for pixels).
    private const double BounceRestSpeed = 10;

    private readonly double _start;
    private readonly double _target;
    private readonly double _amplitude;
    private readonly double _projected;
    private readonly double _timeConstantSec;
    private readonly double _restDelta;
    private readonly double _delayMs;
    private readonly double? _min;
    private readonly double? _max;
    private readonly double _bounceStiffness;
    private readonly double _bounceDamping;
    private readonly double _startVelocity;
    private readonly Action<double> _apply;

    private double _elapsed;
    private double _lastTs = double.NaN; // NaN = not started: a timestamp can be negative (a fast-forward from a clock near 0)
    private double _startTs = double.NaN;
    private bool _cancelled;
    private double _fastForwardMs;
    private BmotionSpringDriver? _bounce;

    public BmotionInertiaDriver(double from, BmotionTransitionConfig config, Action<double> apply)
    {
        // Non-finite inertia inputs poison the decay math: a NaN/Infinity time constant, delay,
        // power, velocity or bound produces NaN positions through _apply and can make the rest test
        // (|projected - pos| < restDelta) unsatisfiable, so the driver ticks forever. Reject them up
        // front like the tween/spring/keyframe drivers do.
        if (!double.IsFinite(config.TimeConstant) || !double.IsFinite(config.Delay) ||
            !double.IsFinite(config.Power) || !double.IsFinite(config.InertiaVelocity) ||
            !double.IsFinite(config.BounceStiffness ?? 0) || !double.IsFinite(config.BounceDamping ?? 0) ||
            (config.InertiaMax.HasValue && !double.IsFinite(config.InertiaMax.Value)) ||
            (config.InertiaMin.HasValue && !double.IsFinite(config.InertiaMin.Value)))
            throw new ArgumentException(
                "Inertia configuration values (TimeConstant, Delay, Power, InertiaVelocity, BounceStiffness, " +
                "BounceDamping and the optional InertiaMin/InertiaMax bounds) must be finite.", nameof(config));

        _start = from;
        _timeConstantSec = config.TimeConstant > 0 ? config.TimeConstant / 1000.0 : 1e-6;
        // Rest delta must be strictly positive, otherwise the completion test
        // (|target - pos| < restDelta) can never pass and the driver runs forever.
        _restDelta = config.InertiaRestDelta > 0 ? config.InertiaRestDelta : 0.01;
        // A negative delay starts the coast that far in (WAAPI semantics, and what seeking relies on).
        _delayMs = Math.Max(0, config.Delay * 1000);
        _fastForwardMs = Math.Max(0, -config.Delay * 1000);
        _min = config.InertiaMin;
        _max = config.InertiaMax;
        _bounceStiffness = config.BounceStiffness is > 0 and var k ? k : 500;
        _bounceDamping = config.BounceDamping is >= 0 and var c ? c : 10;
        _startVelocity = config.InertiaVelocity;
        _apply = apply;

        // The coast heads for the (snapped) projection itself; the bounds are enforced by the bounce once it gets
        // there, so a throw keeps the speed it was thrown with.
        _target = Target(from, config);
        _amplitude = _target - from;
        _projected = ProjectRest(from, config);
    }

    // The projected end of the decay, snapped by ModifyTarget (a non-finite snap is ignored).
    private static double Target(double from, BmotionTransitionConfig config)
    {
        double projected = from + config.Power * config.InertiaVelocity;
        // Snap-to-grid / snap-to-page hook (motion.dev's modifyTarget): the consumer rounds the
        // projected resting place to whatever grid it wants; ignore a non-finite result rather than
        // letting it poison the decay math (which would make the rest test unsatisfiable - see above).
        if (config.ModifyTarget is { } modify)
        {
            double snapped = modify(projected);
            if (double.IsFinite(snapped)) projected = snapped;
        }
        return double.IsFinite(projected) ? projected : from;
    }

    /// <summary>
    /// Where an inertia starting at <paramref name="from"/> comes to rest: the decay's projected
    /// end, snapped by <c>ModifyTarget</c> and clamped to the bounds (where the bounce settles). Also
    /// what a runtime without a frame loop lands on at once (see <c>BmotionElementAnimationState.ForceInstant</c>).
    /// </summary>
    internal static double ProjectRest(double from, BmotionTransitionConfig config)
    {
        double projected = Target(from, config);
        if (config.InertiaMax.HasValue) projected = Math.Min(projected, config.InertiaMax.Value);
        if (config.InertiaMin.HasValue) projected = Math.Max(projected, config.InertiaMin.Value);
        return projected;
    }

    private bool IsOutOfBounds(double value) => value < _min || value > _max;

    private double NearestBound(double value)
    {
        if (_min is not { } min) return _max!.Value;
        if (_max is not { } max) return min;
        return Math.Abs(min - value) < Math.Abs(max - value) ? min : max;
    }

    // The spring that takes over at a bound: from where the element is, with the velocity it has (units/s), settling
    // on the bound. Its rest thresholds are the inertia's own, in units - the spring driver scales its thresholds by
    // the distance it travels, so they are given pre-divided.
    private BmotionSpringDriver BounceFrom(double position, double velocity)
    {
        double bound = NearestBound(position);
        double scale = Math.Max(Math.Abs(bound - position), 1.0);
        return new BmotionSpringDriver(position, bound, new BmotionTransitionConfig
        {
            Type = BmotionTransitionType.Spring,
            Stiffness = _bounceStiffness,
            Damping = _bounceDamping,
            Mass = 1,
            Velocity = velocity,
            RestDelta = _restDelta / scale,
            RestSpeed = BounceRestSpeed / scale,
        }, _apply);
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) return true;

        if (_fastForwardMs > 0)
        {
            // Started part-way in: the skipped time is played frame by frame, so a bound crossed meanwhile still
            // hands over to the bounce where it would have - up to the last ten minutes of it, which only a bounce
            // that never settles notices (see BmotionFastForward).
            double ahead = _fastForwardMs;
            _fastForwardMs = 0;
            if (BmotionFastForward.Play(ahead, timestamp, Tick)) return true;
        }

        if (double.IsNaN(_startTs)) _startTs = timestamp;
        if (timestamp - _startTs < _delayMs) { _apply(_start); return false; }

        if (_bounce is not null) return _bounce.Tick(timestamp);

        // Let go outside the bounds: straight back, carrying the release velocity.
        if (double.IsNaN(_lastTs) && IsOutOfBounds(_start))
        {
            _bounce = BounceFrom(_start, _startVelocity);
            return _bounce.Tick(timestamp);
        }

        if (double.IsNaN(_lastTs)) _lastTs = timestamp;

        _elapsed += Math.Min((timestamp - _lastTs) / 1000.0, 0.064);
        _lastTs = timestamp;

        double tau = _timeConstantSec > 0 ? _timeConstantSec : 1e-6;
        double decay = Math.Exp(-_elapsed / tau);
        double pos = _target - (_amplitude * decay);

        if (IsOutOfBounds(pos))
        {
            // Crossed a bound: the spring takes the coast's velocity from here.
            _bounce = BounceFrom(pos, _amplitude / tau * decay);
            return _bounce.Tick(timestamp);
        }

        _apply(pos);

        if (Math.Abs(_target - pos) < _restDelta)
        {
            _apply(_target);
            return true;
        }
        return false;
    }

    public void Cancel()
    {
        _cancelled = true;
        _bounce?.Cancel();
    }

    public void Complete() => _apply(_projected);
}
