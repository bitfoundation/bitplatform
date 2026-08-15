namespace Bit.BlazorUI;

/// <summary>
/// The size of an element and its position relative to the viewport, as returned by the
/// getBoundingClientRect function of the browser.
/// </summary>
public class BoundingClientRect
{
    public double Bottom { get; set; }
    public double Height { get; set; }
    public double Left { get; set; }
    public double Right { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
