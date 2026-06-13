using Bit.Bmotion.Models;

namespace Bit.Bmotion.Engine;

/// <summary>Keyframe animation driver for numeric (double) properties.</summary>
internal sealed class NumericKeyframesDriver : IAnimationDriver
{
    private readonly double[] _frames;
    private readonly double _durationMs;
    private readonly double _delayMs;
    private readonly double[] _times;
    private readonly Func<double, double>[] _eases;
    private readonly int _repeat;
    private readonly bool _isInfinite;
    private readonly RepeatType _repeatType;
    private readonly double _repeatDelayMs;
    private readonly Action<double> _apply;

    private double _startTime = -1;
    private bool _cancelled;
    private int _iteration;
    private double[] _curFrames;

    public NumericKeyframesDriver(double[] frames, TransitionConfig config, Action<double> apply)
    {
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
        _times = config.Times ?? Enumerable.Range(0, n).Select(i => (double)i / (n - 1)).ToArray();

        // Per-segment easing: if ease is an array of length n-1, use one per segment; otherwise use same for all
        _eases = new Func<double, double>[n - 1];
        var globalEase = EasingFunctions.Get(config);
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
                _iteration++;
                _startTime = timestamp + _repeatDelayMs;
                if (_repeatType == RepeatType.Mirror || _repeatType == RepeatType.Reverse)
                    Array.Reverse(_curFrames);
                return false;
            }
            return true;
        }
        return false;
    }

    public void Cancel() => _cancelled = true;

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

/// <summary>Keyframe animation driver for CSS color string properties.</summary>
internal sealed class ColorKeyframesDriver : IAnimationDriver
{
    private readonly string[] _frames;
    private readonly double _durationMs;
    private readonly double _delayMs;
    private readonly double[] _times;
    private readonly Func<double, double>[] _eases;
    private readonly int _repeat;
    private readonly bool _isInfinite;
    private readonly RepeatType _repeatType;
    private readonly double _repeatDelayMs;
    private readonly Action<string> _apply;

    private double _startTime = -1;
    private bool _cancelled;
    private int _iteration;
    private string[] _curFrames;

    public ColorKeyframesDriver(string[] frames, TransitionConfig config, Action<string> apply)
    {
        _frames = frames;
        _curFrames = (string[])frames.Clone();
        _durationMs = config.Duration * 1000;
        _delayMs = config.Delay * 1000;
        _repeat = config.Repeat;
        _isInfinite = config.Repeat == int.MaxValue;
        _repeatType = config.RepeatType;
        _repeatDelayMs = config.RepeatDelay * 1000;
        _apply = apply;

        int n = frames.Length;
        _times = config.Times ?? Enumerable.Range(0, n).Select(i => (double)i / (n - 1)).ToArray();
        var globalEase = EasingFunctions.Get(config);
        _eases = Enumerable.Repeat(globalEase, n - 1).ToArray();
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) { _apply(_frames[^1]); return true; }

        if (_startTime < 0) _startTime = timestamp + _delayMs;
        if (timestamp < _startTime) { _apply(_curFrames[0]); return false; }

        double t = _durationMs > 0 ? Math.Min((timestamp - _startTime) / _durationMs, 1.0) : 1.0;

        int n = _curFrames.Length;
        int seg = n - 2;
        for (int i = 0; i < n - 1; i++) { if (t <= _times[i + 1]) { seg = i; break; } }
        double segLen = _times[seg + 1] - _times[seg];
        double segT = segLen > 0 ? (t - _times[seg]) / segLen : 1.0;
        double easedT = _eases[seg](Math.Min(segT, 1.0));
        _apply(ColorInterpolator.Lerp(_curFrames[seg], _curFrames[seg + 1], easedT));

        if (t >= 1.0)
        {
            if (_isInfinite || _iteration < _repeat)
            {
                _iteration++;
                _startTime = timestamp + _repeatDelayMs;
                if (_repeatType == RepeatType.Mirror || _repeatType == RepeatType.Reverse)
                    Array.Reverse(_curFrames);
                return false;
            }
            return true;
        }
        return false;
    }

    public void Cancel() => _cancelled = true;
}
