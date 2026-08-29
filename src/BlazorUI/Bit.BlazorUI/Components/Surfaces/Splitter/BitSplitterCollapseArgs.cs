namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnCollapsing callback of BitSplitter.
/// Set <see cref="Cancel"/> to true to leave the panel as it is.
/// </summary>
public class BitSplitterCollapseArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitSplitterCollapseArgs"/>.
    /// </summary>
    /// <param name="isCollapsing">
    /// Whether the first panel is about to be folded away.
    /// </param>
    /// <param name="reason">
    /// What made the first panel collapse or expand.
    /// </param>
    public BitSplitterCollapseArgs(bool isCollapsing, BitSplitterCollapseReason reason)
    {
        IsCollapsing = isCollapsing;
        Reason = reason;
    }

    /// <summary>
    /// The state the first panel is about to move to: true while it is being folded away, false while it
    /// is being brought back.
    /// </summary>
    public bool IsCollapsing { get; }

    /// <summary>
    /// What made the first panel collapse or expand: the gutter, a drag that snapped it shut, a call to one
    /// of the Collapse, Expand and ToggleCollapse methods, or the remembered position being restored.
    /// </summary>
    public BitSplitterCollapseReason Reason { get; }

    /// <summary>
    /// Set to true to cancel the collapse or the expansion and leave the panel as it is.
    /// </summary>
    public bool Cancel { get; set; }
}
