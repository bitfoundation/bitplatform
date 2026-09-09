namespace Bit.Butil;

/// <summary>
/// Options for <see href="https://developer.mozilla.org/en-US/docs/Web/API/IntersectionObserver/IntersectionObserver">IntersectionObserver()</see>.
/// </summary>
public class IntersectionObserverOptions
{
    /// <summary>CSS-style margin around the root, e.g. "0px 0px -50px 0px".</summary>
    public string? RootMargin { get; set; }

    /// <summary>One or more thresholds in [0, 1]. Defaults to a single 0 threshold.</summary>
    public double[]? Thresholds { get; set; }

    /// <summary>
    /// The shortest time allowed between two calls into the .NET handler, in milliseconds.
    /// <c>0</c> - the default - forwards every batch of entries.
    /// <br/>
    /// Worth setting whenever the observed elements are in a scrolling list: a scroll crosses
    /// thresholds on nearly every frame, and each crossing is otherwise an interop round trip.
    /// </summary>
    /// <remarks>
    /// Applied in JavaScript, before the round trip. Leading-edge with a trailing send, so the
    /// batch that leaves the element in its settled state is always delivered - a lazy-loading
    /// tracker never misses the "now visible" it is waiting for, it only hears it a little later.
    /// <br/>
    /// The unit is milliseconds rather than a <see cref="System.TimeSpan"/> because this type is
    /// serialized to the browser as-is.
    /// </remarks>
    public double MinInterval { get; set; }
}
