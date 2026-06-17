namespace Bit.Butil;

/// <summary>
/// Options for registering a DOM event listener, mirroring the browser's
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/EventTarget/addEventListener#options">addEventListener options</see>.
/// </summary>
public sealed class ButilEventListenerOptions
{
    /// <summary>
    /// When <c>true</c>, the listener is invoked during the capture phase (before it reaches the
    /// target) instead of the bubbling phase. This value must match between add and remove.
    /// </summary>
    public bool Capture { get; set; }

    /// <summary>
    /// When <c>true</c>, signals the browser that the listener will never call
    /// <c>preventDefault()</c>, letting it optimize scrolling/touch performance. Setting this
    /// alongside <c>PreventDefault</c> is contradictory - the <c>preventDefault()</c> call is
    /// ignored (and the browser logs a console error) for passive listeners.
    /// </summary>
    public bool Passive { get; set; }

    /// <summary>
    /// When <c>true</c>, the listener is automatically removed after it fires once. The Butil
    /// bookkeeping is reconciled on the JS side after the single invocation, so disposing the
    /// returned subscription afterwards is a harmless no-op.
    /// </summary>
    public bool Once { get; set; }
}
