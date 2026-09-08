namespace Bit.Butil;

/// <summary>
/// Where the on-screen keyboard is - the payload of
/// <see cref="VirtualKeyboard.GetBoundingRect"/> and of the <c>geometrychange</c> event.
/// </summary>
/// <remarks>
/// All zeros means the keyboard is not showing. The rectangle is in CSS pixels, relative to the
/// viewport, and is only ever non-zero once
/// <see cref="VirtualKeyboard.SetOverlaysContent(bool)"/> has been turned on - otherwise the browser
/// resizes the viewport instead of overlaying it, and there is nothing to report.
/// </remarks>
public class VirtualKeyboardGeometry
{
    /// <summary>Left edge of the keyboard, in CSS pixels.</summary>
    public double X { get; set; }

    /// <summary>Top edge of the keyboard, in CSS pixels - the line your content must stay above.</summary>
    public double Y { get; set; }

    /// <summary>Width of the keyboard, in CSS pixels.</summary>
    public double Width { get; set; }

    /// <summary>Height of the keyboard, in CSS pixels.</summary>
    public double Height { get; set; }

    /// <summary>True when the keyboard is showing, i.e. the rectangle has a size.</summary>
    public bool IsVisible => Width > 0 && Height > 0;
}
