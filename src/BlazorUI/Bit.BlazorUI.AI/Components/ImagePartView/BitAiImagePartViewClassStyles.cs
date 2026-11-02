namespace Bit.BlazorUI;

public class BitAiImagePartViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element (the figure) of the BitAiImagePartView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image of the BitAiImagePartView.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button the BitAiImagePartView shows in place of an image the image policy
    /// loads only on request.
    /// </summary>
    public BitActionButtonClassStyles? ImagePlaceholder { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the alternative text the BitAiImagePartView shows in place of an image the image
    /// policy blocks.
    /// </summary>
    public string? BlockedImage { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the caption of the BitAiImagePartView.
    /// </summary>
    public string? Caption { get; set; }
}
