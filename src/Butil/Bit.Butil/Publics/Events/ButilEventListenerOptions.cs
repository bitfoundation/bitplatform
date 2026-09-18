using System;

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

    /// <summary>
    /// The shortest time allowed between two calls into the .NET handler. <c>null</c> or
    /// <see cref="TimeSpan.Zero"/> - the default - forwards every event.
    /// <br/>
    /// This is the single most effective thing you can set on a high-frequency event
    /// (<c>mousemove</c>, <c>pointermove</c>, <c>scroll</c>, <c>resize</c>, <c>wheel</c>,
    /// <c>touchmove</c>): each of those fires around once a frame, and each call is a JSON
    /// serialization plus - under Blazor Server - a SignalR message and a network round trip. A
    /// 16 ms interval caps the traffic at one frame's worth; 50-100 ms is plenty for anything
    /// driving a re-render.
    /// </summary>
    /// <remarks>
    /// The gate is applied in JavaScript, before the round trip, so a suppressed event costs
    /// nothing but the event mapping. It is leading-edge with a trailing send: the first event
    /// after an idle gap goes through immediately, and the newest event suppressed during an
    /// interval is delivered when that interval elapses - so the handler always ends up seeing
    /// where the pointer stopped, not where it was one sample earlier.
    /// <br/>
    /// <c>preventDefault</c> and <c>stopPropagation</c> are unaffected: they still run on every
    /// event, because dropping them would change what the page does rather than how often .NET
    /// hears about it.
    /// </remarks>
    public TimeSpan? MinInterval { get; set; }
}
