namespace Bit.BlazorUI;

/// <summary>An image to be persisted by the host's <c>OnImageUpload</c> delegate.</summary>
/// <param name="FileName">Original file name, when available.</param>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Content">Raw image bytes.</param>
public sealed record BitRichTextEditorImageUpload(string FileName, string ContentType, byte[] Content);
