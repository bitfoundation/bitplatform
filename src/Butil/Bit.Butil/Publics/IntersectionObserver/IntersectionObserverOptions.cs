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
}
