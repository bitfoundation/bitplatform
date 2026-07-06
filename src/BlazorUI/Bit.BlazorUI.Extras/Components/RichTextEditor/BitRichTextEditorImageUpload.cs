namespace Bit.BlazorUI;

/// <summary>An image to be persisted by the host's <c>OnImageUpload</c> delegate.</summary>
public sealed class BitRichTextEditorImageUpload
{
    /// <param name="fileName">Original file name, when available.</param>
    /// <param name="contentType">MIME type, e.g. "image/png".</param>
    /// <param name="content">Raw image bytes.</param>
    public BitRichTextEditorImageUpload(string fileName, string contentType, byte[] content)
    {
        // Enforce the non-nullable contract at runtime: even when a caller bypasses nullable
        // warnings, a null fileName/contentType/content must not reach OnImageUpload as an
        // invalid payload. Reject null content the same way as the other required fields rather
        // than silently converting it to an empty array.
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(content);
        FileName = fileName;
        ContentType = contentType;
        // Defensively copy so the caller's array can't mutate the stored payload after creation.
        _content = (byte[])content.Clone();
    }

    private readonly byte[] _content;

    /// <summary>Original file name, when available.</summary>
    public string FileName { get; }

    /// <summary>MIME type, e.g. "image/png".</summary>
    public string ContentType { get; }

    /// <summary>Raw image bytes. Returns a fresh copy so the stored payload stays immutable.</summary>
    public byte[] Content => (byte[])_content.Clone();
}
