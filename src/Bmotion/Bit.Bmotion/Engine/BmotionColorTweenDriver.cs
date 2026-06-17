
namespace Bit.Bmotion;
/// <summary>Tween animation driver for CSS color string properties.</summary>
internal sealed class BmotionColorTweenDriver : IBmotionAnimationDriver
{
    private readonly string _to;
    private readonly string _from;
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
    private double[]? _curFromCh;
    private double[]? _curToCh;

    public BmotionColorTweenDriver(string from, string to, BmotionTransitionConfig config, Action<string> apply)
    {
        _curFrom = _from = from;
        _curTo = _to = to;
        // Parse once up-front so Tick() doesn't run the color regex ~60 times per second.
        _curFromCh = BmotionColorInterpolator.Parse(from);
        _curToCh = BmotionColorInterpolator.Parse(to);
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
        // Fall back to the raw target string when a color couldn't be parsed (matches the
        // string Lerp's behaviour of returning 'to' for unparseable input).
        _apply(_curFromCh != null && _curToCh != null
            ? BmotionColorInterpolator.Lerp(_curFromCh, _curToCh, p)
            : _curTo);

        if (t >= 1.0)
        {
            if (_isInfinite || _iteration < _repeat)
            {
                if (!_isInfinite) _iteration++;
                _startTime = timestamp + _repeatDelayMs;
                if (_repeatType == BmotionRepeatType.Mirror || _repeatType == BmotionRepeatType.Reverse)
                {
                    (_curFrom, _curTo) = (_curTo, _curFrom);
                    (_curFromCh, _curToCh) = (_curToCh, _curFromCh);
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
        // Mirror/Reverse ping-pong each pass, so the natural terminal colour depends on how many
        // passes run: total passes = _repeat + 1. An even count ends back on _from, an odd count
        // ends on _to. (Infinite repeats have no natural end, so fall through to _to.)
        if (!_isInfinite && (_repeatType == BmotionRepeatType.Mirror || _repeatType == BmotionRepeatType.Reverse))
        {
            _apply((_repeat + 1) % 2 == 0 ? _from : _to);
            return;
        }
        _apply(_to);
    }
}
