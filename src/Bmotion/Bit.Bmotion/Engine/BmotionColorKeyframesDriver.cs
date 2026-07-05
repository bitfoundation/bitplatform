namespace Bit.Bmotion;
/// <summary>Keyframe animation driver for CSS color string properties.</summary>
internal sealed class BmotionColorKeyframesDriver : IBmotionAnimationDriver
{
    private readonly string[] _frames;
    private readonly double _durationMs;
    private readonly double _delayMs;
    private readonly double[] _times;
    private readonly Func<double, double>[] _eases;
    private readonly int _repeat;
    private readonly bool _isInfinite;
    private readonly BmRepeatType _repeatType;
    private readonly double _repeatDelayMs;
    private readonly Action<string> _apply;

    private double _startTime = -1;
    private bool _cancelled;
    private int _iteration;
    private bool _reversed;
    private string[] _curFrames;
    private readonly double[]?[] _curChannels;

    public BmotionColorKeyframesDriver(string[] frames, BmotionTransitionConfig config, Action<string> apply)
    {
        if (config is null)
            throw new ArgumentException("Transition config must not be null.", nameof(config));
        if (apply is null)
            throw new ArgumentException("Apply callback must not be null.", nameof(apply));
        if (frames is null || frames.Length < 2)
            throw new ArgumentException("Keyframe animations require at least 2 frames.", nameof(frames));
        if (!double.IsFinite(config.Duration) || !double.IsFinite(config.Delay) || !double.IsFinite(config.RepeatDelay)
            || config.Duration < 0)
            // NaN/infinite timing values poison _startTime in the progress math, pushing invalid
            // values through _apply. A negative duration keeps t below 1.0 so the animation never
            // completes. Reject both up front (matches the numeric keyframes / tween drivers, which
            // also permit negative Delay/RepeatDelay so a shared config behaves consistently).
            throw new ArgumentException(
                "Duration, Delay and RepeatDelay must be finite values and Duration must be non-negative.", nameof(config));
        if (config.Times != null && config.Times.Length != frames.Length)
            throw new ArgumentException("Times array length must match the number of frames.", nameof(config));
        if (config.Times != null)
        {
            // Times feed the segment math; non-monotonic or out-of-range values produce
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

        _frames = (string[])frames.Clone();
        _curFrames = (string[])frames.Clone();
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
        // Per-segment easing: config.Eases maps entry i onto the segment frames[i] → frames[i+1]
        // (last entry repeating when shorter); otherwise every segment shares the single easing.
        _eases = BmEaseFunctions.GetSegmentEases(config, n - 1);

        // Parse each frame's color once up-front; Tick() then only interpolates pre-parsed
        // channels instead of running the color regex on every frame (~60 fps).
        _curChannels = new double[]?[n];
        for (int i = 0; i < n; i++)
            _curChannels[i] = BmotionColorInterpolator.Parse(_curFrames[i]);
    }

    public bool Tick(double timestamp)
    {
        // Freeze at the current value on cancel (consistent with the other drivers); callers
        // remove the driver immediately after Cancel(), so this branch is defensive only.
        if (_cancelled) return true;

        if (_startTime < 0) _startTime = timestamp + _delayMs;
        if (timestamp < _startTime) { _apply(_curFrames[0]); return false; }

        double t = _durationMs > 0 ? Math.Min((timestamp - _startTime) / _durationMs, 1.0) : 1.0;

        int n = _curFrames.Length;
        int seg = n - 2;
        for (int i = 0; i < n - 1; i++) { if (t <= _times[i + 1]) { seg = i; break; } }
        double segLen = _times[seg + 1] - _times[seg];
        double segT = segLen > 0 ? (t - _times[seg]) / segLen : 1.0;
        double easedT = _eases[seg](Math.Clamp(segT, 0.0, 1.0));
        var ca = _curChannels[seg];
        var cb = _curChannels[seg + 1];
        // Fall back to the raw target frame string when a color couldn't be parsed
        // (matches the string Lerp returning 'to' for unparseable input).
        _apply(ca != null && cb != null
            ? BmotionColorInterpolator.Lerp(ca, cb, easedT)
            : _curFrames[seg + 1]);

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
                if (_repeatType == BmRepeatType.Mirror)
                {
                    Array.Reverse(_curFrames);
                    Array.Reverse(_curChannels);
                    MirrorTimes(_times);
                    // Keep each segment paired with its easing when the frame order flips.
                    Array.Reverse(_eases);
                }
                else if (_repeatType == BmRepeatType.Reverse && !_reversed)
                {
                    Array.Reverse(_curFrames);
                    Array.Reverse(_curChannels);
                    MirrorTimes(_times);
                    Array.Reverse(_eases);
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
        if (!_isInfinite && _repeatType == BmRepeatType.Mirror)
        {
            _apply((_repeat + 1) % 2 == 0 ? _frames[0] : _frames[^1]);
            return;
        }
        if (!_isInfinite && _repeatType == BmRepeatType.Reverse)
        {
            _apply(_repeat == 0 ? _frames[^1] : _frames[0]);
            return;
        }
        _apply(_frames[^1]);
    }

    /// <summary>
    /// Mirrors a (possibly non-uniform) times array in place so segment durations line up with the
    /// reversed frame order: <c>newTimes[i] = 1 - times[n-1-i]</c>. Applying it twice restores the
    /// original, matching how Mirror/Reverse alternate direction each iteration.
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
}
