using System;

namespace Bit.Butil;


// see https://developer.mozilla.org/en-US/docs/Web/API/MouseEvent
/// <summary>
/// Contains properties of the event describing user interaction with the mouse.
/// </summary>
public class ButilMouseEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = [
        "altKey", "button", "buttons", "clientX", "clientY", "ctrlKey", "layerX", "layerY", "metaKey", "movementX", 
        "movementY", "offsetX", "offsetY", "pageX", "pageY", "relatedTarget", "screenX", "screenY", "x", "y","shiftKey"];

    /// <summary>
    /// Returns true if the alt key was down when the mouse event was fired.
    /// </summary>
    public bool AltKey { get; set; }

    /// <summary>
    /// The button number that was pressed (if applicable) when the mouse event was fired.
    /// </summary>
    public int Button { get; set; }

    /// <summary>
    /// The buttons being pressed (if any) when the mouse event was fired.
    /// </summary>
    public int Buttons { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer in viewport coordinates.
    /// </summary>
    public double ClientX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer in viewport coordinates.
    /// </summary>
    public double ClientY { get; set; }

    /// <summary>
    /// Returns true if the control key was down when the mouse event was fired.
    /// </summary>
    public bool CtrlKey { get; set; }

    /// <summary>
    /// Returns the horizontal coordinate of the event relative to the current layer.
    /// </summary>
    public double LayerX { get; set; }

    /// <summary>
    /// Returns the vertical coordinate of the event relative to the current layer.
    /// </summary>
    public double LayerY { get; set; }

    /// <summary>
    /// Returns true if the meta key was down when the mouse event was fired.
    /// </summary>
    public bool MetaKey { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer relative to the position of the last mousemove event.
    /// </summary>
    public double MovementX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer relative to the position of the last mousemove event.
    /// </summary>
    public double MovementY { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer relative to the position of the padding edge of the target node.
    /// </summary>
    public double OffsetX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer relative to the position of the padding edge of the target node.
    /// </summary>
    public double OffsetY { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer relative to the whole document.
    /// </summary>
    public double PageX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer relative to the whole document.
    /// </summary>
    public double PageY { get; set; }

    /// <summary>
    /// The secondary target for the event, if there is one.
    /// </summary>
    public string RelatedTarget { get; set; } = string.Empty;

    /// <summary>
    /// The X coordinate of the mouse pointer in screen coordinates.
    /// </summary>
    public double ScreenX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer in screen coordinates.
    /// </summary>
    public double ScreenY { get; set; }

    /// <summary>
    /// Returns a boolean value that is true if the Shift key was active when the key event was generated.
    /// </summary>
    public bool ShiftKey { get; set; }

    /// <summary>
    /// Alias for ClientX.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Alias for ClientY.
    /// </summary>
    public double Y { get; set; }
}
