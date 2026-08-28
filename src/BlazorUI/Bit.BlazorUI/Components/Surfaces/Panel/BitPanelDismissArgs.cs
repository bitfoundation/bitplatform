namespace Bit.BlazorUI;

/// <summary>
/// The arguments of the <see cref="BitPanel.OnDismissing"/> callback, which tell what is closing the panel
/// and let the closing be refused.
/// </summary>
public class BitPanelDismissArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitPanelDismissArgs"/>.
    /// </summary>
    /// <param name="reason">What is closing the panel.</param>
    /// <param name="mouse">The click that is closing the panel, where there was one.</param>
    public BitPanelDismissArgs(BitPanelDismissReason reason, MouseEventArgs? mouse = null)
    {
        Reason = reason;
        Mouse = mouse;
    }

    /// <summary>
    /// What is closing the panel: a click on the overlay, the Escape key, a swipe, or the code that
    /// opened it.
    /// </summary>
    public BitPanelDismissReason Reason { get; }

    /// <summary>
    /// The click that is closing the panel, which is only there for a dismissal that came from a pointer.
    /// </summary>
    public MouseEventArgs? Mouse { get; }

    /// <summary>
    /// Set to true to refuse the dismissal and leave the panel open.
    /// </summary>
    public bool Cancel { get; set; }
}
