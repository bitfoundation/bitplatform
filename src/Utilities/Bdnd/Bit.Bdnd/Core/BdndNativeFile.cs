namespace Bit.Bdnd;

/// <summary>
/// A single file of a native file drop: its metadata, and - through <see cref="OpenReadStreamAsync"/> - its contents.
/// </summary>
public sealed class BdndNativeFile
{
    /// <summary>The file's name, without its path.</summary>
    public string Name { get; set; } = "";

    /// <summary>The file's size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>The file's MIME type as the browser reports it; empty when unknown.</summary>
    public string Type { get; set; } = "";

    /// <summary>When the file was last modified, as the browser reports it; null when unknown.</summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// The file's path inside a dropped folder, with <c>/</c> separators (<c>photos/2024/beach.jpg</c>), when the target
    /// expands folders (<see cref="BdndDropTarget{TItem}.ExpandDirectories"/>); empty for a file dropped by itself.
    /// </summary>
    public string RelativePath { get; set; } = "";

    /// <summary>
    /// True for a dropped folder the target did not expand: it has no contents to read. Set
    /// <see cref="BdndDropTarget{TItem}.ExpandDirectories"/> to receive the files inside it instead.
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>Where the contents are read from: the drop the file came with, kept by the browser module.</summary>
    internal BdndNativeFileSource? Source { get; set; }

    /// <summary>
    /// Whether <see cref="OpenReadStreamAsync"/> can read the file: it came with a drop the browser still keeps and is
    /// not a folder.
    /// </summary>
    public bool CanRead => Source is { Released: false } && IsDirectory is false;

    /// <summary>
    /// Opens a stream of the file's contents, streamed from the browser (over the circuit, on Blazor Server). A drop
    /// target keeps the files of its last drop until the next one, or until it is disposed: read them in
    /// <c>OnNativeDrop</c> or soon after.
    /// </summary>
    /// <param name="maxAllowedSize">
    /// The largest size, in bytes, the file may have; a larger file throws an <see cref="IOException"/> before anything
    /// is read. Defaults to 512,000 bytes, as <c>IBrowserFile</c> does.
    /// </param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="InvalidOperationException">The file cannot be read: a folder, or its drop is no longer kept.</exception>
    /// <exception cref="IOException">The file is larger than <paramref name="maxAllowedSize"/>.</exception>
    public async Task<Stream> OpenReadStreamAsync(long maxAllowedSize = 512_000, CancellationToken cancellationToken = default)
    {
        if (IsDirectory) throw new InvalidOperationException($"'{Name}' is a folder: it has no contents to read.");
        if (Source is not { } source) throw new InvalidOperationException($"'{Name}' did not come with a drop whose files can be read.");
        if (source.Released)
            throw new InvalidOperationException(
                $"'{Name}' is no longer available: a drop target keeps the files of its last drop, until the next drop on it or until it is disposed.");
        if (Size > maxAllowedSize)
            throw new IOException($"The file '{Name}' has {Size} bytes, more than the {maxAllowedSize} bytes allowed.");
        return await source.OpenReadStreamAsync(maxAllowedSize, cancellationToken);
    }
}
