namespace Bit.BlazorUI;

/// <summary>An image to be persisted by the host's <c>OnImageUpload</c> delegate.</summary>
public sealed class BitRichTextEditorImageUpload
{
    /// <param name="fileName">Original file name, when available.</param>
    /// <param name="contentType">MIME type, e.g. "image/png".</param>
    /// <param name="content">Raw image bytes.</param>
    public BitRichTextEditorImageUpload(string fileName, string contentType, byte[] content)
    {
        FileName = fileName;
        ContentType = contentType;
        // Defensively copy so the caller's array can't mutate the stored payload after creation.
        _content = content is null ? [] : (byte[])content.Clone();
    }

    private readonly byte[] _content;

    /// <summary>Original file name, when available.</summary>
    public string FileName { get; init; }

    /// <summary>MIME type, e.g. "image/png".</summary>
    public string ContentType { get; init; }

    /// <summary>Raw image bytes. Returns a fresh copy so the stored payload stays immutable.</summary>
    public byte[] Content
    {
        get => (byte[])_content.Clone();
        init => _content = value is null ? [] : (byte[])value.Clone();
    }
}
