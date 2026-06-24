namespace Bit.Bmotion;
/// <summary>Keyframe animation driver for numeric (double) properties.</summary>
internal sealed class BmotionNumericKeyframesDriver : IBmotionAnimationDriver
{
    private readonly double[] _frames;
    private readonly double _durationMs;
    private readonly double _delayMs;
    private readonly double[] _times;
    private readonly Func<double, double>[] _eases;
    private readonly int _repeat;
    private readonly bool _isInfinite;
    private readonly BmotionRepeatType _repeatType;
    private readonly double _repeatDelayMs;
    private readonly Action<double> _apply;

    private double _startTime = -1;
    private bool _cancelled;
    private int _iteration;
    private bool _reversed;
    private double[] _curFrames;

    public BmotionNumericKeyframesDriver(double[] frames, BmotionTransitionConfig config, Action<double> apply)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(apply);
        if (frames is null || frames.Length < 2)
            throw new ArgumentException("Keyframe animations require at least 2 frames.", nameof(frames));
        if (!double.IsFinite(config.Duration) || !double.IsFinite(config.Delay) || !double.IsFinite(config.RepeatDelay)
            || config.Duration < 0)
            // NaN/infinite timing values poison _startTime in the progress math (e.g. a NaN delay
            // makes _startTime NaN), pushing invalid values through _apply. A negative duration
            // makes the (durationMs > 0 ? … : 1.0) gate snap straight to the final frame instead of
            // animating. Reject both up front (matches BmotionColorTweenDriver; zero is allowed and
            // completes instantly).
            throw new ArgumentException(
                "Duration, Delay and RepeatDelay must be finite values and Duration must be non-negative.", nameof(config));
        if (config.Times != null && config.Times.Length != frames.Length)
            throw new ArgumentException("Times array length must match the number of frames.", nameof(config));
        if (config.Times != null)
        {
            // Times feed the Interpolate segment math; non-monotonic or out-of-range values produce
            // negative/zero segment lengths and NaN output, so reject them up front.
            for (int i = 0; i < config.Times.Length; i++)
            {
                // double.NaN/infinity slip past the relational checks below (every comparison with
                // NaN is false), so reject non-finite entries explicitly before the range/order tests.
                if (!double.IsFinite(config.Times[i]))
                    throw new ArgumentException("Times values must be finite.", nameof(config));
                if (config.Times[i] < 0 || config.Times[i] > 1)
                    throw new ArgumentException("Times values must be within the range [0, 1].", nameof(config));
                if (i > 0 && config.Times[i] < config.Times[i - 1])
                    throw new ArgumentException("Times values must be in monotonically ascending order.", nameof(config));
            }
        }

        _frames = (double[])frames.Clone();
        _curFrames = (double[])frames.Clone();
        _durationMs = config.Duration * 1000;
        _delayMs = config.Delay * 1000;
        _repeat = config.Repeat;
        _isInfinite = config.IsInfiniteRepeat;
        _repeatType = config.RepeatType;
        _repeatDelayMs = config.RepeatDelay * 1000;
        _apply = apply;

        int n = frames.Length;
        // Clone the caller's Times so the in-place MirrorTimes mutation never touches their config.
        _times = config.Times != null
            ? (double[])config.Times.Clone()
            : Enumerable.Range(0, n).Select(i => (double)i / (n - 1)).ToArray();

        // Per-segment easing array. Per-segment easing isn't exposed on the transition config yet,
        // so every segment currently shares the single configured easing function; the array shape
        // is kept so adding per-segment curves later doesn't change the interpolation code path.
        _eases = new Func<double, double>[n - 1];
        var globalEase = BmotionEasingFunctions.Get(config);
        for (int i = 0; i < n - 1; i++)
            _eases[i] = globalEase;
    }

    public bool Tick(double timestamp)
    {
        // Freeze at the current value on cancel (consistent with the other drivers); callers
        // remove the driver immediately after Cancel(), so this branch is defensive only.
        if (_cancelled) return true;

        if (_startTime < 0) _startTime = timestamp + _delayMs;
        if (timestamp < _startTime) { _apply(_curFrames[0]); return false; }

        double t = _durationMs > 0 ? Math.Min((timestamp - _startTime) / _durationMs, 1.0) : 1.0;
        _apply(Interpolate(_curFrames, _times, _eases, t));

        if (t >= 1.0)
        {
            if (_isInfinite || _iteration < _repeat)
            {
                if (!_isInfinite) _iteration++;
                _startTime = timestamp + _repeatDelayMs;
                // Mirror ping-pongs: reverse the playback direction every cycle (0→1, 1→0, …).
                // Reverse plays the frames backwards repeatedly (1→0, 1→0, …): reverse once on the
                // first repeat, then keep that order so each subsequent cycle replays in reverse
                // rather than toggling back to forward.
                if (_repeatType == BmotionRepeatType.Mirror)
                {
                    Array.Reverse(_curFrames);
                    MirrorTimes(_times);
                }
                else if (_repeatType == BmotionRepeatType.Reverse && !_reversed)
                {
                    Array.Reverse(_curFrames);
                    MirrorTimes(_times);
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
        // Mirror/Reverse don't always terminate on the last frame, so snap to the correct natural
        // terminal frame (computed from the original forward-order _frames):
        //  • Mirror ping-pongs each pass (total passes = _repeat + 1). An even count ends back on
        //    the first frame, an odd count on the last.
        //  • Reverse plays forward once then replays reversed for every later pass, so it ends on
        //    the last frame only when there are no repeats, otherwise on the first frame.
        // Infinite repeats have no natural end, so fall through to the last frame.
        if (!_isInfinite && _repeatType == BmotionRepeatType.Mirror)
        {
            _apply((_repeat + 1) % 2 == 0 ? _frames[0] : _frames[^1]);
            return;
        }
        if (!_isInfinite && _repeatType == BmotionRepeatType.Reverse)
        {
            _apply(_repeat == 0 ? _frames[^1] : _frames[0]);
            return;
        }
        _apply(_frames[^1]);
    }

    /// <summary>
    /// Mirrors a (possibly non-uniform) times array in place so segment durations line up with the
    /// reversed frame order: <c>newTimes[i] = 1 - times[n-1-i]</c>. Applying it twice restores the
    /// original: Mirror calls it every pass to ping-pong, while Reverse calls it once to latch the
    /// reversed playback direction.
    /// </summary>
    private static void MirrorTimes(double[] times)
    {
        int n = times.Length;
        for (int i = 0; i < n / 2; i++)
        {
            double a = 1 - times[n - 1 - i];
            double b = 1 - times[i];
            times[i] = a;
            times[n - 1 - i] = b;
        }
        if (n % 2 == 1) times[n / 2] = 1 - times[n / 2];
    }

    private static double Interpolate(double[] frames, double[] times, Func<double, double>[] eases, double t)
    {
        int n = frames.Length;
        int seg = n - 2;
        for (int i = 0; i < n - 1; i++)
        {
            if (t <= times[i + 1]) { seg = i; break; }
        }
        double segLen = times[seg + 1] - times[seg];
        double segT = segLen > 0 ? (t - times[seg]) / segLen : 1.0;
        double easedT = eases[seg](Math.Clamp(segT, 0.0, 1.0));
        return frames[seg] + (frames[seg + 1] - frames[seg]) * easedT;
    }
}
