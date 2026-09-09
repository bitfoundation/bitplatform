namespace Bit.BlazorUI;

/// <summary>
/// Why the <see cref="BitMarkdownEditor"/> refused to upload a pasted or dropped image.
/// </summary>
public enum BitMarkdownEditorImageRejectionReason
{
    /// <summary>
    /// The file's type is not one of the accepted image types.
    /// </summary>
    Type,

    /// <summary>
    /// The file is larger than the allowed maximum size.
    /// </summary>
    Size
}
