namespace Bit.BlazorUI;

/// <summary>
/// How <see cref="BitMap{TMapProvider}"/> renders the text alternative to its markers.
/// <para>
/// A map is a picture: its markers are drawn, not written, and a screen-reader user gets nothing
/// from panning it. A list of the same markers - name and coordinates, each row able to bring its
/// marker into view - is the standard non-visual equivalent, and the single highest-value
/// accessibility feature a map component can offer.
/// </para>
/// </summary>
public enum BitMapMarkerListMode
{
    /// <summary>No list is rendered.</summary>
    None,

    /// <summary>
    /// The list is in the accessibility tree but not visible on screen. The map looks unchanged
    /// while screen-reader and keyboard users get a reachable equivalent.
    /// </summary>
    ScreenReaderOnly,

    /// <summary>The list is rendered below the map for everyone.</summary>
    Visible,
}
