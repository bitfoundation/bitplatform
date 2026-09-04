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
}
