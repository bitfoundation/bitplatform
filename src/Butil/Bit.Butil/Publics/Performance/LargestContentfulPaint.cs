namespace Bit.Butil;

/// <summary>
/// The largest image or text block painted in the viewport - the Web Vital that stands in for "when
/// did the page look loaded".
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/LargestContentfulPaint">https://developer.mozilla.org/en-US/docs/Web/API/LargestContentfulPaint</see>
/// </summary>
/// <remarks>
/// The browser reports a new candidate every time something larger paints, so the entry that counts
/// is the <i>last</i> one before the user first interacts - not the first, and not an average. Read
/// <see cref="Performance.GetWebVitals"/> if that bookkeeping is all you want.
/// <br/>
/// Use <see cref="RenderTime"/> when it is non-zero and <see cref="LoadTime"/> otherwise: a
/// cross-origin image without <c>Timing-Allow-Origin</c> is not allowed to report its render time.
/// </remarks>
public class LargestContentfulPaint : PerformanceEntry
{
    /// <summary>When the element was painted, in milliseconds since the time origin. <c>0</c> for a cross-origin image without <c>Timing-Allow-Origin</c>.</summary>
    public double RenderTime { get; set; }

    /// <summary>When the element's resource finished loading.</summary>
    public double LoadTime { get; set; }

    /// <summary>The element's intrinsic size in pixels - width times height, ignoring any CSS scaling.</summary>
    public long Size { get; set; }

    /// <summary>The <c>id</c> of the element, when it has one.</summary>
    public string? Id { get; set; }

    /// <summary>The URL of the image, for an image candidate. Empty for a text block.</summary>
    public string? Url { get; set; }
}
