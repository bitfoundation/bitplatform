namespace Bit.BlazorUI;

public class BitSortableClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitSortable.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every item wrapper of the BitSortable.
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the item being dragged, and for the drop preview shown in another list.
    /// </summary>
    public string? Ghost { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the item picked up.
    /// </summary>
    public string? Chosen { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the item highlighted as the swap target while dragging in swap mode.
    /// </summary>
    public string? SwapTarget { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the items of a MultiDrag selection.
    /// </summary>
    public string? Selected { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the items the Filter locks.
    /// </summary>
    public string? Filtered { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the preview of the item that follows a finger, a pen, or the mouse with
    /// ForceFallback, which is placed above the page.
    /// </summary>
    public string? Drag { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for what an empty list shows.
    /// </summary>
    public string? Empty { get; set; }
}
