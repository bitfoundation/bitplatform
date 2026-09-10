// Embedded-file (attachment) exposure. A pdf can carry arbitrary files, either
// document-wide through the catalog's /Names /EmbeddedFiles name tree or pinned
// to a page through a /FileAttachment annotation.

namespace Bit.BlazorUI;

/// <summary>A file embedded in the document.</summary>
public sealed class BitPdfAttachment
{
    /// <summary>The file name the document gives the attachment.</summary>
    public required string Name { get; init; }

    /// <summary>The description (<c>/Desc</c>) the document gives it, when present.</summary>
    public string? Description { get; init; }

    /// <summary>The declared MIME type (<c>/Subtype</c> of the embedded stream), when present.</summary>
    public string? MimeType { get; init; }

    /// <summary>The page the attachment is pinned to (1-based), or <c>null</c> for a
    /// document-wide attachment.</summary>
    public int? PageNumber { get; init; }

    /// <summary>The decoded bytes of the attachment. Empty when the embedded stream
    /// could not be decoded.</summary>
    public required byte[] Content { get; init; }

    /// <summary>The size in bytes of <see cref="Content"/>.</summary>
    public long Size => Content.LongLength;
}
