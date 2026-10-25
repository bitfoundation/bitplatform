namespace Bit.Bdnd;

/// <summary>Payload delivered to <see cref="BdndDropTarget{TItem}.OnNativeDrop"/>.</summary>
public sealed class BdndNativeDropEventArgs
{
    /// <summary>The matched native item type (one of <see cref="BdndNativeTypes"/>).</summary>
    public required string ItemType { get; init; }

    /// <summary>The edge of the target the drop was closest to, of the target's <see cref="BdndDropTarget{TItem}.Edges"/>, or null.</summary>
    public BdndEdge? ClosestEdge { get; init; }

    /// <summary>
    /// Files for a <see cref="BdndNativeTypes.File"/> drop - those of the target's
    /// <see cref="BdndDropTarget{TItem}.AcceptFileTypes"/>, up to its <see cref="BdndDropTarget{TItem}.MaxFiles"/> - whose
    /// contents <see cref="BdndNativeFile.OpenReadStreamAsync"/> reads.
    /// </summary>
    public IReadOnlyList<BdndNativeFile> Files { get; init; } = [];

    /// <summary>
    /// How many files the drop carried before the target's file types and file limit were applied: more than
    /// <see cref="Files"/> holds when some were left out, so the application can say so.
    /// </summary>
    public int TotalFileCount { get; init; }

    /// <summary>
    /// True when the dropped folders (<see cref="BdndDropTarget{TItem}.ExpandDirectories"/>) were not listed in full:
    /// an entry could not be read (no permission), the walk stopped at <see cref="BdndDropTarget{TItem}.MaxFiles"/>, or a
    /// folder held more than 10,000 entries. <see cref="TotalFileCount"/> then counts only the files the walk saw.
    /// </summary>
    public bool FileListIncomplete { get; init; }

    /// <summary>
    /// Raw data keyed by DataTransfer type (e.g. <c>text/plain</c>, <c>text/html</c>). Only the types the
    /// target accepts are read: a target accepting <see cref="BdndNativeTypes.Text"/> alone gets the
    /// <c>text/plain</c> of a drag and not its (possibly large) <c>text/html</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Data { get; init; } =
        new Dictionary<string, string>();

    /// <summary>The dropped plain text (<c>text/plain</c>), if any.</summary>
    public string? Text => Data.TryGetValue("text/plain", out var v) ? v : null;

    /// <summary>The dropped HTML (<c>text/html</c>), if any.</summary>
    public string? Html => Data.TryGetValue("text/html", out var v) ? v : null;

    /// <summary>The raw dropped uri-list (<c>text/uri-list</c>), if any.</summary>
    public string? Url => Data.TryGetValue("text/uri-list", out var v) ? v : null;

    /// <summary>URLs for a <see cref="BdndNativeTypes.Url"/> drop (uri-list split by lines).</summary>
    public IReadOnlyList<string> Urls => Url is null
        ? []
        : Url.Split('\n').Select(s => s.Trim()).Where(s => s.Length > 0 && s.StartsWith('#') is false).ToArray();
}
