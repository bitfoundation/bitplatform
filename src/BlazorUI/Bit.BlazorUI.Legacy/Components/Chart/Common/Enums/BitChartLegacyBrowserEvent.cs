namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a browser event. Not all browser events are listed
/// as properties but you can use <see cref="Custom"/> to create
/// events that aren't listed as static properties.
/// <para>
/// Reference for browser events can be found here:
/// <a href="https://developer.mozilla.org/en-US/docs/Web/Events"/>
/// </para>
/// </summary>
public sealed class BitChartLegacyBrowserEvent : BitChartLegacyStringEnum
{
    /// <summary>
    /// A pointing device button (ANY button; soon to be primary button only)
    /// has been pressed and released on an element.
    /// </summary>
    public static BitChartLegacyBrowserEvent Click => new BitChartLegacyBrowserEvent("click");

    /// <summary>
    /// The right button of the mouse is clicked (before the context menu is displayed).
    /// </summary>
    public static BitChartLegacyBrowserEvent ContextMenu => new BitChartLegacyBrowserEvent("contextmenu");

    /// <summary>
    /// A pointing device button is clicked twice on an element.
    /// </summary>
    public static BitChartLegacyBrowserEvent DoubleClick => new BitChartLegacyBrowserEvent("dblclick");

    /// <summary>
    /// A pointing device button is pressed on an element.
    /// </summary>
    public static BitChartLegacyBrowserEvent MouseDown => new BitChartLegacyBrowserEvent("mousedown");

    /// <summary>
    /// A pointing device is moved onto the element that has the listener attached.
    /// </summary>
    public static BitChartLegacyBrowserEvent MouseEnter => new BitChartLegacyBrowserEvent("mouseenter");

    /// <summary>
    /// A pointing device is moved off the element that has the listener attached.
    /// </summary>
    public static BitChartLegacyBrowserEvent MouseLeave => new BitChartLegacyBrowserEvent("mouseleave");

    /// <summary>
    /// A pointing device is moved over an element. (Fired continously as the mouse moves.)
    /// </summary>
    public static BitChartLegacyBrowserEvent MouseMove => new BitChartLegacyBrowserEvent("mousemove");

    /// <summary>
    /// A pointing device is moved onto the element that has the listener attached or onto one of its children.
    /// </summary>
    public static BitChartLegacyBrowserEvent MouseOver => new BitChartLegacyBrowserEvent("mouseover");

    /// <summary>
    /// A pointing device is moved off the element that has the listener attached or off one of its children.
    /// </summary>
    public static BitChartLegacyBrowserEvent MouseOut => new BitChartLegacyBrowserEvent("mouseout");

    /// <summary>
    /// A pointing device button is released over an element.
    /// </summary>
    public static BitChartLegacyBrowserEvent MouseUp => new BitChartLegacyBrowserEvent("mouseup");

    /// <summary>
    /// One or more touch points are placed on the touch surface.
    /// </summary>
    public static BitChartLegacyBrowserEvent TouchStart => new BitChartLegacyBrowserEvent("touchstart");

    /// <summary>
    /// One or more touch points are moved along the touch surface.
    /// </summary>
    public static BitChartLegacyBrowserEvent TouchMove => new BitChartLegacyBrowserEvent("touchmove");

    /// <summary>
    /// One or more touch points are removed from the touch surface.
    /// </summary>
    public static BitChartLegacyBrowserEvent TouchEnd => new BitChartLegacyBrowserEvent("touchend");

    /// <summary>
    /// This method constructs a <see cref="BitChartLegacyBrowserEvent"/> which represents the given value.
    /// Use this method if the event is not available as a static property.
    /// </summary>
    /// <param name="browserEvent">The string representation of your browser event.</param>
    public static BitChartLegacyBrowserEvent Custom(string browserEvent) => new BitChartLegacyBrowserEvent(browserEvent);

    private BitChartLegacyBrowserEvent(string stringRep) : base(stringRep) { }
}
