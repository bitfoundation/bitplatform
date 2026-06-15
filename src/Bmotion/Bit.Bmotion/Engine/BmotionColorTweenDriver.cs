
namespace Bit.Bmotion;
/// <summary>Tween animation driver for CSS color string properties.</summary>
internal sealed class BmotionColorTweenDriver : IBmotionAnimationDriver
{
    private readonly string _to;
    private readonly double _durationMs;
    private readonly double _delayMs;
    private readonly Func<double, double> _easeFn;
    private readonly int _repeat;
    private readonly bool _isInfinite;
    private readonly BmotionRepeatType _repeatType;
    private readonly double _repeatDelayMs;
    private readonly Action<string> _apply;

    private double _startTime = -1;
    private bool _cancelled;
    private int _iteration;
    private string _curFrom;
    private string _curTo;

    public BmotionColorTweenDriver(string from, string to, BmotionTransitionConfig config, Action<string> apply)
    {
        _curFrom = from;
        _curTo = _to = to;
        _durationMs = config.Duration * 1000;
        _delayMs = config.Delay * 1000;
        _easeFn = BmotionEasingFunctions.Get(config);
        _repeat = config.Repeat;
        _isInfinite = config.Repeat == int.MaxValue;
        _repeatType = config.RepeatType;
        _repeatDelayMs = config.RepeatDelay * 1000;
        _apply = apply;
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) { _apply(_to); return true; }

        if (_startTime < 0) _startTime = timestamp + _delayMs;
        if (timestamp < _startTime) { _apply(_curFrom); return false; }

        double elapsed = timestamp - _startTime;
        double t = _durationMs > 0 ? Math.Min(elapsed / _durationMs, 1.0) : 1.0;
        double p = _easeFn(t);
        _apply(BmotionColorInterpolator.Lerp(_curFrom, _curTo, p));

        if (t >= 1.0)
        {
            if (_isInfinite || _iteration < _repeat)
            {
                if (!_isInfinite) _iteration++;
                _startTime = timestamp + _repeatDelayMs;
                if (_repeatType == BmotionRepeatType.Mirror || _repeatType == BmotionRepeatType.Reverse)
                    (_curFrom, _curTo) = (_curTo, _curFrom);
                return false;
            }
            return true;
        }
        return false;
    }

    public void Cancel() => _cancelled = true;

    public void Complete() => _apply(_to);
}
