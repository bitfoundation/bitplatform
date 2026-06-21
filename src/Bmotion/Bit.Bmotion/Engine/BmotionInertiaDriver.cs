
namespace Bit.Bmotion;
/// <summary>
/// Exponential-decay inertia driver. Decelerates from an initial velocity toward
/// an optional projected target, with optional bounds clamping.
/// </summary>
internal sealed class BmotionInertiaDriver : IBmotionAnimationDriver
{
    private readonly double _start;
    private readonly double _projected;
    private readonly double _delta;
    private readonly double _timeConstantSec;
    private readonly double _restDelta;
    private readonly double _delayMs;
    private readonly Action<double> _apply;

    private double _elapsed;
    private double _lastTs = -1;
    private double _startTs = -1;
    private bool _cancelled;

    public BmotionInertiaDriver(double from, BmotionTransitionConfig config, Action<double> apply)
    {
        // Non-finite inertia inputs poison the decay math: a NaN/Infinity time constant, delay,
        // power, velocity or bound produces NaN positions through _apply and can make the rest test
        // (|projected - pos| < restDelta) unsatisfiable, so the driver ticks forever. Reject them up
        // front like the tween/spring/keyframe drivers do.
        if (!double.IsFinite(config.TimeConstant) || !double.IsFinite(config.Delay) ||
            !double.IsFinite(config.Power) || !double.IsFinite(config.InertiaVelocity) ||
            (config.InertiaMax.HasValue && !double.IsFinite(config.InertiaMax.Value)) ||
            (config.InertiaMin.HasValue && !double.IsFinite(config.InertiaMin.Value)))
            throw new ArgumentException(
                "Inertia configuration values (TimeConstant, Delay, Power, InertiaVelocity and the " +
                "optional InertiaMin/InertiaMax bounds) must be finite.", nameof(config));

        _start = from;
        _timeConstantSec = config.TimeConstant > 0 ? config.TimeConstant / 1000.0 : 1e-6;
        // Rest delta must be strictly positive, otherwise the completion test
        // (|projected - pos| < restDelta) can never pass and the driver runs forever.
        _restDelta = config.InertiaRestDelta > 0 ? config.InertiaRestDelta : 0.01;
        _delayMs = config.Delay * 1000;
        _apply = apply;

        double power = config.Power;
        double velocity = config.InertiaVelocity;

        double projected = from + power * velocity;
        if (config.InertiaMax.HasValue) projected = Math.Min(projected, config.InertiaMax.Value);
        if (config.InertiaMin.HasValue) projected = Math.Max(projected, config.InertiaMin.Value);

        _projected = projected;
        _delta = projected - from;
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) return true;

        if (_startTs < 0) _startTs = timestamp;
        if (timestamp - _startTs < _delayMs) { _apply(_start); return false; }

        if (_lastTs < 0) _lastTs = timestamp;

        _elapsed += Math.Min((timestamp - _lastTs) / 1000.0, 0.064);
        _lastTs = timestamp;

        double tau = _timeConstantSec > 0 ? _timeConstantSec : 1e-6;
        double pos = _start + _delta * (1 - Math.Exp(-_elapsed / tau));
        _apply(pos);

        if (Math.Abs(_projected - pos) < _restDelta)
        {
            _apply(_projected);
            return true;
        }
        return false;
    }

    public void Cancel() => _cancelled = true;

    public void Complete() => _apply(_projected);
}
