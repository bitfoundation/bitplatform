namespace Bit.BlazorUI;

public class BitImageViewerClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitImageViewer.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the viewport of the BitImageViewer, the frame the image is zoomed and panned in.
    /// </summary>
    public string? Viewport { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image of the BitImageViewer.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the toolbar of the BitImageViewer.
    /// </summary>
    public string? Toolbar { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each button of the toolbar of the BitImageViewer.
    /// </summary>
    public string? Button { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the zoom level shown in the toolbar of the BitImageViewer.
    /// </summary>
    public string? ZoomText { get; set; }
}
