using System;

namespace Bit.Butil;

/// <summary>
/// Pointer event payload - see <see href="https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent">PointerEvent</see>.
/// Pointer events unify mouse, pen and touch interaction.
/// </summary>
public class ButilPointerEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = [
        "altKey", "button", "buttons", "clientX", "clientY", "ctrlKey", "metaKey",
        "movementX", "movementY", "offsetX", "offsetY", "pageX", "pageY",
        "screenX", "screenY", "shiftKey", "x", "y",
        "pointerId", "width", "height", "pressure", "tangentialPressure",
        "tiltX", "tiltY", "twist", "pointerType", "isPrimary"];

    public bool AltKey { get; set; }
    public int Button { get; set; }
    public int Buttons { get; set; }
    public double ClientX { get; set; }
    public double ClientY { get; set; }
    public bool CtrlKey { get; set; }
    public bool MetaKey { get; set; }
    public double MovementX { get; set; }
    public double MovementY { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double PageX { get; set; }
    public double PageY { get; set; }
    public double ScreenX { get; set; }
    public double ScreenY { get; set; }
    public bool ShiftKey { get; set; }
    public double X { get; set; }
    public double Y { get; set; }

    /// <summary>Identifier for the pointer that produced the event (see PointerEvent.pointerId).</summary>
    public int PointerId { get; set; }

    /// <summary>Width (magnitude on the X axis), in CSS pixels, of the contact geometry.</summary>
    public double Width { get; set; }

    /// <summary>Height (magnitude on the Y axis), in CSS pixels, of the contact geometry.</summary>
    public double Height { get; set; }

    /// <summary>Normalized pressure of the pointer input in the range 0 to 1.</summary>
    public double Pressure { get; set; }

    /// <summary>Normalized tangential pressure (also called barrel pressure) for stylus inputs.</summary>
    public double TangentialPressure { get; set; }

    /// <summary>Plane angle (degrees) between the Y-Z plane and the plane containing the pointer axis and Y axis.</summary>
    public double TiltX { get; set; }

    /// <summary>Plane angle (degrees) between the X-Z plane and the plane containing the pointer axis and X axis.</summary>
    public double TiltY { get; set; }

    /// <summary>Clockwise rotation of the pointer (e.g. pen barrel) in degrees, 0–359.</summary>
    public double Twist { get; set; }

    /// <summary><c>"mouse"</c>, <c>"pen"</c>, <c>"touch"</c>, or empty for unknown.</summary>
    public string PointerType { get; set; } = string.Empty;

    /// <summary>True if this pointer is the primary pointer of its type.</summary>
    public bool IsPrimary { get; set; }
}
