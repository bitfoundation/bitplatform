namespace Bit.BlazorUI;

/// <summary>
/// A file handed to the <see cref="BitAiPromptInput"/> by the browser, from a paste or a drop.
/// </summary>
/// <param name="Name">The file name.</param>
/// <param name="ContentType">The MIME type.</param>
/// <param name="Size">The size in bytes.</param>
/// <param name="Data">
/// The content, base64 encoded, when it was sent inline. The browser helpers send
/// <see cref="Content"/> instead.
/// </param>
/// <remarks>
/// The browser describes the file first and streams the bytes only when they are read. A single
/// interop message carrying the whole file would exceed the SignalR message limit on Blazor Server
/// (32 KB by default) for anything but a small screenshot, and the file would be dropped.
/// </remarks>
public sealed record BitAiComposerFile(string Name, string ContentType, long Size, string? Data = null)
{
    /// <summary>
    /// The content as a browser stream, or <see langword="null"/> when it was sent inline in
    /// <see cref="Data"/> or withheld because the file was over the size limit.
    /// </summary>
    /// <remarks>
    /// Read it with <see cref="ToAttachmentAsync"/>, which releases it once the content has been
    /// buffered, or keeps it open behind <see cref="BitAiAttachment.ContentFactory"/> for a file too large
    /// to buffer.
    /// </remarks>
    public IJSStreamReference? Content { get; init; }

    /// <summary>
    /// A short browser-side URL for an image preview, created with <c>URL.createObjectURL</c>, or
    /// <see langword="null"/> for files that are not images.
    /// </summary>
    /// <remarks>
    /// Becomes <see cref="BitAiAttachment.ThumbnailUrl"/>. The bytes stay in the browser; only the URL
    /// crosses the circuit. The composer revokes it when the attachment is removed.
    /// </remarks>
    public string? PreviewUrl { get; init; }

    /// <summary>
    /// Converts a file sent inline into an attachment with the content as a data URL.
    /// </summary>
    /// <returns>The attachment.</returns>
    /// <exception cref="InvalidOperationException">
    /// The content was not sent inline; use <see cref="ToAttachmentAsync"/>.
    /// </exception>
    /// <remarks>
    /// Inline data suits screenshots and small files, which is what pasting and dropping produce.
    /// Applications that accept large uploads should intercept the composer event and upload to
    /// their own storage instead, then attach a URL.
    /// </remarks>
    public BitAiAttachment ToAttachment()
    {
        if (Data is null)
        {
            throw new InvalidOperationException(
                $"'{Name}' was not sent inline. Read it with {nameof(ToAttachmentAsync)} instead.");
        }

        return Create(Convert.FromBase64String(Data));
    }

    /// <summary>
    /// Turns the file into an attachment: buffered, with the content as a data URL, when it is no
    /// larger than <paramref name="maxInlineSize"/>; otherwise read on demand through
    /// <see cref="BitAiAttachment.ContentFactory"/>.
    /// </summary>
    /// <param name="maxAllowedSize">The most bytes to accept; a longer stream fails the read.</param>
    /// <param name="maxInlineSize">
    /// The largest file to buffer in memory. Larger files keep <see cref="Content"/> open and read it
    /// when the attachment is first opened. Defaults to buffering everything.
    /// </param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The attachment.</returns>
    /// <exception cref="InvalidOperationException">The file carries no content at all.</exception>
    /// <remarks>
    /// On Blazor Server the stream arrives in chunks sized to fit the circuit, so the file size is
    /// bounded by <paramref name="maxAllowedSize"/> rather than by the SignalR message limit. A
    /// buffered file releases the browser stream; a lazy one keeps it, since it is the only route to
    /// the bytes, and the caller releases it when the attachment is discarded.
    /// </remarks>
    public async Task<BitAiAttachment> ToAttachmentAsync(
        long maxAllowedSize,
        long maxInlineSize = long.MaxValue,
        CancellationToken cancellationToken = default)
    {
        if (Content is not { } content) return ToAttachment();

        if (Size > maxInlineSize)
        {
            return CreateLazyAttachment(
                Name,
                ContentType,
                Size,
                ct => content.OpenReadStreamAsync(maxAllowedSize, ct),
                PreviewUrl);
        }

        await using (content.ConfigureAwait(false))
        {
            await using var stream = await content.OpenReadStreamAsync(maxAllowedSize, cancellationToken).ConfigureAwait(false);
            using var buffer = new MemoryStream((int)Math.Clamp(Size, 0, Math.Min(maxAllowedSize, int.MaxValue)));
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            return Create(buffer.ToArray());
        }
    }

    private BitAiAttachment Create(byte[] bytes) => CreateAttachment(Name, ContentType, bytes, PreviewUrl);

    internal static BitAiAttachment CreateAttachment(string name, string contentType, byte[] bytes, string? previewUrl = null) =>
        new(BitAiId.New("att"), name)
        {
            ContentType = contentType,
            Size = bytes.LongLength,
            Kind = BitAiAttachment.InferKind(contentType, name),
            Data = bytes,
            Url = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}",
            ThumbnailUrl = previewUrl,
            Status = BitAiAttachmentStatus.Ready,
        };

    /// <remarks>
    /// No bytes are held: the factory opens the browser file when something needs the content, and
    /// the preview, when there is one, is an object URL the browser owns.
    /// </remarks>
    internal static BitAiAttachment CreateLazyAttachment(
        string name,
        string contentType,
        long size,
        Func<CancellationToken, ValueTask<Stream>> open,
        string? previewUrl) =>
        new(BitAiId.New("att"), name)
        {
            ContentType = contentType,
            Size = size,
            Kind = BitAiAttachment.InferKind(contentType, name),
            ContentFactory = open,
            ThumbnailUrl = previewUrl,
            Status = BitAiAttachmentStatus.Ready,
        };
}
