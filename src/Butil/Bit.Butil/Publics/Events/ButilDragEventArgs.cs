using System;

namespace Bit.Butil;

/// <summary>
/// Drag event payload - see <see href="https://developer.mozilla.org/en-US/docs/Web/API/DragEvent">DragEvent</see>.
/// </summary>
/// <remarks>
/// The DataTransfer object can't be passed straight across JS interop because it holds
/// browser-side resources; we surface its inert metadata only. To actually read the
/// dropped files use the standard <c>InputFile</c> component or call back into JS for
/// each file.
/// </remarks>
public class ButilDragEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = [
        "altKey", "button", "buttons", "clientX", "clientY", "ctrlKey", "metaKey",
        "offsetX", "offsetY", "pageX", "pageY", "screenX", "screenY", "shiftKey",
        "x", "y"];

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
    public double X { get; set; }
    public double Y { get; set; }
}
