namespace Bit.BlazorUI;

/// <summary>
/// Represents a browser event. Not all browser events are listed
/// as properties but you can use <see cref="Custom"/> to create
/// events that aren't listed as static properties.
/// <para>
/// Reference for browser events can be found here:
/// <a href="https://developer.mozilla.org/en-US/docs/Web/Events"/>
/// </para>
/// </summary>
public sealed class BrowserEvent : StringEnum
{
    /// <summary>
    /// A pointing device button (ANY button; soon to be primary button only)
    /// has been pressed and released on an element.
    /// </summary>
    public static BrowserEvent Click => new BrowserEvent("click");

    /// <summary>
    /// The right button of the mouse is clicked (before the context menu is displayed).
    /// </summary>
    public static BrowserEvent ContextMenu => new BrowserEvent("contextmenu");

    /// <summary>
    /// A pointing device button is clicked twice on an element.
    /// </summary>
    public static BrowserEvent DoubleClick => new BrowserEvent("dblclick");

    /// <summary>
    /// A pointing device button is pressed on an element.
    /// </summary>
    public static BrowserEvent MouseDown => new BrowserEvent("mousedown");

    /// <summary>
    /// A pointing device is moved onto the element that has the listener attached.
    /// </summary>
    public static BrowserEvent MouseEnter => new BrowserEvent("mouseenter");

    /// <summary>
    /// A pointing device is moved off the element that has the listener attached.
    /// </summary>
    public static BrowserEvent MouseLeave => new BrowserEvent("mouseleave");

    /// <summary>
    /// A pointing device is moved over an element. (Fired continously as the mouse moves.)
    /// </summary>
    public static BrowserEvent MouseMove => new BrowserEvent("mousemove");

    /// <summary>
    /// A pointing device is moved onto the element that has the listener attached or onto one of its children.
    /// </summary>
    public static BrowserEvent MouseOver => new BrowserEvent("mouseover");

    /// <summary>
    /// A pointing device is moved off the element that has the listener attached or off one of its children.
    /// </summary>
    public static BrowserEvent MouseOut => new BrowserEvent("mouseout");

    /// <summary>
    /// A pointing device button is released over an element.
    /// </summary>
    public static BrowserEvent MouseUp => new BrowserEvent("mouseup");

    /// <summary>
    /// One or more touch points are placed on the touch surface.
    /// </summary>
    public static BrowserEvent TouchStart => new BrowserEvent("touchstart");

    /// <summary>
    /// One or more touch points are moved along the touch surface.
    /// </summary>
    public static BrowserEvent TouchMove => new BrowserEvent("touchmove");

    /// <summary>
    /// One or more touch points are removed from the touch surface.
    /// </summary>
    public static BrowserEvent TouchEnd => new BrowserEvent("touchend");

    /// <summary>
    /// This method constructs a <see cref="BrowserEvent"/> which represents the given value.
    /// Use this method if the event is not available as a static property.
    /// </summary>
    /// <param name="browserEvent">The string representation of your browser event.</param>
    public static BrowserEvent Custom(string browserEvent) => new BrowserEvent(browserEvent);

    private BrowserEvent(string stringRep) : base(stringRep) { }
}
