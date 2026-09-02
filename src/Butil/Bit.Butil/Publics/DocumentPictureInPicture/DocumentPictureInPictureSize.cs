namespace Bit.Butil;

/// <summary>
/// The inner size of a document picture-in-picture window, in CSS pixels.
/// </summary>
/// <remarks>
/// The user can resize the floating window at any time, so this is a reading rather than a setting -
/// re-read it when the layout inside the window depends on it.
/// </remarks>
public class DocumentPictureInPictureSize
{
    /// <summary>The window's inner width.</summary>
    public int Width { get; set; }

    /// <summary>The window's inner height.</summary>
    public int Height { get; set; }
}
