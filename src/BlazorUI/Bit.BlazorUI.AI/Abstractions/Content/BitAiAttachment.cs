using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Broad attachment categories that drive the default preview affordance.
/// </summary>
public enum BitAiAttachmentKind
{
    /// <summary>Anything without a more specific category.</summary>
    File = 0,

    /// <summary>A raster or vector image, previewed as a thumbnail.</summary>
    Image = 1,

    /// <summary>An audio clip, shown with a microphone glyph.</summary>
    Audio = 2,

    /// <summary>A video clip, shown with a video glyph.</summary>
    Video = 3,

    /// <summary>A PDF or other paginated document.</summary>
    Document = 4,
}

/// <summary>
/// Upload lifecycle of an attachment, so the composer can show progress and failures.
/// </summary>
public enum BitAiAttachmentStatus
{
    /// <summary>Selected locally and not yet uploaded.</summary>
    Pending = 0,

    /// <summary>Upload in progress; see <see cref="BitAiAttachment.Progress"/>.</summary>
    Uploading = 1,

    /// <summary>Available to the model.</summary>
    Ready = 2,

    /// <summary>Upload failed.</summary>
    Failed = 3,
}

/// <summary>
/// A file carried by a message or staged in the composer.
/// </summary>
/// <remarks>
/// <para>
/// Content is deliberately not a <see cref="byte"/> array on this type. Blazor Server should not
/// hold multi-megabyte uploads in circuit memory, so an attachment references its bytes through
/// <see cref="Url"/>, <see cref="Data"/>, or <see cref="ContentFactory"/> and the framework only
/// materialises them when something actually needs them.
/// </para>
/// </remarks>
public sealed record BitAiAttachment
{
    /// <summary>Creates an attachment.</summary>
    /// <param name="attachmentId">Identifier unique within its message or composer session.</param>
    /// <param name="name">File name shown to the user.</param>
    public BitAiAttachment(string attachmentId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attachmentId);
        ArgumentNullException.ThrowIfNull(name);
        AttachmentId = attachmentId;
        Name = name;
    }

    /// <summary>
    /// Creates an attachment with nothing but the initial values, for the conversation snapshots' JSON contract
    /// (<see cref="BitAiSnapshotJson"/>), which sets what the JSON holds afterwards and checks what the other
    /// constructor checks: creating it first is what keeps <see cref="Status"/> at <see cref="BitAiAttachmentStatus.Ready"/>
    /// when the JSON has none.
    /// </summary>
    private BitAiAttachment()
    {
        AttachmentId = null!;
        Name = null!;
    }

    /// <summary>Identifier unique within its message or composer session.</summary>
    /// <remarks>The private init accessor is for the snapshots' JSON contract only (<see cref="BitAiSnapshotJson"/>).</remarks>
    public string AttachmentId { get; private init; }

    /// <summary>File name shown to the user.</summary>
    public string Name { get; init; }

    /// <summary>MIME type, for example <c>image/png</c>.</summary>
    public string? ContentType { get; init; }

    /// <summary>Size in bytes, when known.</summary>
    public long? Size { get; init; }

    /// <summary>Category driving the default preview.</summary>
    public BitAiAttachmentKind Kind { get; init; } = BitAiAttachmentKind.File;

    /// <summary>Upload lifecycle state.</summary>
    public BitAiAttachmentStatus Status { get; init; } = BitAiAttachmentStatus.Ready;

    /// <summary>Upload progress in the range 0..1 while <see cref="Status"/> is uploading.</summary>
    public double? Progress { get; init; }

    /// <summary>Why the upload failed, when it did.</summary>
    public string? Error { get; init; }

    /// <summary>An addressable URL for the content, which may be a data URL for small inline items.</summary>
    public string? Url { get; init; }

    /// <summary>A URL for a small preview image, distinct from the full-size content.</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Inline bytes, for small attachments that are cheap to keep in memory.</summary>
    public ReadOnlyMemory<byte>? Data { get; init; }

    /// <summary>
    /// Opens the content on demand. Preferred over <see cref="Data"/> for anything large.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Func<CancellationToken, ValueTask<Stream>>? ContentFactory { get; init; }

    /// <summary>Application-defined values carried through to custom attachment renderers.</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    /// <summary><see langword="true"/> when the content can be read without a network round trip.</summary>
    [MemberNotNullWhen(true, nameof(Data))]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasInlineData => Data is not null;

    /// <summary>
    /// Opens the attachment content from whichever source is available.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A readable stream over the content.</returns>
    /// <exception cref="InvalidOperationException">
    /// The attachment carries neither inline data nor a content factory.
    /// </exception>
    public async ValueTask<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
    {
        if (ContentFactory is not null) return await ContentFactory(cancellationToken).ConfigureAwait(false);
        if (Data is { } data) return new MemoryStream(data.ToArray(), writable: false);
        throw new InvalidOperationException(
            $"Attachment '{Name}' has no inline data and no content factory. Set {nameof(Data)} or {nameof(ContentFactory)}.");
    }

    /// <summary>
    /// Infers an <see cref="BitAiAttachmentKind"/> from a MIME type and file name.
    /// </summary>
    /// <param name="contentType">The MIME type, if known.</param>
    /// <param name="fileName">The file name, used when the MIME type is missing or generic.</param>
    /// <returns>The inferred kind.</returns>
    public static BitAiAttachmentKind InferKind(string? contentType, string? fileName = null)
    {
        if (!string.IsNullOrEmpty(contentType))
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return BitAiAttachmentKind.Image;
            if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return BitAiAttachmentKind.Audio;
            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return BitAiAttachmentKind.Video;
            if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)) return BitAiAttachmentKind.Document;
        }

        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg" or ".bmp" or ".avif" => BitAiAttachmentKind.Image,
            ".mp3" or ".wav" or ".ogg" or ".m4a" or ".flac" => BitAiAttachmentKind.Audio,
            ".mp4" or ".webm" or ".mov" or ".mkv" => BitAiAttachmentKind.Video,
            ".pdf" or ".docx" or ".doc" or ".pptx" or ".odt" => BitAiAttachmentKind.Document,
            _ => BitAiAttachmentKind.File,
        };
    }
}
