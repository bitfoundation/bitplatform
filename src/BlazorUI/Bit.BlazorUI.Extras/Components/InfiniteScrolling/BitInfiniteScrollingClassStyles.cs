namespace Bit.BlazorUI;

/// <summary>
/// Custom CSS classes/styles for different parts of the <see cref="BitInfiniteScrolling{TItem}"/>.
/// </summary>
public class BitInfiniteScrollingClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitInfiniteScrolling.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element that wraps each item of the BitInfiniteScrolling: the focusable
    /// article of the Feed mode, or the element that carries the key of a keyed item (which is laid out with
    /// display:contents, so only what it passes down to the item takes effect there).
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the sentinel (last) element of the BitInfiniteScrolling that triggers the loading.
    /// </summary>
    public string? LastElement { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the loading container of the BitInfiniteScrolling.
    /// </summary>
    public string? Loading { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner of the default loading container of the BitInfiniteScrolling.
    /// </summary>
    public string? Spinner { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the empty container of the BitInfiniteScrolling.
    /// </summary>
    public string? Empty { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the end container of the BitInfiniteScrolling that renders after the last page.
    /// </summary>
    public string? End { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the error container of the BitInfiniteScrolling.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button of the BitInfiniteScrolling that loads the next page in manual mode and retries a failed load.
    /// </summary>
    public string? Button { get; set; }
}
