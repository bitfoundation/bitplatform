
namespace Bit.Bmotion;
/// <summary>Tween (duration-based) animation driver for numeric properties.</summary>
internal sealed class BmotionTweenDriver : IBmotionAnimationDriver
{
    private readonly double _to;
    private readonly double _from;
    private readonly double _durationMs;
    private readonly double _delayMs;
    private readonly Func<double, double> _easeFn;
    private readonly int _repeat;
    private readonly bool _isInfinite;
    private readonly BmotionRepeatType _repeatType;
    private readonly double _repeatDelayMs;
    private readonly Action<double> _apply;

    private double _startTime = -1;
    private bool _cancelled;
    private int _iteration;
    private bool _reversed;
    private double _curFrom;
    private double _curTo;

    public BmotionTweenDriver(double from, double to, BmotionTransitionConfig config, Action<double> apply)
    {
        if (!double.IsFinite(config.Duration) || !double.IsFinite(config.Delay) || !double.IsFinite(config.RepeatDelay)
            || config.Duration < 0)
            // NaN/infinite timing values poison _startTime in the progress math, pushing invalid
            // values through _apply. A negative duration keeps t below 1.0 so the tween never
            // completes. Reject both up front (matches BmotionColorTweenDriver; a zero duration is
            // allowed: it completes instantly).
            throw new ArgumentException(
                "Duration, Delay and RepeatDelay must be finite values and Duration must be non-negative.", nameof(config));

        _curFrom = _from = from;
        _curTo = _to = to;
        _durationMs = config.Duration * 1000;
        _delayMs = config.Delay * 1000;
        _easeFn = BmotionEasingFunctions.Get(config);
        _repeat = config.Repeat;
        _isInfinite = config.IsInfiniteRepeat;
        _repeatType = config.RepeatType;
        _repeatDelayMs = config.RepeatDelay * 1000;
        _apply = apply;
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) return true;

        if (_startTime < 0) _startTime = timestamp + _delayMs;
        if (timestamp < _startTime) { _apply(_curFrom); return false; }

        double elapsed = timestamp - _startTime;
        double t = _durationMs > 0 ? Math.Min(elapsed / _durationMs, 1.0) : 1.0;
        double p = _easeFn(t);
        double value = _curFrom + (_curTo - _curFrom) * p;
        _apply(value);

        if (t >= 1.0)
        {
            if (_isInfinite || _iteration < _repeat)
            {
                if (!_isInfinite) _iteration++;
                _startTime = timestamp + _repeatDelayMs;
                // Mirror ping-pongs: swap from/to every pass (0→1, 1→0, …).
                // Reverse plays backwards repeatedly (1→0, 1→0, …): swap once on the first repeat,
                // then keep that order so each later cycle replays in reverse rather than toggling
                // back to forward (matches BmotionColorTweenDriver / BmotionSpringDriver).
                if (_repeatType == BmotionRepeatType.Mirror)
                    (_curFrom, _curTo) = (_curTo, _curFrom);
                else if (_repeatType == BmotionRepeatType.Reverse && !_reversed)
                {
                    (_curFrom, _curTo) = (_curTo, _curFrom);
                    _reversed = true;
                }
                return false;
            }
            return true;
        }
        return false;
    }

    public void Cancel() => _cancelled = true;

    public void Complete()
    {
        // _curFrom/_curTo may have been swapped by repeats, so compute the natural terminal value
        // from the original endpoints (mirrors BmotionColorTweenDriver / BmotionSpringDriver):
        //  • Mirror ping-pongs each pass (total passes = _repeat + 1): an even count ends back on
        //    _from, an odd count on _to.
        //  • Reverse plays forward once (ending on _to) then replays reversed for every later pass
        //    (ending on _from), so it ends on _to only when there are no repeats.
        // Loop (and infinite repeats, which have no natural end) terminate on _to.
        if (!_isInfinite && _repeatType == BmotionRepeatType.Mirror)
        {
            _apply((_repeat + 1) % 2 == 0 ? _from : _to);
            return;
        }
        if (!_isInfinite && _repeatType == BmotionRepeatType.Reverse)
        {
            _apply(_repeat == 0 ? _to : _from);
            return;
        }
        _apply(_to);
    }
}
