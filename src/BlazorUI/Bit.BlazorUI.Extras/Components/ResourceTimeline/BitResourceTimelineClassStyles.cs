namespace Bit.BlazorUI;

public class BitResourceTimelineClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitResourceTimeline.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the focusable scroll viewport of the BitResourceTimeline, which the active renderer
    /// draws its surface into.
    /// </summary>
    public string? Viewport { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the top-start corner (where the time axis and the resource axis meet) that holds the
    /// TopStartContent of the BitResourceTimeline.
    /// </summary>
    public string? Corner { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the overlay of the resource column that renders the ResourceTemplate of the
    /// BitResourceTimeline.
    /// </summary>
    public string? ResourceOverlay { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each row of the resource overlay of the BitResourceTimeline.
    /// </summary>
    public string? ResourceRow { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the content (the rendered ResourceTemplate) of each row of the resource overlay of the
    /// BitResourceTimeline.
    /// </summary>
    public string? ResourceContent { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the expand/collapse button of each group row of the resource overlay of the
    /// BitResourceTimeline.
    /// </summary>
    public string? GroupToggle { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the positioned overlay that renders the TooltipTemplate of the BitResourceTimeline.
    /// </summary>
    public string? TooltipOverlay { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the overlay the BitResourceTimeline shows while it loads its data.
    /// </summary>
    public string? Loading { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the default spinner of the loading overlay of the BitResourceTimeline.
    /// </summary>
    public string? Spinner { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the default text of the loading overlay of the BitResourceTimeline.
    /// </summary>
    public string? LoadingText { get; set; }
}
