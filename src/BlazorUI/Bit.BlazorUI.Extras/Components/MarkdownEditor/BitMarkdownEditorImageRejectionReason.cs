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
    Size,

    /// <summary>
    /// The editor's <see cref="BitMarkdownEditor.MaxLength"/> leaves no room for the image's
    /// markdown, so nothing was inserted (or the progress placeholder was taken back out).
    /// </summary>
    Length
}
