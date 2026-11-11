namespace Bit.BlazorUI;

public class BitAiAttachmentChipClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiAttachmentChip.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitTag of the BitAiAttachmentChip that carries the name, the details and the
    /// remove button.
    /// </summary>
    public BitTagClassStyles? Tag { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the thumbnail of the BitAiAttachmentChip: the picture of an image, or the box holding
    /// the glyph of any other kind of file.
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the glyph of the BitAiAttachmentChip shown in place of a picture.
    /// </summary>
    public string? Icon { get; set; }
}
