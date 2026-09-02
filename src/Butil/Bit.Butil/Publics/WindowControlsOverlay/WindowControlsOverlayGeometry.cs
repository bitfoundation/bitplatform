namespace Bit.Butil;

/// <summary>
/// Where the draggable title-bar area is, and whether the overlay is showing at all - the payload of
/// <see cref="WindowControlsOverlay.GetTitlebarAreaRect"/> and of the <c>geometrychange</c> event.
/// </summary>
/// <remarks>
/// The rectangle is the region <b>your</b> content may use: it starts after the window controls on
/// the leading side and stops before them on the trailing side, which is why <see cref="X"/> is not
/// always zero. It is expressed in CSS pixels relative to the viewport.
/// </remarks>
public class WindowControlsOverlayGeometry
{
    /// <summary>True while the app is drawing its own title bar. False in a browser tab, or when the user turned the overlay off.</summary>
    public bool Visible { get; set; }

    /// <summary>Left edge of the available title-bar area, in CSS pixels.</summary>
    public double X { get; set; }

    /// <summary>Top edge of the available title-bar area, in CSS pixels.</summary>
    public double Y { get; set; }

    /// <summary>Width of the available title-bar area, in CSS pixels.</summary>
    public double Width { get; set; }

    /// <summary>Height of the available title-bar area, in CSS pixels.</summary>
    public double Height { get; set; }
}
