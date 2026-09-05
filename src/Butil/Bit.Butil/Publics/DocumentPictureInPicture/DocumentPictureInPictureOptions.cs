namespace Bit.Butil;

/// <summary>
/// How the floating window should open, mirroring the options of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DocumentPictureInPicture/requestWindow">DocumentPictureInPicture.requestWindow()</see>,
/// plus one convenience of Butil's own.
/// </summary>
public class DocumentPictureInPictureOptions
{
    /// <summary>Requested width in pixels. Left unset, the browser picks a size.</summary>
    public int? Width { get; set; }

    /// <summary>Requested height in pixels.</summary>
    public int? Height { get; set; }

    /// <summary>
    /// True to hide the "back to tab" button. Worth setting for a window whose content only makes
    /// sense on its own, since the button otherwise promises a return the app may not handle.
    /// </summary>
    public bool? DisallowReturnToOpener { get; set; }

    /// <summary>
    /// True to open at the browser's default position and size rather than reusing where the user
    /// last left a picture-in-picture window.
    /// </summary>
    public bool? PreferInitialWindowPlacement { get; set; }

    /// <summary>
    /// Butil's own: copy the page's stylesheets into the new window. The window is a separate
    /// document and inherits no CSS, so content moved into it renders unstyled without this - which
    /// is the first surprise everyone hits with this API. Cross-origin sheets that cannot be read are
    /// re-linked by URL instead.
    /// </summary>
    public bool CopyStyleSheets { get; set; } = true;
}
