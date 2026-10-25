namespace Bit.BlazorUI;

/// <summary>
/// Defines per-part CSS class/style values for <see cref="BitMap{TMapProvider}"/>.
/// </summary>
public class BitMapClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitMap.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the focusable canvas the provider draws the map into.
    /// </summary>
    public string? Canvas { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the layer holding the ChildContent above the map.
    /// </summary>
    public string? Overlay { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the keyboard instructions shown while the canvas has keyboard focus.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the hint shown while a gesture is blocked by the cooperative gestures.
    /// </summary>
    public string? GestureHint { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the cover shown while the map is loading, has failed, or is unsupported.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner of the built-in loading indicator.
    /// </summary>
    public string? Spinner { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the popup the MarkerPopupTemplate is rendered in.
    /// </summary>
    public string? Popup { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the close button of the popup.
    /// </summary>
    public string? PopupCloseButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body of the popup, holding the MarkerPopupTemplate content.
    /// </summary>
    public string? PopupBody { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the marker list rendered next to the map.
    /// </summary>
    public string? MarkerList { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each "Show on map" button of the built-in marker table.
    /// </summary>
    public string? MarkerListButton { get; set; }
}
