namespace Bit.BlazorUI;

public class BitCalloutClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitCallout.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the anchor container element of the BitCallout.
    /// </summary>
    public string? AnchorContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the arrow (beak) element of the BitCallout.
    /// </summary>
    public string? Arrow { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the opened callout state of the BitCallout.
    /// </summary>
    public string? Opened { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the content of the BitCallout.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header element of the BitCallout, which is rendered when the
    /// Header parameter is set.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the scrolling body element of the BitCallout, which is rendered when
    /// the Header or the Footer parameter is set.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footer element of the BitCallout, which is rendered when the
    /// Footer parameter is set.
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the overlay of the BitCallout.
    /// </summary>
    public string? Overlay { get; set; }
}
