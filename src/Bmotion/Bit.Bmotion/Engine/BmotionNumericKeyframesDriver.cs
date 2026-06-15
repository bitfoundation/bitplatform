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
    private double[] _curFrames;

    public BmotionNumericKeyframesDriver(double[] frames, BmotionTransitionConfig config, Action<double> apply)
    {
        if (frames is null || frames.Length < 2)
            throw new ArgumentException("Keyframe animations require at least 2 frames.", nameof(frames));
        if (config.Times != null && config.Times.Length != frames.Length)
            throw new ArgumentException("Times array length must match the number of frames.", nameof(config));

        _frames = frames;
        _curFrames = (double[])frames.Clone();
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

        // Per-segment easing: if ease is an array of length n-1, use one per segment; otherwise use same for all
        _eases = new Func<double, double>[n - 1];
        var globalEase = BmotionEasingFunctions.Get(config);
        for (int i = 0; i < n - 1; i++)
            _eases[i] = globalEase;
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) { _apply(_frames[^1]); return true; }

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
                if (_repeatType == BmotionRepeatType.Mirror || _repeatType == BmotionRepeatType.Reverse)
                {
                    Array.Reverse(_curFrames);
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
        double easedT = eases[seg](Math.Min(segT, 1.0));
        return frames[seg] + (frames[seg + 1] - frames[seg]) * easedT;
    }
}
