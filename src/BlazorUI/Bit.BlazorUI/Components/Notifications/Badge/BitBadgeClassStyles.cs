namespace Bit.BlazorUI;

public class BitBadgeClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitBadge.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the badge wrapper of the BitBadge.
    /// </summary>
    public string? BadgeWrapper { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the badge of the BitBadge.
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the BitBadge.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the content of the BitBadge.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden description of the BitBadge.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region of the BitBadge, rendered while
    /// <see cref="BitBadge.Live"/> is on and the badge is not a button.
    /// </summary>
    public string? LiveRegion { get; set; }
}
