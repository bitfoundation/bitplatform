namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/ResizeObserverEntry">ResizeObserverEntry</see>
/// with a flattened set of fields convenient for typical layout work.
/// </summary>
public class ResizeObserverEntry
{
    public Rect? ContentRect { get; set; }
    public double InlineSize { get; set; }
    public double BlockSize { get; set; }
    public double DevicePixelInlineSize { get; set; }
    public double DevicePixelBlockSize { get; set; }
}
