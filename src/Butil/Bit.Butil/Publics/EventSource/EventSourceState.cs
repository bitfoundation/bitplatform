namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/EventSource/readyState">EventSource.readyState</see>.
/// </summary>
public enum EventSourceState
{
    /// <summary>
    /// Connecting, or reconnecting after a drop - the browser retries by itself, so this is a
    /// normal state to see mid-stream rather than a failure.
    /// </summary>
    Connecting = 0,

    /// <summary>Connected and delivering events.</summary>
    Open = 1,

    /// <summary>
    /// Closed for good; the browser will not retry. Also what a disposed handle reports.
    /// </summary>
    Closed = 2,
}
