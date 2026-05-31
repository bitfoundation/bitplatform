namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/IntersectionObserverEntry">IntersectionObserverEntry</see>.
/// </summary>
public class IntersectionObserverEntry
{
    /// <summary>True when the target intersects the root with at least one threshold.</summary>
    public bool IsIntersecting { get; set; }

    /// <summary>Ratio of the target's bounding rect that intersects the root, in [0, 1].</summary>
    public double IntersectionRatio { get; set; }

    /// <summary>Time at which the intersection was detected (DOMHighResTimeStamp, in ms).</summary>
    public double Time { get; set; }

    public Rect? BoundingClientRect { get; set; }
    public Rect? IntersectionRect { get; set; }
    public Rect? RootBounds { get; set; }
}
