namespace Bit.Butil;

/// <summary>
/// The options bag passed to a clipboard read: the MIME types whose payload should be handed over
/// exactly as it was written, instead of being sanitized by the browser first.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/read">Clipboard.read()</see>
/// </summary>
public class ClipboardFormats
{
    /// <summary>
    /// MIME types to read unsanitized - in practice only <c>"text/html"</c>, the one format the
    /// browser otherwise rewrites. Empty means every item is sanitized.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/read#unsanitized">Clipboard.read() unsanitized</see>
    /// </summary>
    public string[] Unsanitized { get; set; } = [];
}
