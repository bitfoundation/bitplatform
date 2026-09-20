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
    private long? _lastThrottleRun;

    /// <summary>
    /// Invokes the provided <paramref name="handler"/> for the given <paramref name="value"/>,
    /// applying a debounce when <paramref name="debounceTime"/> is greater than zero,
    /// a throttle when <paramref name="throttleTime"/> is greater than zero,
    /// or invoking it immediately otherwise.
    /// When throttling, a value that arrives once the interval has passed is handled right away, and the
    /// last value of a burst is handled once the input has been quiet for the same interval.
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
            // The handler runs from within the call that delivered the value whenever it can. A handler run
            // from a timer while the user is still typing renders a value the browser has already moved
            // past, since the keystrokes typed in the meantime have not reached the component yet, and
            // that render writes the older text back over the input. So a timer only ever handles the
            // last value of a burst, once nothing has been typed for the whole interval.
            var now = Environment.TickCount64;

            if (_lastThrottleRun is null || now - _lastThrottleRun.Value >= throttleTime)
            {
                _lastThrottleRun = now;
                _debouncer.Cancel();
                await handler(value);
            }
            else
            {
                await _debouncer.Do(throttleTime, () =>
                {
                    _lastThrottleRun = Environment.TickCount64;
                    return handler(value);
                });
            }
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
        _lastThrottleRun = null;
    }
}
