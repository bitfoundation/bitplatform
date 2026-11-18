namespace Bit.Bink;

/// <summary>What the mouse wheel and a trackpad do over a <see cref="BinkCanvas"/>.</summary>
public enum BinkWheelMode : byte
{
    /// <summary>Nothing: the page scrolls as usual. Right for a canvas embedded in a form.</summary>
    None = 0,

    /// <summary>
    /// The wheel and two-finger trackpad scrolling pan the view; Ctrl (Command on macOS) with the wheel, and a
    /// trackpad pinch, zoom around the pointer. Shift with the wheel pans sideways. This is the convention of design
    /// tools and whiteboards.
    /// </summary>
    PanAndZoom = 1,

    /// <summary>The wheel zooms around the pointer; Shift with the wheel pans. This is the convention of maps.</summary>
    Zoom = 2,
}
