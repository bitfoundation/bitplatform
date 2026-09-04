namespace Bit.Butil;

/// <summary>
/// A rectangle in CSS pixels, as returned by the DOM's own geometry APIs.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DOMRect">DOMRect</see>
/// </summary>
public class Rect
{
    /// <summary>The rectangle's height.</summary>
    public double Height { get; set; }

    /// <summary>The rectangle's width.</summary>
    public double Width { get; set; }

    /// <summary>The x coordinate of the rectangle's origin.</summary>
    public double X { get; set; }

    /// <summary>The y coordinate of the rectangle's origin.</summary>
    public double Y { get; set; }
}
