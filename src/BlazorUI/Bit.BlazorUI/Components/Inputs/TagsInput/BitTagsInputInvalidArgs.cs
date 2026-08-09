namespace Bit.BlazorUI;

/// <summary>
/// Arguments of the OnInvalid callback of <see cref="BitTagsInput"/>, describing the tag that was
/// rejected along with the rule that rejected it.
/// </summary>
public class BitTagsInputInvalidArgs
{
    /// <summary>
    /// The tag text that was rejected, after the trimming and the transformation were applied to it.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// The rule that rejected the tag.
    /// </summary>
    public BitTagsInputInvalidReason Reason { get; set; }
}
