namespace Bit.Butil;

/// <summary>
/// One item on the system clipboard: a single MIME type and the bytes recorded under it. The
/// clipboard holds a list of these, so a copy can offer the same content as plain text, as HTML
/// and as an image at once.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ClipboardItem">ClipboardItem</see>
/// </summary>
public class ClipboardItem
{
    /// <summary>
    /// The MIME type this item's <see cref="Data"/> is in - <c>"text/plain"</c>, <c>"text/html"</c>
    /// or <c>"image/png"</c>. Browsers refuse to write types outside a short allow-list.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ClipboardItem/types">ClipboardItem.types</see>
    /// </summary>
    public string MimeType { get; set; } = default!;

    /// <summary>
    /// The item's payload. Text formats are the UTF-8 bytes of the string; image formats are the
    /// encoded file.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ClipboardItem/getType">ClipboardItem.getType()</see>
    /// </summary>
    public byte[] Data { get; set; } = default!;
}
