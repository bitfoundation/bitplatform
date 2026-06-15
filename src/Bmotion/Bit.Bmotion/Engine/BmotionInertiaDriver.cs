
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
        _start = from;
        _timeConstantSec = config.TimeConstant > 0 ? config.TimeConstant / 1000.0 : 1e-6;
        _restDelta = config.InertiaRestDelta;
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
