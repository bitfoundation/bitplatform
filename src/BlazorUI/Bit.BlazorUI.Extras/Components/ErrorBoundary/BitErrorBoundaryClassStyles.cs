namespace Bit.BlazorUI;

/// <summary>
/// Custom CSS classes/styles for the parts of the <see cref="BitErrorBoundary"/>'s default error UI.
/// </summary>
/// <remarks>
/// Nothing here reaches the boundary's own content: while no error has been caught the boundary renders
/// its children and nothing of its own, so there is no element of the boundary's to style.
/// </remarks>
public class BitErrorBoundaryClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitErrorBoundary's error UI.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the BitErrorBoundary.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title of the BitErrorBoundary.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the message of the BitErrorBoundary.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the exception details block of the BitErrorBoundary.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footer of the BitErrorBoundary, which holds a replaced
    /// <see cref="BitErrorBoundary.Footer"/> exactly as it holds the default buttons.
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the Refresh button of the BitErrorBoundary.
    /// </summary>
    public BitButtonClassStyles? RefreshButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the Home button of the BitErrorBoundary.
    /// </summary>
    public BitButtonClassStyles? HomeButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the Recover button of the BitErrorBoundary.
    /// </summary>
    public BitButtonClassStyles? RecoverButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the Copy button of the BitErrorBoundary.
    /// </summary>
    public BitButtonClassStyles? CopyButton { get; set; }
}
