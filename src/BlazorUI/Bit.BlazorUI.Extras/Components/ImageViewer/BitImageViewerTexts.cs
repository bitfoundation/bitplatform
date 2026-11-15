namespace Bit.BlazorUI;

/// <summary>
/// The words a <see cref="BitImageViewer"/> says: the names of its buttons and the instructions of its keyboard, for
/// a page in another language to translate.
/// </summary>
public class BitImageViewerTexts
{
    /// <summary>
    /// What the viewer is announced as, after its name. Default: "image viewer".
    /// </summary>
    public string RoleDescription { get; set; } = "image viewer";

    /// <summary>
    /// How the viewer is operated from the keyboard, which a screen reader reads once the viewer has the focus.
    /// </summary>
    public string Instructions { get; set; } = "Plus and minus zoom, arrow keys move a zoomed image, 0 fits it again.";

    /// <summary>
    /// The name of the group of the zoom buttons. Default: "Zoom".
    /// </summary>
    public string ToolbarAriaLabel { get; set; } = "Zoom";

    /// <summary>
    /// The name of the zoom in button. Default: "Zoom in".
    /// </summary>
    public string ZoomIn { get; set; } = "Zoom in";

    /// <summary>
    /// The name of the zoom out button. Default: "Zoom out".
    /// </summary>
    public string ZoomOut { get; set; } = "Zoom out";

    /// <summary>
    /// The name of the button that fits the image in the viewer again. Default: "Fit to view".
    /// </summary>
    public string Reset { get; set; } = "Fit to view";

    /// <summary>
    /// The name of the button that shows the viewer full screen. Default: "Full screen".
    /// </summary>
    public string EnterFullScreen { get; set; } = "Full screen";

    /// <summary>
    /// The name of the buttons that show a full screen viewer back in the page. Default: "Exit full screen".
    /// </summary>
    public string ExitFullScreen { get; set; } = "Exit full screen";
}
