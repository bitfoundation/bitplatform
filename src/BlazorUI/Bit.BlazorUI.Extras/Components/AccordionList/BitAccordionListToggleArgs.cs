namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnToggling callback of <see cref="BitAccordionList{TItem}"/>.
/// Set <see cref="Cancel"/> to true to leave the item as it is.
/// </summary>
public class BitAccordionListToggleArgs<TItem> where TItem : class
{
    /// <summary>
    /// Creates a new instance of <see cref="BitAccordionListToggleArgs{TItem}"/>.
    /// </summary>
    /// <param name="item">
    /// The item that is about to expand or collapse.
    /// </param>
    /// <param name="key">
    /// The key of the item that is about to expand or collapse.
    /// </param>
    /// <param name="isExpanding">
    /// Whether the item is about to expand.
    /// </param>
    /// <param name="reason">
    /// What made the item expand or collapse.
    /// </param>
    public BitAccordionListToggleArgs(TItem item, string? key, bool isExpanding, BitAccordionToggleReason reason)
    {
        Item = item;
        Key = key;
        IsExpanding = isExpanding;
        Reason = reason;
    }

    /// <summary>
    /// The item that is about to expand or collapse.
    /// </summary>
    public TItem Item { get; }

    /// <summary>
    /// The key of the item that is about to expand or collapse.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// The state the item is about to move to: true while it is expanding, false while it is collapsing.
    /// </summary>
    public bool IsExpanding { get; }

    /// <summary>
    /// What made the item expand or collapse: a click on its header, or a call to one of the
    /// Expand, Collapse, Toggle, ExpandAll and CollapseAll methods of the AccordionList.
    /// </summary>
    public BitAccordionToggleReason Reason { get; }

    /// <summary>
    /// Set to true to cancel the expansion or the collapse and leave the item as it is.
    /// </summary>
    public bool Cancel { get; set; }
}
