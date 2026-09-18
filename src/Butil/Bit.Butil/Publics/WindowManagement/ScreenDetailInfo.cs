namespace Bit.Butil;

/// <summary>
/// One screen of a multi-monitor setup, with the geometry needed to place a window on it.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenDetailed">ScreenDetailed</see>
/// </summary>
/// <remarks>
/// <see cref="Left"/> and <see cref="Top"/> are the screen's position in the multi-screen
/// coordinate space, which is what makes them comparable across monitors - the plain
/// <see cref="Screen"/> service has no such notion, because it only ever describes one screen.
/// </remarks>
public class ScreenDetailInfo
{
    /// <summary>
    /// The name the operating system gives the screen. Empty on a browser that withholds it, and
    /// only populated at all once window-management permission has been granted.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>The x coordinate of the screen's left edge, in the multi-screen coordinate space.</summary>
    public int Left { get; set; }

    /// <summary>The y coordinate of the screen's top edge, in the multi-screen coordinate space.</summary>
    public int Top { get; set; }

    /// <summary>The screen's full width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>The screen's full height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>The left edge of the area not taken by OS bars - where a window may actually go.</summary>
    public int AvailLeft { get; set; }

    /// <summary>The top edge of the area not taken by OS bars.</summary>
    public int AvailTop { get; set; }

    /// <summary>The usable width, with OS bars excluded.</summary>
    public int AvailWidth { get; set; }

    /// <summary>The usable height, with OS bars excluded.</summary>
    public int AvailHeight { get; set; }

    /// <summary>The screen's color depth in bits.</summary>
    public int ColorDepth { get; set; }

    /// <summary>The screen's pixel depth in bits.</summary>
    public int PixelDepth { get; set; }

    /// <summary>
    /// The screen's device pixel ratio. It differs per screen, which is exactly what makes a
    /// window dragged between a laptop panel and an external monitor re-render.
    /// </summary>
    public double DevicePixelRatio { get; set; }

    /// <summary>True for the operating system's primary screen.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>True for a screen built into the device, as opposed to an attached monitor.</summary>
    public bool IsInternal { get; set; }

    /// <summary>The screen's orientation type, e.g. <c>"landscape-primary"</c>.</summary>
    public string? OrientationType { get; set; }

    /// <summary>The screen's orientation angle in degrees.</summary>
    public int OrientationAngle { get; set; }

    /// <summary>True for the screen this window is currently on.</summary>
    public bool IsCurrent { get; set; }
}
