
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
    private readonly BmRepeatType _repeatType;
    private readonly double _repeatDelayMs;
    private readonly BmColorSpace _colorSpace;
    private readonly Action<string> _apply;

    private double _startTime = -1;
    private bool _cancelled;
    private int _iteration;
    private bool _reversed;
    private string _curFrom;
    private string _curTo;
    private double[]? _curFromCh;
    private double[]? _curToCh;

    public BmotionColorTweenDriver(string from, string to, BmotionTransitionConfig config, Action<string> apply)
    {
        if (!double.IsFinite(config.Duration) || !double.IsFinite(config.Delay) || !double.IsFinite(config.RepeatDelay)
            || config.Duration < 0)
            // NaN/infinite timing values poison _startTime in the progress math, pushing invalid
            // values through _apply. A negative duration keeps t below 1.0 so the tween never
            // completes. Reject both up front (a zero duration is allowed: it completes instantly).
            throw new ArgumentException(
                "Duration, Delay and RepeatDelay must be finite values and Duration must be non-negative.", nameof(config));

        _curFrom = _from = from;
        _curTo = _to = to;
        // Parse once up-front so Tick() doesn't run the color regex ~60 times per second.
        _curFromCh = BmotionColorInterpolator.Parse(from);
        _curToCh = BmotionColorInterpolator.Parse(to);
        _durationMs = config.Duration * 1000;
        _delayMs = config.Delay * 1000;
        _easeFn = BmEaseFunctions.Get(config);
        _repeat = config.Repeat;
        _isInfinite = config.IsInfiniteRepeat;
        _repeatType = config.RepeatType;
        _repeatDelayMs = config.RepeatDelay * 1000;
        _colorSpace = config.ColorSpace;
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
            ? BmotionColorInterpolator.Lerp(_curFromCh, _curToCh, p, _colorSpace)
            : _curTo);

        if (t >= 1.0)
        {
            if (_isInfinite || _iteration < _repeat)
            {
                if (!_isInfinite) _iteration++;
                _startTime = timestamp + _repeatDelayMs;
                // Mirror ping-pongs: swap from/to every pass (0→1, 1→0, …).
                // Reverse plays the colour backwards repeatedly (1→0, 1→0, …): swap once on the
                // first repeat, then keep that order so each later cycle replays in reverse rather
                // than toggling back to forward (matches the keyframe drivers).
                if (_repeatType == BmRepeatType.Mirror)
                {
                    (_curFrom, _curTo) = (_curTo, _curFrom);
                    (_curFromCh, _curToCh) = (_curToCh, _curFromCh);
                }
                else if (_repeatType == BmRepeatType.Reverse && !_reversed)
                {
                    (_curFrom, _curTo) = (_curTo, _curFrom);
                    (_curFromCh, _curToCh) = (_curToCh, _curFromCh);
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
        // Mirror ping-pongs each pass, so the natural terminal colour depends on how many passes
        // run: total passes = _repeat + 1. An even count ends back on _from, an odd count on _to.
        if (!_isInfinite && _repeatType == BmRepeatType.Mirror)
        {
            _apply((_repeat + 1) % 2 == 0 ? _from : _to);
            return;
        }
        // Reverse plays forward once (ending on _to) then replays reversed for every later pass
        // (ending on _from), so it ends on _to only when there are no repeats.
        if (!_isInfinite && _repeatType == BmRepeatType.Reverse)
        {
            _apply(_repeat == 0 ? _to : _from);
            return;
        }
        // Loop (and infinite repeats, which have no natural end) terminate on _to.
        _apply(_to);
    }
}
