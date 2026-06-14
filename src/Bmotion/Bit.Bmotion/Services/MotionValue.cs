namespace Bit.Bmotion.Services;

/// <summary>
/// A reactive numeric value whose changes can be observed and linked to animations.
/// Analogous to Framer Motion's <c>MotionValue&lt;T&gt;</c>.
/// Purely C# - no JS synchronisation required.
/// </summary>
public class MotionValue<T> : IDisposable where T : struct
{
    private readonly string _id;
    private T _value;
    private readonly List<Func<T, Task>> _subscribers = new();

    /// <summary>Subscription to a parent MotionValue when this instance is a derived/transformed value.</summary>
    private IDisposable? _upstream;

    internal MotionValue(string id, T initial)
    {
        _id    = id;
        _value = initial;
    }

    // ── Value access ──────────────────────────────────────────────────────────

    public T Value
    {
        get => _value;
        set => SetSync(value);
    }

    /// <summary>
    /// Synchronously updates the value and notifies subscribers. Subscriber tasks are
    /// observed (rather than dropped) so their exceptions don't go unobserved.
    /// </summary>
    public void SetSync(T value)
    {
        _value = value;
        foreach (var sub in _subscribers.ToArray())
            _ = ObserveAsync(sub(value));
    }

    private static async Task ObserveAsync(Task task)
    {
        try { await task; }
        catch { /* subscriber failures are swallowed to avoid faulting the host */ }
    }

    /// <summary>Update the value and notify all subscribers.</summary>
    public async Task SetAsync(T value)
    {
        _value = value;
        foreach (var sub in _subscribers.ToArray())
            await sub(value);
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────

    /// <summary>Subscribe to value changes. Returns an unsubscribe action.</summary>
    public IDisposable Subscribe(Func<T, Task> callback)
    {
        _subscribers.Add(callback);
        return new Subscription(() => _subscribers.Remove(callback));
    }

    /// <summary>Synchronous convenience overload.</summary>
    public IDisposable Subscribe(Action<T> callback)
        => Subscribe(v => { callback(v); return Task.CompletedTask; });

    // ── Transforms ────────────────────────────────────────────────────────────

    /// <summary>
    /// Create a derived MotionValue that applies a transformation function.
    /// Analogous to Framer Motion's <c>useTransform</c>.
    /// </summary>
    public MotionValue<TOut> Transform<TOut>(Func<T, TOut> fn) where TOut : struct
    {
        var derived = new MotionValue<TOut>($"{_id}_t", fn(_value));
        // Keep the parent→derived link so it can be torn down when the derived value is disposed,
        // otherwise the parent would hold the derived value alive indefinitely (a leak).
        derived._upstream = Subscribe(async v => await derived.SetAsync(fn(v)));
        return derived;
    }

    /// <summary>
    /// Map from an input range to an output range using linear interpolation.
    /// </summary>
    public MotionValue<double> Transform(double[] inputRange, double[] outputRange)
    {
        if (inputRange.Length != outputRange.Length)
            throw new ArgumentException("inputRange and outputRange must have the same length.");
        if (inputRange.Length < 2)
            throw new ArgumentException("inputRange and outputRange must contain at least 2 points.");
        for (int i = 0; i < inputRange.Length - 1; i++)
            if (inputRange[i + 1] <= inputRange[i])
                throw new ArgumentException("inputRange must be strictly increasing (no repeated or decreasing points).");

        double Map(T v)
        {
            double x = Convert.ToDouble(v);
            for (int i = 0; i < inputRange.Length - 1; i++)
            {
                if (x >= inputRange[i] && x <= inputRange[i + 1])
                {
                    double t = (x - inputRange[i]) / (inputRange[i + 1] - inputRange[i]);
                    return outputRange[i] + t * (outputRange[i + 1] - outputRange[i]);
                }
            }
            return x < inputRange[0] ? outputRange[0] : outputRange[^1];
        }

        var derived = new MotionValue<double>($"{_id}_tr", Map(_value));
        derived._upstream = Subscribe(async v => await derived.SetAsync(Map(v)));
        return derived;
    }

    public void Dispose()
    {
        _upstream?.Dispose();
        _upstream = null;
        _subscribers.Clear();
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _dispose;
        public Subscription(Action dispose) => _dispose = dispose;
        public void Dispose() => _dispose();
    }
}

/// <summary>Factory helper for creating MotionValues.</summary>
public static class MotionValueFactory
{
    public static MotionValue<T> Create<T>(T initial) where T : struct
        => new($"mv_{Guid.NewGuid():N}", initial);
}
