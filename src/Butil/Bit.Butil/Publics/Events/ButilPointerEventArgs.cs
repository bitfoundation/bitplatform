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

    /// <summary>True when Alt was down as the event fired.</summary>
    public bool AltKey { get; set; }

    /// <summary>The button that changed state, when one did: 0 primary, 1 middle, 2 secondary.</summary>
    public int Button { get; set; }

    /// <summary>A bitmask of every button held at that moment: 1 primary, 2 secondary, 4 middle.</summary>
    public int Buttons { get; set; }

    /// <summary>The X coordinate in viewport coordinates.</summary>
    public double ClientX { get; set; }

    /// <summary>The Y coordinate in viewport coordinates.</summary>
    public double ClientY { get; set; }

    /// <summary>True when Ctrl was down as the event fired.</summary>
    public bool CtrlKey { get; set; }

    /// <summary>True when the Meta key (Command on macOS, the Windows key elsewhere) was down as the event fired.</summary>
    public bool MetaKey { get; set; }

    /// <summary>The X distance moved since the previous event of the same kind.</summary>
    public double MovementX { get; set; }

    /// <summary>The Y distance moved since the previous event of the same kind.</summary>
    public double MovementY { get; set; }

    /// <summary>The X coordinate relative to the target's padding edge.</summary>
    public double OffsetX { get; set; }

    /// <summary>The Y coordinate relative to the target's padding edge.</summary>
    public double OffsetY { get; set; }

    /// <summary>The X coordinate relative to the whole document, so it includes the page scroll.</summary>
    public double PageX { get; set; }

    /// <summary>The Y coordinate relative to the whole document, so it includes the page scroll.</summary>
    public double PageY { get; set; }

    /// <summary>The X coordinate in screen coordinates.</summary>
    public double ScreenX { get; set; }

    /// <summary>The Y coordinate in screen coordinates.</summary>
    public double ScreenY { get; set; }

    /// <summary>True when Shift was down as the event fired.</summary>
    public bool ShiftKey { get; set; }

    /// <summary>Alias for <see cref="ClientX"/>.</summary>
    public double X { get; set; }
    
    /// <summary>Alias for <see cref="ClientY"/>.</summary>
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
