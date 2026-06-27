namespace Bit.Bmotion;
/// <summary>
/// A reactive numeric value whose changes can be observed and linked to animations.
/// Analogous to Framer Motion's <c>MotionValue&lt;T&gt;</c>.
/// Purely C# - no JS synchronisation required.
/// </summary>
public class BmotionValue<T> : IDisposable where T : struct
{
    private readonly string _id;
    private T _value;
    private readonly List<Func<T, Task>> _subscribers = new();

    /// <summary>Numeric value types accepted by the range-mapping <c>Transform</c> overload.</summary>
    private static readonly HashSet<Type> _numericTypes = new()
    {
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
    };

    /// <summary>Subscription to a parent BmotionValue when this instance is a derived/transformed value.</summary>
    private IDisposable? _upstream;

    internal BmotionValue(string id, T initial)
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
        {
            // Guard the invocation itself: a subscriber may throw synchronously before returning a
            // Task. Catch so one faulty subscriber can't skip the rest of the chain.
            try { _ = ObserveAsync(sub(value)); }
            catch { /* subscriber failures are swallowed to avoid faulting the host */ }
        }
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
        {
            // Catch both synchronous throws and faulted tasks so a single failing subscriber
            // doesn't prevent the remaining subscribers from being notified.
            try { await sub(value); }
            catch { /* subscriber failures are swallowed to avoid faulting the host */ }
        }
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────

    /// <summary>Subscribe to value changes. Returns an unsubscribe action.</summary>
    public IDisposable Subscribe(Func<T, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _subscribers.Add(callback);
        return new Subscription(() => _subscribers.Remove(callback));
    }

    /// <summary>Synchronous convenience overload.</summary>
    public IDisposable Subscribe(Action<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return Subscribe(v => { callback(v); return Task.CompletedTask; });
    }

    // ── Transforms ────────────────────────────────────────────────────────────

    /// <summary>
    /// Create a derived BmotionValue that applies a transformation function.
    /// Analogous to Framer Motion's <c>useTransform</c>.
    /// </summary>
    public BmotionValue<TOut> Transform<TOut>(Func<T, TOut> fn) where TOut : struct
    {
        ArgumentNullException.ThrowIfNull(fn);
        var derived = new BmotionValue<TOut>($"{_id}_t", fn(_value));
        // Subscribe weakly: the parent must not keep the derived value alive (that would make the
        // parent→derived link a leak for callers that drop the derived). The derived keeps the
        // parent alive via _upstream for as long as the caller holds the derived, which is the
        // intended direction. The subscription self-removes once the derived is collected.
        derived._upstream = SubscribeWeak(derived, fn);
        return derived;
    }

    /// <summary>
    /// Map from an input range to an output range using linear interpolation.
    /// </summary>
    public BmotionValue<double> Transform(double[] inputRange, double[] outputRange)
    {
        ArgumentNullException.ThrowIfNull(inputRange);
        ArgumentNullException.ThrowIfNull(outputRange);
        if (!_numericTypes.Contains(typeof(T)))
            throw new ArgumentException(
                $"Transform(inputRange, outputRange) only supports numeric value types; '{typeof(T).Name}' is not numeric.");
        if (inputRange.Length != outputRange.Length)
            throw new ArgumentException("inputRange and outputRange must have the same length.");
        if (inputRange.Length < 2)
            throw new ArgumentException("inputRange and outputRange must contain at least 2 points.");
        for (int i = 0; i < inputRange.Length - 1; i++)
            if (inputRange[i + 1] <= inputRange[i])
                throw new ArgumentException("inputRange must be strictly increasing (no repeated or decreasing points).");

        // Snapshot the ranges so the Map closure isn't affected by the caller mutating the
        // passed-in arrays after this method returns (which would bypass the validation above).
        var inRange = (double[])inputRange.Clone();
        var outRange = (double[])outputRange.Clone();

        double Map(T v)
        {
            double x = Convert.ToDouble(v);
            for (int i = 0; i < inRange.Length - 1; i++)
            {
                if (x >= inRange[i] && x <= inRange[i + 1])
                {
                    double t = (x - inRange[i]) / (inRange[i + 1] - inRange[i]);
                    return outRange[i] + t * (outRange[i + 1] - outRange[i]);
                }
            }
            return x < inRange[0] ? outRange[0] : outRange[^1];
        }

        var derived = new BmotionValue<double>($"{_id}_tr", Map(_value));
        derived._upstream = SubscribeWeak(derived, Map);
        return derived;
    }

    /// <summary>
    /// Subscribes the <paramref name="derived"/> value to this value's changes through a weak
    /// reference, so this (parent) value never keeps the derived one alive. The subscription
    /// removes itself the first time it fires after the derived value has been collected.
    /// </summary>
    private IDisposable SubscribeWeak<TOut>(BmotionValue<TOut> derived, Func<T, TOut> project)
        where TOut : struct
    {
        var weak = new WeakReference<BmotionValue<TOut>>(derived);
        IDisposable? sub = null;
        sub = Subscribe(async v =>
        {
            if (weak.TryGetTarget(out var target))
                await target.SetAsync(project(v));
            else
                sub?.Dispose(); // derived value collected - drop the dead subscription
        });
        return sub;
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
