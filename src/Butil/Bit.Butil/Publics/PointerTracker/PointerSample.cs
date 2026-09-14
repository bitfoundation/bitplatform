namespace Bit.Butil;

/// <summary>
/// One pointer position and its pen state - a single sample from a <see cref="PointerFrame"/>.
/// </summary>
public class PointerSample
{
    /// <summary>X relative to the tracked element's padding box, in CSS pixels.</summary>
    public double X { get; set; }

    /// <summary>Y relative to the tracked element's padding box, in CSS pixels.</summary>
    public double Y { get; set; }

    /// <summary>X relative to the viewport, in CSS pixels.</summary>
    public double ClientX { get; set; }

    /// <summary>Y relative to the viewport, in CSS pixels.</summary>
    public double ClientY { get; set; }

    /// <summary>Pen or touch pressure, 0 to 1. A mouse reports 0.5 while a button is down, 0 otherwise.</summary>
    public double Pressure { get; set; }

    /// <summary>Barrel pressure of a pen, -1 to 1. Zero for everything else.</summary>
    public double TangentialPressure { get; set; }

    /// <summary>Pen tilt away from vertical along X, in degrees (-90 to 90).</summary>
    public double TiltX { get; set; }

    /// <summary>Pen tilt away from vertical along Y, in degrees (-90 to 90).</summary>
    public double TiltY { get; set; }

    /// <summary>Pen rotation around its own axis, in degrees (0 to 359).</summary>
    public double Twist { get; set; }

    /// <summary>Width of the contact area, in CSS pixels.</summary>
    public double Width { get; set; }

    /// <summary>Height of the contact area, in CSS pixels.</summary>
    public double Height { get; set; }

    /// <summary>
    /// When the sample was taken, in milliseconds on the page's own timeline. The samples in a
    /// frame are ordered by this, which is what lets a stroke be drawn in the right order.
    /// </summary>
    public double TimeStamp { get; set; }
}
