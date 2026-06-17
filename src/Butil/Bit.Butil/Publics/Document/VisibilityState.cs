namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/visibilityState">Document.visibilityState</see>.
/// </summary>
public enum VisibilityState
{
    /// <summary>
    /// The page content may be at least partially visible. In practice this means
    /// the tab is the foreground tab of a non-minimized window.
    /// </summary>
    Visible,

    /// <summary>
    /// The page content is not visible to the user - the tab is in the background or
    /// the window is minimized, or the OS screen lock is active.
    /// </summary>
    Hidden
}
