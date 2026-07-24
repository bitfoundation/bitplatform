namespace Bit.BlazorUI;

/// <summary>
/// Describes an image pasted into or dropped onto the <see cref="BitMarkdownEditor"/>,
/// passed to the <see cref="BitMarkdownEditor.OnImageUpload"/> handler.
/// </summary>
/// <param name="FileName">The original file name (may be a generic name for clipboard images).</param>
/// <param name="ContentType">The MIME type of the image, for example <c>image/png</c>.</param>
/// <param name="Data">The raw image bytes.</param>
public readonly record struct BitMarkdownEditorImageUploadInfo(string FileName, string ContentType, byte[] Data);
