namespace Bit.Belemetry;

/// <summary>
/// The <see cref="BelemetryErrorOptions"/> rules: suppresses an error identical to one recorded within the duplicate
/// window, then caps the rest per minute. The same rules are implemented in <c>Scripts/limits.ts</c>; keep both in step.
/// </summary>
internal sealed class BelemetryErrorLimiter
{
    // Enough distinct errors for any real app; past it the oldest signatures are forgotten (worst case, a duplicate
    // gets through - the per-minute cap still holds).
    private const int MaxSignatures = 128;

    private readonly object _lock = new();
    private readonly Dictionary<string, long> _recent = new(StringComparer.Ordinal);
    private readonly Queue<string> _order = new();
    private readonly long _windowMs;
    private readonly BelemetryRateLimiter? _rate;
    private readonly Func<long> _clock;

    /// <param name="options">The rules.</param>
    /// <param name="clock">Milliseconds from any fixed point; settable for tests.</param>
    public BelemetryErrorLimiter(BelemetryErrorOptions options, Func<long>? clock = null)
    {
        _clock = clock ?? (static () => Environment.TickCount64);
        _windowMs = (long)Math.Max(0, options.DuplicateWindow.TotalMilliseconds);
        _rate = options.MaxPerMinute > 0 ? new BelemetryRateLimiter(options.MaxPerMinute, TimeSpan.FromMinutes(1), _clock) : null;
    }

    /// <summary>Whether <paramref name="log"/>, an error record, should be recorded.</summary>
    public bool ShouldRecord(BelemetryLogRecord log)
    {
        if (_windowMs <= 0) return _rate is null || _rate.Take(1) == 1;

        var signature = Signature(log);
        var now = _clock();
        lock (_lock)
        {
            if (_recent.TryGetValue(signature, out var recorded) && now - recorded < _windowMs) return false;

            // Only a recorded error opens a window: one the per-minute cap dropped must get through once tokens are back.
            if (_rate is not null && _rate.Take(1) == 0) return false;

            if (_recent.ContainsKey(signature) is false)
            {
                _order.Enqueue(signature);
                if (_order.Count > MaxSignatures) _recent.Remove(_order.Dequeue());
            }
            _recent[signature] = now;
            return true;
        }
    }

    /// <summary>
    /// The scope, the exception type and message (or the body), and the first two lines of the stack trace - the
    /// message line and the first frame in .NET and Chrome, the first two frames in Firefox and Safari.
    /// </summary>
    internal static string Signature(BelemetryLogRecord log)
    {
        var attributes = log.Attributes;
        var stack = attributes.GetString(BelemetrySemanticConventions.ExceptionStacktrace);
        var head = stack;
        if (stack is not null)
        {
            var first = stack.IndexOf('\n', StringComparison.Ordinal);
            var second = first < 0 ? -1 : stack.IndexOf('\n', first + 1);
            if (second >= 0) head = stack[..second];
        }

        return string.Join('\u0001', log.Scope.Name,
                           attributes.GetString(BelemetrySemanticConventions.ExceptionType),
                           attributes.GetString(BelemetrySemanticConventions.ExceptionMessage) ?? log.Body,
                           head?.TrimEnd());
    }
}
