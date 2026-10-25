namespace Bit.BlazorUI;

/// <summary>
/// Arguments of the OnEdit callback of <see cref="BitTagsInput"/>, describing an inline edit of a tag
/// that is about to be committed. Set <see cref="Cancel"/> to true to leave the tag as it was.
/// </summary>
public class BitTagsInputEditArgs
{
    /// <summary>
    /// The tag as it stands in the list, before the edit.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// The text the tag is about to become, after the trimming and the transformation were applied to it.
    /// <br />
    /// Writing to it corrects the tag on its way in. What is written goes through the same trimming,
    /// transformation and validation rules as the typed text did, so a rewrite that breaks one of them is
    /// refused and reported through OnInvalid; leaving it empty keeps the text as it was typed.
    /// </summary>
    public string NewTag { get; set; } = string.Empty;

    /// <summary>
    /// Set to true to cancel the edit, leaving the tag as it was.
    /// </summary>
    public bool Cancel { get; set; }
}
