namespace Bit.BlazorUI;

public class BitTagClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitTag.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the content element of the BitTag, which is the anchor or the button
    /// the tag becomes while it is a link or a control, and a plain span otherwise.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label of the BitTag, which is the element holding its text and secondary text.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the text of the BitTag.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the secondary text of the BitTag.
    /// </summary>
    public string? SecondaryText { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the BitTag.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image of the BitTag.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the trailing icon of the BitTag, which is rendered after the label.
    /// </summary>
    public string? SecondaryIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitTag while it is selected.
    /// </summary>
    public string? Selected { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the checkmark icon a selected BitTag shows.
    /// </summary>
    public string? SelectedIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the dismiss button of the BitTag.
    /// </summary>
    public string? DismissButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the dismiss icon of the BitTag.
    /// </summary>
    public string? DismissIcon { get; set; }
}
