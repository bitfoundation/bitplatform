using System;

namespace Bit.Butil;

/// <summary>
/// Touch event payload - see <see href="https://developer.mozilla.org/en-US/docs/Web/API/TouchEvent">TouchEvent</see>.
/// Note: many platforms have moved to <see cref="ButilPointerEventArgs"/>; expose touch when you need
/// access to multi-touch lists explicitly.
/// </summary>
public class ButilTouchEventArgs : EventArgs
{
    // Touches are object lists, not primitive members; events.ts maps them to JSON arrays.
    internal static readonly string[] EventArgsMembers = [
        "altKey", "ctrlKey", "metaKey", "shiftKey",
        "touches", "targetTouches", "changedTouches"];

    public bool AltKey { get; set; }
    public bool CtrlKey { get; set; }
    public bool MetaKey { get; set; }
    public bool ShiftKey { get; set; }

    public ButilTouchPoint[] Touches { get; set; } = [];
    public ButilTouchPoint[] TargetTouches { get; set; } = [];
    public ButilTouchPoint[] ChangedTouches { get; set; } = [];
}

/// <summary>
/// Individual touch point inside a <see cref="ButilTouchEventArgs"/>.
/// </summary>
public class ButilTouchPoint
{
    public int Identifier { get; set; }
    public double ClientX { get; set; }
    public double ClientY { get; set; }
    public double PageX { get; set; }
    public double PageY { get; set; }
    public double ScreenX { get; set; }
    public double ScreenY { get; set; }
    public double RadiusX { get; set; }
    public double RadiusY { get; set; }
    public double RotationAngle { get; set; }
    public double Force { get; set; }
}
