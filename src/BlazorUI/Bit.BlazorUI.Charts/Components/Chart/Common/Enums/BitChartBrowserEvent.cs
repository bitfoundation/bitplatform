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
public sealed class BitChartBrowserEvent : BitChartStringEnum
{
    /// <summary>
    /// A pointing device button (ANY button; soon to be primary button only)
    /// has been pressed and released on an element.
    /// </summary>
    public static BitChartBrowserEvent Click => new BitChartBrowserEvent("click");

    /// <summary>
    /// The right button of the mouse is clicked (before the context menu is displayed).
    /// </summary>
    public static BitChartBrowserEvent ContextMenu => new BitChartBrowserEvent("contextmenu");

    /// <summary>
    /// A pointing device button is clicked twice on an element.
    /// </summary>
    public static BitChartBrowserEvent DoubleClick => new BitChartBrowserEvent("dblclick");

    /// <summary>
    /// A pointing device button is pressed on an element.
    /// </summary>
    public static BitChartBrowserEvent MouseDown => new BitChartBrowserEvent("mousedown");

    /// <summary>
    /// A pointing device is moved onto the element that has the listener attached.
    /// </summary>
    public static BitChartBrowserEvent MouseEnter => new BitChartBrowserEvent("mouseenter");

    /// <summary>
    /// A pointing device is moved off the element that has the listener attached.
    /// </summary>
    public static BitChartBrowserEvent MouseLeave => new BitChartBrowserEvent("mouseleave");

    /// <summary>
    /// A pointing device is moved over an element. (Fired continously as the mouse moves.)
    /// </summary>
    public static BitChartBrowserEvent MouseMove => new BitChartBrowserEvent("mousemove");

    /// <summary>
    /// A pointing device is moved onto the element that has the listener attached or onto one of its children.
    /// </summary>
    public static BitChartBrowserEvent MouseOver => new BitChartBrowserEvent("mouseover");

    /// <summary>
    /// A pointing device is moved off the element that has the listener attached or off one of its children.
    /// </summary>
    public static BitChartBrowserEvent MouseOut => new BitChartBrowserEvent("mouseout");

    /// <summary>
    /// A pointing device button is released over an element.
    /// </summary>
    public static BitChartBrowserEvent MouseUp => new BitChartBrowserEvent("mouseup");

    /// <summary>
    /// One or more touch points are placed on the touch surface.
    /// </summary>
    public static BitChartBrowserEvent TouchStart => new BitChartBrowserEvent("touchstart");

    /// <summary>
    /// One or more touch points are moved along the touch surface.
    /// </summary>
    public static BitChartBrowserEvent TouchMove => new BitChartBrowserEvent("touchmove");

    /// <summary>
    /// One or more touch points are removed from the touch surface.
    /// </summary>
    public static BitChartBrowserEvent TouchEnd => new BitChartBrowserEvent("touchend");

    /// <summary>
    /// This method constructs a <see cref="BitChartBrowserEvent"/> which represents the given value.
    /// Use this method if the event is not available as a static property.
    /// </summary>
    /// <param name="browserEvent">The string representation of your browser event.</param>
    public static BitChartBrowserEvent Custom(string browserEvent) => new BitChartBrowserEvent(browserEvent);

    private BitChartBrowserEvent(string stringRep) : base(stringRep) { }
}
