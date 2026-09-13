using System;

namespace Bit.Butil;

/// <summary>
/// Individual touch point inside a <see cref="ButilTouchEventArgs"/>.
/// </summary>
public class ButilTouchPoint
{
    /// <summary>A number identifying this contact for as long as the finger stays down.</summary>
    public int Identifier { get; set; }

    /// <summary>The X coordinate in viewport coordinates.</summary>
    public double ClientX { get; set; }

    /// <summary>The Y coordinate in viewport coordinates.</summary>
    public double ClientY { get; set; }

    /// <summary>The X coordinate relative to the whole document, so it includes the page scroll.</summary>
    public double PageX { get; set; }

    /// <summary>The Y coordinate relative to the whole document, so it includes the page scroll.</summary>
    public double PageY { get; set; }

    /// <summary>The X coordinate in screen coordinates.</summary>
    public double ScreenX { get; set; }

    /// <summary>The Y coordinate in screen coordinates.</summary>
    public double ScreenY { get; set; }

    /// <summary>Half the width of the contact area, in CSS pixels. 1 where the device does not report it.</summary>
    public double RadiusX { get; set; }

    /// <summary>Half the height of the contact area, in CSS pixels. 1 where the device does not report it.</summary>
    public double RadiusY { get; set; }

    /// <summary>Degrees of rotation of the contact ellipse. 0 where the device does not report it.</summary>
    public double RotationAngle { get; set; }
    
    /// <summary>Contact pressure in [0, 1]. 0 where the device does not report it.</summary>
    public double Force { get; set; }
}
