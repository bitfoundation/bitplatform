namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnToggling callback of BitAccordion.
/// Set <see cref="Cancel"/> to true to leave the accordion as it is.
/// </summary>
public class BitAccordionToggleArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitAccordionToggleArgs"/>.
    /// </summary>
    /// <param name="isExpanding">
    /// Whether the accordion is about to expand.
    /// </param>
    /// <param name="reason">
    /// What made the accordion expand or collapse.
    /// </param>
    public BitAccordionToggleArgs(bool isExpanding, BitAccordionToggleReason reason)
    {
        IsExpanding = isExpanding;
        Reason = reason;
    }

    /// <summary>
    /// The state the accordion is about to move to: true while it is expanding, false while it is collapsing.
    /// </summary>
    public bool IsExpanding { get; }

    /// <summary>
    /// What made the accordion expand or collapse: a click on its header, or a call to one of its
    /// Expand, Collapse and Toggle methods.
    /// </summary>
    public BitAccordionToggleReason Reason { get; }

    /// <summary>
    /// Set to true to cancel the expansion or the collapse and leave the accordion as it is.
    /// </summary>
    public bool Cancel { get; set; }
}
