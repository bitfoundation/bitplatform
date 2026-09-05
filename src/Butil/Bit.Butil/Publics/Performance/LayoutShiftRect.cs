namespace Bit.Butil;

/// <summary>
/// A rectangle in the viewport, as a layout shift reports it - where an element was, and where it
/// ended up.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/LayoutShiftAttribution">https://developer.mozilla.org/en-US/docs/Web/API/LayoutShiftAttribution</see>
/// </summary>
public class LayoutShiftRect
{
    /// <summary>Distance from the viewport's left edge, in CSS pixels.</summary>
    public double X { get; set; }

    /// <summary>Distance from the viewport's top edge, in CSS pixels.</summary>
    public double Y { get; set; }

    /// <summary>Width in CSS pixels.</summary>
    public double Width { get; set; }

    /// <summary>Height in CSS pixels.</summary>
    public double Height { get; set; }

    /// <summary>The left edge - the same value as <see cref="X"/>.</summary>
    public double Left { get; set; }

    /// <summary>The top edge - the same value as <see cref="Y"/>.</summary>
    public double Top { get; set; }

    /// <summary>The right edge.</summary>
    public double Right { get; set; }

    /// <summary>The bottom edge.</summary>
    public double Bottom { get; set; }
}
