namespace Bit.BlazorUI;

public class BitPullToRefreshClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the PullToRefresh.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the loading element.
    /// </summary>
    public string? Loading { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner wrapper element.
    /// </summary>
    public string? SpinnerWrapper { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner wrapper element in refreshing mode.
    /// </summary>
    public string? SpinnerWrapperRefreshing { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner element.
    /// </summary>
    public string? Spinner { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner element in refreshing mode.
    /// </summary>
    public string? SpinnerRefreshing { get; set; }
}
