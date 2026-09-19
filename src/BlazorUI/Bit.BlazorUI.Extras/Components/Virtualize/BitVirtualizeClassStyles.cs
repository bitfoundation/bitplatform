namespace Bit.BlazorUI;

public class BitVirtualizeClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root (scroll container) element of the BitVirtualize.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header container of the BitVirtualize.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the wrapper element of each rendered item (and placeholder) of the BitVirtualize.
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the pinned sticky item container of the BitVirtualize.
    /// </summary>
    public string? Sticky { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footer container of the BitVirtualize.
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the loading container of the BitVirtualize.
    /// </summary>
    public string? Loading { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the empty container of the BitVirtualize.
    /// </summary>
    public string? Empty { get; set; }
}
