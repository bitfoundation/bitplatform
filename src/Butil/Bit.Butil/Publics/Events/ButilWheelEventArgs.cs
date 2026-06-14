using System;

namespace Bit.Butil;

/// <summary>
/// Wheel event payload - see <see href="https://developer.mozilla.org/en-US/docs/Web/API/WheelEvent">WheelEvent</see>.
/// </summary>
public class ButilWheelEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = [
        "altKey", "button", "buttons", "clientX", "clientY", "ctrlKey", "metaKey",
        "offsetX", "offsetY", "pageX", "pageY", "screenX", "screenY", "shiftKey",
        "deltaX", "deltaY", "deltaZ", "deltaMode"];

    public bool AltKey { get; set; }
    public int Button { get; set; }
    public int Buttons { get; set; }
    public double ClientX { get; set; }
    public double ClientY { get; set; }
    public bool CtrlKey { get; set; }
    public bool MetaKey { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double PageX { get; set; }
    public double PageY { get; set; }
    public double ScreenX { get; set; }
    public double ScreenY { get; set; }
    public bool ShiftKey { get; set; }

    /// <summary>Horizontal scroll amount.</summary>
    public double DeltaX { get; set; }

    /// <summary>Vertical scroll amount.</summary>
    public double DeltaY { get; set; }

    /// <summary>Z-axis (depth) scroll amount.</summary>
    public double DeltaZ { get; set; }

    /// <summary>0 = pixel, 1 = line, 2 = page.</summary>
    public int DeltaMode { get; set; }
}
