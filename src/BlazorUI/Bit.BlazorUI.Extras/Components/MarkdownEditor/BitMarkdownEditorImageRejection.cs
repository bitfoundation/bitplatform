namespace Bit.BlazorUI;

/// <summary>
/// Describes an image the <see cref="BitMarkdownEditor"/> refused to upload, passed to the
/// <see cref="BitMarkdownEditor.OnImageRejected"/> callback so the app can tell the user why
/// nothing was inserted.
/// </summary>
/// <param name="FileName">The original file name (may be a generic name for clipboard images).</param>
/// <param name="ContentType">The MIME type of the image, for example <c>image/png</c>.</param>
/// <param name="Size">The size of the file in bytes.</param>
/// <param name="Reason">Why the file was refused.</param>
public readonly record struct BitMarkdownEditorImageRejection(string FileName, string ContentType, long Size, BitMarkdownEditorImageRejectionReason Reason);
