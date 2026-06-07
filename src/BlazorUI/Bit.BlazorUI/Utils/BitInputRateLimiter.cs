namespace Bit.BlazorUI;

/// <summary>
/// Combines the debounce and throttle behaviors to rate-limit the handling of immediate input events.
/// This is shared by the text based input components (through <see cref="BitTextInputBase{TValue}"/>) and
/// any other component (like the dropdown) that needs the same Immediate/DebounceTime/ThrottleTime semantics.
/// </summary>
/// <typeparam name="TValue">The type of the payload that is passed to the handler (e.g. the event args).</typeparam>
public class BitInputRateLimiter<TValue>
{
    private readonly BitDebouncer _debouncer = new();
    private readonly BitThrottler _throttler = new();
    private TValue? _lastThrottleValue;

    /// <summary>
    /// Invokes the provided <paramref name="handler"/> for the given <paramref name="value"/>,
    /// applying a debounce when <paramref name="debounceTime"/> is greater than zero,
    /// a throttle when <paramref name="throttleTime"/> is greater than zero,
    /// or invoking it immediately otherwise.
    /// When throttling, the latest received value is used for the trailing invocation.
    /// </summary>
    /// <remarks>
    /// If both <paramref name="debounceTime"/> and <paramref name="throttleTime"/> are greater than zero,
    /// debounce takes precedence and throttle is ignored.
    /// </remarks>
    public async Task Run(TValue value, int debounceTime, int throttleTime, Func<TValue, Task> handler)
    {
        if (debounceTime > 0)
        {
            await _debouncer.Do(debounceTime, () => handler(value));
        }
        else if (throttleTime > 0)
        {
            _lastThrottleValue = value;
            await _throttler.Do(throttleTime, () => handler(_lastThrottleValue!));
        }
        else
        {
            await handler(value);
        }
    }

    /// <summary>
    /// Cancels any pending debounced/throttled handler and resets the internal state
    /// so that stale callbacks are not invoked after the consumer's UI state has changed
    /// (e.g. when a dropdown is closed or an input is cleared/disposed).
    /// </summary>
    public void Reset()
    {
        _debouncer.Cancel();
        _throttler.Reset();
        _lastThrottleValue = default;
    }
}
