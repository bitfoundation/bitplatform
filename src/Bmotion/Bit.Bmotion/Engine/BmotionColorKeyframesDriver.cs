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
    private readonly BmotionRepeatType _repeatType;
    private readonly double _repeatDelayMs;
    private readonly Action<string> _apply;

    private double _startTime = -1;
    private bool _cancelled;
    private int _iteration;
    private string[] _curFrames;
    private readonly double[]?[] _curChannels;

    public BmotionColorKeyframesDriver(string[] frames, BmotionTransitionConfig config, Action<string> apply)
    {
        if (frames is null || frames.Length < 2)
            throw new ArgumentException("Keyframe animations require at least 2 frames.", nameof(frames));
        if (config.Times != null && config.Times.Length != frames.Length)
            throw new ArgumentException("Times array length must match the number of frames.", nameof(config));

        _frames = (string[])frames.Clone();
        _curFrames = (string[])frames.Clone();
        _durationMs = config.Duration * 1000;
        _delayMs = config.Delay * 1000;
        _repeat = config.Repeat;
        _isInfinite = config.Repeat == int.MaxValue;
        _repeatType = config.RepeatType;
        _repeatDelayMs = config.RepeatDelay * 1000;
        _apply = apply;

        int n = frames.Length;
        // Clone the caller's Times so the in-place MirrorTimes mutation never touches their config.
        _times = config.Times != null
            ? (double[])config.Times.Clone()
            : Enumerable.Range(0, n).Select(i => (double)i / (n - 1)).ToArray();
        var globalEase = BmotionEasingFunctions.Get(config);
        _eases = Enumerable.Repeat(globalEase, n - 1).ToArray();

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
        double easedT = _eases[seg](Math.Min(segT, 1.0));
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
                if (_repeatType == BmotionRepeatType.Mirror || _repeatType == BmotionRepeatType.Reverse)
                {
                    Array.Reverse(_curFrames);
                    Array.Reverse(_curChannels);
                    MirrorTimes(_times);
                }
                return false;
            }
            return true;
        }
        return false;
    }

    public void Cancel() => _cancelled = true;

    public void Complete() => _apply(_frames[^1]);

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
