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

    /// <summary>True when Alt was down as the event fired.</summary>
    public bool AltKey { get; set; }

    /// <summary>True when Ctrl was down as the event fired.</summary>
    public bool CtrlKey { get; set; }

    /// <summary>True when the Meta key (Command on macOS, the Windows key elsewhere) was down as the event fired.</summary>
    public bool MetaKey { get; set; }

    /// <summary>True when Shift was down as the event fired.</summary>
    public bool ShiftKey { get; set; }

    /// <summary>Every touch point currently on the surface, anywhere in the document.</summary>
    public ButilTouchPoint[] Touches { get; set; } = [];

    /// <summary>The subset of <see cref="Touches"/> whose contact started on this element.</summary>
    public ButilTouchPoint[] TargetTouches { get; set; } = [];

    /// <summary>The touch points this event is about - the ones that went down, moved or came up.</summary>
    public ButilTouchPoint[] ChangedTouches { get; set; } = [];
}
