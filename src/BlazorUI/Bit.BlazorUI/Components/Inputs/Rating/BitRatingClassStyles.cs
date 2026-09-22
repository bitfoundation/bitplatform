namespace Bit.BlazorUI;

public class BitRatingClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the rating.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the label of the rating.
    /// </summary>
    public string? LabelContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label of the rating.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the description of the rating.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the rating items.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button of each rating item, which is the pointer target that holds
    /// the glyphs and carries the <c>data-is-current</c> attribute marking the item the shown value lands in.
    /// </summary>
    public string? Button { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the rating icon container.
    /// </summary>
    public string? IconContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the rating selected icon.
    /// </summary>
    public string? SelectedIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the rating unselected icon.
    /// </summary>
    public string? UnselectedIcon { get; set; }
}
