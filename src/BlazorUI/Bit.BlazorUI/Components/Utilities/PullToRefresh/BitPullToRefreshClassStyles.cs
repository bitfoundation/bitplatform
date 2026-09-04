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
    /// Custom CSS classes/styles for the spinner wrapper element when the pull passed the trigger and releasing starts the refresh.
    /// </summary>
    public string? SpinnerWrapperCanRelease { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner wrapper element in refreshing mode.
    /// </summary>
    public string? SpinnerWrapperRefreshing { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner wrapper element while the complete state is visible after a successful refresh.
    /// </summary>
    public string? SpinnerWrapperComplete { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner element.
    /// </summary>
    public string? Spinner { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner element when the pull passed the trigger and releasing starts the refresh.
    /// </summary>
    public string? SpinnerCanRelease { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner element in refreshing mode.
    /// </summary>
    public string? SpinnerRefreshing { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner element while the complete state is visible after a successful refresh.
    /// </summary>
    public string? SpinnerComplete { get; set; }
}
