namespace Bit.Butil;

/// <summary>
/// One entry in a file picker's filter dropdown, e.g. "Text files (*.txt, *.md)".
/// </summary>
/// <remarks>
/// A type with no <see cref="Extensions"/> is dropped rather than sent: the underlying API rejects
/// an empty filter outright instead of ignoring it.
/// </remarks>
public class FilePickerType
{
    /// <summary>What the dropdown shows for this group, e.g. <c>"Text files"</c>.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The MIME type this group accepts, e.g. <c>"text/plain"</c>. Defaults to any type.</summary>
    public string MimeType { get; set; } = "*/*";

    /// <summary>The extensions, each with a leading dot, e.g. <c>[".txt", ".md"]</c>.</summary>
    public string[] Extensions { get; set; } = [];
}
