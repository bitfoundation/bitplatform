namespace Bit.Bdnd;

/// <summary>A single file described by a native file drop (metadata only; no contents).</summary>
public sealed class BdndNativeFile
{
    /// <summary>The file's name, without its path.</summary>
    public string Name { get; set; } = "";

    /// <summary>The file's size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>The file's MIME type as the browser reports it; empty when unknown.</summary>
    public string Type { get; set; } = "";
}
