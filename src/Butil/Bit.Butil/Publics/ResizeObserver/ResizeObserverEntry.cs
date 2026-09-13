namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/ResizeObserverEntry">ResizeObserverEntry</see>
/// with a flattened set of fields convenient for typical layout work.
/// </summary>
public class ResizeObserverEntry
{
    /// <summary>The target's content box at the moment of the callback.</summary>
    public Rect? ContentRect { get; set; }

    /// <summary>The content box's inline size - its width in a horizontal writing mode.</summary>
    public double InlineSize { get; set; }

    /// <summary>The content box's block size - its height in a horizontal writing mode.</summary>
    public double BlockSize { get; set; }

    /// <summary>The inline size in device pixels, which is what a canvas backing store should be sized to.</summary>
    public double DevicePixelInlineSize { get; set; }
    
    /// <summary>The block size in device pixels, which is what a canvas backing store should be sized to.</summary>
    public double DevicePixelBlockSize { get; set; }
}
