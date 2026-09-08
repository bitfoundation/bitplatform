namespace Bit.Butil;

/// <summary>
/// What JS reports about a <c>VideoFrame</c> it is holding. The frame itself stays in JS - it wraps
/// memory (often a GPU surface) that cannot cross the interop boundary - and is reached through
/// <see cref="VideoFrameHandle"/>.
/// </summary>
internal class VideoFrameJsInfo
{
    public long Timestamp { get; set; }

    public long? Duration { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string Format { get; set; } = string.Empty;
}
