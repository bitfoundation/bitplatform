namespace Bit.Bdnd;

/// <summary>Payload delivered to <see cref="BdndDropTarget{TItem}.OnNativeDrop"/>.</summary>
public sealed class BdndNativeDropEventArgs
{
    /// <summary>The matched native item type (one of <see cref="BdndNativeTypes"/>).</summary>
    public required string ItemType { get; init; }

    /// <summary>Files for a <see cref="BdndNativeTypes.File"/> drop.</summary>
    public IReadOnlyList<BdndNativeFile> Files { get; init; } = [];

    /// <summary>Raw data keyed by DataTransfer type (e.g. <c>text/plain</c>, <c>text/html</c>).</summary>
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
